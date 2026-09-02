using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class UserQuizProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public bool IsMastered { get; set; } = false;
    public int? LastSelectedOptionIndex { get; set; }
    public bool? IsLastAnswerCorrect { get; set; }
    public int CorrectCount { get; set; } = 0;
    public int IncorrectCount { get; set; } = 0;
    public DateTimeOffset? LastAttemptedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;

    public void RecordAttempt(int selectedOptionIndex, bool isCorrect)
    {
        LastSelectedOptionIndex = selectedOptionIndex;
        IsLastAnswerCorrect = isCorrect;
        LastAttemptedAt = DateTimeOffset.UtcNow;

        if (isCorrect)
        {
            IsMastered = true;
            CorrectCount++;
        }
        else
        {
            IsMastered = false;
            IncorrectCount++;
        }

        MarkUpdated();
    }
}
