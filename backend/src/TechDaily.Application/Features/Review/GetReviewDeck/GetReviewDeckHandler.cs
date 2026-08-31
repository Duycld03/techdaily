using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Review.GetReviewDeck;

public record GetReviewDeckRequest(Guid UserId, DateOnly? TargetDate = null);

public class GetReviewDeckResponse
{
    public List<ReviewCardDto> DueCards { get; set; } = new();
    public int TotalCardsDue { get; set; }
}

public class GetReviewDeckHandler : IUseCase<GetReviewDeckRequest, GetReviewDeckResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetReviewDeckHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetReviewDeckResponse>> ExecuteAsync(
        GetReviewDeckRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var dueCards = await _dbContext.SpacedRepetitionCards
            .Include(c => c.Topic)
            .Where(c => c.UserId == request.UserId && c.NextReviewDate <= today)
            .OrderBy(c => c.NextReviewDate)
            .Select(c => new ReviewCardDto
            {
                Id = c.Id,
                TopicId = c.TopicId,
                TopicTitle = c.Topic.Title,
                Category = c.Topic.Category,
                Difficulty = c.Topic.Difficulty,
                TopicSummary = c.Topic.Summary,
                TopicDeepDiveMarkdown = c.Topic.DeepDiveMarkdown,
                RepetitionCount = c.RepetitionCount,
                EaseFactor = c.EaseFactor,
                IntervalDays = c.IntervalDays,
                NextReviewDate = c.NextReviewDate,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);

        return new GetReviewDeckResponse
        {
            DueCards = dueCards,
            TotalCardsDue = dueCards.Count
        };
    }
}
