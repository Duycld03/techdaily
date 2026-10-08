namespace TechDaily.Domain.Enums;

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
