using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Domain.Enums;
using TechDaily.Application.Features.Review.DeleteReviewCard;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class DeleteReviewCardHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public DeleteReviewCardHandlerTests()
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

    private async Task<(Guid UserId, SpacedRepetitionCard Card)> SeedCardAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Flashcard Owner"
        };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Topic for Card",
            Slug = "topic-for-card",
            Category = Category.BackendRuntime,
            Difficulty = Difficulty.Senior
        };
        await _db.Topics.AddAsync(topic);

        var card = SpacedRepetitionCard.Create(userId, topic.Id);
        card.FrontMarkdown = "Card front";
        card.BackMarkdown = "Card back";

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        return (userId, card);
    }

    [Fact]
    public async Task DeleteReviewCard_WhenOwned_ShouldSoftDeleteAndReturnSuccess()
    {
        // Arrange
        var (userId, card) = await SeedCardAsync();
        var handler = new DeleteReviewCardHandler(_db);
        var request = new DeleteReviewCardRequest(card.Id, userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        var persisted = await _db.SpacedRepetitionCards.FindAsync(card.Id);
        persisted!.IsDeleted.Should().BeTrue();
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteReviewCard_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _) = await SeedCardAsync();
        var handler = new DeleteReviewCardHandler(_db);
        var request = new DeleteReviewCardRequest(Guid.NewGuid(), userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task DeleteReviewCard_WhenBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var (_, card) = await SeedCardAsync();
        var otherUserId = Guid.NewGuid();
        var otherUser = new User
        {
            Id = otherUserId,
            Email = $"other_{otherUserId}@techdaily.local",
            Name = "Other User"
        };
        await _db.Users.AddAsync(otherUser);
        await _db.SaveChangesAsync();

        var handler = new DeleteReviewCardHandler(_db);
        var request = new DeleteReviewCardRequest(card.Id, otherUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task DeleteReviewCard_WhenAlreadyDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, card) = await SeedCardAsync();
        card.SoftDelete();
        await _db.SaveChangesAsync();

        var handler = new DeleteReviewCardHandler(_db);
        var request = new DeleteReviewCardRequest(card.Id, userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }
}
