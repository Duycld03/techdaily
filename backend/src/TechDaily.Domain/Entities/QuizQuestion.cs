using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public string Topic { get; set; } = string.Empty;
    public Category Category { get; set; }
    public QuizLevel Level { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; } = 0;
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public ICollection<UserQuizProgress> UserProgresses { get; set; } = new List<UserQuizProgress>();
}
