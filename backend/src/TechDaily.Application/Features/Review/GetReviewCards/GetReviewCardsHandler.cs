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
            .Include(c => c.Topic)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                (c.FrontMarkdown != null && c.FrontMarkdown.ToLower().Contains(search)) ||
                (c.BackMarkdown != null && c.BackMarkdown.ToLower().Contains(search)) ||
                (c.Topic != null && c.Topic.Title.ToLower().Contains(search)));
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

        var cards = await query
            .OrderBy(c => c.NextReviewDate)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ReviewCardDto
            {
                Id = c.Id,
                TopicId = c.TopicId,
                SourceType = c.SourceType,
                SourceHighlightId = c.SourceHighlightId,
                SourceQuizQuestionId = c.SourceQuizQuestionId,
                FrontMarkdown = !string.IsNullOrWhiteSpace(c.FrontMarkdown) ? c.FrontMarkdown : (c.Topic != null ? c.Topic.Title : string.Empty),
                BackMarkdown = !string.IsNullOrWhiteSpace(c.BackMarkdown) ? c.BackMarkdown : (c.Topic != null ? c.Topic.Summary : string.Empty),
                TopicTitle = c.Topic != null ? c.Topic.Title : (c.FrontMarkdown ?? string.Empty),
                Category = c.Topic != null ? c.Topic.Category : Category.FrontendWeb,
                Difficulty = c.Topic != null ? c.Topic.Difficulty : Difficulty.Senior,
                TopicSummary = c.Topic != null ? c.Topic.Summary : (c.BackMarkdown ?? string.Empty),
                TopicDeepDiveMarkdown = c.Topic != null ? c.Topic.DeepDiveMarkdown : (c.BackMarkdown ?? string.Empty),
                RepetitionCount = c.RepetitionCount,
                EaseFactor = c.EaseFactor,
                IntervalDays = c.IntervalDays,
                NextReviewDate = c.NextReviewDate,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);

        return new GetReviewCardsResponse(cards, filteredCount, page, pageSize, statistics);
    }
}
