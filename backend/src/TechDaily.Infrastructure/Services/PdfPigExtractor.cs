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

    private static readonly Regex TitlePatternRegex = new(
        @"^(LỜI\s+NÓI\s+ĐẦU|Phần\s+GIỚI\s+THIỆU|GIỚI\s+THIỆU|Câu\s+chuyện\s+của\s+chính\s+tôi\.?|Chương\s+\d+|Chapter\s+\d+|MỤC\s+LỤC|TIỂU\s+DẪN|KẾT\s+LUẬN|PHẦN\s+([IVXLCDM\d]+|[A-ZÀ-Ỹ]+))(\.|\b|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AllCapsTitleRegex = new(
        @"^[A-Z0-9\s/&-]{4,50}$",
        RegexOptions.Compiled);

    internal record RawBookmark(string Title, int PageNumber, int Level, string? ParentTitle = null);

    public readonly record struct LineGeometry(
        string Text,
        double Top,
        double Bottom,
        double Left,
        double Right,
        double Height);

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
            .Where(b => !IsIgnoredBookmark(b.Title) && b.PageNumber > 0 && b.PageNumber <= pagesToProcess)
            .ToList();

        if (validBookmarks.Count == 0) return slices;

        // Retain all outline bookmarks pointing to distinct destination pages (~5-10 pages per slice).
        // If multiple bookmarks point to the exact same page, prefer the deeper child to preserve parent module context.
        var curatedBookmarks = validBookmarks
            .OrderBy(b => b.PageNumber)
            .ThenByDescending(b => b.Level)
            .GroupBy(b => b.PageNumber)
            .Select(g => g.First())
            .OrderBy(b => b.PageNumber)
            .ToList();

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

            var chapterPages = new List<(int PageNumber, string Text)>();
            for (int p = startPage; p <= endPage; p++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var page = document.GetPage(p);
                    var text = ExtractPageLines(page);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        chapterPages.Add((p, text));
                    }
                }
                catch
                {
                    // Ignore transient page read error
                }
            }

            if (chapterPages.Count == 0) continue;

            AddChapterSlices(slices, ref sliceOrder, sliceTitle, chapterPages);
        }

        return slices;
    }

    private static void AddChapterSlices(
        List<ExtractedPdfSlice> slices,
        ref int sliceOrder,
        string sliceTitle,
        List<(int PageNumber, string Text)> pages)
    {
        if (pages.Count == 0) return;

        var pendingSections = new List<string>();
        var currentSectionPages = new List<string>();
        int currentWordCount = 0;

        foreach (var page in pages)
        {
            var pageWords = page.Text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (pageWords == 0) continue;

            // If a single page is massive (> 2,000 words), split it by paragraphs
            if (pageWords > 2000)
            {
                if (currentSectionPages.Count > 0)
                {
                    var secText = string.Join("\n\n", currentSectionPages).Trim();
                    if (!string.IsNullOrWhiteSpace(secText)) pendingSections.Add(secText);
                    currentSectionPages.Clear();
                    currentWordCount = 0;
                }

                var paragraphs = page.Text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                var paraSb = new StringBuilder();
                int paraWords = 0;

                foreach (var para in paragraphs)
                {
                    var pCount = para.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    if (paraWords + pCount > 1800 && paraWords >= 700)
                    {
                        var text = paraSb.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(text)) pendingSections.Add(text);
                        paraSb.Clear();
                        paraWords = 0;
                    }
                    paraSb.AppendLine(para);
                    paraSb.AppendLine();
                    paraWords += pCount;
                }

                if (paraSb.Length > 0)
                {
                    var text = paraSb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(text)) pendingSections.Add(text);
                }
                continue;
            }

            // If adding this page exceeds 2,000 words and we already have substantial text (>= 700 words), flush
            if (currentWordCount + pageWords > 2000 && currentWordCount >= 700)
            {
                var sectionText = string.Join("\n\n", currentSectionPages).Trim();
                if (!string.IsNullOrWhiteSpace(sectionText))
                {
                    pendingSections.Add(sectionText);
                }
                currentSectionPages.Clear();
                currentWordCount = 0;
            }

            currentSectionPages.Add(page.Text);
            currentWordCount += pageWords;
        }

        if (currentSectionPages.Count > 0)
        {
            var sectionText = string.Join("\n\n", currentSectionPages).Trim();
            if (!string.IsNullOrWhiteSpace(sectionText))
            {
                pendingSections.Add(sectionText);
            }
        }

        if (pendingSections.Count == 0) return;

        if (pendingSections.Count == 1)
        {
            slices.Add(CreateSlice(sliceOrder++, sliceTitle, pendingSections[0]));
        }
        else
        {
            for (int s = 0; s < pendingSections.Count; s++)
            {
                var partTitle = $"{sliceTitle} (Section {s + 1})";
                slices.Add(CreateSlice(sliceOrder++, partTitle, pendingSections[s]));
            }
        }
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
                var cleanLine = line.StartsWith('#') ? line.TrimStart('#', ' ').Trim() : line;
                if ((HeadingRegex.IsMatch(cleanLine) || IsTitlePattern(cleanLine)) && cleanLine.Length >= 4 && cleanLine.Length <= 80)
                {
                    detectedHeading = cleanLine;
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

    private static readonly HashSet<string> BlacklistedTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "table of contents", "contents", "mục lục", "cover", "copyright", "bản quyền",
        "preface", "about the author", "about the authors", "about the reviewer", "about the reviewers",
        "index", "chỉ mục", "contributors", "contribute", "credits", "bibliography", "references", "colophon",
        "api reference"
    };

    private static bool IsIgnoredBookmark(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return true;
        var lower = title.Trim().ToLowerInvariant();
        if (BlacklistedTitles.Contains(lower)) return true;

        if (lower.StartsWith("table of contents") ||
            lower.StartsWith("about the author") ||
            lower.StartsWith("about the reviewer") ||
            lower == "index" ||
            lower.StartsWith("index ") ||
            lower.StartsWith("copyright ") ||
            lower.StartsWith("contributors") ||
            lower.StartsWith("credits") ||
            lower.StartsWith("references") ||
            lower.StartsWith("bibliography"))
        {
            return true;
        }

        return false;
    }

    internal static string ExtractPageLines(UglyToad.PdfPig.Content.Page page)
    {
        try
        {
            var words = page.GetWords()?.ToList();
            if (words != null && words.Count > 0)
            {
                var lines = GroupWordsIntoLines(words);
                if (lines.Count > 0)
                {
                    var text = AssembleLines(lines, page.Width);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
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

    internal static List<LineGeometry> GroupWordsIntoLines(IReadOnlyList<UglyToad.PdfPig.Content.Word> words)
    {
        if (words == null || words.Count == 0)
        {
            return new List<LineGeometry>();
        }

        // Group words by baseline Y coordinate (tolerance ~3.5 points)
        var groups = words
            .GroupBy(w => (int)Math.Round(w.BoundingBox.Bottom / 3.5))
            .OrderByDescending(g => g.Key);

        var result = new List<LineGeometry>();
        foreach (var group in groups)
        {
            var orderedWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
            var text = string.Join(" ", orderedWords.Select(w => w.Text)).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            double top = orderedWords.Max(w => w.BoundingBox.Top);
            double bottom = orderedWords.Min(w => w.BoundingBox.Bottom);
            double left = orderedWords.Min(w => w.BoundingBox.Left);
            double right = orderedWords.Max(w => w.BoundingBox.Right);
            double height = Math.Max(0, top - bottom);

            result.Add(new LineGeometry(text, top, bottom, left, right, height));
        }

        return result;
    }

    internal static string AssembleLines(IReadOnlyList<LineGeometry> lines, double? pageWidth = null)
    {
        var validLines = lines
            .Where(l => !string.IsNullOrWhiteSpace(l.Text))
            .ToList();

        if (validLines.Count == 0)
        {
            return string.Empty;
        }

        // Calculate vertical spacing between consecutive lines: dy_i = Bottom_i - Bottom_{i+1}
        var spacings = new List<double>();
        for (int i = 0; i < validLines.Count - 1; i++)
        {
            double dy = validLines[i].Bottom - validLines[i + 1].Bottom;
            if (dy > 0)
            {
                spacings.Add(dy);
            }
        }

        double medianSpacing;
        if (spacings.Count > 0)
        {
            var sortedSpacings = spacings.OrderBy(s => s).ToList();
            medianSpacing = sortedSpacings[sortedSpacings.Count / 2];
        }
        else
        {
            medianSpacing = validLines[0].Height > 0 ? validLines[0].Height * 1.3 : 14.0;
        }

        var heights = validLines.Select(l => l.Height).Where(h => h > 0).OrderBy(h => h).ToList();
        double medianHeight = heights.Count > 0 ? heights[heights.Count / 2] : 10.0;

        // Calculate left column margin X_min (10th percentile or minimum of substantial lines)
        var substantialLines = validLines.Where(l => l.Text.Length > 20).ToList();
        double xMin;
        double xMax;

        if (substantialLines.Count >= 3)
        {
            var sortedLefts = substantialLines.Select(l => l.Left).OrderBy(x => x).ToList();
            int p10Index = (int)Math.Floor(sortedLefts.Count * 0.10);
            xMin = sortedLefts[Math.Clamp(p10Index, 0, sortedLefts.Count - 1)];

            var sortedRights = substantialLines.Select(l => l.Right).OrderBy(x => x).ToList();
            int p90Index = (int)Math.Floor(sortedRights.Count * 0.90);
            xMax = sortedRights[Math.Clamp(p90Index, 0, sortedRights.Count - 1)];
        }
        else if (substantialLines.Count > 0)
        {
            xMin = substantialLines.Min(l => l.Left);
            xMax = substantialLines.Max(l => l.Right);
        }
        else
        {
            xMin = validLines.Min(l => l.Left);
            xMax = validLines.Max(l => l.Right);
        }

        var sb = new StringBuilder();

        // First line
        if (IsHeadingLine(validLines[0], medianHeight, xMin, xMax, pageWidth))
        {
            sb.Append(FormatHeading(validLines[0].Text));
        }
        else
        {
            sb.Append(validLines[0].Text);
        }

        for (int i = 1; i < validLines.Count; i++)
        {
            var prev = validLines[i - 1];
            var curr = validLines[i];

            bool prevIsHeading = IsHeadingLine(prev, medianHeight, xMin, xMax, pageWidth);
            bool currIsHeading = IsHeadingLine(curr, medianHeight, xMin, xMax, pageWidth);

            string separator;

            if (prevIsHeading || currIsHeading)
            {
                separator = "\n\n";
            }
            else
            {
                double dy = prev.Bottom - curr.Bottom;
                bool isGap = dy > 0 && medianSpacing > 0 && dy >= 1.35 * medianSpacing;
                bool isIndent = (curr.Left - xMin) >= 12.0;

                var prevText = prev.Text.Trim();
                var currText = curr.Text.Trim();

                bool startsWithDialogue =
                    currText.StartsWith('"') ||
                    currText.StartsWith('“') ||
                    currText.StartsWith('”') ||
                    currText.StartsWith('—') ||
                    currText.StartsWith('–') ||
                    (currText.StartsWith('-') && (currText.Length == 1 || char.IsWhiteSpace(currText[1])));

                bool prevEndsPunctuation =
                    prevText.Length > 0 &&
                    (char.IsPunctuation(prevText[^1]) || prevText.EndsWith("…"));

                bool isDialogue = startsWithDialogue && prevEndsPunctuation;

                if (isGap || isIndent || isDialogue)
                {
                    separator = "\n\n";
                }
                else if (IsCodeLike(prevText) || IsCodeLike(currText))
                {
                    separator = "\n";
                }
                else
                {
                    separator = " ";
                }
            }

            if (separator == "\n\n")
            {
                sb.Append("\n\n");
                if (currIsHeading)
                {
                    sb.Append(FormatHeading(curr.Text));
                }
                else
                {
                    sb.Append(curr.Text);
                }
            }
            else if (separator == "\n")
            {
                sb.Append('\n');
                sb.Append(curr.Text);
            }
            else
            {
                // Normal intra-paragraph wrap
                var prevTrimmed = prev.Text.TrimEnd();
                var currTrimmed = curr.Text.TrimStart();
                if (prevTrimmed.EndsWith('-') && prevTrimmed.Length >= 2 && char.IsLetter(prevTrimmed[^2]) &&
                    currTrimmed.Length > 0 && char.IsLower(currTrimmed[0]))
                {
                    // Dehyphenate
                    sb.Length--;
                    sb.Append(currTrimmed);
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(currTrimmed);
                }
            }
        }

        return sb.ToString().Trim();
    }

    private static bool IsCentered(LineGeometry line, double xMin, double xMax, double? pageWidth)
    {
        if (line.Text.Length >= 60) return false;

        if (xMax - xMin > 100)
        {
            double leftGap = line.Left - xMin;
            double rightGap = xMax - line.Right;

            if (leftGap >= 12.0 && rightGap >= 12.0 && Math.Abs(leftGap - rightGap) <= Math.Max(25.0, (xMax - xMin) * 0.15))
            {
                return true;
            }
        }

        if (pageWidth.HasValue && pageWidth.Value > 200)
        {
            double leftMargin = line.Left;
            double rightMargin = pageWidth.Value - line.Right;
            double lineWidth = line.Right - line.Left;

            if (lineWidth < pageWidth.Value * 0.75 &&
                leftMargin >= 20.0 && rightMargin >= 20.0 &&
                Math.Abs(leftMargin - rightMargin) <= Math.Max(30.0, pageWidth.Value * 0.10))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHeadingLine(LineGeometry line, double medianHeight, double xMin, double xMax, double? pageWidth)
    {
        var text = line.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (text.StartsWith('#')) return true;

        if (Regex.IsMatch(text, @"^[\d\s\.\-—–]{1,5}$"))
        {
            return false;
        }

        if (IsTitlePattern(text))
        {
            return true;
        }

        if (text.Length < 60)
        {
            if (medianHeight > 0 && line.Height >= 1.25 * medianHeight)
            {
                return true;
            }

            if (IsCentered(line, xMin, xMax, pageWidth))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsTitlePattern(string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return false;
        if (trimmed.Length > 70) return false;

        if (TitlePatternRegex.IsMatch(trimmed)) return true;

        if (trimmed.Length >= 4 && trimmed.Length <= 50 &&
            !trimmed.Contains('.') && !trimmed.Contains(':') &&
            (AllCapsTitleRegex.IsMatch(trimmed) || (trimmed.Any(char.IsLetter) && trimmed.All(c => !char.IsLetter(c) || char.IsUpper(c)))))
        {
            return true;
        }

        return false;
    }

    private static string FormatHeading(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith('#'))
        {
            return trimmed;
        }
        return $"## {trimmed}";
    }

    private static bool IsCodeLike(string trimmed)
    {
        if (string.IsNullOrWhiteSpace(trimmed)) return false;
        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("@page") || trimmed.StartsWith("@code")) return true;
        if (trimmed is "{" or "}" or "};" or "});") return true;
        if (trimmed.StartsWith("using ") && trimmed.EndsWith(';')) return true;
        if (trimmed.StartsWith("namespace ")) return true;
        if (trimmed.EndsWith(';') && (trimmed.Contains("var ") || trimmed.Contains('=') || trimmed.Contains("return ") || trimmed.Contains('('))) return true;
        return false;
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

        // Strip Microsoft pre-release disclaimer banners (multi-line and with leading punctuation)
        var cleaned = Regex.Replace(
            text,
            @"(?is)(?:###\s*\d{1,2}/\d{1,2}/\d{4}|\b\d{2}/\d{2}/\d{4}\b)?\s*[\)\(\]\s]*Important\s+This information relates to a pre-release product.*?(?:Microsoft makes no warranties[^.\n]*\.|For the current release[^.\n]*\.|\.\s*\n)",
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
        bool lastCodeLineWasClosingDelimiter = false;
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
                    // 1 blank line after closing brace or 2 or more consecutive blank lines closes code block
                    if (lastCodeLineWasClosingDelimiter || emptyLineStreak >= 2)
                    {
                        sb.AppendLine("```");
                        inCodeBlock = false;
                        lastCodeLineWasClosingDelimiter = false;
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
                else
                {
                    if (emptyLineStreak == 1)
                    {
                        sb.AppendLine();
                    }
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

            // Explicit markdown heading
            if (trimmed.StartsWith('#'))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                sb.AppendLine();
                sb.AppendLine(trimmed);
                sb.AppendLine();
                continue;
            }

            // Subheading detection: title patterns or uppercase titles
            if (IsTitlePattern(trimmed))
            {
                if (inCodeBlock)
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                sb.AppendLine();
                sb.AppendLine($"## {trimmed}");
                sb.AppendLine();
                continue;
            }

            if (inCodeBlock)
            {
                // Escape code block if line is clearly prose
                if (IsObviousProse(trimmed))
                {
                    sb.AppendLine("```");
                    inCodeBlock = false;
                    lastCodeLineWasClosingDelimiter = false;
                    sb.AppendLine(trimmed);
                    continue;
                }

                sb.AppendLine(line);
                lastCodeLineWasClosingDelimiter = (trimmed is "}" or "};" or "});");
                continue;
            }

            // Outside code block: Open code block if strong code signals detected
            if (IsStrongCodeStart(trimmed))
            {
                sb.AppendLine("```csharp");
                inCodeBlock = true;
                lastCodeLineWasClosingDelimiter = false;
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
        var result = sb.ToString();
        result = Regex.Replace(result, @"(\r?\n){3,}", "\n\n");
        return result.Trim();
    }

    private static bool IsObviousProse(string trimmed)
    {
        // 1. Markdown indicators
        if (trimmed.StartsWith('#') || trimmed.StartsWith('-') || trimmed.StartsWith('*')) return true;

        // 2. Prose label ending with colon (e.g. "Components/Pages/Counter.razor :", "Change the app:")
        if (trimmed.EndsWith(':') && !trimmed.Contains('{') && !trimmed.Contains(';') && !trimmed.Contains("=>")) return true;

        // 3. Known documentation section titles without punctuation
        if (Regex.IsMatch(trimmed, @"^(New behavior|Previous behavior|Type of breaking change|Reason for change|Recommended action|Affected APIs|Change the app|Prerequisites|Next steps|See also|Important|Note|Overview|Summary|For more information|Version introduced|Category)\b", RegexOptions.IgnoreCase))
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

        // Standalone opening brace
        if (trimmed == "{")
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
