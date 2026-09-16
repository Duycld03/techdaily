using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.DailyFocus.DTOs;

public class TopicDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Category Category { get; set; }
    public Difficulty Difficulty { get; set; }
    public int DayOrder { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string DeepDiveMarkdown { get; set; } = string.Empty;
    public string? BenchmarkSnippet { get; set; }
}

public class InterviewQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int? CorrectOptionIndex { get; set; }
    public string? ExplanationMarkdown { get; set; }
    public List<string> ExpectedKeyPoints { get; set; } = new();
    public string ModelAnswerMarkdown { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
}

public class DocumentChunkDto
{
    public Guid Id { get; set; }
    public int ChunkOrder { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string OriginalTextMarkdown { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public string Language { get; set; } = "en";
    public int EstimatedReadMinutes { get; set; }
    public bool IsAiFormatted { get; set; }
}

public class DailyDrillDto
{
    public Guid Id { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public DrillStatus Status { get; set; }
    public int? SelectedOptionIndex { get; set; }
    public bool? IsCorrect { get; set; }
    public int? Score { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
}

public class PacerDto
{
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public int CurrentChunkOrder { get; set; }
    public int TotalChunks { get; set; }
    public int ProgressPercentage { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public List<PacerBookSummaryDto> AvailableBooks { get; set; } = new();
}

public class PacerBookSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public int TotalChunks { get; set; }
    public int CurrentChunkOrder { get; set; }
    public bool IsActive { get; set; }
}
