using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Catalog.Admin.Review;

public record AdminReviewCatalogRequest(Guid CatalogDocumentId, Guid AdminUserId);

public class AdminReviewCatalogHandler : IUseCase<AdminReviewCatalogRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminReviewCatalogHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminReviewCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .FirstOrDefaultAsync(d => d.Id == request.CatalogDocumentId, cancellationToken);

        if (doc == null || doc.IsDeleted)
        {
            return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        }

        doc.Review(request.AdminUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return doc.ToAdminDto();
    }
}
