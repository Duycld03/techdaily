namespace TechDaily.Domain.Enums;

public enum Category
{
    FrontendWeb,
    BackendDotNet,
    DatabaseStorage,
    SystemDesign
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
