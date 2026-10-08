using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.GetReviewCards;

public record GetReviewCardsRequest(
    Guid UserId,
    string? Search = null,
    CardStatus? Status = null,
    CardSourceType? SourceType = null,
    int Page = 1,
    int PageSize = 20);

public class DeckStatisticsDto
{
    public int TotalCards { get; set; }
    public int LearningCount { get; set; }
    public int ReviewingCount { get; set; }
    public int MasteredCount { get; set; }

    // Compatibility aliases for frontend serialization
    public int LearningCards => LearningCount;
    public int ReviewingCards => ReviewingCount;
    public int MasteredCards => MasteredCount;

    public DeckStatisticsDto() { }

    public DeckStatisticsDto(int totalCards, int learningCount, int reviewingCount, int masteredCount)
    {
        TotalCards = totalCards;
        LearningCount = learningCount;
        ReviewingCount = reviewingCount;
        MasteredCount = masteredCount;
    }
}

public class GetReviewCardsResponse
{
    public List<ReviewCardDto> Cards { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 20));
    public DeckStatisticsDto Statistics { get; set; } = new();

    public GetReviewCardsResponse() { }

    public GetReviewCardsResponse(
        List<ReviewCardDto> cards,
        int totalCount,
        int page,
        int pageSize,
        DeckStatisticsDto statistics)
    {
        Cards = cards;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        Statistics = statistics;
    }
}

public class GetReviewCardsHandler : IUseCase<GetReviewCardsRequest, GetReviewCardsResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetReviewCardsHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetReviewCardsResponse>> ExecuteAsync(
        GetReviewCardsRequest request,
        CancellationToken cancellationToken = default)
    {
        var activeCardsQuery = _dbContext.SpacedRepetitionCards
            .Where(c => c.UserId == request.UserId && !c.IsDeleted);

        // Compute deck statistics across all active cards for user
        var totalCards = await activeCardsQuery.CountAsync(cancellationToken);
        var learningCount = await activeCardsQuery.CountAsync(c => c.Status == CardStatus.Learning, cancellationToken);
        var reviewingCount = await activeCardsQuery.CountAsync(c => c.Status == CardStatus.Reviewing, cancellationToken);
        var masteredCount = await activeCardsQuery.CountAsync(c => c.Status == CardStatus.Mastered, cancellationToken);

        var statistics = new DeckStatisticsDto(totalCards, learningCount, reviewingCount, masteredCount);

        // Apply filters
        var query = activeCardsQuery
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                (c.FrontMarkdown != null && c.FrontMarkdown.ToLower().Contains(search)) ||
                (c.BackMarkdown != null && c.BackMarkdown.ToLower().Contains(search)));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (request.SourceType.HasValue)
        {
            query = query.Where(c => c.SourceType == request.SourceType.Value);
        }

        var filteredCount = await query.CountAsync(cancellationToken);

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var rawCards = await query
            .OrderBy(c => c.NextReviewDate)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var cards = rawCards.Select(c => new ReviewCardDto
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

        return new GetReviewCardsResponse(cards, filteredCount, page, pageSize, statistics);
    }
}
