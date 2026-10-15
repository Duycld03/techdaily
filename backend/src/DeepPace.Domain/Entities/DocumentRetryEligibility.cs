using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public record RetryEligibilityResult(
    bool IsEligible,
    string? IneligibleReasonCode,
    string? LocalizedMessage);

public static class DocumentRetryEligibility
{
    public const string ReasonTargetDeleted = "TargetDeleted";
    public const string ReasonLegacyUnowned = "LegacyUnowned";
    public const string ReasonAlreadyReady = "AlreadyReady";
    public const string ReasonPublishedCatalog = "PublishedCatalog";
    public const string ReasonActiveAttemptInProgress = "ActiveAttemptInProgress";
    public const string ReasonLearningActivityExists = "LearningActivityExists";
    public const string ReasonSourceUnavailable = "SourceUnavailable";
    public const string ReasonVersionMismatch = "VersionMismatch";
    public const string ReasonWrongStatus = "WrongStatus";

    public static RetryEligibilityResult EvaluatePersonalRetry(
        DocumentBook book,
        IngestionSource? source,
        bool hasActiveAttempt,
        bool hasLearningActivity,
        DateTimeOffset nowUtc,
        int? expectedControlVersion = null)
    {
        if (book.IsDeleted)
        {
            return new RetryEligibilityResult(false, ReasonTargetDeleted, "Document is deleted.");
        }

        if (!book.CreatedByUserId.HasValue || book.CreatedByUserId == Guid.Empty)
        {
            return new RetryEligibilityResult(false, ReasonLegacyUnowned, "Legacy unowned document cannot be retried.");
        }

        if (book.Status == ProcessingStatus.Ready)
        {
            return new RetryEligibilityResult(false, ReasonAlreadyReady, "Document is already ready and readable.");
        }

        if (book.Status != ProcessingStatus.Failed)
        {
            return new RetryEligibilityResult(false, ReasonWrongStatus, $"Document status '{book.Status}' is not eligible for retry.");
        }

        if (hasActiveAttempt)
        {
            return new RetryEligibilityResult(false, ReasonActiveAttemptInProgress, "An active ingestion attempt is currently in progress.");
        }

        if (hasLearningActivity)
        {
            return new RetryEligibilityResult(false, ReasonLearningActivityExists, "Document has existing learning references (highlights, cards, drills). Retry is blocked to preserve learner data.");
        }

        if (expectedControlVersion.HasValue && expectedControlVersion.Value != book.IngestionControlVersion)
        {
            return new RetryEligibilityResult(false, ReasonVersionMismatch, $"Version mismatch: expected {expectedControlVersion.Value}, actual {book.IngestionControlVersion}.");
        }

        if (source == null || !source.IsUsableForRetry(nowUtc))
        {
            return new RetryEligibilityResult(false, ReasonSourceUnavailable, "Retained source input is missing, expired, or unavailable. Source replacement is required.");
        }

        return new RetryEligibilityResult(true, null, null);
    }

    public static RetryEligibilityResult EvaluateCatalogRetry(
        CatalogDocument catalogDoc,
        IngestionSource? source,
        bool hasActiveAttempt,
        DateTimeOffset nowUtc,
        int? expectedConcurrencyVersion = null)
    {
        if (catalogDoc.IsDeleted)
        {
            return new RetryEligibilityResult(false, ReasonTargetDeleted, "Catalog document is deleted.");
        }

        if (catalogDoc.Visibility == CatalogVisibility.Published)
        {
            return new RetryEligibilityResult(false, ReasonPublishedCatalog, "Published catalog document source cannot be retried.");
        }

        if (catalogDoc.ProcessingStatus == ProcessingStatus.Ready)
        {
            return new RetryEligibilityResult(false, ReasonAlreadyReady, "Catalog document processing is already ready.");
        }

        if (catalogDoc.ProcessingStatus != ProcessingStatus.Failed)
        {
            return new RetryEligibilityResult(false, ReasonWrongStatus, $"Catalog document status '{catalogDoc.ProcessingStatus}' is not eligible for retry.");
        }

        if (hasActiveAttempt)
        {
            return new RetryEligibilityResult(false, ReasonActiveAttemptInProgress, "An active catalog ingestion attempt is currently in progress.");
        }

        if (expectedConcurrencyVersion.HasValue && expectedConcurrencyVersion.Value != catalogDoc.ConcurrencyVersion)
        {
            return new RetryEligibilityResult(false, ReasonVersionMismatch, $"Concurrency version mismatch: expected {expectedConcurrencyVersion.Value}, actual {catalogDoc.ConcurrencyVersion}.");
        }

        if (source == null || !source.IsUsableForRetry(nowUtc))
        {
            return new RetryEligibilityResult(false, ReasonSourceUnavailable, "Retained source input is missing, expired, or unavailable. Source replacement is required.");
        }

        return new RetryEligibilityResult(true, null, null);
    }

    public static RetryEligibilityResult EvaluatePersonalSourceReplacement(
        DocumentBook book,
        Guid requestingUserId,
        bool hasActiveAttempt,
        bool hasLearningActivity,
        int? expectedControlVersion = null)
    {
        if (book.IsDeleted)
        {
            return new RetryEligibilityResult(false, ReasonTargetDeleted, "Document is deleted.");
        }

        if (!book.CreatedByUserId.HasValue || book.CreatedByUserId.Value != requestingUserId)
        {
            return new RetryEligibilityResult(false, "UnauthorizedOwner", "Only the document owner can replace source and retry.");
        }

        if (book.Status == ProcessingStatus.Ready)
        {
            return new RetryEligibilityResult(false, ReasonAlreadyReady, "Cannot replace source for an already ready document.");
        }

        if (book.Status != ProcessingStatus.Failed)
        {
            return new RetryEligibilityResult(false, ReasonWrongStatus, $"Document status '{book.Status}' is not eligible for source replacement.");
        }

        if (hasActiveAttempt)
        {
            return new RetryEligibilityResult(false, ReasonActiveAttemptInProgress, "An active ingestion attempt is currently in progress.");
        }

        if (hasLearningActivity)
        {
            return new RetryEligibilityResult(false, ReasonLearningActivityExists, "Document has existing learning references. Source replacement is blocked.");
        }

        if (expectedControlVersion.HasValue && expectedControlVersion.Value != book.IngestionControlVersion)
        {
            return new RetryEligibilityResult(false, ReasonVersionMismatch, "Control version mismatch.");
        }

        return new RetryEligibilityResult(true, null, null);
    }
}
