using DeepPace.Application.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Interfaces;

public record AiScenarioDrillVo(
    string QuestionText,
    List<string> Options,
    int CorrectOptionIndex,
    string ExplanationMarkdown,
    List<string> ExpectedKeyPoints);

public record AiFormattedSliceResult(
    string FormattedMarkdown,
    string SummaryMarkdown,
    List<string> KeyTakeaways,
    int EstimatedReadMinutes,
    AiScenarioDrillVo? ScenarioDrill = null);

public interface IAiMarkdownFormatter
{
    Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
        string rawText,
        string chapterTitle,
        string language = "en",
        Category? category = null,
        CancellationToken cancellationToken = default);
}
