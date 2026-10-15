using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IIngestionQuotaService
{
    Task<StorageReservation> ReserveStorageAsync(
        Guid accountId,
        long bytes,
        CancellationToken cancellationToken = default);

    Task CommitReservationAsync(
        string reservationToken,
        long actualChargedBytes,
        CancellationToken cancellationToken = default);

    Task ReleaseReservationAsync(
        string reservationToken,
        CancellationToken cancellationToken = default);

    Task ReleaseChargedBytesAsync(
        Guid accountId,
        long bytes,
        CancellationToken cancellationToken = default);
}
