using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class IngestionAttempt : BaseEntity
{
    public IngestionTargetScope TargetScope { get; private set; }
    public Guid TargetDocumentId { get; private set; }
    public Guid? PredecessorAttemptId { get; private set; }
    public IngestionAttemptOrigin Origin { get; private set; }
    public Guid? InitiatorUserId { get; private set; }
    public string RequestId { get; private set; } = string.Empty;
    public string? CommandFingerprint { get; private set; }
    public Guid? SourceId { get; private set; }
    public long Generation { get; private set; }
    public IngestionStage Stage { get; private set; }
    public IngestionAttemptState State { get; private set; }
    public int ProgressPercent { get; private set; }
    public DateTimeOffset QueuedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public string? ClaimToken { get; private set; }
    public DateTimeOffset? ClaimExpiresAtUtc { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? SafeErrorGuidance { get; private set; }

    // Parameterless constructor for EF Core
    private IngestionAttempt() { }

    public static IngestionAttempt CreateQueued(
        IngestionTargetScope targetScope,
        Guid targetDocumentId,
        IngestionAttemptOrigin origin,
        string requestId,
        long generation,
        Guid? sourceId = null,
        Guid? predecessorAttemptId = null,
        Guid? initiatorUserId = null,
        string? commandFingerprint = null,
        DateTimeOffset? queuedAtUtc = null)
    {
        if (targetDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Target document ID cannot be empty.", nameof(targetDocumentId));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("Request ID cannot be empty.", nameof(requestId));
        }

        if (generation <= 0)
        {
            throw new ArgumentException("Generation must be positive.", nameof(generation));
        }

        var now = queuedAtUtc ?? DateTimeOffset.UtcNow;

        return new IngestionAttempt
        {
            Id = Guid.NewGuid(),
            TargetScope = targetScope,
            TargetDocumentId = targetDocumentId,
            Origin = origin,
            RequestId = requestId.Trim(),
            Generation = generation,
            SourceId = sourceId,
            PredecessorAttemptId = predecessorAttemptId,
            InitiatorUserId = initiatorUserId,
            CommandFingerprint = commandFingerprint?.Trim(),
            Stage = IngestionStage.Admission,
            State = IngestionAttemptState.Queued,
            ProgressPercent = 0,
            QueuedAtUtc = now
        };
    }

    public void Claim(string claimToken, DateTimeOffset claimExpiresAtUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(claimToken))
        {
            throw new ArgumentException("Claim token cannot be empty.", nameof(claimToken));
        }

        if (claimExpiresAtUtc <= nowUtc)
        {
            throw new ArgumentException("Claim expiry must be in the future.", nameof(claimExpiresAtUtc));
        }

        if (State != IngestionAttemptState.Queued && State != IngestionAttemptState.Interrupted)
        {
            throw new InvalidOperationException($"Cannot claim attempt in state '{State}'. Must be Queued or Interrupted.");
        }

        State = IngestionAttemptState.Running;
        ClaimToken = claimToken.Trim();
        ClaimExpiresAtUtc = claimExpiresAtUtc;
        StartedAtUtc ??= nowUtc;
        MarkUpdated();
    }

    public void RenewClaim(string claimToken, DateTimeOffset newExpiresAtUtc, DateTimeOffset nowUtc)
    {
        ValidateActiveClaim(claimToken, nowUtc);

        if (newExpiresAtUtc <= nowUtc)
        {
            throw new ArgumentException("New claim expiry must be in the future.", nameof(newExpiresAtUtc));
        }

        ClaimExpiresAtUtc = newExpiresAtUtc;
        MarkUpdated();
    }

    public void RecordProgress(string claimToken, IngestionStage stage, int progressPercent, DateTimeOffset nowUtc)
    {
        ValidateActiveClaim(claimToken, nowUtc);

        if (progressPercent < 0 || progressPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), "Progress percent must be between 0 and 100.");
        }

        if (progressPercent < ProgressPercent)
        {
            throw new InvalidOperationException($"Progress cannot decrease from {ProgressPercent}% to {progressPercent}%.");
        }

        Stage = stage;
        ProgressPercent = progressPercent;
        MarkUpdated();
    }

    public void CompleteSuccess(string claimToken, DateTimeOffset nowUtc)
    {
        ValidateActiveClaim(claimToken, nowUtc);

        State = IngestionAttemptState.Succeeded;
        Stage = IngestionStage.Finalization;
        ProgressPercent = 100;
        EndedAtUtc = nowUtc;
        ClaimToken = null;
        ClaimExpiresAtUtc = null;
        ErrorCode = null;
        SafeErrorGuidance = null;
        MarkUpdated();
    }

    public void Fail(string claimToken, string errorCode, string? safeGuidance, DateTimeOffset nowUtc)
    {
        ValidateActiveClaim(claimToken, nowUtc);

        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("Error code cannot be empty.", nameof(errorCode));
        }

        State = IngestionAttemptState.Failed;
        EndedAtUtc = nowUtc;
        ClaimToken = null;
        ClaimExpiresAtUtc = null;
        ErrorCode = errorCode.Trim();
        SafeErrorGuidance = safeGuidance?.Trim();
        MarkUpdated();
    }

    public void MarkInterrupted(DateTimeOffset nowUtc)
    {
        if (State != IngestionAttemptState.Running)
        {
            throw new InvalidOperationException($"Cannot mark attempt interrupted from state '{State}'.");
        }

        State = IngestionAttemptState.Interrupted;
        EndedAtUtc = nowUtc;
        ClaimToken = null;
        ClaimExpiresAtUtc = null;
        ErrorCode = "AttemptInterrupted";
        SafeErrorGuidance = "Worker claim expired before completion. Ingestion can be recovered or retried.";
        MarkUpdated();
    }

    public void Cancel(string reasonCode, DateTimeOffset nowUtc)
    {
        if (State == IngestionAttemptState.Succeeded)
        {
            throw new InvalidOperationException("Cannot cancel an attempt that has already succeeded.");
        }

        if (State == IngestionAttemptState.Cancelled)
        {
            return; // Idempotent
        }

        State = IngestionAttemptState.Cancelled;
        EndedAtUtc = nowUtc;
        ClaimToken = null;
        ClaimExpiresAtUtc = null;
        ErrorCode = string.IsNullOrWhiteSpace(reasonCode) ? "Cancelled" : reasonCode.Trim();
        MarkUpdated();
    }

    private void ValidateActiveClaim(string claimToken, DateTimeOffset nowUtc)
    {
        if (State != IngestionAttemptState.Running)
        {
            throw new InvalidOperationException($"Operation invalid: attempt is in state '{State}', expected Running.");
        }

        if (string.IsNullOrWhiteSpace(claimToken) || ClaimToken != claimToken.Trim())
        {
            throw new InvalidOperationException("Invalid or mismatched claim token.");
        }

        if (ClaimExpiresAtUtc.HasValue && nowUtc > ClaimExpiresAtUtc.Value)
        {
            throw new InvalidOperationException($"Claim has expired at {ClaimExpiresAtUtc.Value:O}. Current time: {nowUtc:O}.");
        }
    }
}
