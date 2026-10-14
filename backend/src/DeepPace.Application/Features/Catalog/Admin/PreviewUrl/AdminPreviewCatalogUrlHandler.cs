using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Catalog.Admin.PreviewUrl;

public record AdminPreviewCatalogUrlRequest(string Url);

public record AdminPreviewCatalogUrlResponse(
    string Title,
    string SourceUrl,
    string MarkdownContent,
    int EstimatedWordCount,
    bool IsPdfDetected = false,
    string? DetectedPdfUrl = null);

public class AdminPreviewCatalogUrlValidator : AbstractValidator<AdminPreviewCatalogUrlRequest>
{
    public AdminPreviewCatalogUrlValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid HTTP or HTTPS URL is required.");
    }
}

public class AdminPreviewCatalogUrlHandler : IUseCase<AdminPreviewCatalogUrlRequest, AdminPreviewCatalogUrlResponse>
{
    private readonly IWebArticleCrawler _crawler;
    private readonly IValidator<AdminPreviewCatalogUrlRequest> _validator;

    public AdminPreviewCatalogUrlHandler(
        IWebArticleCrawler crawler,
        IValidator<AdminPreviewCatalogUrlRequest> validator)
    {
        _crawler = crawler;
        _validator = validator;
    }

    public async Task<Result<AdminPreviewCatalogUrlResponse>> ExecuteAsync(
        AdminPreviewCatalogUrlRequest request,
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
            return new AdminPreviewCatalogUrlResponse(
                Title: result.Title,
                SourceUrl: result.SourceUrl,
                MarkdownContent: result.MarkdownContent,
                EstimatedWordCount: result.EstimatedWordCount,
                IsPdfDetected: result.IsPdfDetected,
                DetectedPdfUrl: result.DetectedPdfUrl);
        }
        catch (Exception ex)
        {
            return Error.Custom("Crawler.Failed", $"Failed to preview URL: {ex.Message}");
        }
    }
}
