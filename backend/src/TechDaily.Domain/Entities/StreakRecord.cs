using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class StreakRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; private set; } = 0;
    public int LongestStreak { get; private set; } = 0;
    public DateOnly? LastActiveDate { get; private set; }
    public int FreezeCreditsRemaining { get; private set; } = 2;
    public int LastFreezeMonth { get; private set; } = DateTime.UtcNow.Month;
    public int TotalDrillsCompleted { get; private set; } = 0;
    public decimal AverageScore { get; private set; } = 0.00m;

    // Navigation properties
    public User User { get; set; } = null!;

    public static StreakRecord Create(Guid userId)
    {
        return new StreakRecord
        {
            UserId = userId,
            CurrentStreak = 0,
            LongestStreak = 0,
            FreezeCreditsRemaining = 2,
            LastFreezeMonth = DateTime.UtcNow.Month
        };
    }

    /// <summary>
    /// Records daily completion, handles consecutive streak increments and automatic freeze credit protections.
    /// </summary>
    public void RecordCompletion(DateOnly today, int? drillScore = null)
    {
        // Monthly reset of freeze credits (2 credits per calendar month)
        if (today.Month != LastFreezeMonth)
        {
            FreezeCreditsRemaining = 2;
            LastFreezeMonth = today.Month;
        }

        if (LastActiveDate == today)
        {
            // Already logged activity today - update score metrics without double-incrementing streak
            UpdateScoreMetrics(drillScore);
            MarkUpdated();
            return;
        }

        if (LastActiveDate == null)
        {
            CurrentStreak = 1;
        }
        else
        {
            var dayDifference = today.DayNumber - LastActiveDate.Value.DayNumber;

            if (dayDifference == 1)
            {
                // Consecutive day
                CurrentStreak++;
            }
            else if (dayDifference == 2 && FreezeCreditsRemaining > 0)
            {
                // Missed exactly 1 day, protect with Streak Freeze credit
                FreezeCreditsRemaining--;
                CurrentStreak++;
            }
            else
            {
                // Broken streak
                CurrentStreak = 1;
            }
        }

        LongestStreak = Math.Max(LongestStreak, CurrentStreak);
        LastActiveDate = today;
        UpdateScoreMetrics(drillScore);
        MarkUpdated();
    }

    private void UpdateScoreMetrics(int? drillScore)
    {
        if (drillScore.HasValue && drillScore.Value > 0)
        {
            var totalPoints = (AverageScore * TotalDrillsCompleted) + drillScore.Value;
            TotalDrillsCompleted++;
            AverageScore = decimal.Round(totalPoints / TotalDrillsCompleted, 2);
        }
        else
        {
            TotalDrillsCompleted++;
        }
    }
}
