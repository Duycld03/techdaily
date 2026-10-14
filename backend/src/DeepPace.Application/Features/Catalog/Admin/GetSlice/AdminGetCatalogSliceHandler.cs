using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Catalog.Admin.GetSlice;

public record AdminGetCatalogSliceRequest(Guid CatalogDocumentId, int SliceOrder);

public class AdminGetCatalogSliceHandler : IUseCase<AdminGetCatalogSliceRequest, AdminCatalogSliceDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetCatalogSliceHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCatalogSliceDto>> ExecuteAsync(
        AdminGetCatalogSliceRequest request,
        CancellationToken cancellationToken = default)
    {
        var slice = await _dbContext.CatalogSlices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CatalogDocumentId == request.CatalogDocumentId && s.SliceOrder == request.SliceOrder && !s.IsDeleted, cancellationToken);

        if (slice == null)
        {
            return Error.Custom("CatalogSlice.NotFound", "The requested catalog slice was not found.");
        }

        return slice.ToAdminSliceDto();
    }
}
