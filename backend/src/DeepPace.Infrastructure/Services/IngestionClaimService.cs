using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class IngestionClaimService : IIngestionClaimService
{
    private readonly IDeepPaceDbContext _dbContext;

    public IngestionClaimService(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IngestionAttempt?> TryClaimNextAttemptAsync(
        string claimToken,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = now.Add(leaseDuration);

        // 1. First priority: New queued attempts
        var queuedAttempt = await _dbContext.IngestionAttempts
            .Where(a => a.State == IngestionAttemptState.Queued)
            .OrderBy(a => a.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (queuedAttempt != null)
        {
            queuedAttempt.Claim(claimToken, expiry, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return queuedAttempt;
        }

        // 2. Second priority: Recover interrupted / expired claims
        var expiredClaim = await _dbContext.IngestionAttempts
            .Where(a => a.State == IngestionAttemptState.Running &&
                        a.ClaimExpiresAtUtc.HasValue &&
                        a.ClaimExpiresAtUtc.Value < now)
            .OrderBy(a => a.ClaimExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (expiredClaim != null)
        {
            // Transition through Interrupted then reclaim
            expiredClaim.MarkInterrupted(now);
            expiredClaim.Claim(claimToken, expiry, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return expiredClaim;
        }

        return null;
    }

    public async Task<bool> RenewClaimAsync(
        Guid attemptId,
        string claimToken,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.IngestionAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

        if (attempt == null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            attempt.RenewClaim(claimToken, now.Add(leaseDuration), now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
