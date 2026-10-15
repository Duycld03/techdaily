using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Library.DeleteBook;

public record DeleteBookRequest(Guid BookId, Guid UserId);

public record DeleteBookResponse(bool Success);

public class DeleteBookHandler : IUseCase<DeleteBookRequest, DeleteBookResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public DeleteBookHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DeleteBookResponse>> ExecuteAsync(
        DeleteBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await _dbContext.DocumentBooks
            .Include(b => b.Chunks)
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        if (book.CreatedByUserId == null || book.CreatedByUserId != request.UserId)
        {
            return Error.Custom("LIBRARY_FORBIDDEN", "You do not have permission to delete this book.");
        }

        var hasActiveAttempt = await _dbContext.IngestionAttempts
            .AnyAsync(a => a.TargetScope == DeepPace.Domain.Enums.IngestionTargetScope.Personal &&
                           a.TargetDocumentId == book.Id &&
                           (a.State == DeepPace.Domain.Enums.IngestionAttemptState.Queued || a.State == DeepPace.Domain.Enums.IngestionAttemptState.Running),
                cancellationToken);

        if (hasActiveAttempt)
        {
            return Error.Custom("Document.ActiveAttemptInProgress", "Cannot delete book while document ingestion is actively in progress.");
        }

        book.AdvanceControlVersion();
        // Soft delete book and all its chunks
        book.IsDeleted = true;
        book.UpdatedAt = DateTime.UtcNow;

        foreach (var chunk in book.Chunks)
        {
            chunk.IsDeleted = true;
            chunk.UpdatedAt = DateTime.UtcNow;
        }

        var pacers = await _dbContext.UserBookPacers
            .Where(p => p.DocumentBookId == request.BookId)
            .ToListAsync(cancellationToken);

        foreach (var pacer in pacers)
        {
            pacer.IsActive = false;
            pacer.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteBookResponse(true);
    }
}
