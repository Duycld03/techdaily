using DeepPace.Domain.Entities;

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
            .AsNoTracking()
            .WithReviewSources()
            .Where(c => c.UserId == request.UserId && !c.IsDeleted && c.NextReviewDate <= today)
            .OrderBy(c => c.NextReviewDate)
            .ToListAsync(cancellationToken);

        var dueCards = rawCards.Select(ReviewCardDto.FromEntity).ToList();

        return new GetReviewDeckResponse
        {
            DueCards = dueCards,
            TotalCardsDue = dueCards.Count
        };
    }
}
