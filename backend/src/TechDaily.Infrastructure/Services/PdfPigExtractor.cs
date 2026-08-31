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

    public async Task<PdfExtractionResult> ExtractSlicesAsync(
        Stream pdfStream,
        string? customTitle = null,
        int maxPages = 800,
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

            if (totalPages > maxPages)
            {
                throw new InvalidOperationException(
                    $"PDF exceeds the safety limit of {maxPages} pages (Document has {totalPages} pages).");
            }

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

            var slices = new List<ExtractedPdfSlice>();
            var currentSliceText = new StringBuilder();
            var currentChapterTitle = "Introduction & Overview";
            int sliceOrder = 1;
            int currentWordCount = 0;

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageText = ExtractPageLines(page);
                if (string.IsNullOrWhiteSpace(pageText))
                {
                    continue;
                }

                // Check if page contains a distinct chapter heading in the first few lines
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

                // Segment by detected chapter heading or word threshold
                if ((detectedHeading != null && currentWordCount >= 200) || (currentWordCount + wordsInPage >= 700 && currentSliceText.Length > 0))
                {
                    var content = currentSliceText.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        slices.Add(CreateSlice(sliceOrder++, currentChapterTitle, content));
                    }

                    currentSliceText.Clear();
                    currentWordCount = 0;
                    if (detectedHeading != null)
                    {
                        currentChapterTitle = detectedHeading;
                    }
                    else
                    {
                        currentChapterTitle = $"Slice {sliceOrder}: Page {page.Number}";
                    }
                }
                else if (detectedHeading != null && currentWordCount < 200)
                {
                    currentChapterTitle = detectedHeading;
                }

                currentSliceText.AppendLine(pageText);
                currentSliceText.AppendLine();
                currentWordCount += wordsInPage;
            }

            // Add trailing slice
            if (currentSliceText.Length > 0)
            {
                var content = currentSliceText.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    slices.Add(CreateSlice(sliceOrder++, currentChapterTitle, content));
                }
            }

            // Fallback if no text extracted (e.g. scanned image PDF or single short page)
            if (slices.Count == 0)
            {
                slices.Add(CreateSlice(1, docTitle, "No selectable text found in this PDF. If this is a scanned document, please ensure it has an OCR text layer."));
            }

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
                    sb.AppendLine("```");
                    inCodeBlock = false;
                }
                sb.AppendLine();
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
            bool isCodeLine = Regex.IsMatch(trimmed, @"^(public|private|protected|internal|class|interface|record|struct|enum|using|import|export|function|const|let|var|def|return|SELECT|FROM|WHERE|INSERT|CREATE)\s+", RegexOptions.IgnoreCase) ||
                              trimmed.EndsWith(';') || trimmed.EndsWith('{') || trimmed.EndsWith('}') || trimmed.Contains("=>");

            if (isCodeLine && !trimmed.StartsWith('#') && !trimmed.StartsWith('-'))
            {
                if (!inCodeBlock)
                {
                    sb.AppendLine("```csharp");
                    inCodeBlock = true;
                }
                sb.AppendLine(trimmed);
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
