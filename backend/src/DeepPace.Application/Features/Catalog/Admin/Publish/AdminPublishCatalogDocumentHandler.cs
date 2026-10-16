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
    int ExpectedConcurrencyVersion,
    int PresentedSessionVersion);

public class AdminPublishCatalogDocumentHandler : IUseCase<AdminPublishCatalogDocumentRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;

    public AdminPublishCatalogDocumentHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminPublishCatalogDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.ExecutePrivilegedMutationAsync(
            request.AdminUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageCatalog },
            () => PublishInternalAsync(request, cancellationToken),
            cancellationToken);

        if (result.IsFailure && result.Error.Code is "Catalog.ConcurrencyConflict" or "Catalog.PublicationIneligible")
        {
            try
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId,
                    action: AdminAuditActions.CatalogPublish,
                    targetType: "CatalogDocument",
                    targetId: request.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: result.Error.Code);

                await _unitOfWork.AppendCleanAuditEventAsync(rejectAudit, cancellationToken);
            }
            catch (Exception)
            {
                return Error.Custom("AUDIT_PERSISTENCE_UNAVAILABLE", "Publication rejection audit history could not be persisted.");
            }
        }

        return result;
    }
    private async Task<Result<AdminCatalogDocumentDto>> PublishInternalAsync(
        AdminPublishCatalogDocumentRequest request,
        CancellationToken cancellationToken)
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
            return Error.Custom("Catalog.ConcurrencyConflict",
                "Document has been modified by another administrator. Please reload.");
        }

        var (isEligible, unmet) = doc.CheckPublicationEligibility();
        if (!isEligible)
        {
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
