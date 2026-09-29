using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.GetReviewAnalytics;

public record GetReviewAnalyticsRequest(Guid UserId, DateOnly? TargetDate = null);

public class SourceChannelRetentionDto
{
    public CardSourceType SourceType { get; set; }
    public int Total { get; set; }
    public int Learning { get; set; }
    public int Reviewing { get; set; }
    public int Mastered { get; set; }
    public decimal AverageEaseFactor { get; set; }
}

public class GetReviewAnalyticsResponse
{
    // At-risk segment
    public int OverdueCount { get; set; }
    public int LeechCount { get; set; }
    public int AtRiskCount { get; set; }

    // Maturity distribution
    public int TotalCards { get; set; }
    public int LearningCount { get; set; }
    public int ReviewingCount { get; set; }
    public int MasteredCount { get; set; }

    // Difficulty distribution (EaseFactor buckets over [1.30, 2.50])
    public int StrugglingCount { get; set; }
    public int DevelopingCount { get; set; }
    public int ComfortableCount { get; set; }

    // Source-channel retention breakdown
    public List<SourceChannelRetentionDto> SourceBreakdown { get; set; } = new();
}

public class GetReviewAnalyticsHandler : IUseCase<GetReviewAnalyticsRequest, GetReviewAnalyticsResponse>
{
    /// <summary>
    /// Cards at or below this ease factor have lapsed repeatedly (default is 2.50, floor is 1.30),
    /// so they are treated as leeches. Also the upper bound of the "struggling" difficulty bucket.
    /// </summary>
    public const decimal LeechEaseFactorThreshold = 1.70m;

    /// <summary>
    /// Lower bound of the "comfortable" difficulty bucket; "developing" is the open interval below it.
    /// </summary>
    public const decimal ComfortableEaseFactorThreshold = 2.10m;

    private readonly ITechDailyDbContext _dbContext;

    public GetReviewAnalyticsHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetReviewAnalyticsResponse>> ExecuteAsync(
        GetReviewAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Minimal projection of the user's active cards; aggregation is done in-memory to stay
        // portable across providers (SQLite tests and Npgsql production both compute decimal
        // averages identically). Personal SM-2 decks are small, so this is a single light query.
        var cards = await _dbContext.SpacedRepetitionCards
            .Where(c => c.UserId == request.UserId && !c.IsDeleted)
            .Select(c => new CardProjection(c.Status, c.EaseFactor, c.NextReviewDate, c.SourceType))
            .ToListAsync(cancellationToken);

        var response = new GetReviewAnalyticsResponse
        {
            TotalCards = cards.Count,
            LearningCount = cards.Count(c => c.Status == CardStatus.Learning),
            ReviewingCount = cards.Count(c => c.Status == CardStatus.Reviewing),
            MasteredCount = cards.Count(c => c.Status == CardStatus.Mastered),

            OverdueCount = cards.Count(c => c.NextReviewDate < today),
            LeechCount = cards.Count(c => c.EaseFactor <= LeechEaseFactorThreshold),
            AtRiskCount = cards.Count(c => c.NextReviewDate < today || c.EaseFactor <= LeechEaseFactorThreshold),

            StrugglingCount = cards.Count(c => c.EaseFactor <= LeechEaseFactorThreshold),
            DevelopingCount = cards.Count(c =>
                c.EaseFactor > LeechEaseFactorThreshold && c.EaseFactor < ComfortableEaseFactorThreshold),
            ComfortableCount = cards.Count(c => c.EaseFactor >= ComfortableEaseFactorThreshold),

            SourceBreakdown = cards
                .GroupBy(c => c.SourceType)
                .Select(g => new SourceChannelRetentionDto
                {
                    SourceType = g.Key,
                    Total = g.Count(),
                    Learning = g.Count(c => c.Status == CardStatus.Learning),
                    Reviewing = g.Count(c => c.Status == CardStatus.Reviewing),
                    Mastered = g.Count(c => c.Status == CardStatus.Mastered),
                    AverageEaseFactor = decimal.Round(g.Average(c => c.EaseFactor), 2),
                })
                .OrderBy(b => b.SourceType)
                .ToList(),
        };

        return response;
    }

    private sealed record CardProjection(
        CardStatus Status,
        decimal EaseFactor,
        DateOnly NextReviewDate,
        CardSourceType SourceType);
}
