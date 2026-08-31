using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.CrawlUrl;

public record CrawlUrlRequest(string Url);

public record CrawlUrlResponse(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount);

public class CrawlUrlValidator : AbstractValidator<CrawlUrlRequest>
{
    public CrawlUrlValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid HTTP or HTTPS URL is required.");
    }
}

public class CrawlUrlHandler : IUseCase<CrawlUrlRequest, CrawlUrlResponse>
{
    private readonly IWebArticleCrawler _crawler;
    private readonly IValidator<CrawlUrlRequest> _validator;

    public CrawlUrlHandler(
        IWebArticleCrawler crawler,
        IValidator<CrawlUrlRequest> validator)
    {
        _crawler = crawler;
        _validator = validator;
    }

    public async Task<Result<CrawlUrlResponse>> ExecuteAsync(
        CrawlUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        try
        {
            var result = await _crawler.CrawlUrlAsync(request.Url, cancellationToken);
            return new CrawlUrlResponse(
                Title: result.Title,
                SourceUrl: result.SourceUrl,
                MarkdownContent: result.MarkdownContent,
                EstimatedWordCount: result.EstimatedWordCount
            );
        }
        catch (Exception ex)
        {
            return Error.Custom("Crawler.Failed", $"Failed to crawl document from URL: {ex.Message}");
        }
    }
}
