using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class DailyDrill : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? DocumentChunkId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public DrillStatus Status { get; set; } = DrillStatus.Pending;
    public string? UserAnswerText { get; set; }
    public string? UserAudioUrl { get; set; }
    public int AttemptCount { get; set; } = 0;
    public DateTimeOffset? SubmittedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public InterviewQuestion Question { get; set; } = null!;
    public DocumentChunk? DocumentChunk { get; set; }
    public AiReview? AiReview { get; set; }

    public void Submit(string? answerText, string? audioUrl)
    {
        UserAnswerText = answerText;
        UserAudioUrl = audioUrl;
        Status = DrillStatus.Submitted;
        AttemptCount++;
        SubmittedAt = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public void MarkReviewed()
    {
        Status = DrillStatus.Reviewed;
        MarkUpdated();
    }
}
