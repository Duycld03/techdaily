namespace TechDaily.Application.Features.Library.SynthesizeAudio;

public record SynthesizeChunkAudioRequest(
    Guid ChunkId,
    string VoiceId,
    string? ContentHash = null,
    string? NarrationScript = null);

public class SynthesizeChunkAudioResponse
{
    public byte[] AudioBytes { get; set; } = [];
    public string MimeType { get; set; } = "audio/mpeg";
    public string ContentHash { get; set; } = string.Empty;
    public string VoiceId { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsCacheHit { get; set; }
}

public record GetAudioQuotaRequest;

public class AudioQuotaResponse
{
    public long MonthlyLimit { get; set; } = 950_000;
    public long UsedCharacters { get; set; }
    public long RemainingCharacters { get; set; }
    public bool IsNearLimit { get; set; }
    public bool IsExhausted { get; set; }
}
