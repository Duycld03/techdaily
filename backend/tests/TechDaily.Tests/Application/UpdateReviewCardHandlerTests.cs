using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Domain.Enums;
using TechDaily.Application.Features.Review.UpdateReviewCard;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class UpdateReviewCardHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly UpdateReviewCardValidator _validator;

    public UpdateReviewCardHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _validator = new UpdateReviewCardValidator();
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
            Name = "Flashcard Author"
        };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Database Durability",
            Slug = "db-durability",
            Category = Category.DatabaseStorage,
            Difficulty = Difficulty.Senior
        };
        await _db.Topics.AddAsync(topic);

        var card = SpacedRepetitionCard.Create(userId, topic.Id);
        card.FrontMarkdown = "Old front question";
        card.BackMarkdown = "Old back answer";

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        return (userId, card);
    }

    [Fact]
    public async Task Validator_ValidRequest_ShouldPass()
    {
        var request = new UpdateReviewCardRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Valid front content",
            "Valid back content");

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EmptyIds_ShouldFail()
    {
        var req1 = new UpdateReviewCardRequest(Guid.Empty, Guid.NewGuid(), "F", "B");
        var req2 = new UpdateReviewCardRequest(Guid.NewGuid(), Guid.Empty, "F", "B");

        var res1 = await _validator.ValidateAsync(req1);
        var res2 = await _validator.ValidateAsync(req2);

        res1.IsValid.Should().BeFalse();
        res2.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_EmptyMarkdown_ShouldFail()
    {
        var reqEmptyFront = new UpdateReviewCardRequest(Guid.NewGuid(), Guid.NewGuid(), "", "Back");
        var reqEmptyBack = new UpdateReviewCardRequest(Guid.NewGuid(), Guid.NewGuid(), "Front", "   ");

        var res1 = await _validator.ValidateAsync(reqEmptyFront);
        var res2 = await _validator.ValidateAsync(reqEmptyBack);

        res1.IsValid.Should().BeFalse();
        res2.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_MarkdownExceeding5000Chars_ShouldFail()
    {
        var longText = new string('x', 5001);
        var req = new UpdateReviewCardRequest(Guid.NewGuid(), Guid.NewGuid(), longText, "Back");

        var res = await _validator.ValidateAsync(req);

        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == "FrontMarkdown");
    }

    [Fact]
    public async Task UpdateReviewCard_WhenOwned_ShouldUpdateContentAndReturnDto()
    {
        // Arrange
        var (userId, card) = await SeedCardAsync();
        var handler = new UpdateReviewCardHandler(_db, _validator);

        var request = new UpdateReviewCardRequest(
            card.Id,
            userId,
            "## What is Write-Ahead Logging?",
            "**WAL** guarantees ACID durability by appending changes before flushing to disk.");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Card.FrontMarkdown.Should().Be("## What is Write-Ahead Logging?");
        result.Value.Card.BackMarkdown.Should().Be("**WAL** guarantees ACID durability by appending changes before flushing to disk.");

        // Verify DB persistence
        var persisted = await _db.SpacedRepetitionCards.FindAsync(card.Id);
        persisted!.FrontMarkdown.Should().Be("## What is Write-Ahead Logging?");
        persisted.BackMarkdown.Should().Be("**WAL** guarantees ACID durability by appending changes before flushing to disk.");
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReviewCard_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _) = await SeedCardAsync();
        var handler = new UpdateReviewCardHandler(_db, _validator);

        var request = new UpdateReviewCardRequest(Guid.NewGuid(), userId, "Front", "Back");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task UpdateReviewCard_WhenBelongsToAnotherUser_ShouldReturnNotFound()
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

        var handler = new UpdateReviewCardHandler(_db, _validator);

        var request = new UpdateReviewCardRequest(card.Id, otherUserId, "Front", "Back");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task UpdateReviewCard_WhenSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, card) = await SeedCardAsync();
        card.SoftDelete();
        await _db.SaveChangesAsync();

        var handler = new UpdateReviewCardHandler(_db, _validator);
        var request = new UpdateReviewCardRequest(card.Id, userId, "Front", "Back");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task UpdateReviewCard_WhenValidationFails_ShouldReturnValidationError()
    {
        // Arrange
        var (userId, card) = await SeedCardAsync();
        var handler = new UpdateReviewCardHandler(_db, _validator);

        var request = new UpdateReviewCardRequest(card.Id, userId, "", "");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }
}
