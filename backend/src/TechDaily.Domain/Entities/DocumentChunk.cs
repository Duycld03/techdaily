using Pgvector;
using TechDaily.Domain.Common;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentBookId { get; set; }
    public int ChunkOrder { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string OriginalTextMarkdown { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public MicroQuizVo MicroQuiz { get; set; } = new();
    public string Language { get; set; } = "en";
    public Vector? Embedding { get; set; }
    public int EstimatedReadMinutes { get; set; } = 3;

    // Navigation properties
    public DocumentBook DocumentBook { get; set; } = null!;
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
    public ICollection<UserHighlight> Highlights { get; set; } = new List<UserHighlight>();
}
