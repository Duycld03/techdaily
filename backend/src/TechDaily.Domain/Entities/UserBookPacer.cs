using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class UserBookPacer : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DocumentBookId { get; set; }
    public int CurrentChunkOrder { get; set; } = 1;
    public int DailyPaceChunks { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateOnly? LastReadDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public DocumentBook DocumentBook { get; set; } = null!;

    public void AdvanceToNextChunk(int totalChunks)
    {
        CurrentChunkOrder++;
        LastReadDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (CurrentChunkOrder >= totalChunks)
        {
            CompletedAt = DateTimeOffset.UtcNow;
        }
        MarkUpdated();
    }

    public void JumpToChunk(int chunkOrder, int totalChunks)
    {
        CurrentChunkOrder = Math.Clamp(chunkOrder, 1, Math.Max(1, totalChunks));
        LastReadDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (CurrentChunkOrder >= totalChunks)
        {
            CompletedAt = DateTimeOffset.UtcNow;
        }
        MarkUpdated();
    }
}
