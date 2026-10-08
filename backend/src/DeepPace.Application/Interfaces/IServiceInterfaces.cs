using DeepPace.Application.Common;

namespace DeepPace.Application.Interfaces;

public record TermExplanationResult(string Explanation, bool IsFromCache);

public interface ITermExplanationService
{
    Task<Result<TermExplanationResult>> ExplainTermAsync(
        string term,
        string category,
        string context,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface ITechInsightGenerator
{
    Task<Result<DeepPace.Domain.Entities.TechInsight>> GenerateInsightAsync(
        DeepPace.Domain.Enums.Category? preferredCategory,
        string? preferredTopic,
        List<string>? existingTitlesToAvoid = null,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

public interface IQuizGeneratorService
{
    Task<Result<List<DeepPace.Domain.Entities.QuizQuestion>>> GenerateQuestionsAsync(
        string topic,
        DeepPace.Domain.Enums.Category category,
        DeepPace.Domain.Enums.QuizLevel level,
        int count,
        List<string> existingTitlesToAvoid,
        string locale = "en",
        CancellationToken cancellationToken = default);
}

