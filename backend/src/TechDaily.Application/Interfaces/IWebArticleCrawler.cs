namespace TechDaily.Application.Interfaces;

public record CrawlArticleResult(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount,
    bool IsPdfDetected = false,
    string? DetectedPdfUrl = null);

public interface IWebArticleCrawler
{
    Task<CrawlArticleResult> CrawlUrlAsync(
        string url,
        CancellationToken cancellationToken = default);
}
