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

    private record RawBookmark(string Title, int PageNumber, int Level);

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
        // Deduplicate bookmarks with same page and clean titles
        var distinctBookmarks = bookmarks
            .Where(b => !IsIgnoredBookmark(b.Title) && b.PageNumber <= pagesToProcess)
            .OrderBy(b => b.PageNumber)
            .GroupBy(b => b.PageNumber)
            .Select(g => g.OrderBy(b => b.Level).First())
            .ToList();

        if (distinctBookmarks.Count == 0) return slices;

        int sliceOrder = 1;

        for (int i = 0; i < distinctBookmarks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = distinctBookmarks[i];
            int startPage = Math.Clamp(current.PageNumber, 1, pagesToProcess);
            int endPage = (i + 1 < distinctBookmarks.Count)
                ? Math.Clamp(distinctBookmarks[i + 1].PageNumber - 1, startPage, pagesToProcess)
                : pagesToProcess;

            progress?.Report(new PdfExtractionProgress(startPage, totalPages, $"Extracting chapter: {current.Title}"));

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

            // Split chapters longer than 900 words into readable slices
            var wordCount = chapterText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount <= 900)
            {
                slices.Add(CreateSlice(sliceOrder++, current.Title, chapterText));
            }
            else
            {
                var paragraphs = chapterText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                var partSb = new StringBuilder();
                int partWordCount = 0;
                int partIndex = 1;

                foreach (var para in paragraphs)
                {
                    var pWords = para.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    if (partWordCount + pWords > 700 && partSb.Length > 0)
                    {
                        var partTitle = $"{current.Title} (Part {partIndex++})";
                        slices.Add(CreateSlice(sliceOrder++, partTitle, partSb.ToString().Trim()));
                        partSb.Clear();
                        partWordCount = 0;
                    }
                    partSb.AppendLine(para);
                    partSb.AppendLine();
                    partWordCount += pWords;
                }

                if (partSb.Length > 0)
                {
                    var partTitle = partIndex > 1 ? $"{current.Title} (Part {partIndex})" : current.Title;
                    slices.Add(CreateSlice(sliceOrder++, partTitle, partSb.ToString().Trim()));
                }
            }
        }

        return slices;
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

            void Walk(UglyToad.PdfPig.Tokens.IndirectReferenceToken itemRef, int depth)
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

                    if (!string.IsNullOrWhiteSpace(title) && page.HasValue && page.Value >= 1 && page.Value <= document.NumberOfPages)
                    {
                        bookmarks.Add(new RawBookmark(title.Trim(), page.Value, depth));
                    }

                    if (dict.TryGet(UglyToad.PdfPig.Tokens.NameToken.First, out var cFirst) && cFirst is UglyToad.PdfPig.Tokens.IndirectReferenceToken cRef)
                    {
                        Walk(cRef, depth + 1);
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

            Walk(rootFirstRef, 0);
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

    private static string FormatAsMarkdown(string text, string heading)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(heading))
        {
            sb.AppendLine($"# {heading}");
            sb.AppendLine();
        }

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        bool inCodeBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine();
                }
                continue;
            }

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

            // Subheading detection: ALL CAPS lines (e.g. WORK EXPERIENCE, EDUCATION, SKILLS)
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

            // Code line heuristic detection
            bool isCodeLine = false;

            if (inCodeBlock)
            {
                // When already inside a code block, stay inside code block unless line is clearly prose or section header
                bool isProseSentence = (trimmed.EndsWith('.') && char.IsUpper(trimmed[0]) && !trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains('}') && !trimmed.StartsWith("//") && !trimmed.Contains("()"));
                bool isExplicitHeading = trimmed.StartsWith('#');
                isCodeLine = !isProseSentence && !isExplicitHeading;
            }
            else
            {
                // Start a code block on programming keywords with word boundary, comments, method calls or braces
                bool startsWithCodeKeyword = Regex.IsMatch(trimmed, @"^(public|private|protected|internal|class|interface|record|struct|enum|import|export|function|const|let|var|def|return|namespace|static|void|async)\b\s+", RegexOptions.IgnoreCase) ||
                                             Regex.IsMatch(trimmed, @"^(using\s+[A-Za-z0-9_.]+\s*;|using\s*\()", RegexOptions.IgnoreCase) ||
                                             Regex.IsMatch(trimmed, @"^(Task<|Console\.|Registry\.|RegistryKey|for\s*\(|while\s*\(|foreach\s*\(|if\s*\()", RegexOptions.IgnoreCase);

                bool isProseSentence = trimmed.EndsWith('.') && !trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains('}') && !trimmed.Contains("=>");

                isCodeLine = (!isProseSentence && startsWithCodeKeyword) ||
                             trimmed.StartsWith("//") || trimmed.StartsWith("/*") ||
                             trimmed.EndsWith(';') || trimmed.EndsWith('{') || trimmed.EndsWith('}') || trimmed.Contains("=>");
            }

            if (isCodeLine && !trimmed.StartsWith('#') && !trimmed.StartsWith('-'))
            {
                if (!inCodeBlock)
                {
                    sb.AppendLine("```csharp");
                    inCodeBlock = true;
                }
                sb.AppendLine(line);
                continue;
            }

            if (inCodeBlock)
            {
                sb.AppendLine("```");
                inCodeBlock = false;
            }

            sb.AppendLine(trimmed);
        }

        if (inCodeBlock)
        {
            sb.AppendLine("```");
        }

        return sb.ToString();
    }

    private static List<string> ExtractKeyTakeaways(string text)
    {
        var sentences = text.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeText)
            .Where(s => s.Length >= 25 && s.Length <= 140)
            .Take(3)
            .ToList();

        if (sentences.Count == 0)
        {
            return new() { "Core Architecture Principle", "Key Technical Invariant" };
        }

        return sentences;
    }

    private static string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        // Strip null bytes (\0) and illegal ASCII/UTF-8 control chars that break PostgreSQL
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "").Trim();
    }
}
