using DeepPace.Domain.Entities;

using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
namespace DeepPace.Application.Features.Catalog.Admin.ImportUrl;

public record AdminImportCatalogUrlRequest(
    string Url,
    string Title,
    string Description,
    Guid CategoryId,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    Guid? AdminUserId,
    int PresentedSessionVersion = 1);

public class AdminImportCatalogUrlValidator : AbstractValidator<AdminImportCatalogUrlRequest>
{
    public AdminImportCatalogUrlValidator()
    {
        RuleFor(x => x.Url).NotEmpty()
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid HTTP or HTTPS URL is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class AdminImportCatalogUrlHandler : IUseCase<AdminImportCatalogUrlRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IWebArticleCrawler _crawler;
    private readonly IAiMarkdownFormatter _aiFormatter;
    private readonly IEmbeddingService _embeddingService;
    private readonly IPdfIngestionQueue _pdfQueue;
    private readonly IValidator<AdminImportCatalogUrlRequest> _validator;
    private readonly ILogger<AdminImportCatalogUrlHandler> _logger;
    private readonly ICategoryAssignmentGuard _categoryGuard;
    private readonly IAdministrativeUnitOfWork _unitOfWork;

    public AdminImportCatalogUrlHandler(
        IDeepPaceDbContext dbContext,
        IWebArticleCrawler crawler,
        IAiMarkdownFormatter aiFormatter,
        IEmbeddingService embeddingService,
        IPdfIngestionQueue pdfQueue,
        IValidator<AdminImportCatalogUrlRequest> validator,
        ILogger<AdminImportCatalogUrlHandler> logger,
        ICategoryAssignmentGuard categoryGuard,
        IAdministrativeUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _crawler = crawler;
        _aiFormatter = aiFormatter;
        _embeddingService = embeddingService;
        _pdfQueue = pdfQueue;
        _validator = validator;
        _logger = logger;
        _categoryGuard = categoryGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminImportCatalogUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }
        var categoryValidation = await _categoryGuard.ValidateAsync(request.CategoryId, cancellationToken);
        if (!categoryValidation.IsSuccess) return categoryValidation.Error;


        CrawlArticleResult crawlResult;
        try
        {
            crawlResult = await _crawler.CrawlUrlAsync(request.Url, cancellationToken);
        }
        catch (Exception ex)
        {
            return Error.Custom("Crawler.Failed", $"Failed to crawl document from URL: {ex.Message}");
        }

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Language = request.Language,
            SourceType = crawlResult.IsPdfDetected ? SourceType.PdfBook : SourceType.WebDocUrl,
            AuthorOrSourceUrl = request.AuthorOrSourceUrl ?? request.Url,
            AttributionText = string.IsNullOrWhiteSpace(request.AttributionText) ? (request.AuthorOrSourceUrl ?? request.Url) : request.AttributionText,
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Processing,
            ProcessingGeneration = 1,
            CreatedByUserId = request.AdminUserId
        };

        if (crawlResult.IsPdfDetected && !string.IsNullOrWhiteSpace(crawlResult.DetectedPdfUrl))
        {
            // Detected PDF: Download to temporary file and enqueue
            var tempPdfPath = Path.Combine(Path.GetTempPath(), $"catalog-remote-{doc.Id:N}.pdf");
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var pdfBytes = await httpClient.GetByteArrayAsync(crawlResult.DetectedPdfUrl, cancellationToken);
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes, cancellationToken);

            doc.ProcessingStatus = ProcessingStatus.Pending;
            doc.ProcessingStatusMessage = "Detected remote PDF enqueued for extraction";
            var admitted = await PersistAsync(doc, request, AdminAuditOutcome.Accepted, cancellationToken);
            if (!admitted.IsSuccess)
            {
                File.Delete(tempPdfPath);
                return admitted.Error;
            }

            try
            {
                await _pdfQueue.EnqueueAsync(new PdfIngestJob(
                    TargetId: doc.Id,
                    TempFilePath: tempPdfPath,
                    DocumentTitle: doc.Title,
                    CategoryId: request.CategoryId,
                    Language: doc.Language,
                    DestinationType: IngestionDestinationType.CatalogDocument,
                    Generation: doc.ProcessingGeneration), cancellationToken);
            }
            catch (Exception)
            {
                if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
                {
                    var failAudit = AdminAuditEvent.Create(
                        actorUserId: request.AdminUserId.Value,
                        action: AdminAuditActions.CatalogDispatchFailed,
                        targetType: "CatalogDocument",
                        targetId: doc.Id.ToString(),
                        outcome: AdminAuditOutcome.Failed,
                        errorCode: "DISPATCH_FAILED",
                        correlationId: doc.Id.ToString());
                    await _dbContext.AdminAuditEvents.AddAsync(failAudit, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return doc.ToAdminDto();

        }
        // Web article: split into chunks and prepare
        var rawChunks = SplitIntoChunks(crawlResult.MarkdownContent);
        if (rawChunks.Count == 0)
        {
            return Error.Custom("Crawler.NoContent", "No readable content could be extracted from this URL.");
        }

        int order = 1;
        var slices = new List<CatalogSlice>();
        foreach (var chunkText in rawChunks)
        {
            var title = ExtractTitle(chunkText, order);
            var estimatedMinutes = Math.Max(1, chunkText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200);

            var contentWithoutHeading = Regex.Replace(chunkText, @"^\s*#+\s+[^\n\r]+(\r?\n)*", "").Trim();
            var summaryText = string.IsNullOrWhiteSpace(contentWithoutHeading) ? title : contentWithoutHeading;

            var slice = new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = order++,
                ChapterTitle = title,
                OriginalTextMarkdown = chunkText,
                SummaryMarkdown = summaryText.Length > 280 ? summaryText.Substring(0, 277) + "..." : summaryText,
                KeyTakeaways = new() { "Core Architecture Principle", "System Invariant" },
                Language = request.Language,
                EstimatedReadMinutes = estimatedMinutes,
                IsAiFormatted = false
            };
            slices.Add(slice);
            doc.Slices.Add(slice);
        }

        doc.TotalSlices = slices.Count;
        doc.EstimatedReadMinutes = slices.Sum(s => s.EstimatedReadMinutes);

        // AI Format initial min(3, N) slices
        int initialCount = Math.Min(3, slices.Count);
        bool aiSuccess = true;
        string? aiError = null;

        for (int i = 0; i < initialCount; i++)
        {
            var slice = slices[i];
            var aiResult = await _aiFormatter.FormatSliceAsync(slice.OriginalTextMarkdown, slice.ChapterTitle, slice.Language, cancellationToken);

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Value?.FormattedMarkdown))
            {
                aiSuccess = false;
                aiError = aiResult.Error?.Message ?? "AI returned empty formatted content";
                break;
            }

            var val = aiResult.Value!;
            slice.SummaryMarkdown = !string.IsNullOrWhiteSpace(val.SummaryMarkdown)
                ? val.SummaryMarkdown
                : val.FormattedMarkdown;
            slice.KeyTakeaways = val.KeyTakeaways;
            slice.EstimatedReadMinutes = val.EstimatedReadMinutes;
            slice.IsAiFormatted = true;
        }

        if (aiSuccess)
        {
            doc.ProcessingStatus = ProcessingStatus.Ready;
            doc.ProcessingStatusMessage = "Preparation complete. Ready for review.";
            doc.ProcessingErrorMessage = null;
        }
        else
        {
            doc.ProcessingStatus = ProcessingStatus.Failed;
            doc.ProcessingErrorMessage = $"Initial AI formatting failed: {aiError}";
            doc.ProcessingStatusMessage = "Preparation failed during initial slice AI formatting";
        }

        return await PersistAsync(doc, request, AdminAuditOutcome.Succeeded, cancellationToken);
    }

    private Task<Result<AdminCatalogDocumentDto>> PersistAsync(CatalogDocument doc, AdminImportCatalogUrlRequest request, AdminAuditOutcome outcome, CancellationToken ct)
        => _unitOfWork.ExecutePrivilegedMutationAsync(request.AdminUserId ?? Guid.Empty, request.PresentedSessionVersion,
            new[] { AdminPermission.ManageCatalog },
            () => _categoryGuard.ExecuteAsync<AdminCatalogDocumentDto>(request.CategoryId, async category =>
            {
                doc.Category = category;
                await _dbContext.CatalogDocuments.AddAsync(doc, ct);
                await _dbContext.AdminAuditEvents.AddAsync(AdminAuditEvent.Create(request.AdminUserId!.Value,
                    AdminAuditActions.CatalogImportUrl, "CatalogDocument", doc.Id.ToString(), outcome,
                    correlationId: doc.Id.ToString(), changedFields: new[] { "Visibility", "ProcessingStatus", "TotalSlices", "CategoryId" },
                    statePayload: JsonSerializer.Serialize(new { doc.Visibility, doc.TotalSlices })), ct);
                return doc.ToAdminDto();
            }, ct), ct);

    private static List<string> SplitIntoChunks(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return new List<string>();

        var headingPattern = @"(?=(?:\r?\n){1,2}#{1,3}\s+)";
        var parts = Regex.Split(markdown, headingPattern, RegexOptions.Multiline)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p) && p.Length >= 50)
            .ToList();

        if (parts.Count == 0)
        {
            parts = new List<string> { markdown.Trim() };
        }

        return parts;
    }

    private static string ExtractTitle(string chunk, int order)
    {
        var match = Regex.Match(chunk, @"^\s*#+\s+([^\n\r]+)", RegexOptions.Multiline);
        if (match.Success)
        {
            var title = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(title)) return title;
        }

        return $"Section {order}";
    }
}
