using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.GetBookSlice;

public record GetBookSliceRequest(Guid BookId, int ChunkOrder);

public class GetBookSliceResponse
{
    public ChunkSummaryDto Slice { get; set; } = null!;
}

public class GetBookSliceHandler : IUseCase<GetBookSliceRequest, GetBookSliceResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetBookSliceHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetBookSliceResponse>> ExecuteAsync(
        GetBookSliceRequest request,
        CancellationToken cancellationToken = default)
    {
        var chunk = await _dbContext.DocumentChunks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.DocumentBookId == request.BookId && c.ChunkOrder == request.ChunkOrder,
                cancellationToken);

        if (chunk == null)
        {
            return Error.NotFound;
        }

        var sliceDto = new ChunkSummaryDto
        {
            Id = chunk.Id,
            ChunkOrder = chunk.ChunkOrder,
            ChapterTitle = chunk.ChapterTitle,
            SummaryMarkdown = chunk.SummaryMarkdown,
            OriginalTextMarkdown = chunk.OriginalTextMarkdown,
            KeyTakeaways = chunk.KeyTakeaways,
            EstimatedReadMinutes = chunk.EstimatedReadMinutes,
            IsAiFormatted = chunk.IsAiFormatted
        };

        return new GetBookSliceResponse { Slice = sliceDto };
    }
}
