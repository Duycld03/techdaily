using DeepPace.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Learner.GetDocuments;

public record GetPublishedCatalogDocumentsRequest(
    string? Search = null,
    Guid? CategoryId = null,
    string? Language = null,
    SourceType? SourceType = null,
    int Page = 1,
    int PageSize = 12,
    Guid? UserId = null);

public record GetPublishedCatalogDocumentsResponse(
    IReadOnlyList<LearnerCatalogDocumentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class GetPublishedCatalogDocumentsHandler : IUseCase<GetPublishedCatalogDocumentsRequest, GetPublishedCatalogDocumentsResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetPublishedCatalogDocumentsHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetPublishedCatalogDocumentsResponse>> ExecuteAsync(
        GetPublishedCatalogDocumentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.CatalogDocuments
            .AsNoTracking()
            .Include(d => d.Category)
            .Where(d => d.Visibility == CatalogVisibility.Published && !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(search) || d.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            query = query.Where(d => d.Language == request.Language);
        }

        if (request.SourceType.HasValue)
        {
            query = query.Where(d => d.SourceType == request.SourceType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var docs = await query
            .OrderByDescending(d => d.PublishedAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map ExistingBookId for authenticated learner
        Dictionary<Guid, Guid> existingAdoptions = new();
        if (request.UserId.HasValue && docs.Count > 0)
        {
            var docIds = docs.Select(d => d.Id).ToList();
            var adoptedBooks = await _dbContext.DocumentBooks
                .AsNoTracking()
                .Where(b => b.CreatedByUserId == request.UserId.Value
                    && b.SourceCatalogDocumentId.HasValue
                    && docIds.Contains(b.SourceCatalogDocumentId.Value)
                    && !b.IsDeleted)
                .Select(b => new { CatalogId = b.SourceCatalogDocumentId!.Value, BookId = b.Id })
                .ToListAsync(cancellationToken);

            existingAdoptions = adoptedBooks.ToDictionary(a => a.CatalogId, a => a.BookId);
        }

        var dtos = docs.Select(d => d.ToLearnerDto(existingAdoptions.TryGetValue(d.Id, out var bId) ? bId : null)).ToList();

        return new GetPublishedCatalogDocumentsResponse(
            Items: dtos,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize);
    }
}
