using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class AiReview : BaseEntity
{
    public Guid DailyDrillId { get; set; }
    public int Score { get; set; } // 1 to 10
    public string SummaryFeedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
    public string ImprovedAnswerMarkdown { get; set; } = string.Empty;
    public string AiModelUsed { get; set; } = "gemini-2.5-flash";

    // Navigation properties
    public DailyDrill DailyDrill { get; set; } = null!;
}
