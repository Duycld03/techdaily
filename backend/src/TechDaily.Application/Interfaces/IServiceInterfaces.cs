using TechDaily.Application.Common;

namespace TechDaily.Application.Interfaces;

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
