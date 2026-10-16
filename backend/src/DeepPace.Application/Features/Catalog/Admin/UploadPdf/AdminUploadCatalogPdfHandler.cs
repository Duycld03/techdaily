using System.Text.Json;
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
    Guid CategoryId,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    string TempFilePath,
    Guid? AdminUserId,
    int PresentedSessionVersion = 1);

public class AdminUploadCatalogPdfValidator : AbstractValidator<AdminUploadCatalogPdfRequest>
{
    public AdminUploadCatalogPdfValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.TempFilePath).NotEmpty()
            .Must(File.Exists).WithMessage("Uploaded PDF temporary file does not exist on disk.");
    }
}

public class AdminUploadCatalogPdfHandler : IUseCase<AdminUploadCatalogPdfRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IPdfIngestionQueue _queue;
    private readonly IValidator<AdminUploadCatalogPdfRequest> _validator;
    private readonly ICategoryAssignmentGuard _categoryGuard;
    private readonly IAdministrativeUnitOfWork _unitOfWork;

    public AdminUploadCatalogPdfHandler(
        IDeepPaceDbContext dbContext,
        IPdfIngestionQueue queue,
        IValidator<AdminUploadCatalogPdfRequest> validator,
        ICategoryAssignmentGuard categoryGuard,
        IAdministrativeUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _queue = queue;
        _validator = validator;
        _categoryGuard = categoryGuard;
        _unitOfWork = unitOfWork;
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
            CategoryId = request.CategoryId,
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

        var admitted = await _unitOfWork.ExecutePrivilegedMutationAsync(request.AdminUserId ?? Guid.Empty, request.PresentedSessionVersion,
            new[] { AdminPermission.ManageCatalog },
            () => _categoryGuard.ExecuteAsync<AdminCatalogDocumentDto>(request.CategoryId, async category =>
            {
                doc.Category = category;
                await _dbContext.CatalogDocuments.AddAsync(doc, cancellationToken);
                await _dbContext.AdminAuditEvents.AddAsync(AdminAuditEvent.Create(request.AdminUserId!.Value,
                    AdminAuditActions.CatalogUploadPdf, "CatalogDocument", doc.Id.ToString(), AdminAuditOutcome.Accepted,
                    correlationId: doc.Id.ToString(), changedFields: new[] { "Visibility", "ProcessingStatus", "ProcessingGeneration", "CategoryId" },
                    statePayload: JsonSerializer.Serialize(new { doc.Visibility, doc.ProcessingGeneration })), cancellationToken);
                return doc.ToAdminDto();
            }, cancellationToken), cancellationToken);
        if (!admitted.IsSuccess)
        {
            File.Delete(request.TempFilePath);
            return admitted.Error;
        }

        try
        {
            await _queue.EnqueueAsync(new PdfIngestJob(
                TargetId: doc.Id,
                TempFilePath: request.TempFilePath,
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
}
