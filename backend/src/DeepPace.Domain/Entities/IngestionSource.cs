using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class IngestionSource : BaseEntity
{
    public IngestionSourceKind SourceKind { get; private set; }
    public string StorageIdentity { get; private set; } = string.Empty;
    public long ByteLength { get; private set; }
    public string IntegritySha256 { get; private set; } = string.Empty;
    public Guid QuotaAccountId { get; private set; }
    public string DeclaredLanguage { get; private set; } = "en";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? FirstFailureAtUtc { get; private set; }
    public DateTimeOffset RetentionExpiresAtUtc { get; private set; }
    public IngestionSourceAvailability Availability { get; private set; }
    public SourceDeletionState DeletionState { get; private set; }

    private IngestionSource() { }

    public static IngestionSource Create(
        IngestionSourceKind kind,
        string storageIdentity,
        long byteLength,
        string integritySha256,
        Guid quotaAccountId,
        string language,
        DateTimeOffset nowUtc,
        int retentionDays = 7)
    {
        if (string.IsNullOrWhiteSpace(storageIdentity))
        {
            throw new ArgumentException("Storage identity cannot be empty.", nameof(storageIdentity));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Byte length must be positive.");
        }

        if (string.IsNullOrWhiteSpace(integritySha256))
        {
            throw new ArgumentException("Integrity hash cannot be empty.", nameof(integritySha256));
        }

        if (quotaAccountId == Guid.Empty)
        {
            throw new ArgumentException("Quota account ID cannot be empty.", nameof(quotaAccountId));
        }

        var clampedRetentionDays = Math.Clamp(retentionDays, 1, 7);
        var retentionExpiry = nowUtc.AddDays(clampedRetentionDays);

        return new IngestionSource
        {
            Id = Guid.NewGuid(),
            SourceKind = kind,
            StorageIdentity = storageIdentity.Trim(),
            ByteLength = byteLength,
            IntegritySha256 = integritySha256.Trim(),
            QuotaAccountId = quotaAccountId,
            DeclaredLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant(),
            CreatedAtUtc = nowUtc,
            RetentionExpiresAtUtc = retentionExpiry,
            Availability = IngestionSourceAvailability.Available,
            DeletionState = SourceDeletionState.Retained
        };
    }

    public void RecordFailure(DateTimeOffset nowUtc)
    {
        if (!FirstFailureAtUtc.HasValue)
        {
            FirstFailureAtUtc = nowUtc;
            var failureBasedExpiry = nowUtc.AddDays(7);
            if (failureBasedExpiry < RetentionExpiresAtUtc)
            {
                RetentionExpiresAtUtc = failureBasedExpiry;
            }
        }

        CheckAndApplyExpiry(nowUtc);
        MarkUpdated();
    }

    public void RevokeOnSuccess(DateTimeOffset nowUtc)
    {
        Availability = IngestionSourceAvailability.Revoked;
        DeletionState = SourceDeletionState.PendingDeletion;
        MarkUpdated();
    }

    public void CheckAndApplyExpiry(DateTimeOffset nowUtc)
    {
        if (nowUtc >= RetentionExpiresAtUtc && Availability == IngestionSourceAvailability.Available)
        {
            Availability = IngestionSourceAvailability.Expired;
            DeletionState = SourceDeletionState.PendingDeletion;
            MarkUpdated();
        }
    }

    public void MarkDeleted()
    {
        DeletionState = SourceDeletionState.Deleted;
        Availability = IngestionSourceAvailability.Deleted;
        MarkUpdated();
    }

    public void MarkDeletionFailed()
    {
        DeletionState = SourceDeletionState.DeletionFailed;
        MarkUpdated();
    }

    public bool IsUsableForRetry(DateTimeOffset nowUtc)
    {
        CheckAndApplyExpiry(nowUtc);
        return Availability == IngestionSourceAvailability.Available &&
               DeletionState == SourceDeletionState.Retained;
    }
}
