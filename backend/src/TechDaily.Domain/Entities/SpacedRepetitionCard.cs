using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class SpacedRepetitionCard : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public int RepetitionCount { get; private set; } = 0;
    public decimal EaseFactor { get; private set; } = 2.50m;
    public int IntervalDays { get; private set; } = 1;
    public DateOnly NextReviewDate { get; private set; }
    public DateOnly? LastReviewDate { get; private set; }
    public CardStatus Status { get; private set; } = CardStatus.Learning;

    // Navigation properties
    public User User { get; set; } = null!;
    public Topic Topic { get; set; } = null!;

    public SpacedRepetitionCard()
    {
        NextReviewDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static SpacedRepetitionCard Create(Guid userId, Guid topicId, DateOnly? initialDate = null)
    {
        return new SpacedRepetitionCard
        {
            UserId = userId,
            TopicId = topicId,
            NextReviewDate = initialDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = CardStatus.Learning
        };
    }

    /// <summary>
    /// Applies SuperMemo SM-2 algorithm grade (0 to 5) to calculate next interval and ease factor.
    /// </summary>
    public void ApplyReview(int qualityGrade, DateOnly? reviewDate = null)
    {
        if (qualityGrade < 0 || qualityGrade > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(qualityGrade), "Quality grade must be between 0 and 5.");
        }

        var today = reviewDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        LastReviewDate = today;

        if (qualityGrade < 3)
        {
            // Reset streak if failed
            RepetitionCount = 0;
            IntervalDays = 1;
            Status = CardStatus.Learning;
        }
        else
        {
            // Successful recall
            if (RepetitionCount == 0)
            {
                IntervalDays = 1;
            }
            else if (RepetitionCount == 1)
            {
                IntervalDays = 6;
            }
            else
            {
                IntervalDays = (int)Math.Round(IntervalDays * EaseFactor, MidpointRounding.AwayFromZero);
            }

            RepetitionCount++;
            Status = RepetitionCount >= 4 ? CardStatus.Mastered : CardStatus.Reviewing;
        }

        // Calculate new Ease Factor: EF' = EF + (0.1 - (5 - grade) * (0.08 + (5 - grade) * 0.02))
        var diff = 5 - qualityGrade;
        var delta = 0.1m - (diff * (0.08m + (diff * 0.02m)));
        var newEf = EaseFactor + delta;

        // Bound EaseFactor between 1.30 and 2.50
        EaseFactor = Math.Max(1.30m, Math.Min(2.50m, decimal.Round(newEf, 2)));

        NextReviewDate = today.AddDays(IntervalDays);
        MarkUpdated();
    }
}
