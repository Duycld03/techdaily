using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IIngestionClaimService
{
    Task<IngestionAttempt?> TryClaimNextAttemptAsync(
        string claimToken,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> RenewClaimAsync(
        Guid attemptId,
        string claimToken,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);
}
