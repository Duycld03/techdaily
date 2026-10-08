using DeepPace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Review.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Review.GetReviewDeck;

public record GetReviewDeckRequest(Guid UserId, DateOnly? TargetDate = null);

public class GetReviewDeckResponse
{
    public List<ReviewCardDto> DueCards { get; set; } = new();
    public int TotalCardsDue { get; set; }
}

public class GetReviewDeckHandler : IUseCase<GetReviewDeckRequest, GetReviewDeckResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetReviewDeckHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetReviewDeckResponse>> ExecuteAsync(
        GetReviewDeckRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var rawCards = await _dbContext.SpacedRepetitionCards
            .Where(c => c.UserId == request.UserId && c.NextReviewDate <= today)
            .OrderBy(c => c.NextReviewDate)
            .Select(c => new
            {
                c.Id,
                c.SourceType,
                c.SourceHighlightId,
                c.SourceQuizQuestionId,
                c.SourceDocumentChunkId,
                FrontMarkdown = c.FrontMarkdown ?? string.Empty,
                BackMarkdown = c.BackMarkdown ?? string.Empty,
                QuizTopic = c.SourceQuizQuestion != null ? c.SourceQuizQuestion.Topic : null,
                QuizCategory = c.SourceQuizQuestion != null ? (Category?)c.SourceQuizQuestion.Category : null,
                QuizLevel = c.SourceQuizQuestion != null ? (QuizLevel?)c.SourceQuizQuestion.Level : null,
                ChunkChapter = c.SourceDocumentChunk != null ? c.SourceDocumentChunk.ChapterTitle : null,
                ChunkCategory = c.SourceDocumentChunk != null ? (Category?)c.SourceDocumentChunk.DocumentBook.Category : null,
                HighlightChapter = c.SourceHighlight != null && c.SourceHighlight.DocumentChunk != null ? c.SourceHighlight.DocumentChunk.ChapterTitle : null,
                HighlightCategory = c.SourceHighlight != null && c.SourceHighlight.DocumentChunk != null ? (Category?)c.SourceHighlight.DocumentChunk.DocumentBook.Category : null,
                c.RepetitionCount,
                c.EaseFactor,
                c.IntervalDays,
                c.NextReviewDate,
                c.Status
            })
            .ToListAsync(cancellationToken);

        var dueCards = rawCards.Select(c => new ReviewCardDto
        {
            Id = c.Id,
            SourceType = c.SourceType,
            SourceHighlightId = c.SourceHighlightId,
            SourceQuizQuestionId = c.SourceQuizQuestionId,
            SourceDocumentChunkId = c.SourceDocumentChunkId,
            FrontMarkdown = c.FrontMarkdown,
            BackMarkdown = c.BackMarkdown,
            TopicTitle = !string.IsNullOrEmpty(c.QuizTopic) ? c.QuizTopic
                : (!string.IsNullOrEmpty(c.ChunkChapter) ? c.ChunkChapter
                : (!string.IsNullOrEmpty(c.HighlightChapter) ? c.HighlightChapter
                : c.FrontMarkdown)),
            Category = c.QuizCategory ?? c.ChunkCategory ?? c.HighlightCategory ?? Category.FrontendWeb,
            Difficulty = c.QuizLevel.HasValue
                ? (c.QuizLevel.Value == QuizLevel.Mastery ? Difficulty.Lead : (c.QuizLevel.Value == QuizLevel.Advanced ? Difficulty.Senior : Difficulty.Intermediate))
                : Difficulty.Senior,
            TopicSummary = c.BackMarkdown,
            TopicDeepDiveMarkdown = c.BackMarkdown,
            RepetitionCount = c.RepetitionCount,
            EaseFactor = c.EaseFactor,
            IntervalDays = c.IntervalDays,
            NextReviewDate = c.NextReviewDate,
            Status = c.Status
        }).ToList();

        return new GetReviewDeckResponse
        {
            DueCards = dueCards,
            TotalCardsDue = dueCards.Count
        };
    }
}
