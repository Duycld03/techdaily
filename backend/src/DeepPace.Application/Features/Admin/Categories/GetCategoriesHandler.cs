using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Admin.Categories;

public class GetCategoriesHandler : IUseCase<GetCategoriesRequest, IReadOnlyList<CategoryDescriptorDto>>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetCategoriesHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CategoryDescriptorDto>>> ExecuteAsync(
        GetCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.DocumentCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Slug)
            .Select(c => new CategoryDescriptorDto(
                c.Id,
                c.Slug,
                c.NameEn,
                c.NameVi,
                c.SortOrder,
                c.IsActive,
                c.ConcurrencyVersion))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CategoryDescriptorDto>>.Success(categories);
    }
}
