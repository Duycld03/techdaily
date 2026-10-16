using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
namespace DeepPace.Application.Features.Catalog.Admin.Review;

public record AdminReviewCatalogRequest(
    Guid CatalogDocumentId,
    Guid AdminUserId,
    int PresentedSessionVersion);

public class AdminReviewCatalogHandler : IUseCase<AdminReviewCatalogRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;

    public AdminReviewCatalogHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminReviewCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecutePrivilegedMutationAsync(
            request.AdminUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageCatalog },
            () => ReviewInternalAsync(request, cancellationToken),
            cancellationToken);
    }
    private async Task<Result<AdminCatalogDocumentDto>> ReviewInternalAsync(
        AdminReviewCatalogRequest request,
        CancellationToken cancellationToken)
    {
        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .FirstOrDefaultAsync(d => d.Id == request.CatalogDocumentId, cancellationToken);

        if (doc == null || doc.IsDeleted)
        {
            return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        }

        doc.Review(request.AdminUserId);
        if (request.AdminUserId != Guid.Empty)
        {
            var audit = AdminAuditEvent.Create(
                actorUserId: request.AdminUserId,
                action: AdminAuditActions.CatalogReview,
                targetType: "CatalogDocument",
                targetId: doc.Id.ToString(),
                outcome: AdminAuditOutcome.Succeeded,
                correlationId: doc.Id.ToString(),
                changedFields: new[] { "ReviewedByUserId", "ReviewedAt" },
                statePayload: JsonSerializer.Serialize(new { Visibility = doc.Visibility.ToString(), ReviewStatus = "Reviewed" }));
            await _dbContext.AdminAuditEvents.AddAsync(audit, cancellationToken);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return doc.ToAdminDto();
    }
}
