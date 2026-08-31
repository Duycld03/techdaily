using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class DocumentBook : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public Category Category { get; set; }
    public int TotalChunks { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
