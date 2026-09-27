using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class DocumentChunkAudio : BaseEntity
{
    public Guid DocumentChunkId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string VoiceId { get; set; } = string.Empty;
    public string MimeType { get; set; } = "audio/mpeg";
    public byte[] AudioData { get; set; } = [];
    public int CharacterCount { get; set; }
    public double DurationSeconds { get; set; }

    // Navigation property
    public DocumentChunk DocumentChunk { get; set; } = null!;
}
