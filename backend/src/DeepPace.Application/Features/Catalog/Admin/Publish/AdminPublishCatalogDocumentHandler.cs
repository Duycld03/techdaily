using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;

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
            await _dbContext.SaveChangesAsync(cancellationToken);
            return doc.ToAdminDto();
        }
        catch (InvalidOperationException ex)
        {
            return Error.Custom("Catalog.PublicationFailed", ex.Message);
        }
    }
}
