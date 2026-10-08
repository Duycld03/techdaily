using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

/// <summary>
/// Represents a deliberate practice Decision Drill synthesized from a document chunk,
/// supporting both technical architecture trade-offs and behavioral mindset scenarios.
/// </summary>
public class InterviewQuestion : BaseEntity
{
    public Guid? DocumentChunkId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; } = 0;
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> ExpectedKeyPoints { get; set; } = new();
    public string ModelAnswerMarkdown { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }

    // Navigation properties
    public DocumentChunk? DocumentChunk { get; set; }
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
}
