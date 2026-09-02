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
    public int? SelectedOptionIndex { get; set; }
    public bool? IsCorrect { get; set; }
    public int? Score { get; set; }
    public int AttemptCount { get; set; } = 0;
    public DateTimeOffset? SubmittedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public InterviewQuestion Question { get; set; } = null!;
    public DocumentChunk? DocumentChunk { get; set; }

    public void SubmitOption(int selectedIndex, bool isCorrect, int score)
    {
        SelectedOptionIndex = selectedIndex;
        IsCorrect = isCorrect;
        Score = score;
        Status = DrillStatus.Reviewed;
        AttemptCount++;
        SubmittedAt = DateTimeOffset.UtcNow;
        MarkUpdated();
    }
}
