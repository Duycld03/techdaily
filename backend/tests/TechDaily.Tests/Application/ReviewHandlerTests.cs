using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class ReviewHandlerTests
{
    private TechDailyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TechDailyDbContext(options);
    }

    [Fact]
    public async Task GetReviewDeck_ShouldReturnOnlyDueCards()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var today = new DateOnly(2026, 3, 15);

        var topic1 = new Topic { Id = Guid.NewGuid(), Title = "Postgres WAL", Slug = "wal", Category = Category.DatabaseStorage, Difficulty = Difficulty.Senior, DayOrder = 16 };
        var topic2 = new Topic { Id = Guid.NewGuid(), Title = "Vue Reactivity", Slug = "vue", Category = Category.FrontendWeb, Difficulty = Difficulty.Senior, DayOrder = 1 };

        await db.Topics.AddRangeAsync(topic1, topic2);

        // Due card (due today)
        var dueCard = SpacedRepetitionCard.Create(userId, topic1.Id, today);
        // Future card (due in 5 days)
        var futureCard = SpacedRepetitionCard.Create(userId, topic2.Id, today.AddDays(5));

        await db.SpacedRepetitionCards.AddRangeAsync(dueCard, futureCard);
        await db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(db);
        var request = new GetReviewDeckRequest(userId, today);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCardsDue.Should().Be(1);
        result.Value.DueCards.First().TopicTitle.Should().Be("Postgres WAL");
    }

    [Fact]
    public async Task GradeReviewCard_ValidGrade_ShouldApplySM2Algorithm()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var topic = new Topic { Id = Guid.NewGuid(), Title = "Topic", Slug = "slug", Category = Category.SystemDesign, Difficulty = Difficulty.Senior, DayOrder = 24 };
        await db.Topics.AddAsync(topic);

        var card = SpacedRepetitionCard.Create(userId, topic.Id, new DateOnly(2026, 3, 15));
        await db.SpacedRepetitionCards.AddAsync(card);
        await db.SaveChangesAsync();

        var validator = new GradeReviewCardValidator();
        var handler = new GradeReviewCardHandler(db, validator);

        // Act - Grade 4 (Good recall)
        var request = new GradeReviewCardRequest(card.Id, userId, 4);
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RepetitionCount.Should().Be(1);
        result.Value.IntervalDays.Should().Be(1);
        result.Value.Status.Should().Be(CardStatus.Reviewing);

        var updatedCard = await db.SpacedRepetitionCards.FindAsync(card.Id);
        updatedCard!.RepetitionCount.Should().Be(1);
    }
}
