using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.DeleteBook;

public record DeleteBookRequest(Guid BookId);

public record DeleteBookResponse(bool Success);

public class DeleteBookHandler : IUseCase<DeleteBookRequest, DeleteBookResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public DeleteBookHandler(ITechDailyDbContext dbContext)
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

        // Soft delete book and all its chunks
        book.IsDeleted = true;
        book.UpdatedAt = DateTime.UtcNow;

        foreach (var chunk in book.Chunks)
        {
            chunk.IsDeleted = true;
            chunk.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteBookResponse(true);
    }
}
