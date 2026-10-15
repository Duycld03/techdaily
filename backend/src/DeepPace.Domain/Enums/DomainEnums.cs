namespace DeepPace.Domain.Enums;

public enum Category
{
    FrontendWeb = 0,
    BackendRuntime = 1,
    DatabaseStorage = 2,
    SystemDesign = 3,
    EngineeringCraft = 4,
    MentalModels = 5,
    HabitsProductivity = 6
}

public enum Difficulty
{
    Intermediate,
    Senior,
    Lead
}

public enum SourceType
{
    PdfBook,
    MarkdownSeries,
    WebDocUrl
}

public enum DrillStatus
{
    Pending,
    Submitted,
    Reviewed,
    Skipped
}

public enum CardStatus
{
    Learning,
    Reviewing,
    Mastered
}

public enum QuizLevel
{
    Foundation = 0,
    Applied = 1,
    Advanced = 2,
    Mastery = 3,

    // Backward compatibility aliases
    Fresher = 0,
    Junior = 1,
    Middle = 2,
    Senior = 3
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Ready,
    Failed
}

public enum CatalogVisibility
{
    Draft = 0,
    Published = 1,
    Hidden = 2
}

public enum AccountStatus
{
    Active = 1,
    Suspended = 2
}

public enum AdminAuditOutcome
{
    Succeeded = 1,
    Accepted = 2,
    Rejected = 3,
    Failed = 4
}

public static class AdminAuditActions
{
    public const string UserSuspend = "User.Suspend";
    public const string UserReactivate = "User.Reactivate";
    public const string UserRevokeSessions = "User.RevokeSessions";
    public const string UserAssignRole = "User.AssignRole";

    public const string CatalogUploadPdf = "Catalog.UploadPdf";
    public const string CatalogImportUrl = "Catalog.ImportUrl";
    public const string CatalogUpdateMetadata = "Catalog.UpdateMetadata";
    public const string CatalogPrepare = "Catalog.Prepare";
    public const string CatalogReview = "Catalog.Review";
    public const string CatalogPublish = "Catalog.Publish";
    public const string CatalogHide = "Catalog.Hide";
    public const string CatalogDispatchFailed = "Catalog.DispatchFailed";

    public const string DocumentIngestionRetryRequested = "Document.IngestionRetryRequested";
    public const string DocumentIngestionRetryCompleted = "Document.IngestionRetryCompleted";
    public const string DocumentSourceReplaced = "Document.SourceReplaced";
}

public enum IngestionAttemptOrigin
{
    UserUpload = 1,
    UserUrl = 2,
    UserMarkdown = 3,
    AdminCatalogUpload = 4,
    AdminCatalogUrl = 5,
    AdminRetry = 6,
    UserReplacement = 7,
    AdminCatalogReplacement = 8
}

public enum IngestionStage
{
    Admission = 1,
    Fetch = 2,
    Extraction = 3,
    Slicing = 4,
    InitialFormatting = 5,
    Enrichment = 6,
    Finalization = 7
}

public enum IngestionAttemptState
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    Interrupted = 5,
    Cancelled = 6
}

public enum IngestionTargetScope
{
    Personal = 1,
    Catalog = 2
}

public enum IngestionSourceKind
{
    Pdf = 1,
    WebArticle = 2,
    Markdown = 3
}

public enum IngestionSourceAvailability
{
    Available = 1,
    Missing = 2,
    Expired = 3,
    Revoked = 4,
    Deleted = 5
}

public enum SourceDeletionState
{
    Retained = 1,
    PendingDeletion = 2,
    Deleted = 3,
    DeletionFailed = 4
}
