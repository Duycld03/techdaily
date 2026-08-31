using TechDaily.Application.DTOs;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

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
    public MicroQuizVo MicroQuiz { get; set; } = new();
    public string Language { get; set; } = "en";
    public int EstimatedReadMinutes { get; set; }
}

public class DailyDrillDto
{
    public Guid Id { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public DrillStatus Status { get; set; }
    public string? UserAnswerText { get; set; }
    public string? UserAudioUrl { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public AiReviewDto? AiReview { get; set; }
}
