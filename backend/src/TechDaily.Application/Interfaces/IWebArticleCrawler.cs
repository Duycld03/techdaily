namespace TechDaily.Application.Interfaces;

public record CrawlArticleResult(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount);

public interface IWebArticleCrawler
{
    Task<CrawlArticleResult> CrawlUrlAsync(
        string url,
        CancellationToken cancellationToken = default);
}
