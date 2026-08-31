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
