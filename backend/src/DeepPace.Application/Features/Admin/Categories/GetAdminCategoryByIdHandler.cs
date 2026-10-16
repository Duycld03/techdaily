using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Admin.Categories;

public class GetAdminCategoryByIdHandler : IUseCase<Guid, AdminCategoryDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetAdminCategoryByIdHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCategoryDetailDto>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Error.Custom("Category.ValidationFailed", "Category ID cannot be empty.");
        }

        var category = await _dbContext.DocumentCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category == null)
        {
            return Error.Custom("Category.NotFound", $"Category with ID '{id}' was not found.");
        }

        var booksCount = await _dbContext.DocumentBooks.IgnoreQueryFilters()
            .CountAsync(b => b.CategoryId == id, cancellationToken);

        var catalogCount = await _dbContext.CatalogDocuments.IgnoreQueryFilters()
            .CountAsync(c => c.CategoryId == id, cancellationToken);

        var quizCount = await _dbContext.QuizQuestions.IgnoreQueryFilters()
            .CountAsync(q => q.CategoryId == id, cancellationToken);

        var insightCount = await _dbContext.TechInsights.IgnoreQueryFilters()
            .CountAsync(t => t.CategoryId == id, cancellationToken);

        var totalUsage = booksCount + catalogCount + quizCount + insightCount;
        var canDelete = totalUsage == 0;

        var descriptor = new CategoryDescriptorDto(
            category.Id,
            category.Slug,
            category.NameEn,
            category.NameVi,
            category.SortOrder,
            category.IsActive,
            category.ConcurrencyVersion);

        return new AdminCategoryDetailDto(
            Category: descriptor,
            BooksCount: booksCount,
            CatalogDocumentsCount: catalogCount,
            QuizQuestionsCount: quizCount,
            TechInsightsCount: insightCount,
            TotalUsageCount: totalUsage,
            CanDelete: canDelete);
    }
}
