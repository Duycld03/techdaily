using TechDaily.Application.Common;

namespace TechDaily.Application.Interfaces;

public record AiFormattedSliceResult(
    string FormattedMarkdown,
    string SummaryMarkdown,
    List<string> KeyTakeaways,
    int EstimatedReadMinutes);

public interface IAiMarkdownFormatter
{
    Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
        string rawText,
        string chapterTitle,
        string language = "en",
        CancellationToken cancellationToken = default);
}
