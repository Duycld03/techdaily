using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.GetBooks;

public record GetBooksRequest(Category? Category = null, string? Search = null);

public class GetBooksResponse
{
    public List<BookDto> Books { get; set; } = new();
}

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
        var query = _dbContext.DocumentBooks.AsNoTracking();

        if (request.Category.HasValue)
        {
            query = query.Where(b => b.Category == request.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(search) || b.Slug.ToLower().Contains(search));
        }

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
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
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetBooksResponse { Books = books };
    }
}
