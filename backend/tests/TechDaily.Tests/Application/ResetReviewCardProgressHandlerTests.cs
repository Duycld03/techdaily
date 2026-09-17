using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.ResetReviewCardProgress;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class ResetReviewCardProgressHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public ResetReviewCardProgressHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(Guid UserId, SpacedRepetitionCard Card)> SeedCardWithMasteryAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Mastery Student"
        };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Distributed Consensus",
            Slug = "distributed-consensus",
            Category = Category.SystemDesign,
            Difficulty = Difficulty.Senior
        };
        await _db.Topics.AddAsync(topic);

        var card = SpacedRepetitionCard.Create(userId, topic.Id);
        card.FrontMarkdown = "Distributed Consensus (Raft)";
        card.BackMarkdown = "Leader election, log replication, safety invariants.";

        // Progress card to Mastered state
        card.ApplyReview(5, new DateOnly(2026, 1, 1));
        card.ApplyReview(5, new DateOnly(2026, 1, 2));
        card.ApplyReview(5, new DateOnly(2026, 1, 8));
        card.ApplyReview(5, new DateOnly(2026, 1, 24));

        card.Status.Should().Be(CardStatus.Mastered);
        card.RepetitionCount.Should().Be(4);

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        return (userId, card);
    }

    [Fact]
    public async Task ResetReviewCardProgress_WhenOwned_ShouldResetSM2MetricsAndReturnDto()
    {
        // Arrange
        var (userId, card) = await SeedCardWithMasteryAsync();
        var handler = new ResetReviewCardProgressHandler(_db);
        var targetResetDate = new DateOnly(2026, 4, 15);

        var request = new ResetReviewCardProgressRequest(card.Id, userId, targetResetDate);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Card.RepetitionCount.Should().Be(0);
        result.Value.Card.IntervalDays.Should().Be(1);
        result.Value.Card.EaseFactor.Should().Be(2.50m);
        result.Value.Card.Status.Should().Be(CardStatus.Learning);
        result.Value.Card.NextReviewDate.Should().Be(targetResetDate);

        // Verify DB persistence
        var persisted = await _db.SpacedRepetitionCards.FindAsync(card.Id);
        persisted!.RepetitionCount.Should().Be(0);
        persisted.IntervalDays.Should().Be(1);
        persisted.EaseFactor.Should().Be(2.50m);
        persisted.Status.Should().Be(CardStatus.Learning);
        persisted.NextReviewDate.Should().Be(targetResetDate);
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetReviewCardProgress_WhenResetDateNull_ShouldDefaultToToday()
    {
        // Arrange
        var (userId, card) = await SeedCardWithMasteryAsync();
        var handler = new ResetReviewCardProgressHandler(_db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new ResetReviewCardProgressRequest(card.Id, userId, null);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Card.NextReviewDate.Should().Be(today);
        result.Value.Card.Status.Should().Be(CardStatus.Learning);
    }

    [Fact]
    public async Task ResetReviewCardProgress_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _) = await SeedCardWithMasteryAsync();
        var handler = new ResetReviewCardProgressHandler(_db);
        var request = new ResetReviewCardProgressRequest(Guid.NewGuid(), userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task ResetReviewCardProgress_WhenBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var (_, card) = await SeedCardWithMasteryAsync();
        var otherUserId = Guid.NewGuid();
        var otherUser = new User
        {
            Id = otherUserId,
            Email = $"other_{otherUserId}@techdaily.local",
            Name = "Other User"
        };
        await _db.Users.AddAsync(otherUser);
        await _db.SaveChangesAsync();

        var handler = new ResetReviewCardProgressHandler(_db);
        var request = new ResetReviewCardProgressRequest(card.Id, otherUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task ResetReviewCardProgress_WhenSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, card) = await SeedCardWithMasteryAsync();
        card.SoftDelete();
        await _db.SaveChangesAsync();

        var handler = new ResetReviewCardProgressHandler(_db);
        var request = new ResetReviewCardProgressRequest(card.Id, userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }
}
