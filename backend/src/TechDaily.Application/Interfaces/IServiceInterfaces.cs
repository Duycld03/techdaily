using TechDaily.Application.Common;
using TechDaily.Application.DTOs;

namespace TechDaily.Application.Interfaces;

public interface IAiReviewService
{
    Task<Result<AiReviewDto>> EvaluateSubmissionAsync(
        string questionText,
        List<string> expectedKeyPoints,
        string modelAnswer,
        string? userAnswerText,
        byte[]? audioBytes,
        string? audioMimeType,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface ITermExplanationService
{
    Task<Result<string>> ExplainTermAsync(
        string term,
        string category,
        string context,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface ITelegramNotifier
{
    Task<bool> SendDailyDispatchAsync(
        long chatId,
        string topicTitle,
        string locale = "en",
        CancellationToken cancellationToken = default);

    Task<bool> SendStreakWarningAsync(
        long chatId,
        int currentStreak,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface IAudioStorageService
{
    Task<string> SaveAudioAsync(
        Guid drillId,
        Stream audioStream,
        string fileExtension,
        CancellationToken cancellationToken = default);

    string GetAudioUrl(string relativePath);
}

public interface ITechInsightGenerator
{
    Task<Result<TechDaily.Domain.Entities.TechInsight>> GenerateInsightAsync(
        TechDaily.Domain.Enums.Category? preferredCategory,
        string? preferredTopic,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface IQuizGeneratorService
{
    Task<Result<List<TechDaily.Domain.Entities.QuizQuestion>>> GenerateQuestionsAsync(
        string topic,
        TechDaily.Domain.Enums.Category category,
        TechDaily.Domain.Enums.QuizLevel level,
        int count,
        List<string> existingTitlesToAvoid,
        string locale = "en",
        CancellationToken cancellationToken = default);
}
