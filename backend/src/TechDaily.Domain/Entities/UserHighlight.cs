using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class UserHighlight : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DocumentChunkId { get; set; }
    public string SelectedText { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<string> Tags { get; set; } = new();

    // Navigation properties
    public User User { get; set; } = null!;
    public DocumentChunk DocumentChunk { get; set; } = null!;
}
