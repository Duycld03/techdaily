using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.CurateSlice;

public record CurateSliceRequest(Guid BookId, int ChunkOrder);

public class CurateSliceResponse
{
    public ChunkSummaryDto Chunk { get; set; } = null!;
}

public class CurateSliceHandler : IUseCase<CurateSliceRequest, CurateSliceResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IAiMarkdownFormatter _aiFormatter;
    private readonly ILogger<CurateSliceHandler> _logger;

    public CurateSliceHandler(
        ITechDailyDbContext dbContext,
        IAiMarkdownFormatter aiFormatter,
        ILogger<CurateSliceHandler> logger)
    {
        _dbContext = dbContext;
        _aiFormatter = aiFormatter;
        _logger = logger;
    }

    public async Task<Result<CurateSliceResponse>> ExecuteAsync(
        CurateSliceRequest request,
        CancellationToken cancellationToken = default)
    {
        var chunk = await _dbContext.DocumentChunks
            .FirstOrDefaultAsync(c => c.DocumentBookId == request.BookId && c.ChunkOrder == request.ChunkOrder, cancellationToken);

        if (chunk == null)
        {
            return Error.NotFound;
        }

        if (!chunk.IsAiFormatted)
        {
            try
            {
                var aiResult = await _aiFormatter.FormatSliceAsync(
                    chunk.OriginalTextMarkdown,
                    chunk.ChapterTitle,
                    chunk.Language,
                    cancellationToken);

                if (aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                {
                    chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                    chunk.SummaryMarkdown = aiResult.Value.SummaryMarkdown;
                    chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
                    chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                    chunk.IsAiFormatted = true;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to curate slice on-demand for book {BookId}, order {Order}", request.BookId, request.ChunkOrder);
            }
        }

        var dto = new ChunkSummaryDto
        {
            Id = chunk.Id,
            ChunkOrder = chunk.ChunkOrder,
            ChapterTitle = chunk.ChapterTitle,
            SummaryMarkdown = chunk.SummaryMarkdown,
            OriginalTextMarkdown = chunk.OriginalTextMarkdown,
            KeyTakeaways = chunk.KeyTakeaways,
            MicroQuiz = chunk.MicroQuiz,
            EstimatedReadMinutes = chunk.EstimatedReadMinutes,
            IsAiFormatted = chunk.IsAiFormatted
        };

        return new CurateSliceResponse { Chunk = dto };
    }
}
