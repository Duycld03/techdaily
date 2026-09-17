using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.CurateSlice;

public record CurateSliceRequest(Guid BookId, int ChunkOrder);

public class CurateSliceResponse
{
    public ChunkSummaryDto Chunk { get; set; } = null!;
}

public class CurateSliceHandler : IUseCase<CurateSliceRequest, CurateSliceResponse>
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _sliceLocks = new();

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
            .Include(c => c.DocumentBook)
            .FirstOrDefaultAsync(c => c.DocumentBookId == request.BookId && c.ChunkOrder == request.ChunkOrder, cancellationToken);
        if (chunk == null)
        {
            return Error.NotFound;
        }

        if (!chunk.IsAiFormatted)
        {
            var lockKey = $"{request.BookId:N}:{request.ChunkOrder}";
            var semaphore = _sliceLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-checked locking: re-query chunk state under lock to see if a concurrent request already curated it
                var freshChunk = await _dbContext.DocumentChunks
                    .Include(c => c.DocumentBook)
                    .FirstOrDefaultAsync(c => c.DocumentBookId == request.BookId && c.ChunkOrder == request.ChunkOrder, cancellationToken);
                if (freshChunk == null)
                {
                    return Error.NotFound;
                }

                chunk = freshChunk;

                if (!chunk.IsAiFormatted)
                {
                    try
                    {
                        var aiResult = await _aiFormatter.FormatSliceAsync(
                            chunk.OriginalTextMarkdown,
                            chunk.ChapterTitle,
                            chunk.Language,
                            chunk.DocumentBook?.Category,
                            cancellationToken);

                        if (aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                        {
                            if (!string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                            {
                                chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                            }

                            chunk.SummaryMarkdown = !string.IsNullOrWhiteSpace(aiResult.Value.SummaryMarkdown)
                                ? aiResult.Value.SummaryMarkdown
                                : aiResult.Value.FormattedMarkdown;
                            chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
                            chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                            chunk.IsAiFormatted = true;

                            if (aiResult.Value.ScenarioDrill != null)
                            {
                                var drill = aiResult.Value.ScenarioDrill;

                                var existingQuestion = await _dbContext.InterviewQuestions
                                    .FirstOrDefaultAsync(q => q.DocumentChunkId == chunk.Id, cancellationToken);

                                if (existingQuestion != null)
                                {
                                    existingQuestion.QuestionText = drill.QuestionText;
                                    existingQuestion.Options = drill.Options;
                                    existingQuestion.CorrectOptionIndex = drill.CorrectOptionIndex;
                                    existingQuestion.ExplanationMarkdown = drill.ExplanationMarkdown;
                                    existingQuestion.ExpectedKeyPoints = drill.ExpectedKeyPoints;
                                    existingQuestion.ModelAnswerMarkdown = drill.ExplanationMarkdown;
                                    existingQuestion.Difficulty = TechDaily.Domain.Enums.Difficulty.Senior;
                                }
                                else
                                {
                                    var newQuestion = new TechDaily.Domain.Entities.InterviewQuestion
                                    {
                                        DocumentChunkId = chunk.Id,
                                        QuestionText = drill.QuestionText,
                                        Options = drill.Options,
                                        CorrectOptionIndex = drill.CorrectOptionIndex,
                                        ExplanationMarkdown = drill.ExplanationMarkdown,
                                        ExpectedKeyPoints = drill.ExpectedKeyPoints,
                                        ModelAnswerMarkdown = drill.ExplanationMarkdown,
                                        Difficulty = TechDaily.Domain.Enums.Difficulty.Senior
                                    };
                                    await _dbContext.InterviewQuestions.AddAsync(newQuestion, cancellationToken);
                                }
                            }

                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("AI curation returned failure for book {BookId}, order {Order}", request.BookId, request.ChunkOrder);
                            return new Error("CurateSlice.Failed", "AI formatting service failed to curate this chapter.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to curate slice on-demand for book {BookId}, order {Order}", request.BookId, request.ChunkOrder);
                        return new Error("CurateSlice.Exception", ex.Message);
                    }
                }
            }
            finally
            {
                semaphore.Release();
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
            EstimatedReadMinutes = chunk.EstimatedReadMinutes,
            IsAiFormatted = chunk.IsAiFormatted
        };

        return new CurateSliceResponse { Chunk = dto };
    }
}
