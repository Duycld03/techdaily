using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Library.UpdateBook;

public class UpdateBookHandler : IUseCase<UpdateBookRequest, UpdateBookResponse>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<UpdateBookRequest> _validator;

    public UpdateBookHandler(
        IDeepPaceDbContext dbContext,
        IValidator<UpdateBookRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<UpdateBookResponse>> ExecuteAsync(
        UpdateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var book = await _dbContext.DocumentBooks
            .FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        if (book.CreatedByUserId == null || book.CreatedByUserId != request.UserId)
        {
            return Error.Custom("LIBRARY_FORBIDDEN", "You do not have permission to modify this book.");
        }

        book.Title = request.Title.Trim();
        book.AuthorOrSourceUrl = string.IsNullOrWhiteSpace(request.AuthorOrSourceUrl)
            ? null
            : request.AuthorOrSourceUrl.Trim();
        book.Category = request.Category;
        book.MarkUpdated();

        string? resolvedLanguage = null;
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            resolvedLanguage = request.Language.Trim().ToLowerInvariant();
            var chunks = await _dbContext.DocumentChunks
                .Where(c => c.DocumentBookId == book.Id)
                .ToListAsync(cancellationToken);

            foreach (var chunk in chunks)
            {
                chunk.Language = resolvedLanguage;
                chunk.MarkUpdated();
            }
        }
        else
        {
            resolvedLanguage = await _dbContext.DocumentChunks
                .Where(c => c.DocumentBookId == book.Id)
                .Select(c => c.Language)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new UpdateBookResponse
        {
            Book = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Slug = book.Slug,
                SourceType = book.SourceType,
                Category = book.Category,
                AuthorOrSourceUrl = book.AuthorOrSourceUrl,
                TotalChunks = book.TotalChunks,
                IsPublished = book.IsPublished,
                IsFeatured = book.IsFeatured,
                Status = book.Status,
                ProgressPercentage = book.ProgressPercentage,
                StatusMessage = book.StatusMessage,
                ErrorMessage = book.ErrorMessage,
                CreatedAt = book.CreatedAt,
                Language = resolvedLanguage ?? "en"
            }
        };

        return response;
    }
}
