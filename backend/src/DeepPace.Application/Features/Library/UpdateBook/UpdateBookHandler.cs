using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Library.UpdateBook;

public class UpdateBookHandler(
    IDeepPaceDbContext dbContext,
    IValidator<UpdateBookRequest> validator,
    ICategoryAssignmentGuard categoryGuard) : IUseCase<UpdateBookRequest, UpdateBookResponse>
{
    public async Task<Result<UpdateBookResponse>> ExecuteAsync(UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Error.Custom(request.CategoryId == Guid.Empty ? "Category.ValidationFailed" : "Validation.Failed", validation.Errors.First().ErrorMessage);

        var candidate = await dbContext.DocumentBooks.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookId && !b.IsDeleted, cancellationToken);
        if (candidate == null) return Error.NotFound;
        if (candidate.CreatedByUserId != request.UserId || candidate.CreatedByUserId == null)
            return Error.Custom("LIBRARY_FORBIDDEN", "You do not have permission to modify this book.");

        if (candidate.CategoryId == request.CategoryId)
            return await categoryGuard.ExecuteForBookAsync(request.BookId, request.UserId, candidate.CategoryId,
                (book, category) => UpdateAsync(book, category, request, cancellationToken), cancellationToken);

        return await categoryGuard.ExecuteAsync<UpdateBookResponse>(request.CategoryId, async category =>
        {
            if (dbContext is not DbContext context)
                return Error.Custom("Category.Unavailable", "Document persistence is unavailable.");
            var book = context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                ? await dbContext.DocumentBooks.FromSqlInterpolated($"SELECT * FROM \"DocumentBooks\" WHERE \"Id\" = {request.BookId} FOR UPDATE").IgnoreAutoIncludes().FirstOrDefaultAsync(cancellationToken)
                : await dbContext.DocumentBooks.IgnoreAutoIncludes().FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);
            if (book == null) return Error.NotFound;
            await context.Entry(book).ReloadAsync(cancellationToken);
            if (book.IsDeleted) return Error.NotFound;
            if (book.CreatedByUserId != request.UserId || book.CreatedByUserId == null)
                return Error.Custom("LIBRARY_FORBIDDEN", "You do not have permission to modify this book.");
            if (book.CategoryId != candidate.CategoryId)
                return Error.Custom("Category.ConcurrencyConflict", "The book category changed. Reload before editing.");
            return await UpdateAsync(book, category, request, cancellationToken);
        }, cancellationToken);
    }

    private async Task<Result<UpdateBookResponse>> UpdateAsync(DocumentBook book, DocumentCategory category, UpdateBookRequest request, CancellationToken ct)
    {
        book.Title = request.Title.Trim();
        book.AuthorOrSourceUrl = string.IsNullOrWhiteSpace(request.AuthorOrSourceUrl) ? null : request.AuthorOrSourceUrl.Trim();
        book.CategoryId = category.Id;
        book.Category = category;
        book.MarkUpdated();
        var chunks = await dbContext.DocumentChunks.Where(c => c.DocumentBookId == book.Id).ToListAsync(ct);
        var language = string.IsNullOrWhiteSpace(request.Language) ? chunks.FirstOrDefault()?.Language ?? "en" : request.Language.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            foreach (var chunk in chunks)
            {
                chunk.Language = language;
                chunk.MarkUpdated();
            }
        }
        return new UpdateBookResponse
        {
            Book = new BookDto
            {
                Id = book.Id, Title = book.Title, Slug = book.Slug, SourceType = book.SourceType,
                CategoryId = book.CategoryId, Category = category.ToDescriptor(), AuthorOrSourceUrl = book.AuthorOrSourceUrl,
                TotalChunks = book.TotalChunks, IsPublished = book.IsPublished, IsFeatured = book.IsFeatured,
                Status = book.Status, ProgressPercentage = book.ProgressPercentage, StatusMessage = book.StatusMessage,
                ErrorMessage = book.ErrorMessage, CreatedAt = book.CreatedAt, Language = language
            }
        };
    }
}
