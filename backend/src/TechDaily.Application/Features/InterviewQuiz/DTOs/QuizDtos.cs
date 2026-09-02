using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.InterviewQuiz.DTOs;

public class QuizQuestionDto
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public Category Category { get; set; }
    public QuizLevel Level { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; }
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsMastered { get; set; }
    public int? LastSelectedOptionIndex { get; set; }
    public bool? IsLastAnswerCorrect { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
}

public record GenerateQuizRequest(
    Guid UserId,
    string Topic,
    Category? Category,
    QuizLevel Level,
    int Count = 5,
    string Locale = "en"
);

public record GenerateQuizResponse(
    List<QuizQuestionDto> Questions,
    string Topic,
    QuizLevel Level,
    int TotalCount
);

public record SubmitQuizAnswerRequest(
    Guid UserId,
    Guid QuestionId,
    int SelectedOptionIndex
);

public record SubmitQuizAnswerResponse(
    bool IsCorrect,
    int CorrectOptionIndex,
    string ExplanationMarkdown,
    bool IsMastered,
    int CorrectCount,
    int IncorrectCount
);

public record GetQuizReviewQueueRequest(
    Guid UserId,
    Category? Category = null,
    QuizLevel? Level = null,
    string? Topic = null,
    int Page = 1,
    int PageSize = 20
);

public record GetQuizReviewQueueResponse(
    List<QuizQuestionDto> Questions,
    int TotalCount,
    int Page,
    int PageSize
);

public record LevelStatDto(
    QuizLevel Level,
    int AnsweredCount,
    int MasteredCount,
    decimal AccuracyRate
);

public record TopicStatDto(
    string Topic,
    int AnsweredCount,
    int MasteredCount,
    decimal AccuracyRate
);

public record GetQuizStatsRequest(Guid UserId);

public record GetQuizStatsResponse(
    int TotalAnswered,
    int MasteredCount,
    int ReviewQueueCount,
    decimal AccuracyRate,
    List<LevelStatDto> LevelBreakdown,
    List<TopicStatDto> TopicBreakdown
);
