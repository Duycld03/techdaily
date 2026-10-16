using Microsoft.EntityFrameworkCore;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Admin.Categories;

public class GetAdminCategoriesHandler : IUseCase<GetAdminCategoriesRequest, AdminPagedResult<AdminCategoryDetailDto>>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<GetAdminCategoriesRequest> _validator;

    public GetAdminCategoriesHandler(
        IDeepPaceDbContext dbContext,
        IValidator<GetAdminCategoriesRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<AdminPagedResult<AdminCategoryDetailDto>>> ExecuteAsync(
        GetAdminCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Category.ValidationFailed", validation.Errors.First().ErrorMessage);
        }

        var query = _dbContext.DocumentCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Slug.ToLower().Contains(search) ||
                c.NameEn.ToLower().Contains(search) ||
                c.NameVi.ToLower().Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDescriptorDto(
                c.Id,
                c.Slug,
                c.NameEn,
                c.NameVi,
                c.SortOrder,
                c.IsActive,
                c.ConcurrencyVersion))
            .ToListAsync(cancellationToken);

        var categoryIds = items.Select(c => c.Id).ToList();

        var bookCounts = await _dbContext.DocumentBooks.IgnoreQueryFilters()
            .Where(b => categoryIds.Contains(b.CategoryId))
            .GroupBy(b => b.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var catalogCounts = await _dbContext.CatalogDocuments.IgnoreQueryFilters()
            .Where(c => categoryIds.Contains(c.CategoryId))
            .GroupBy(c => c.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var quizCounts = await _dbContext.QuizQuestions.IgnoreQueryFilters()
            .Where(q => categoryIds.Contains(q.CategoryId))
            .GroupBy(q => q.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var insightCounts = await _dbContext.TechInsights.IgnoreQueryFilters()
            .Where(t => categoryIds.Contains(t.CategoryId))
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var detailItems = items.Select(c =>
        {
            var bCount = bookCounts.GetValueOrDefault(c.Id, 0);
            var catCount = catalogCounts.GetValueOrDefault(c.Id, 0);
            var qCount = quizCounts.GetValueOrDefault(c.Id, 0);
            var iCount = insightCounts.GetValueOrDefault(c.Id, 0);
            var total = bCount + catCount + qCount + iCount;
            return new AdminCategoryDetailDto(c, bCount, catCount, qCount, iCount, total, total == 0);
        }).ToList();

        return new AdminPagedResult<AdminCategoryDetailDto>(detailItems, totalCount, page, pageSize);
    }
}
