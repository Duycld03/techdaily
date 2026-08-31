using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Notes.DeleteHighlight;

public record DeleteHighlightRequest(Guid HighlightId, Guid UserId);

public class DeleteHighlightResponse
{
    public bool Success { get; set; } = true;
}

public class DeleteHighlightHandler : IUseCase<DeleteHighlightRequest, DeleteHighlightResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public DeleteHighlightHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DeleteHighlightResponse>> ExecuteAsync(
        DeleteHighlightRequest request,
        CancellationToken cancellationToken = default)
    {
        var highlight = await _dbContext.UserHighlights
            .FirstOrDefaultAsync(h => h.Id == request.HighlightId && h.UserId == request.UserId, cancellationToken);

        if (highlight == null)
        {
            return Error.NotFound;
        }

        highlight.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeleteHighlightResponse { Success = true };
    }
}
