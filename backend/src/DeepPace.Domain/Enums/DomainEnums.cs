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

    public const string CatalogUploadPdf = "Catalog.UploadPdf";
    public const string CatalogImportUrl = "Catalog.ImportUrl";
    public const string CatalogUpdateMetadata = "Catalog.UpdateMetadata";
    public const string CatalogPrepare = "Catalog.Prepare";
    public const string CatalogReview = "Catalog.Review";
    public const string CatalogPublish = "Catalog.Publish";
    public const string CatalogHide = "Catalog.Hide";
    public const string CatalogDispatchFailed = "Catalog.DispatchFailed";
}
