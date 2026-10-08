using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Review.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Review.ResetReviewCardProgress;

public record ResetReviewCardProgressRequest(
    Guid CardId,
    Guid UserId,
    DateOnly? ResetDate = null);

public class ResetReviewCardProgressResponse
{
    public ReviewCardDto Card { get; set; } = null!;

    public ResetReviewCardProgressResponse() { }

    public ResetReviewCardProgressResponse(ReviewCardDto card)
    {
        Card = card;
    }
}

public class ResetReviewCardProgressHandler : IUseCase<ResetReviewCardProgressRequest, ResetReviewCardProgressResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public ResetReviewCardProgressHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ResetReviewCardProgressResponse>> ExecuteAsync(
        ResetReviewCardProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var card = await _dbContext.SpacedRepetitionCards
            .FirstOrDefaultAsync(
                c => c.Id == request.CardId && c.UserId == request.UserId && !c.IsDeleted,
                cancellationToken);

        if (card == null)
        {
            return Error.NotFound;
        }

        card.ResetProgression(request.ResetDate);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ResetReviewCardProgressResponse(ReviewCardDto.FromEntity(card));
    }
}
