using TechDaily.Application.Common;

namespace TechDaily.Application.Interfaces;

public sealed record GoogleCloudTtsResult(
    byte[] AudioBytes,
    int CharacterCount,
    double DurationSeconds);

public interface IGoogleCloudTtsService
{
    Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(
        string text,
        string voiceId,
        string? languageCode = null,
        CancellationToken cancellationToken = default);
}
