using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Library.GetBooks;

public class GetBooksHandler : IUseCase<GetBooksRequest, GetBooksResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetBooksHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetBooksResponse>> ExecuteAsync(
        GetBooksRequest request,
        CancellationToken cancellationToken = default)
    {
        var language = request.Language?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(language) && language is not ("en" or "vi"))
        {
            return Error.Custom("Library.InvalidLanguage", "Language must be either 'en' or 'vi'.");
        }

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 12;

        var query = _dbContext.DocumentBooks
            .AsNoTracking()
            .Where(b => b.IsPublished && !b.IsDeleted);

        if (request.UserId.HasValue)
        {
            query = query.Where(b => b.CreatedByUserId == request.UserId.Value);
        }
        if (request.CategoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(search) || b.Slug.ToLower().Contains(search));
        }

        var booksQuery = query.Select(b => new
        {
            b.Id,
            b.Title,
            b.Slug,
            b.SourceType,
            b.CategoryId,
            b.Category,
            b.AuthorOrSourceUrl,
            b.TotalChunks,
            b.IsPublished,
            b.IsFeatured,
            b.Status,
            b.ProgressPercentage,
            b.StatusMessage,
            b.ErrorMessage,
            b.CreatedAt,
            Language = b.Chunks
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.ChunkOrder)
                .Select(c => c.Language)
                .FirstOrDefault() ?? b.DeclaredLanguage ?? "en"
        });

        if (!string.IsNullOrEmpty(language))
        {
            booksQuery = booksQuery.Where(b => b.Language == language);
        }

        var totalCount = await booksQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var rawBooks = await booksQuery
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var books = rawBooks.Select(b => new BookDto
        {
            Id = b.Id,
            Title = b.Title,
            Slug = b.Slug,
            SourceType = b.SourceType,
            CategoryId = b.CategoryId,
            Category = b.Category!.ToDescriptor(),
            AuthorOrSourceUrl = b.AuthorOrSourceUrl,
            TotalChunks = b.TotalChunks,
            IsPublished = b.IsPublished,
            IsFeatured = b.IsFeatured,
            Status = b.Status,
            ProgressPercentage = b.ProgressPercentage,
            StatusMessage = b.StatusMessage,
            ErrorMessage = b.ErrorMessage,
            CreatedAt = b.CreatedAt,
            Language = b.Language
        }).ToList();

        return new GetBooksResponse(books, totalCount, page, pageSize, totalPages);
    }
}
