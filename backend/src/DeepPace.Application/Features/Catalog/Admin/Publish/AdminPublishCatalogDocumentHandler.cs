using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
namespace DeepPace.Application.Features.Catalog.Admin.Publish;

public record AdminPublishCatalogDocumentRequest(
    Guid Id,
    Guid AdminUserId,
    int ExpectedConcurrencyVersion);

public class AdminPublishCatalogDocumentHandler : IUseCase<AdminPublishCatalogDocumentRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminPublishCatalogDocumentHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminPublishCatalogDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
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
            return Error.Custom("Catalog.ActiveAttemptInProgress", "Cannot publish document while ingestion is actively in progress.");
        }

        if (doc.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            if (request.AdminUserId != Guid.Empty)
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId,
                    action: AdminAuditActions.CatalogPublish,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: "Catalog.ConcurrencyConflict");
                await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Error.Custom("Catalog.ConcurrencyConflict",
                "Document has been modified by another administrator. Please reload.");
        }

        var (isEligible, unmet) = doc.CheckPublicationEligibility();
        if (!isEligible)
        {
            if (request.AdminUserId != Guid.Empty)
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId,
                    action: AdminAuditActions.CatalogPublish,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: "Catalog.PublicationIneligible");
                await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Error.Custom("Catalog.PublicationIneligible",
                $"Cannot publish document: {string.Join(" ", unmet)}");
        }
        try
        {
            doc.Publish(request.AdminUserId);
            if (request.AdminUserId != Guid.Empty)
            {
                var audit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId,
                    action: AdminAuditActions.CatalogPublish,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    correlationId: doc.Id.ToString(),
                    changedFields: new[] { "Visibility", "PublishedAt", "PublishedByUserId", "ConcurrencyVersion" },
                    statePayload: JsonSerializer.Serialize(new { Visibility = doc.Visibility.ToString(), ConcurrencyVersion = doc.ConcurrencyVersion }));
                await _dbContext.AdminAuditEvents.AddAsync(audit, cancellationToken);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            return doc.ToAdminDto();
        }
        catch (InvalidOperationException ex)
        {
            return Error.Custom("Catalog.PublicationFailed", ex.Message);
        }
    }
}
