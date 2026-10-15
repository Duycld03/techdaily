using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public class AccountSourceQuota : BaseEntity
{
    public Guid AccountId { get; private set; }
    public long ChargedBytes { get; private set; }
    public long ReservedBytes { get; private set; }

    public long TotalAllocatedBytes => ChargedBytes + ReservedBytes;

    private AccountSourceQuota() { }

    public static AccountSourceQuota Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));
        }

        return new AccountSourceQuota
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ChargedBytes = 0,
            ReservedBytes = 0
        };
    }

    public void Reserve(long bytes, long perAccountLimit, long globalLimit, long currentGlobalAllocated)
    {
        if (bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Reservation bytes must be positive.");
        }

        if (perAccountLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(perAccountLimit), "Per-account quota limit must be positive.");
        }

        if (globalLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(globalLimit), "Global quota limit must be positive.");
        }

        if (perAccountLimit > globalLimit)
        {
            throw new InvalidOperationException("Per-account quota limit cannot exceed global quota limit.");
        }

        if (TotalAllocatedBytes + bytes > perAccountLimit)
        {
            throw new InvalidOperationException($"Account source quota exceeded. Current: {TotalAllocatedBytes} bytes, Requested: {bytes} bytes, Limit: {perAccountLimit} bytes.");
        }

        if (currentGlobalAllocated + bytes > globalLimit)
        {
            throw new InvalidOperationException($"Global source quota exceeded. Current: {currentGlobalAllocated} bytes, Requested: {bytes} bytes, Limit: {globalLimit} bytes.");
        }

        ReservedBytes += bytes;
        MarkUpdated();
    }

    public void CommitReservation(long reservedBytes, long actualChargedBytes)
    {
        if (reservedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedBytes), "Reserved bytes to commit cannot be negative.");
        }

        if (actualChargedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actualChargedBytes), "Actual charged bytes cannot be negative.");
        }

        ReservedBytes = Math.Max(0, ReservedBytes - reservedBytes);
        ChargedBytes += actualChargedBytes;
        MarkUpdated();
    }

    public void ReleaseReservation(long reservedBytes)
    {
        if (reservedBytes <= 0)
        {
            return;
        }

        ReservedBytes = Math.Max(0, ReservedBytes - reservedBytes);
        MarkUpdated();
    }

    public void ReleaseChargedBytes(long chargedBytes)
    {
        if (chargedBytes <= 0)
        {
            return;
        }

        ChargedBytes = Math.Max(0, ChargedBytes - chargedBytes);
        MarkUpdated();
    }
}
