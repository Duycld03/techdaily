using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.Review.GetReviewDeck;
using DeepPace.Application.Features.Review.GradeReviewCard;
using DeepPace.Application.Features.Review.GetReviewCards;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class ReviewHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public ReviewHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetReviewDeck_ShouldReturnOnlyDueCards()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var today = new DateOnly(2026, 3, 15);

        // Due card (due today)
        var dueCard = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Postgres WAL", "Write-ahead logging", today);
        // Future card (due in 5 days)
        var futureCard = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Vue Reactivity", "Proxy engine", today.AddDays(5));

        await _db.SpacedRepetitionCards.AddRangeAsync(dueCard, futureCard);
        await _db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(_db);
        var request = new GetReviewDeckRequest(userId, today);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCardsDue.Should().Be(1);
        result.Value.DueCards.First().FrontMarkdown.Should().Be("Postgres WAL");
    }

    [Fact]
    public async Task GradeReviewCard_ValidGrade_ShouldApplySM2Algorithm()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Front", "Back", new DateOnly(2026, 3, 15));
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var validator = new GradeReviewCardValidator();
        var handler = new GradeReviewCardHandler(_db, validator);

        // Act - Grade 4 (Good recall)
        var request = new GradeReviewCardRequest(card.Id, userId, 4);
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RepetitionCount.Should().Be(1);
        result.Value.IntervalDays.Should().Be(1);
        result.Value.Status.Should().Be(CardStatus.Reviewing);

        var updatedCard = await _db.SpacedRepetitionCards.FindAsync(card.Id);
        updatedCard!.RepetitionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewDeck_ShouldReturnFrontBackForDocumentChunkCard_WithoutTopicRow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "reader@techdaily.local", Name = "Reader" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook { Id = Guid.NewGuid(), Title = "Designing Data-Intensive Applications", Slug = "ddia", Category = Category.DatabaseStorage };
        var chunk = new DocumentChunk { Id = Guid.NewGuid(), DocumentBookId = book.Id, ChunkOrder = 1, ChapterTitle = "Reliability" };
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);

        var today = new DateOnly(2026, 3, 15);
        var card = SpacedRepetitionCard.CreateFromDrillMistake(
            userId,
            chunk.Id,
            "What is write amplification?",
            "Write amplification is data written to storage vs data written by user.",
            today);

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(_db);
        var request = new GetReviewDeckRequest(userId, today);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCardsDue.Should().Be(1);
        var dueCard = result.Value.DueCards.First();
        dueCard.SourceType.Should().Be(CardSourceType.DocumentChunk);
        dueCard.SourceDocumentChunkId.Should().Be(chunk.Id);
        dueCard.FrontMarkdown.Should().Be("What is write amplification?");
        dueCard.BackMarkdown.Should().Be("Write amplification is data written to storage vs data written by user.");
    }

    [Fact]
    public async Task GetReviewCards_ShouldReturnFrontBackForDocumentChunkCard_WithoutTopicRow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "reader2@techdaily.local", Name = "Reader 2" };
        await _db.Users.AddAsync(user);

        var book2 = new DocumentBook { Id = Guid.NewGuid(), Title = "Database Internals", Slug = "db-internals", Category = Category.DatabaseStorage };
        var chunk2 = new DocumentChunk { Id = Guid.NewGuid(), DocumentBookId = book2.Id, ChunkOrder = 1, ChapterTitle = "LSM Trees" };
        await _db.DocumentBooks.AddAsync(book2);
        await _db.DocumentChunks.AddAsync(chunk2);

        var today = new DateOnly(2026, 3, 15);
        var card = SpacedRepetitionCard.CreateFromDrillMistake(
            userId,
            chunk2.Id,
            "What is LSM compaction?",
            "LSM compaction merges SSTables to reclaim space.",
            today);

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);
        var request = new GetReviewCardsRequest(userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        var fetchedCard = result.Value.Cards.First();
        fetchedCard.SourceType.Should().Be(CardSourceType.DocumentChunk);
        fetchedCard.SourceDocumentChunkId.Should().Be(chunk2.Id);
        fetchedCard.FrontMarkdown.Should().Be("What is LSM compaction?");
        fetchedCard.BackMarkdown.Should().Be("LSM compaction merges SSTables to reclaim space.");
    }
}
