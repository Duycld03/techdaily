using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.UploadPdf;

public record AdminUploadCatalogPdfRequest(
    string Title,
    string Description,
    Category Category,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    string TempFilePath,
    Guid? AdminUserId);

public class AdminUploadCatalogPdfValidator : AbstractValidator<AdminUploadCatalogPdfRequest>
{
    public AdminUploadCatalogPdfValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
        RuleFor(x => x.TempFilePath).NotEmpty()
            .Must(File.Exists).WithMessage("Uploaded PDF temporary file does not exist on disk.");
    }
}

public class AdminUploadCatalogPdfHandler : IUseCase<AdminUploadCatalogPdfRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IPdfIngestionQueue _queue;
    private readonly IValidator<AdminUploadCatalogPdfRequest> _validator;

    public AdminUploadCatalogPdfHandler(
        IDeepPaceDbContext dbContext,
        IPdfIngestionQueue queue,
        IValidator<AdminUploadCatalogPdfRequest> validator)
    {
        _dbContext = dbContext;
        _queue = queue;
        _validator = validator;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminUploadCatalogPdfRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Language = request.Language,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = request.AuthorOrSourceUrl,
            AttributionText = string.IsNullOrWhiteSpace(request.AttributionText) ? (request.AuthorOrSourceUrl ?? request.Title) : request.AttributionText,
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Pending,
            ProcessingGeneration = 1,
            ProcessingStatusMessage = "Enqueued for background extraction and preparation",
            CreatedByUserId = request.AdminUserId
        };

        await _dbContext.CatalogDocuments.AddAsync(doc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueAsync(new PdfIngestJob(
            TargetId: doc.Id,
            TempFilePath: request.TempFilePath,
            DocumentTitle: doc.Title,
            Category: doc.Category,
            Language: doc.Language,
            DestinationType: IngestionDestinationType.CatalogDocument,
            Generation: doc.ProcessingGeneration), cancellationToken);

        return doc.ToAdminDto();
    }
}
