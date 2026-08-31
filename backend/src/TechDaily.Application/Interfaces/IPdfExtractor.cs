namespace TechDaily.Application.Interfaces;

public record ExtractedPdfSlice(
    int Order,
    string ChapterTitle,
    string ContentMarkdown,
    int EstimatedReadMinutes,
    List<string> KeyTakeaways);

public record PdfExtractionResult(
    string DocumentTitle,
    int TotalPages,
    List<ExtractedPdfSlice> Slices);

public interface IPdfExtractor
{
    Task<PdfExtractionResult> ExtractSlicesAsync(
        Stream pdfStream,
        string? customTitle = null,
        int maxPages = 800,
        CancellationToken cancellationToken = default);
}
