using DeepPace.Application.Common;

namespace DeepPace.Application.Interfaces;

public interface IGeminiAiService
{
    Task<Result<(string Front, string Back)>> SynthesizeActiveRecallCardAsync(
        string quote,
        string? note,
        string chapterTitle,
        string locale = "en",
        CancellationToken ct = default);
}
