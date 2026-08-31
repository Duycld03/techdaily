using FluentAssertions;
using TechDaily.Domain.Entities;
using Xunit;

namespace TechDaily.Tests.Domain;

public class StreakRecordTests
{
    [Fact]
    public void Create_ShouldInitializeWithTwoFreezeCredits()
    {
        var userId = Guid.NewGuid();
        var record = StreakRecord.Create(userId);

        record.UserId.Should().Be(userId);
        record.CurrentStreak.Should().Be(0);
        record.LongestStreak.Should().Be(0);
        record.FreezeCreditsRemaining.Should().Be(2);
        record.TotalDrillsCompleted.Should().Be(0);
    }

    [Fact]
    public void RecordCompletion_FirstDay_ShouldSetStreakToOne()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);

        record.RecordCompletion(day1, 8);

        record.CurrentStreak.Should().Be(1);
        record.LongestStreak.Should().Be(1);
        record.LastActiveDate.Should().Be(day1);
        record.TotalDrillsCompleted.Should().Be(1);
        record.AverageScore.Should().Be(8.00m);
    }

    [Fact]
    public void RecordCompletion_ConsecutiveDays_ShouldIncrementStreak()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);
        var day2 = new DateOnly(2026, 3, 2);
        var day3 = new DateOnly(2026, 3, 3);

        record.RecordCompletion(day1, 8);
        record.RecordCompletion(day2, 9);
        record.RecordCompletion(day3, 10);

        record.CurrentStreak.Should().Be(3);
        record.LongestStreak.Should().Be(3);
        record.TotalDrillsCompleted.Should().Be(3);
        record.AverageScore.Should().Be(9.00m); // (8 + 9 + 10) / 3 = 9.00
    }

    [Fact]
    public void RecordCompletion_SameDay_ShouldNotDoubleIncrementStreak()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);

        record.RecordCompletion(day1, 8);
        record.RecordCompletion(day1, 10);

        record.CurrentStreak.Should().Be(1);
        record.TotalDrillsCompleted.Should().Be(2);
        record.AverageScore.Should().Be(9.00m);
    }

    [Fact]
    public void RecordCompletion_MissedOneDay_WithFreezeCredits_ShouldConsumeCreditAndKeepStreak()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);
        var day3 = new DateOnly(2026, 3, 3); // Missed day 2!

        record.RecordCompletion(day1, 8);
        record.FreezeCreditsRemaining.Should().Be(2);

        // Act - Complete on day 3
        record.RecordCompletion(day3, 8);

        // Assert - Freeze credit protected streak
        record.FreezeCreditsRemaining.Should().Be(1);
        record.CurrentStreak.Should().Be(2);
        record.LongestStreak.Should().Be(2);
    }

    [Fact]
    public void RecordCompletion_MissedOneDay_WithoutFreezeCredits_ShouldResetStreakToOne()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);
        var day3 = new DateOnly(2026, 3, 3);
        var day5 = new DateOnly(2026, 3, 5);
        var day7 = new DateOnly(2026, 3, 7);

        // Use up 2 freeze credits
        record.RecordCompletion(day1);
        record.RecordCompletion(day3); // Credit 2 -> 1, Streak = 2
        record.RecordCompletion(day5); // Credit 1 -> 0, Streak = 3

        record.FreezeCreditsRemaining.Should().Be(0);

        // Act - Miss day 6, complete on day 7 with 0 credits
        record.RecordCompletion(day7);

        // Assert - Streak reset to 1
        record.CurrentStreak.Should().Be(1);
        record.LongestStreak.Should().Be(3); // Preserved highest streak
    }

    [Fact]
    public void RecordCompletion_MissedMultipleDays_ShouldResetStreakToOne()
    {
        var record = StreakRecord.Create(Guid.NewGuid());
        var day1 = new DateOnly(2026, 3, 1);
        var day10 = new DateOnly(2026, 3, 10); // Missed 8 days

        record.RecordCompletion(day1);
        record.RecordCompletion(day10);

        record.CurrentStreak.Should().Be(1);
        record.FreezeCreditsRemaining.Should().Be(2); // Credits not consumed for multiday gaps
    }
}
