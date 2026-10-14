using Pgvector;
using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public class CatalogSlice : BaseEntity
{
    public Guid CatalogDocumentId { get; set; }
    public int SliceOrder { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string OriginalTextMarkdown { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public string Language { get; set; } = "en";
    public Vector? Embedding { get; set; }
    public int EstimatedReadMinutes { get; set; } = 3;
    public bool IsAiFormatted { get; set; } = false;

    // Navigation property
    public CatalogDocument CatalogDocument { get; set; } = null!;
}
