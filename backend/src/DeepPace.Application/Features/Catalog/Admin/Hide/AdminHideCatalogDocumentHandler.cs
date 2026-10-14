using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Catalog.Admin.Hide;

public record AdminHideCatalogDocumentRequest(
    Guid Id,
    Guid AdminUserId,
    int ExpectedConcurrencyVersion);

public class AdminHideCatalogDocumentHandler : IUseCase<AdminHideCatalogDocumentRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminHideCatalogDocumentHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminHideCatalogDocumentRequest request,
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

        doc.Hide(request.AdminUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return doc.ToAdminDto();
    }
}
