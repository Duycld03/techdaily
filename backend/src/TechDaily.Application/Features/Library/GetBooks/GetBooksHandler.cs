using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.GetBooks;

public class GetBooksHandler : IUseCase<GetBooksRequest, GetBooksResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetBooksHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetBooksResponse>> ExecuteAsync(
        GetBooksRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 12;

        var query = _dbContext.DocumentBooks
            .AsNoTracking()
            .Where(b => b.IsPublished);

        if (request.Category.HasValue)
        {
            query = query.Where(b => b.Category == request.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(search) || b.Slug.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                SourceType = b.SourceType,
                Category = b.Category,
                AuthorOrSourceUrl = b.AuthorOrSourceUrl,
                TotalChunks = b.TotalChunks,
                IsPublished = b.IsPublished,
                IsFeatured = b.IsFeatured,
                Status = b.Status,
                ProgressPercentage = b.ProgressPercentage,
                StatusMessage = b.StatusMessage,
                ErrorMessage = b.ErrorMessage,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetBooksResponse(books, totalCount, page, pageSize, totalPages);
    }
}
