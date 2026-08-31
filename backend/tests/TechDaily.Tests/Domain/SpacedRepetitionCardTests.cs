using FluentAssertions;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using Xunit;

namespace TechDaily.Tests.Domain;

public class SpacedRepetitionCardTests
{
    [Fact]
    public void Create_ShouldInitializeWithDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var today = new DateOnly(2026, 1, 1);

        // Act
        var card = SpacedRepetitionCard.Create(userId, topicId, today);

        // Assert
        card.UserId.Should().Be(userId);
        card.TopicId.Should().Be(topicId);
        card.RepetitionCount.Should().Be(0);
        card.EaseFactor.Should().Be(2.50m);
        card.IntervalDays.Should().Be(1);
        card.Status.Should().Be(CardStatus.Learning);
        card.NextReviewDate.Should().Be(today);
    }

    [Fact]
    public void ApplyReview_Grade5_ShouldFollowSM2Progression()
    {
        // Arrange
        var card = SpacedRepetitionCard.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));

        // First successful review (Repetition 0 -> 1)
        card.ApplyReview(5, new DateOnly(2026, 1, 1));
        card.RepetitionCount.Should().Be(1);
        card.IntervalDays.Should().Be(1);
        card.NextReviewDate.Should().Be(new DateOnly(2026, 1, 2));
        card.Status.Should().Be(CardStatus.Reviewing);

        // Second successful review (Repetition 1 -> 2)
        card.ApplyReview(5, new DateOnly(2026, 1, 2));
        card.RepetitionCount.Should().Be(2);
        card.IntervalDays.Should().Be(6);
        card.NextReviewDate.Should().Be(new DateOnly(2026, 1, 8));

        // Third successful review (Repetition 2 -> 3)
        card.ApplyReview(5, new DateOnly(2026, 1, 8));
        card.RepetitionCount.Should().Be(3);
        card.IntervalDays.Should().Be(15); // 6 * 2.50 = 15

        // Fourth successful review (Repetition 3 -> 4) -> Mastered
        card.ApplyReview(5, new DateOnly(2026, 1, 23));
        card.RepetitionCount.Should().Be(4);
        card.IntervalDays.Should().Be(38); // 15 * 2.50 = 37.5 -> 38
        card.Status.Should().Be(CardStatus.Mastered);
    }

    [Fact]
    public void ApplyReview_GradeBelow3_ShouldResetStreakToLearning()
    {
        // Arrange
        var card = SpacedRepetitionCard.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));
        card.ApplyReview(5, new DateOnly(2026, 1, 1));
        card.ApplyReview(5, new DateOnly(2026, 1, 2));
        card.RepetitionCount.Should().Be(2);

        // Act - Failed review (Grade 2)
        card.ApplyReview(2, new DateOnly(2026, 1, 8));

        // Assert
        card.RepetitionCount.Should().Be(0);
        card.IntervalDays.Should().Be(1);
        card.Status.Should().Be(CardStatus.Learning);
        card.NextReviewDate.Should().Be(new DateOnly(2026, 1, 9));
        card.EaseFactor.Should().BeLessThan(2.50m);
    }

    [Fact]
    public void ApplyReview_EaseFactor_ShouldNeverDropBelowMinimum1Point30()
    {
        // Arrange
        var card = SpacedRepetitionCard.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));

        // Act - Repeatedly fail
        for (int i = 0; i < 15; i++)
        {
            card.ApplyReview(0, new DateOnly(2026, 1, 1).AddDays(i));
        }

        // Assert
        card.EaseFactor.Should().Be(1.30m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void ApplyReview_InvalidGrade_ShouldThrowException(int invalidGrade)
    {
        var card = SpacedRepetitionCard.Create(Guid.NewGuid(), Guid.NewGuid());
        var act = () => card.ApplyReview(invalidGrade);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
