using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public class AdminGetDocumentInventoryHandler : IUseCase<AdminDocumentInventoryQuery, AdminPagedResult<AdminDocumentInventoryItemDto>>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetDocumentInventoryHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminPagedResult<AdminDocumentInventoryItemDto>>> ExecuteAsync(
        AdminDocumentInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

        var scope = query.Scope?.Trim().ToLowerInvariant();
        var includePersonal = string.IsNullOrEmpty(scope) || scope == "personal";
        var includeCatalog = string.IsNullOrEmpty(scope) || scope == "catalog";

        var now = DateTimeOffset.UtcNow;
        var items = new List<AdminDocumentInventoryItemDto>();

        // Query Personal Books
        if (includePersonal)
        {
            var personalQuery = _dbContext.DocumentBooks
                .AsNoTracking()
                .Include(b => b.CreatedByUser)
                .Where(b => !b.IsDeleted);

            if (query.OwnerId.HasValue)
            {
                personalQuery = personalQuery.Where(b => b.CreatedByUserId == query.OwnerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                personalQuery = personalQuery.Where(b => b.Title.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProcessingStatus>(query.Status, true, out var pStatus))
            {
                personalQuery = personalQuery.Where(b => b.Status == pStatus);
            }

            if (!string.IsNullOrWhiteSpace(query.SourceType) && Enum.TryParse<SourceType>(query.SourceType, true, out var sType))
            {
                personalQuery = personalQuery.Where(b => b.SourceType == sType);
            }

            if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<Category>(query.Category, true, out var cat))
            {
                personalQuery = personalQuery.Where(b => b.Category == cat);
            }

            var personalItems = await personalQuery.Select(b => new
            {
                b.Id,
                b.Title,
                b.CreatedByUserId,
                OwnerName = b.CreatedByUser != null ? b.CreatedByUser.Name : null,
                OwnerEmail = b.CreatedByUser != null ? b.CreatedByUser.Email : null,
                SourceType = b.SourceType.ToString(),
                Category = b.Category.ToString(),
                Language = b.DeclaredLanguage ?? "en",
                Status = b.Status.ToString(),
                b.CreatedAt,
                b.CurrentAttemptId
            }).ToListAsync(cancellationToken);

            // Fetch attempt/source availability for personal items
            var attemptIds = personalItems.Where(p => p.CurrentAttemptId.HasValue).Select(p => p.CurrentAttemptId!.Value).ToList();
            var sources = await _dbContext.IngestionAttempts
                .AsNoTracking()
                .Where(a => attemptIds.Contains(a.Id) && a.SourceId.HasValue)
                .Join(_dbContext.IngestionSources.AsNoTracking(),
                    a => a.SourceId!.Value,
                    s => s.Id,
                    (a, s) => new { AttemptId = a.Id, SourceAvailability = s.Availability.ToString() })
                .ToDictionaryAsync(x => x.AttemptId, x => x.SourceAvailability, cancellationToken);

            foreach (var p in personalItems)
            {
                var availability = p.CurrentAttemptId.HasValue && sources.TryGetValue(p.CurrentAttemptId.Value, out var avail)
                    ? avail
                    : "Missing";

                items.Add(new AdminDocumentInventoryItemDto(
                    p.Id,
                    "Personal",
                    p.Title,
                    p.CreatedByUserId,
                    p.OwnerName,
                    p.OwnerEmail,
                    p.SourceType,
                    p.Category,
                    p.Language,
                    p.Status,
                    availability,
                    p.CreatedAt,
                    now));
            }
        }

        // Query Catalog Documents
        if (includeCatalog && !query.OwnerId.HasValue) // Catalog docs are not owned by arbitrary learners
        {
            var catalogQuery = _dbContext.CatalogDocuments
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                catalogQuery = catalogQuery.Where(c => c.Title.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProcessingStatus>(query.Status, true, out var pStatus))
            {
                catalogQuery = catalogQuery.Where(c => c.ProcessingStatus == pStatus);
            }

            if (!string.IsNullOrWhiteSpace(query.SourceType) && Enum.TryParse<SourceType>(query.SourceType, true, out var sType))
            {
                catalogQuery = catalogQuery.Where(c => c.SourceType == sType);
            }

            if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<Category>(query.Category, true, out var cat))
            {
                catalogQuery = catalogQuery.Where(c => c.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(query.Language))
            {
                catalogQuery = catalogQuery.Where(c => c.Language == query.Language.Trim().ToLowerInvariant());
            }

            var catalogItems = await catalogQuery.Select(c => new
            {
                c.Id,
                c.Title,
                SourceType = c.SourceType.ToString(),
                Category = c.Category.ToString(),
                Language = c.Language,
                Status = c.ProcessingStatus.ToString(),
                c.CreatedAt,
                c.CurrentAttemptId
            }).ToListAsync(cancellationToken);

            var catAttemptIds = catalogItems.Where(c => c.CurrentAttemptId.HasValue).Select(c => c.CurrentAttemptId!.Value).ToList();
            var catSources = await _dbContext.IngestionAttempts
                .AsNoTracking()
                .Where(a => catAttemptIds.Contains(a.Id) && a.SourceId.HasValue)
                .Join(_dbContext.IngestionSources.AsNoTracking(),
                    a => a.SourceId!.Value,
                    s => s.Id,
                    (a, s) => new { AttemptId = a.Id, SourceAvailability = s.Availability.ToString() })
                .ToDictionaryAsync(x => x.AttemptId, x => x.SourceAvailability, cancellationToken);

            foreach (var c in catalogItems)
            {
                var availability = c.CurrentAttemptId.HasValue && catSources.TryGetValue(c.CurrentAttemptId.Value, out var avail)
                    ? avail
                    : "Missing";

                items.Add(new AdminDocumentInventoryItemDto(
                    c.Id,
                    "Catalog",
                    c.Title,
                    null,
                    "System Curated",
                    null,
                    c.SourceType,
                    c.Category,
                    c.Language,
                    c.Status,
                    availability,
                    c.CreatedAt,
                    now));
            }
        }

        // Stable ordering: CreatedAt descending, Id descending, Scope
        var sorted = items
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ThenBy(i => i.Scope)
            .ToList();

        var total = sorted.Count;
        var pagedItems = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new AdminPagedResult<AdminDocumentInventoryItemDto>(pagedItems, total, page, pageSize);
    }
}
