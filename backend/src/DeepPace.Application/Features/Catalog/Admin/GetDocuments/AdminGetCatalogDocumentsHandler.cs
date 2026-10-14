using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.GetDocuments;

public record AdminGetCatalogDocumentsRequest(
    string? Search = null,
    CatalogVisibility? Visibility = null,
    ProcessingStatus? Status = null,
    Category? Category = null,
    int Page = 1,
    int PageSize = 20);

public record AdminGetCatalogDocumentsResponse(
    IReadOnlyList<AdminCatalogDocumentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class AdminGetCatalogDocumentsHandler : IUseCase<AdminGetCatalogDocumentsRequest, AdminGetCatalogDocumentsResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetCatalogDocumentsHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminGetCatalogDocumentsResponse>> ExecuteAsync(
        AdminGetCatalogDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(search) || d.Description.ToLower().Contains(search));
        }

        if (request.Visibility.HasValue)
        {
            query = query.Where(d => d.Visibility == request.Visibility.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(d => d.ProcessingStatus == request.Status.Value);
        }

        if (request.Category.HasValue)
        {
            query = query.Where(d => d.Category == request.Category.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(d => d.ToAdminDto()).ToList();

        return new AdminGetCatalogDocumentsResponse(
            Items: dtos,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize);
    }
}
