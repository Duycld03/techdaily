namespace TechDaily.Application.DTOs;

public class AiReviewDto
{
    public int Score { get; set; }
    public string SummaryFeedback { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
    public string ImprovedAnswerMarkdown { get; set; } = string.Empty;
    public string AiModelUsed { get; set; } = "gemini-2.5-flash";
}
