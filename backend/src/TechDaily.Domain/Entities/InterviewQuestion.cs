using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class InterviewQuestion : BaseEntity
{
    public Guid TopicId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; } = 0;
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> ExpectedKeyPoints { get; set; } = new();
    public string ModelAnswerMarkdown { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }

    // Navigation properties
    public Topic Topic { get; set; } = null!;
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
}
