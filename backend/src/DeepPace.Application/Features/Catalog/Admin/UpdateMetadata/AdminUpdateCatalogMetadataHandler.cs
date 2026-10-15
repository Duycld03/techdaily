using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.UpdateMetadata;
public record AdminUpdateCatalogMetadataRequest(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    int ExpectedConcurrencyVersion,
    Guid? AdminUserId = null);

public class AdminUpdateCatalogMetadataValidator : AbstractValidator<AdminUpdateCatalogMetadataRequest>
{
    public AdminUpdateCatalogMetadataValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
    }
}

public class AdminUpdateCatalogMetadataHandler : IUseCase<AdminUpdateCatalogMetadataRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<AdminUpdateCatalogMetadataRequest> _validator;

    public AdminUpdateCatalogMetadataHandler(
        IDeepPaceDbContext dbContext,
        IValidator<AdminUpdateCatalogMetadataRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminUpdateCatalogMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doc == null || doc.IsDeleted)
        {
            return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        }

        var hasActiveAttempt = await _dbContext.IngestionAttempts
            .AnyAsync(a => a.TargetScope == IngestionTargetScope.Catalog &&
                           a.TargetDocumentId == doc.Id &&
                           (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                cancellationToken);

        if (hasActiveAttempt)
        {
            return Error.Custom("Catalog.ActiveAttemptInProgress", "Cannot modify metadata while document ingestion is actively in progress.");
        }

        if (doc.Visibility == CatalogVisibility.Published)
        {
            if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId.Value,
                    action: AdminAuditActions.CatalogUpdateMetadata,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: "Catalog.PublishedImmutable");
                await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Error.Custom("Catalog.PublishedImmutable",
                "Published documents are immutable. The document must be hidden before editing metadata.");
        }

        if (doc.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId.Value,
                    action: AdminAuditActions.CatalogUpdateMetadata,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: "Catalog.ConcurrencyConflict");
                await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Error.Custom("Catalog.ConcurrencyConflict",
                "Document metadata has been modified by another administrator. Please reload.");
        }
        try
        {
            doc.UpdateMetadata(
                title: request.Title,
                description: request.Description,
                category: request.Category,
                language: request.Language,
                authorOrSourceUrl: request.AuthorOrSourceUrl,
                attributionText: string.IsNullOrWhiteSpace(request.AttributionText) ? (request.AuthorOrSourceUrl ?? request.Title) : request.AttributionText);
            if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
            {
                var audit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId.Value,
                    action: AdminAuditActions.CatalogUpdateMetadata,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    correlationId: doc.Id.ToString(),
                    changedFields: new[] { "Title", "Description", "Category", "Language", "AuthorOrSourceUrl", "AttributionText", "ConcurrencyVersion" },
                    statePayload: JsonSerializer.Serialize(new { ConcurrencyVersion = doc.ConcurrencyVersion }));
                await _dbContext.AdminAuditEvents.AddAsync(audit, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return doc.ToAdminDto();
        }
        catch (InvalidOperationException ex)
        {
            return Error.Custom("Catalog.InvalidOperation", ex.Message);
        }
    }
}
