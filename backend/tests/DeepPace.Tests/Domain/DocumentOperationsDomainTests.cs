using FluentAssertions;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using Xunit;

namespace DeepPace.Tests.Domain;

public class DocumentOperationsDomainTests
{
    private readonly Guid _targetId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sourceId = Guid.NewGuid();
    private readonly DateTimeOffset _baseTime = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    #region Task 2.1: IngestionAttempt Invariants

    [Fact]
    public void IngestionAttempt_CreateQueued_ShouldInitializeWithValidState()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            sourceId: _sourceId,
            initiatorUserId: _userId,
            commandFingerprint: "sha256-abc",
            queuedAtUtc: _baseTime);

        attempt.TargetScope.Should().Be(IngestionTargetScope.Personal);
        attempt.TargetDocumentId.Should().Be(_targetId);
        attempt.Origin.Should().Be(IngestionAttemptOrigin.UserUpload);
        attempt.RequestId.Should().Be("req-101");
        attempt.Generation.Should().Be(1);
        attempt.Stage.Should().Be(IngestionStage.Admission);
        attempt.State.Should().Be(IngestionAttemptState.Queued);
        attempt.ProgressPercent.Should().Be(0);
        attempt.QueuedAtUtc.Should().Be(_baseTime);
        attempt.ClaimToken.Should().BeNull();
        attempt.ClaimExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public void IngestionAttempt_Claim_ShouldTransitionToRunning()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        var expiry = _baseTime.AddMinutes(10);
        attempt.Claim("token-123", expiry, _baseTime);

        attempt.State.Should().Be(IngestionAttemptState.Running);
        attempt.ClaimToken.Should().Be("token-123");
        attempt.ClaimExpiresAtUtc.Should().Be(expiry);
        attempt.StartedAtUtc.Should().Be(_baseTime);
    }

    [Fact]
    public void IngestionAttempt_RecordProgress_Monotonic_ShouldUpdateStageAndPercent()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        var expiry = _baseTime.AddMinutes(10);
        attempt.Claim("token-123", expiry, _baseTime);

        attempt.RecordProgress("token-123", IngestionStage.Extraction, 35, _baseTime.AddMinutes(1));
        attempt.ProgressPercent.Should().Be(35);
        attempt.Stage.Should().Be(IngestionStage.Extraction);

        // Advancing progress succeeds
        attempt.RecordProgress("token-123", IngestionStage.Slicing, 60, _baseTime.AddMinutes(2));
        attempt.ProgressPercent.Should().Be(60);
        attempt.Stage.Should().Be(IngestionStage.Slicing);

        // Regressing progress throws
        var act = () => attempt.RecordProgress("token-123", IngestionStage.Slicing, 50, _baseTime.AddMinutes(3));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Progress cannot decrease*");
    }

    [Fact]
    public void IngestionAttempt_ExpiredClaim_ShouldRejectProgressAndCompletion()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        var expiry = _baseTime.AddMinutes(10);
        attempt.Claim("token-123", expiry, _baseTime);

        // Current time is after expiry
        var afterExpiry = expiry.AddSeconds(1);

        var actProgress = () => attempt.RecordProgress("token-123", IngestionStage.Extraction, 40, afterExpiry);
        actProgress.Should().Throw<InvalidOperationException>()
            .WithMessage("*Claim has expired*");

        var actSuccess = () => attempt.CompleteSuccess("token-123", afterExpiry);
        actSuccess.Should().Throw<InvalidOperationException>()
            .WithMessage("*Claim has expired*");
    }

    [Fact]
    public void IngestionAttempt_CompleteSuccess_ShouldClearClaimAndSetFinalState()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        attempt.Claim("token-123", _baseTime.AddMinutes(10), _baseTime);
        attempt.CompleteSuccess("token-123", _baseTime.AddMinutes(5));

        attempt.State.Should().Be(IngestionAttemptState.Succeeded);
        attempt.Stage.Should().Be(IngestionStage.Finalization);
        attempt.ProgressPercent.Should().Be(100);
        attempt.EndedAtUtc.Should().Be(_baseTime.AddMinutes(5));
        attempt.ClaimToken.Should().BeNull();
        attempt.ClaimExpiresAtUtc.Should().BeNull();

        // Duplicate completion throws
        var duplicateAct = () => attempt.CompleteSuccess("token-123", _baseTime.AddMinutes(6));
        duplicateAct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IngestionAttempt_MismatchedToken_ShouldBeRejected()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        attempt.Claim("valid-token", _baseTime.AddMinutes(10), _baseTime);

        var act = () => attempt.CompleteSuccess("stale-or-wrong-token", _baseTime.AddMinutes(2));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mismatched claim token*");
    }

    [Fact]
    public void IngestionAttempt_MarkInterrupted_FromRunning_ShouldSucceed()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            _targetId,
            IngestionAttemptOrigin.UserUpload,
            "req-101",
            generation: 1,
            queuedAtUtc: _baseTime);

        attempt.Claim("token-1", _baseTime.AddMinutes(5), _baseTime);
        attempt.MarkInterrupted(_baseTime.AddMinutes(6));

        attempt.State.Should().Be(IngestionAttemptState.Interrupted);
        attempt.ErrorCode.Should().Be("AttemptInterrupted");
        attempt.ClaimToken.Should().BeNull();
        attempt.EndedAtUtc.Should().Be(_baseTime.AddMinutes(6));
    }

    #endregion

    #region Task 2.2: IngestionSource Retention Lifecycle

    [Fact]
    public void IngestionSource_Create_ShouldBoundRetentionToSevenDaysMax()
    {
        // Requesting 30 days should clamp to 7
        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf,
            "sources/2026/10/file-1.pdf",
            1024 * 1024,
            "sha256-validhash",
            _userId,
            "en",
            _baseTime,
            retentionDays: 30);

        source.RetentionExpiresAtUtc.Should().Be(_baseTime.AddDays(7));
        source.Availability.Should().Be(IngestionSourceAvailability.Available);
        source.DeletionState.Should().Be(SourceDeletionState.Retained);
    }

    [Fact]
    public void IngestionSource_RecordFailure_ShouldNotExtendDeadline()
    {
        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf,
            "sources/file.pdf",
            5000,
            "hash",
            _userId,
            "en",
            _baseTime,
            retentionDays: 7);

        var originalExpiry = source.RetentionExpiresAtUtc;

        // First failure on day 2
        var day2 = _baseTime.AddDays(2);
        source.RecordFailure(day2);
        source.FirstFailureAtUtc.Should().Be(day2);
        source.RetentionExpiresAtUtc.Should().Be(originalExpiry); // Does not extend

        // Second failure (e.g. after retry on day 4)
        var day4 = _baseTime.AddDays(4);
        source.RecordFailure(day4);
        source.FirstFailureAtUtc.Should().Be(day2); // Unchanged
        source.RetentionExpiresAtUtc.Should().Be(originalExpiry); // Still unchanged!
    }

    [Fact]
    public void IngestionSource_RevokeOnSuccess_ShouldRevokeImmediately()
    {
        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf,
            "sources/file.pdf",
            5000,
            "hash",
            _userId,
            "en",
            _baseTime);

        source.RevokeOnSuccess(_baseTime.AddMinutes(30));

        source.Availability.Should().Be(IngestionSourceAvailability.Revoked);
        source.DeletionState.Should().Be(SourceDeletionState.PendingDeletion);
        source.IsUsableForRetry(_baseTime.AddMinutes(31)).Should().BeFalse();
    }

    [Fact]
    public void IngestionSource_Expiry_ShouldTransitionToPendingDeletion()
    {
        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf,
            "sources/file.pdf",
            5000,
            "hash",
            _userId,
            "en",
            _baseTime,
            retentionDays: 7);

        // Before expiry
        source.IsUsableForRetry(_baseTime.AddDays(6)).Should().BeTrue();

        // At/after expiry
        source.IsUsableForRetry(_baseTime.AddDays(7).AddSeconds(1)).Should().BeFalse();
        source.Availability.Should().Be(IngestionSourceAvailability.Expired);
        source.DeletionState.Should().Be(SourceDeletionState.PendingDeletion);
    }

    [Fact]
    public void IngestionSource_DeletionStates_DeletionPendingCannotMasqueradeAsDeleted()
    {
        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf,
            "sources/file.pdf",
            5000,
            "hash",
            _userId,
            "en",
            _baseTime);

        source.RevokeOnSuccess(_baseTime.AddMinutes(10));
        source.DeletionState.Should().Be(SourceDeletionState.PendingDeletion);
        source.DeletionState.Should().NotBe(SourceDeletionState.Deleted);

        // Physical deletion confirmed
        source.MarkDeleted();
        source.DeletionState.Should().Be(SourceDeletionState.Deleted);
        source.Availability.Should().Be(IngestionSourceAvailability.Deleted);
    }

    #endregion

    #region Task 2.3: Target Control Versions and Attempt Tracking

    [Fact]
    public void DocumentBook_ControlVersion_AdvancesOnAttemptAndLanguageCorrection_StableOnProgress()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Distributed Systems",
            CreatedByUserId = _userId,
            Status = ProcessingStatus.Pending
        };

        book.IngestionControlVersion.Should().Be(1);
        book.CurrentAttemptGeneration.Should().Be(0);

        // Assigning attempt advances generation and control version
        var attemptId = Guid.NewGuid();
        book.AssignAttempt(attemptId);
        book.CurrentAttemptId.Should().Be(attemptId);
        book.CurrentAttemptGeneration.Should().Be(1);
        book.IngestionControlVersion.Should().Be(2);

        // Progress heartbeats do NOT change IngestionControlVersion
        book.UpdateHeartbeatProgress(45, "Extracting text...");
        book.IngestionControlVersion.Should().Be(2); // Stable!
        book.ProgressPercentage.Should().Be(45);

        // Explicit language correction advances version
        book.SetDeclaredLanguage("vi");
        book.DeclaredLanguage.Should().Be("vi");
        book.IngestionControlVersion.Should().Be(3);
    }

    [Fact]
    public void CatalogDocument_ConcurrencyVersion_AdvancesOnAttemptAssignment()
    {
        var doc = new CatalogDocument
        {
            CategoryId = CategoryDefaults.DatabaseStorageId,
            Id = Guid.NewGuid(),
            Title = "Postgres Internals"
        };

        doc.ConcurrencyVersion.Should().Be(1);
        doc.ProcessingGeneration.Should().Be(0);

        var attemptId = Guid.NewGuid();
        doc.AssignAttempt(attemptId);

        doc.CurrentAttemptId.Should().Be(attemptId);
        doc.ProcessingGeneration.Should().Be(1);
        doc.ConcurrencyVersion.Should().Be(2);
    }

    #endregion

    #region Task 2.4: Retry and Source Replacement Eligibility

    [Fact]
    public void DocumentRetryEligibility_Personal_ReadyBook_ShouldBeIneligible()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Id = Guid.NewGuid(),
            Title = "Go Concurrency",
            CreatedByUserId = _userId,
            Status = ProcessingStatus.Ready
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/go.pdf", 2048, "hash", _userId, "en", _baseTime);

        var result = DocumentRetryEligibility.EvaluatePersonalRetry(
            book, source, hasActiveAttempt: false, hasLearningActivity: false, _baseTime);

        result.IsEligible.Should().BeFalse();
        result.IneligibleReasonCode.Should().Be(DocumentRetryEligibility.ReasonAlreadyReady);
    }

    [Fact]
    public void DocumentRetryEligibility_Personal_LegacyUnowned_ShouldBeIneligible()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.EngineeringCraftId,
            Id = Guid.NewGuid(),
            Title = "Legacy Book",
            CreatedByUserId = null, // Unowned
            Status = ProcessingStatus.Failed
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/legacy.pdf", 2048, "hash", _userId, "en", _baseTime);

        var result = DocumentRetryEligibility.EvaluatePersonalRetry(
            book, source, hasActiveAttempt: false, hasLearningActivity: false, _baseTime);

        result.IsEligible.Should().BeFalse();
        result.IneligibleReasonCode.Should().Be(DocumentRetryEligibility.ReasonLegacyUnowned);
    }

    [Fact]
    public void DocumentRetryEligibility_Personal_WithLearningActivity_ShouldBeIneligible()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.EngineeringCraftId,
            Id = Guid.NewGuid(),
            Title = "Clean Architecture",
            CreatedByUserId = _userId,
            Status = ProcessingStatus.Failed
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/clean.pdf", 2048, "hash", _userId, "en", _baseTime);

        var result = DocumentRetryEligibility.EvaluatePersonalRetry(
            book, source, hasActiveAttempt: false, hasLearningActivity: true, _baseTime);

        result.IsEligible.Should().BeFalse();
        result.IneligibleReasonCode.Should().Be(DocumentRetryEligibility.ReasonLearningActivityExists);
    }

    [Fact]
    public void DocumentRetryEligibility_Catalog_Published_ShouldBeIneligible()
    {
        var doc = new CatalogDocument
        {
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Id = Guid.NewGuid(),
            Title = "Docker Deep Dive",
            Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Failed
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/docker.pdf", 2048, "hash", _userId, "en", _baseTime);

        var result = DocumentRetryEligibility.EvaluateCatalogRetry(
            doc, source, hasActiveAttempt: false, _baseTime);

        result.IsEligible.Should().BeFalse();
        result.IneligibleReasonCode.Should().Be(DocumentRetryEligibility.ReasonPublishedCatalog);
    }

    [Fact]
    public void DocumentRetryEligibility_Personal_EligibleFailed_ShouldBeAllowed()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Id = Guid.NewGuid(),
            Title = "Kubernetes in Action",
            CreatedByUserId = _userId,
            Status = ProcessingStatus.Failed,
            IngestionControlVersion = 2
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/k8s.pdf", 2048, "hash", _userId, "en", _baseTime);

        var result = DocumentRetryEligibility.EvaluatePersonalRetry(
            book, source, hasActiveAttempt: false, hasLearningActivity: false, _baseTime, expectedControlVersion: 2);

        result.IsEligible.Should().BeTrue();
        result.IneligibleReasonCode.Should().BeNull();
    }

    #endregion

    #region Task 2.5: Quota & Storage Reservation Invariants

    [Fact]
    public void AccountSourceQuota_Reserve_ShouldEnforceLimits()
    {
        var quota = AccountSourceQuota.Create(_userId);

        long perAccountLimit = 1000;
        long globalLimit = 5000;
        long currentGlobal = 2000;

        // Normal reservation within limits succeeds
        quota.Reserve(400, perAccountLimit, globalLimit, currentGlobal);
        quota.ReservedBytes.Should().Be(400);
        quota.ChargedBytes.Should().Be(0);
        quota.TotalAllocatedBytes.Should().Be(400);

        // Exceeding per-account limit throws
        var actPerAccount = () => quota.Reserve(700, perAccountLimit, globalLimit, currentGlobal + 400);
        actPerAccount.Should().Throw<InvalidOperationException>()
            .WithMessage("*Account source quota exceeded*");

        // Exceeding global limit throws
        var actGlobal = () => quota.Reserve(500, 2000, globalLimit, 4800);
        actGlobal.Should().Throw<InvalidOperationException>()
            .WithMessage("*Global source quota exceeded*");
    }

    [Fact]
    public void AccountSourceQuota_CommitAndRelease_ShouldUpdateBalancesCorrectly()
    {
        var quota = AccountSourceQuota.Create(_userId);

        quota.Reserve(500, 1000, 5000, 0);

        // Actual file was only 450 bytes (unknown-length download scenario)
        quota.CommitReservation(reservedBytes: 500, actualChargedBytes: 450);

        quota.ReservedBytes.Should().Be(0);
        quota.ChargedBytes.Should().Be(450);
        quota.TotalAllocatedBytes.Should().Be(450);

        // Release on physical deletion
        quota.ReleaseChargedBytes(450);
        quota.ChargedBytes.Should().Be(0);
        quota.TotalAllocatedBytes.Should().Be(0);
    }

    [Fact]
    public void StorageReservation_DuplicateRelease_ShouldBeIdempotent()
    {
        var res = StorageReservation.Create("res-token-1", _userId, 1024, _baseTime);

        res.IsReleased.Should().BeFalse();
        res.IsCommitted.Should().BeFalse();

        // First release returns true
        res.Release().Should().BeTrue();
        res.IsReleased.Should().BeTrue();

        // Duplicate release returns false without throwing
        res.Release().Should().BeFalse();
    }

    #endregion
}
