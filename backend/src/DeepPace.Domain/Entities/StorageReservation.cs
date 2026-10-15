using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public class StorageReservation : BaseEntity
{
    public string ReservationToken { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public long ReservedBytes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public bool IsReleased { get; private set; }
    public bool IsCommitted { get; private set; }

    private StorageReservation() { }

    public static StorageReservation Create(
        string reservationToken,
        Guid accountId,
        long reservedBytes,
        DateTimeOffset nowUtc,
        TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(reservationToken))
        {
            throw new ArgumentException("Reservation token cannot be empty.", nameof(reservationToken));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));
        }

        if (reservedBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedBytes), "Reserved bytes must be positive.");
        }

        var validLifetime = lifetime ?? TimeSpan.FromMinutes(30);

        return new StorageReservation
        {
            Id = Guid.NewGuid(),
            ReservationToken = reservationToken.Trim(),
            AccountId = accountId,
            ReservedBytes = reservedBytes,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(validLifetime),
            IsReleased = false,
            IsCommitted = false
        };
    }

    public void Commit(DateTimeOffset nowUtc)
    {
        if (IsReleased)
        {
            throw new InvalidOperationException("Cannot commit a released reservation.");
        }

        if (nowUtc > ExpiresAtUtc)
        {
            throw new InvalidOperationException("Cannot commit an expired reservation.");
        }

        IsCommitted = true;
        MarkUpdated();
    }

    public bool Release()
    {
        if (IsReleased || IsCommitted)
        {
            return false; // Idempotent, already processed
        }

        IsReleased = true;
        MarkUpdated();
        return true;
    }
}
