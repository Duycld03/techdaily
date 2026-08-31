using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class Topic : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Category Category { get; set; }
    public Difficulty Difficulty { get; set; }
    public int DayOrder { get; set; } // 1 to 30
    public string Summary { get; set; } = string.Empty;
    public string DeepDiveMarkdown { get; set; } = string.Empty;
    public string? BenchmarkSnippet { get; set; }

    // Navigation properties
    public ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();
    public ICollection<SpacedRepetitionCard> SpacedRepetitionCards { get; set; } = new List<SpacedRepetitionCard>();
}
