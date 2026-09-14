using System.Text;
using System.Text.RegularExpressions;
using TechDaily.Application.Interfaces;
using UglyToad.PdfPig;

namespace TechDaily.Infrastructure.Services;

public class PdfPigExtractor : IPdfExtractor
{
    private static readonly Regex HeadingRegex = new(
        @"^(Chương\s+\d+|Chapter\s+\d+|Chuyên\s+đề\s+\d+|Part\s+\d+|Section\s+\d+|Bài\s+\d+|Topic\s+\d+|[A-Z0-9\.\s]{4,60}$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    internal record RawBookmark(string Title, int PageNumber, int Level, string? ParentTitle = null);

    public async Task<PdfExtractionResult> ExtractSlicesAsync(
        Stream pdfStream,
        string? customTitle = null,
        int maxPages = int.MaxValue,
        IProgress<PdfExtractionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Ensure seekable stream for PdfPig
        Stream workingStream = pdfStream;
        MemoryStream? bufferStream = null;

        try
        {
            if (!pdfStream.CanSeek)
            {
                bufferStream = new MemoryStream();
                await pdfStream.CopyToAsync(bufferStream, cancellationToken);
                bufferStream.Position = 0;
                workingStream = bufferStream;
            }
            else
            {
                if (pdfStream.Position != 0)
                {
                    pdfStream.Position = 0;
                }
            }

            using var document = PdfDocument.Open(workingStream);
            var totalPages = document.NumberOfPages;
            var pagesToProcess = Math.Min(totalPages, maxPages);

            string? metadataTitle = null;
            try
            {
                metadataTitle = document.Information?.Title?.Trim();
            }
            catch
            {
                // ignore metadata error
            }

            var docTitle = !string.IsNullOrWhiteSpace(customTitle)
                ? customTitle
                : (!string.IsNullOrWhiteSpace(metadataTitle)
                    ? metadataTitle
                    : "Uploaded Technical Document");

            // 2. Attempt native bookmark extraction first
            progress?.Report(new PdfExtractionProgress(0, totalPages, "Analyzing PDF structure and bookmarks..."));
            var bookmarks = ExtractNativeBookmarks(document);

            var slices = new List<ExtractedPdfSlice>();

            if (bookmarks.Count >= 2)
            {
                // Native Bookmark Outline Extraction
                slices = ExtractSlicesFromBookmarks(document, bookmarks, pagesToProcess, totalPages, docTitle, progress, cancellationToken);
            }

            // 3. Fallback to visual heading detection if bookmarks unavailable or yielded 0 slices
            if (slices.Count == 0)
            {
                slices = ExtractSlicesFromHeuristics(document, pagesToProcess, totalPages, docTitle, progress, cancellationToken);
            }

            // Final fallback if no text extracted (e.g. scanned image PDF or single short page)
            if (slices.Count == 0)
            {
                slices.Add(CreateSlice(1, docTitle, "No selectable text found in this PDF. If this is a scanned document, please ensure it has an OCR text layer."));
            }

            progress?.Report(new PdfExtractionProgress(totalPages, totalPages, $"Extracted {slices.Count} slices. Complete!"));
            return new PdfExtractionResult(docTitle, totalPages, slices);
        }
        finally
        {
            if (bufferStream != null)
            {
                await bufferStream.DisposeAsync();
            }
        }
    }

    private static List<ExtractedPdfSlice> ExtractSlicesFromBookmarks(
        PdfDocument document,
        List<RawBookmark> bookmarks,
        int pagesToProcess,
        int totalPages,
        string docTitle,
        IProgress<PdfExtractionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var slices = new List<ExtractedPdfSlice>();
        var validBookmarks = bookmarks
            .Where(b => !IsIgnoredBookmark(b.Title) && b.PageNumber <= pagesToProcess)
            .ToList();

        if (validBookmarks.Count == 0) return slices;

        // Determine optimal curation depth threshold:
        // Level 0: Volume / Book Title
        // Level 1: Modules / Major Sections
        // Level 2: Standalone Topics / Articles
        // Level 3+: Minor subheadings inside articles (to be aggregated)
        int targetMaxDepth = 1;
        var level0Count = validBookmarks.Count(b => b.Level == 0);
        var level1Count = validBookmarks.Count(b => b.Level == 1);
        var level2Count = validBookmarks.Count(b => b.Level == 2);

        if (level0Count <= 3 && level1Count <= 25 && level2Count >= 10)
        {
            // Root is document title, Level 1 has a few modules, Level 2 contains the articles
            targetMaxDepth = 2;
        }
        else if (level0Count <= 3 && level1Count > 25)
        {
            // Level 1 itself contains plenty of chapters/articles
            targetMaxDepth = 1;
        }
        else if (level0Count > 10)
        {
            // Level 0 contains the chapters
            targetMaxDepth = 0;
        }
        else
        {
            targetMaxDepth = Math.Min(validBookmarks.Max(b => b.Level), 2);
        }

        // Filter bookmarks by targetMaxDepth
        var curatedBookmarks = validBookmarks
            .Where(b => b.Level <= targetMaxDepth)
            .OrderBy(b => b.PageNumber)
            .ThenBy(b => b.Level)
            .GroupBy(b => b.PageNumber)
            .Select(g => g.First()) // Keep top-level bookmark when multiple share a page
            .OrderBy(b => b.PageNumber)
            .ToList();

        // Fallback: If filtering resulted in too few (< 2) but validBookmarks had more, include all valid
        if (curatedBookmarks.Count < 2 && validBookmarks.Count >= 2)
        {
            curatedBookmarks = validBookmarks
                .OrderBy(b => b.PageNumber)
                .ThenBy(b => b.Level)
                .GroupBy(b => b.PageNumber)
                .Select(g => g.First())
                .OrderBy(b => b.PageNumber)
                .ToList();
        }

        if (curatedBookmarks.Count == 0) return slices;

        int sliceOrder = 1;

        for (int i = 0; i < curatedBookmarks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = curatedBookmarks[i];
            int startPage = Math.Clamp(current.PageNumber, 1, pagesToProcess);
            int endPage = (i + 1 < curatedBookmarks.Count)
                ? Math.Clamp(curatedBookmarks[i + 1].PageNumber - 1, startPage, pagesToProcess)
                : pagesToProcess;

            var sliceTitle = FormatCuratedTitle(current, sliceOrder);
            progress?.Report(new PdfExtractionProgress(startPage, totalPages, $"Extracting topic: {sliceTitle}"));

            var chapterSb = new StringBuilder();
            for (int p = startPage; p <= endPage; p++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var page = document.GetPage(p);
                    var text = ExtractPageLines(page);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        chapterSb.AppendLine(text);
                        chapterSb.AppendLine();
                    }
                }
                catch
                {
                    // Ignore transient page read error
                }
            }

            var chapterText = chapterSb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(chapterText)) continue;

            // Semantic chapter splitting: keep complete topics intact (up to 5,000 words).
            // Only split at natural ## headings if topic is exceptionally monolithic.
            var wordCount = chapterText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount <= 5000)
            {
                slices.Add(CreateSlice(sliceOrder++, sliceTitle, chapterText));
            }
            else
            {
                // Split only at major section headings ##
                var sections = Regex.Split(chapterText, @"(?m)(?=^#{2,3}\s+)");
                var partSb = new StringBuilder();
                int partWordCount = 0;
                int partIndex = 1;

                foreach (var sec in sections)
                {
                    if (string.IsNullOrWhiteSpace(sec)) continue;
                    var secWords = sec.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    if (partWordCount + secWords > 3500 && partSb.Length > 0)
                    {
                        var partTitle = $"{sliceTitle} (Section {partIndex++})";
                        slices.Add(CreateSlice(sliceOrder++, partTitle, partSb.ToString().Trim()));
                        partSb.Clear();
                        partWordCount = 0;
                    }
                    partSb.AppendLine(sec);
                    partSb.AppendLine();
                    partWordCount += secWords;
                }

                if (partSb.Length > 0)
                {
                    var partTitle = partIndex > 1 ? $"{sliceTitle} (Section {partIndex})" : sliceTitle;
                    slices.Add(CreateSlice(sliceOrder++, partTitle, partSb.ToString().Trim()));
                }
            }
        }

        return slices;
    }

    internal static string FormatCuratedTitle(RawBookmark bookmark, int order)
    {
        var rawTitle = CleanTitle(bookmark.Title, order);
        if (!string.IsNullOrWhiteSpace(bookmark.ParentTitle))
        {
            var cleanParent = CleanTitle(bookmark.ParentTitle, 0);
            if (!string.IsNullOrWhiteSpace(cleanParent) &&
                !cleanParent.Contains("documentation", StringComparison.OrdinalIgnoreCase) &&
                !cleanParent.Contains("contents", StringComparison.OrdinalIgnoreCase))
            {
                // Level 2+ articles inherit parent module context
                if (bookmark.Level >= 2 && !rawTitle.Contains(cleanParent, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{cleanParent}: {rawTitle}";
                }

                // Generic titles at any depth inherit parent
                var isGeneric = rawTitle.Equals("Overview", StringComparison.OrdinalIgnoreCase) ||
                                rawTitle.Equals("Introduction", StringComparison.OrdinalIgnoreCase) ||
                                rawTitle.Equals("Getting Started", StringComparison.OrdinalIgnoreCase) ||
                                rawTitle.Equals("Get Started", StringComparison.OrdinalIgnoreCase) ||
                                rawTitle.Equals("Summary", StringComparison.OrdinalIgnoreCase);

                if (isGeneric && !rawTitle.Contains(cleanParent, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{cleanParent}: {rawTitle}";
                }
            }
        }
        return rawTitle;
    }

    private static List<ExtractedPdfSlice> ExtractSlicesFromHeuristics(
        PdfDocument document,
        int pagesToProcess,
        int totalPages,
        string docTitle,
        IProgress<PdfExtractionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var slices = new List<ExtractedPdfSlice>();
        var currentSliceText = new StringBuilder();
        var currentChapterTitle = "Introduction & Overview";
        int sliceOrder = 1;
        int currentWordCount = 0;

        for (int p = 1; p <= pagesToProcess; p++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (p % 10 == 0 || p == pagesToProcess)
            {
                progress?.Report(new PdfExtractionProgress(p, totalPages, $"Analyzing content: page {p}/{totalPages}"));
            }

            string pageText;
            try
            {
                var page = document.GetPage(p);
                pageText = ExtractPageLines(page);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            var firstLines = pageText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(3)
                .ToList();

            string? detectedHeading = null;
            foreach (var line in firstLines)
            {
                if (HeadingRegex.IsMatch(line) && line.Length >= 4 && line.Length <= 80)
                {
                    detectedHeading = line;
                    break;
                }
            }

            var wordsInPage = pageText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if ((detectedHeading != null && currentWordCount >= 200) || (currentWordCount + wordsInPage >= 700 && currentSliceText.Length > 0))
            {
                var content = currentSliceText.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    slices.Add(CreateSlice(sliceOrder++, currentChapterTitle, content));
                }

                currentSliceText.Clear();
                currentWordCount = 0;
                currentChapterTitle = detectedHeading ?? $"Slice {sliceOrder}: Page {p}";
            }
            else if (detectedHeading != null && currentWordCount < 200)
            {
                currentChapterTitle = detectedHeading;
            }

            currentSliceText.AppendLine(pageText);
            currentSliceText.AppendLine();
            currentWordCount += wordsInPage;
        }

        if (currentSliceText.Length > 0)
        {
            var content = currentSliceText.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                slices.Add(CreateSlice(sliceOrder++, currentChapterTitle, content));
            }
        }

        return slices;
    }

    private static List<RawBookmark> ExtractNativeBookmarks(PdfDocument document)
    {
        var bookmarks = new List<RawBookmark>();
        try
        {
            var catalog = document.Structure.Catalog;
            if (!catalog.CatalogDictionary.TryGet(UglyToad.PdfPig.Tokens.NameToken.Outlines, out var outlinesToken) ||
                outlinesToken is not UglyToad.PdfPig.Tokens.IndirectReferenceToken outlinesRef)
            {
                return bookmarks;
            }

            var outlineObj = document.Structure.GetObject(outlinesRef.Data);
            if (outlineObj?.Data is not UglyToad.PdfPig.Tokens.DictionaryToken outlineDict ||
                !outlineDict.TryGet(UglyToad.PdfPig.Tokens.NameToken.First, out var firstRefTok) ||
                firstRefTok is not UglyToad.PdfPig.Tokens.IndirectReferenceToken rootFirstRef)
            {
                return bookmarks;
            }

            var pageRefToNumber = BuildPageMap(document, catalog.CatalogDictionary);
            var destsDict = GetNamedDestsDictionary(document, catalog.CatalogDictionary);

            int? ResolvePageNumber(UglyToad.PdfPig.Tokens.IToken destTok)
            {
                var resolvedTok = destTok is UglyToad.PdfPig.Tokens.IndirectReferenceToken r ? document.Structure.GetObject(r.Data)?.Data : destTok;
                if (resolvedTok is UglyToad.PdfPig.Tokens.ArrayToken directArr && directArr.Length > 0)
                {
                    if (directArr.Data[0] is UglyToad.PdfPig.Tokens.IndirectReferenceToken directPageRef && pageRefToNumber.TryGetValue(directPageRef.Data.ObjectNumber, out var pNum))
                    {
                        return pNum;
                    }
                    if (directArr.Data[0] is UglyToad.PdfPig.Tokens.NumericToken numTok)
                    {
                        return (int)numTok.Int + 1;
                    }
                }

                string destName = "";
                if (destTok is UglyToad.PdfPig.Tokens.NameToken nt) destName = nt.Data;
                else if (destTok is UglyToad.PdfPig.Tokens.StringToken st) destName = st.Data;

                var cleanDest = destName.TrimStart('/');
                if (!string.IsNullOrEmpty(cleanDest) && destsDict != null)
                {
                    if (destsDict.TryGetValue(cleanDest, out var dVal) ||
                        destsDict.TryGetValue("/" + cleanDest, out dVal))
                    {
                        var resolvedArr = dVal is UglyToad.PdfPig.Tokens.IndirectReferenceToken arRef ? document.Structure.GetObject(arRef.Data)?.Data : dVal;
                        if (resolvedArr is UglyToad.PdfPig.Tokens.ArrayToken arr && arr.Length > 0 && arr.Data[0] is UglyToad.PdfPig.Tokens.IndirectReferenceToken pRef)
                        {
                            if (pageRefToNumber.TryGetValue(pRef.Data.ObjectNumber, out var pNum)) return pNum;
                        }
                    }
                }
                return null;
            }

            void Walk(UglyToad.PdfPig.Tokens.IndirectReferenceToken itemRef, int depth, string? parentTitle)
            {
                var cur = itemRef;
                while (cur != null)
                {
                    var itemObj = document.Structure.GetObject(cur.Data);
                    if (itemObj?.Data is not UglyToad.PdfPig.Tokens.DictionaryToken dict) break;

                    string title = "";
                    if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.Title, out var t))
                    {
                        if (t is UglyToad.PdfPig.Tokens.StringToken str) title = str.Data;
                        else if (t is UglyToad.PdfPig.Tokens.HexToken hex) title = hex.Data;
                    }

                    int? page = null;
                    if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.Dest, out var d))
                    {
                        page = ResolvePageNumber(d);
                    }
                    else if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.A, out var a))
                    {
                        var aObj = a is UglyToad.PdfPig.Tokens.IndirectReferenceToken ar ? document.Structure.GetObject(ar.Data)?.Data : a;
                        if (aObj is UglyToad.PdfPig.Tokens.DictionaryToken ad && ad.TryGet(UglyToad.PdfPig.Tokens.NameToken.D, out var adD))
                        {
                            page = ResolvePageNumber(adD);
                        }
                    }

                    var cleanTitle = title?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(cleanTitle) && page.HasValue && page.Value >= 1 && page.Value <= document.NumberOfPages)
                    {
                        bookmarks.Add(new RawBookmark(cleanTitle, page.Value, depth, parentTitle));
                    }

                    if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.First, out var cFirst) && cFirst is UglyToad.PdfPig.Tokens.IndirectReferenceToken cRef)
                    {
                        Walk(cRef, depth + 1, cleanTitle);
                    }

                    if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.Next, out var nTok) && nTok is UglyToad.PdfPig.Tokens.IndirectReferenceToken nRef)
                    {
                        cur = nRef;
                    }
                    else
                    {
                        cur = null;
                    }
                }
            }

            Walk(rootFirstRef, 0, null);
        }
        catch
        {
            // Graceful fallback to heuristics
        }

        return bookmarks;
    }

    private static Dictionary<long, int> BuildPageMap(PdfDocument document, UglyToad.PdfPig.Tokens.DictionaryToken catalogDictionary)
    {
        var pageRefToNumber = new Dictionary<long, int>();
        if (!catalogDictionary.TryGet(UglyToad.PdfPig.Tokens.NameToken.Pages, out var pagesTok) ||
            pagesTok is not UglyToad.PdfPig.Tokens.IndirectReferenceToken pagesToken)
        {
            return pageRefToNumber;
        }

        int currentPageIndex = 1;
        void WalkPages(UglyToad.PdfPig.Tokens.IndirectReferenceToken pRef)
        {
            var obj = document.Structure.GetObject(pRef.Data);
            if (obj?.Data is not UglyToad.PdfPig.Tokens.DictionaryToken pDict) return;

            if (pDict.TryGet(UglyToad.PdfPig.Tokens.NameToken.Type, out var typeTok) &&
                typeTok is UglyToad.PdfPig.Tokens.NameToken typeName && typeName.Data == "Page")
            {
                pageRefToNumber[pRef.Data.ObjectNumber] = currentPageIndex++;
                return;
            }

            if (pDict.TryGet(UglyToad.PdfPig.Tokens.NameToken.Kids, out var kidsTok) &&
                kidsTok is UglyToad.PdfPig.Tokens.ArrayToken kidsArr)
            {
                foreach (var kid in kidsArr.Data)
                {
                    if (kid is UglyToad.PdfPig.Tokens.IndirectReferenceToken kRef)
                    {
                        WalkPages(kRef);
                    }
                }
            }
        }

        WalkPages(pagesToken);
        return pageRefToNumber;
    }

    private static IReadOnlyDictionary<string, UglyToad.PdfPig.Tokens.IToken>? GetNamedDestsDictionary(PdfDocument document, UglyToad.PdfPig.Tokens.DictionaryToken catalogDictionary)
    {
        if (catalogDictionary.TryGet(UglyToad.PdfPig.Tokens.NameToken.Dests, out var destsToken))
        {
            var resolvedDests = destsToken is UglyToad.PdfPig.Tokens.IndirectReferenceToken r ? document.Structure.GetObject(r.Data)?.Data : destsToken;
            if (resolvedDests is UglyToad.PdfPig.Tokens.DictionaryToken destsDict)
            {
                return destsDict.Data;
            }
        }
        return null;
    }

    private static bool IsIgnoredBookmark(string title)
    {
        var lower = title.ToLowerInvariant().Trim();
        return lower == "api reference" ||
               lower == "contribute" ||
               lower == "table of contents" ||
               lower == "index" ||
               lower == "credits";
    }

    private static string ExtractPageLines(UglyToad.PdfPig.Content.Page page)
    {
        try
        {
            var words = page.GetWords()?.ToList();
            if (words != null && words.Count > 0)
            {
                // Group words by baseline Y coordinate (tolerance ~3 points)
                var lines = words
                    .GroupBy(w => (int)Math.Round(w.BoundingBox.Bottom / 3.5))
                    .OrderByDescending(g => g.Key)
                    .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line));

                var text = string.Join("\n", lines).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        catch
        {
            // fallback to basic text
        }

        try
        {
            return page.Text?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static ExtractedPdfSlice CreateSlice(int order, string title, string content)
    {
        var sanitizedContent = SanitizeText(content);
        var sanitizedTitle = SanitizeText(title);

        var words = sanitizedContent.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var readMinutes = Math.Max(1, words.Length / 200);

        var takeaways = ExtractKeyTakeaways(sanitizedContent);

        return new ExtractedPdfSlice(
            Order: order,
            ChapterTitle: CleanTitle(sanitizedTitle, order),
            ContentMarkdown: FormatAsMarkdown(sanitizedContent, sanitizedTitle),
            EstimatedReadMinutes: readMinutes,
            KeyTakeaways: takeaways
        );
    }

    private static string CleanTitle(string rawTitle, int order)
    {
        var cleaned = SanitizeText(rawTitle).Replace("#", "").Trim();
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 3)
        {
            return $"Slice {order}";
        }
        return cleaned.Length > 80 ? cleaned.Substring(0, 77) + "..." : cleaned;
    }

    internal static string StripBoilerplate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Strip Microsoft pre-release disclaimer banners
        var cleaned = Regex.Replace(
            text,
            @"(?i)(?:###\s*\d{1,2}/\d{1,2}/\d{4}|\b\d{2}/\d{2}/\d{4}\b)?\s*\)?\s*Important\s+This information relates to a pre-release product[^.\n]*\.[^.\n]*\.(?:\s*For the current release[^.\n]*\.)?",
            "",
            RegexOptions.Multiline);

        // Strip standalone publication date lines
        cleaned = Regex.Replace(cleaned, @"(?m)^\s*###?\s*\d{1,2}/\d{1,2}/\d{4}\s*$", "");
        cleaned = Regex.Replace(cleaned, @"(?m)^\s*\d{2}/\d{2}/\d{4}\s*$", "");

        // Strip web landing grid artifacts
        cleaned = Regex.Replace(cleaned, @"(?i)G\s*E\s*T\s*S\s*T\s*A\s*R\s*T\s*E\s*D\s*O\s*V\s*E\s*R\s*V\s*I\s*E\s*W\s*D\s*O\s*W\s*N\s*L\s*O\s*A\s*D", "");
        cleaned = Regex.Replace(cleaned, @"(?i)GETSTARTEDGETSTARTED", "");

        return cleaned.Trim();
    }

    internal static string FormatAsMarkdown(string text, string heading)
    {
        var cleanedText = StripBoilerplate(text);
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(heading))
        {
            sb.AppendLine($"# {heading}");
            sb.AppendLine();
        }

        var lines = cleanedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        bool inCodeBlock = false;
        int emptyLineStreak = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                emptyLineStreak++;
                if (inCodeBlock)
                {
                    // 2 or more consecutive blank lines closes code block
                    if (emptyLineStreak >= 2)
                    {
                        sb.AppendLine("```");
                        inCodeBlock = false;
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine();
                }
                continue;
            }

            emptyLineStreak = 0;

            // Bullet points detection: •, ▪, ⁃, ‣, -, *
            if (Regex.IsMatch(trimmed, @"^[\u2022\u25AA\u2043\u2023\-\*]\s*"))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                var bulletContent = Regex.Replace(trimmed, @"^[\u2022\u25AA\u2043\u2023\-\*]\s*", "").Trim();
                sb.AppendLine($"- {bulletContent}");
                continue;
            }

            // Subheading detection: ALL CAPS lines (e.g. WORK EXPERIENCE, PREREQUISITES)
            if (Regex.IsMatch(trimmed, @"^[A-Z0-9\s/&-]{4,40}$") && trimmed.Length >= 4 && !trimmed.Contains('.') && !trimmed.Contains(':'))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                sb.AppendLine();
                sb.AppendLine($"### {trimmed}");
                sb.AppendLine();
                continue;
            }

            // Explicit markdown heading
            if (trimmed.StartsWith('#'))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                sb.AppendLine(trimmed);
                continue;
            }

            if (inCodeBlock)
            {
                // Escape code block if line is clearly prose
                if (IsObviousProse(trimmed))
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                    sb.AppendLine(trimmed);
                    continue;
                }

                sb.AppendLine(line);
                continue;
            }

            // Outside code block: Open code block if strong code signals detected
            if (IsStrongCodeStart(trimmed))
            {
                sb.AppendLine("```csharp");
                inCodeBlock = true;
                sb.AppendLine(line);
                continue;
            }

            // Regular prose line
            sb.AppendLine(trimmed);
        }

        if (inCodeBlock)
        {
            sb.AppendLine("```");
        }

        return sb.ToString();
    }

    private static bool IsObviousProse(string trimmed)
    {
        // 1. Markdown indicators
        if (trimmed.StartsWith('#') || trimmed.StartsWith('-') || trimmed.StartsWith('*')) return true;

        // 2. Prose label ending with colon (e.g. "Components/Pages/Counter.razor :", "Change the app:")
        if (trimmed.EndsWith(':') && !trimmed.Contains('{') && !trimmed.Contains(';') && !trimmed.Contains("=>")) return true;

        // 3. Known documentation section titles without punctuation
        if (Regex.IsMatch(trimmed, @"^(Change the app|Prerequisites|Next steps|See also|Important|Note|Overview|Summary|For more information)\b", RegexOptions.IgnoreCase))
        {
            return true;
        }

        // 4. Common English sentence structure
        if (char.IsUpper(trimmed[0]))
        {
            var words = trimmed.Split(new[] { ' ', '\t', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            int proseWordHits = 0;
            foreach (var w in words)
            {
                var lower = w.ToLowerInvariant();
                if (lower is "the" or "is" or "in" or "to" or "a" or "an" or "of" or "and" or "with" or "you" or "your" or "can" or "for" or "from" or "that" or "will" or "this" or "using" or "by" or "run" or "click" or "select" or "open" or "create" or "leave")
                {
                    proseWordHits++;
                }
            }

            // If line has 2+ prose words and does NOT contain strong code tokens
            if (proseWordHits >= 2 && !trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains('}') && !trimmed.Contains("=>"))
            {
                return true;
            }

            // Standard sentence ending in period with multiple words
            if (trimmed.EndsWith('.') && words.Length >= 4 && !trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains('}'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStrongCodeStart(string trimmed)
    {
        // Never start code block on obvious prose phrases
        if (IsObviousProse(trimmed)) return false;

        // C# / .NET declarations
        if (Regex.IsMatch(trimmed, @"^(public|private|protected|internal)\s+(static\s+|async\s+)?(class|interface|struct|record|enum|void|Task|string|int|bool|var|IActionResult|EventCallback)\b", RegexOptions.IgnoreCase))
            return true;

        // using System...; (Strict: PascalCase identifier + semicolon)
        if (Regex.IsMatch(trimmed, @"^using\s+[A-Z][A-Za-z0-9_.]*\s*;"))
            return true;

        // namespace MyNamespace...
        if (Regex.IsMatch(trimmed, @"^namespace\s+[A-Z][A-Za-z0-9_.]*"))
            return true;

        // Razor directives
        if (Regex.IsMatch(trimmed, @"^@(page|code|inject|typeparam|bind|layout)\b"))
            return true;

        // Shell CLI commands
        if (Regex.IsMatch(trimmed, @"^(dotnet\s+(new|watch|run|build|add|restore|test)|npm\s+(install|run|start|test)|git\s+(clone|checkout|commit|push|pull)|docker\s+(build|run|compose))", RegexOptions.IgnoreCase))
            return true;

        // Standalone braces
        if (trimmed == "{" || trimmed == "}" || trimmed == "});" || trimmed == "};")
            return true;

        // XML / HTML tags (single tag or self-closing)
        if (Regex.IsMatch(trimmed, @"^<[A-Za-z][A-Za-z0-9_-]*(\s+[^>]*)?>.*(</[A-Za-z0-9_-]+>)?$") && !trimmed.Contains(" the ") && !trimmed.Contains(" is "))
            return true;

        // Comments
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
            return true;

        // Assignment with semicolon: e.g. "var builder = WebApplication.CreateBuilder(args);"
        if (Regex.IsMatch(trimmed, @"^(var|[A-Z][A-Za-z0-9_<>]+)\s+[A-Za-z0-9_]+\s*=\s*.*[;{]$") && !trimmed.Contains(" the ") && !trimmed.Contains(" is "))
            return true;

        return false;
    }

    private static List<string> ExtractKeyTakeaways(string text)
    {
        var cleaned = StripBoilerplate(text);
        var sentences = cleaned.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeText)
            .Where(s => s.Length >= 25 && s.Length <= 140)
            .Where(s => !IsDisclaimerOrJunk(s))
            .Take(3)
            .ToList();

        if (sentences.Count == 0)
        {
            return new() { "Core Architecture Principle", "Key Technical Invariant" };
        }

        return sentences;
    }

    private static bool IsDisclaimerOrJunk(string s)
    {
        var lower = s.ToLowerInvariant();
        return lower.Contains("pre-release product") ||
               lower.Contains("commercially released") ||
               lower.Contains("makes no warranties") ||
               lower.Contains("express or implied") ||
               lower.Contains("copyright") ||
               lower.Contains("all rights reserved") ||
               lower.Contains("get started overview") ||
               lower.Contains("table of contents");
    }

    private static string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        // Strip null bytes (\0) and illegal ASCII/UTF-8 control chars that break PostgreSQL
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "").Trim();
    }
}
