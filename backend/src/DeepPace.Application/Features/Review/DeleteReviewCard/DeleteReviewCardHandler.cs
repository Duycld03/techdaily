using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Review.DeleteReviewCard;

public record DeleteReviewCardRequest(Guid CardId, Guid UserId);

public class DeleteReviewCardResponse
{
    public bool Success { get; set; } = true;

    public DeleteReviewCardResponse() { }

    public DeleteReviewCardResponse(bool success)
    {
        Success = success;
    }
}

public class DeleteReviewCardHandler : IUseCase<DeleteReviewCardRequest, DeleteReviewCardResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public DeleteReviewCardHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DeleteReviewCardResponse>> ExecuteAsync(
        DeleteReviewCardRequest request,
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

        card.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteReviewCardResponse(true);
    }
}
