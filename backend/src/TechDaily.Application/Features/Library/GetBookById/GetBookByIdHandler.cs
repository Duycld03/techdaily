using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.GetBookById;

public record GetBookByIdRequest(Guid BookId);

public class GetBookByIdResponse
{
    public BookDetailDto Book { get; set; } = null!;
}

public class GetBookByIdHandler : IUseCase<GetBookByIdRequest, GetBookByIdResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetBookByIdHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetBookByIdResponse>> ExecuteAsync(
        GetBookByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await _dbContext.DocumentBooks
            .Include(b => b.Chunks.OrderBy(c => c.ChunkOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        var detail = new BookDetailDto
        {
            Id = book.Id,
            Title = book.Title,
            Slug = book.Slug,
            SourceType = book.SourceType,
            Category = book.Category,
            AuthorOrSourceUrl = book.AuthorOrSourceUrl,
            TotalChunks = book.TotalChunks,
            Chunks = book.Chunks.Select(c => new ChunkSummaryDto
            {
                Id = c.Id,
                ChunkOrder = c.ChunkOrder,
                ChapterTitle = c.ChapterTitle,
                SummaryMarkdown = c.SummaryMarkdown,
                OriginalTextMarkdown = c.OriginalTextMarkdown,
                KeyTakeaways = c.KeyTakeaways,
                MicroQuiz = c.MicroQuiz,
                EstimatedReadMinutes = c.EstimatedReadMinutes
            }).ToList()
        };

        return new GetBookByIdResponse { Book = detail };
    }
}
