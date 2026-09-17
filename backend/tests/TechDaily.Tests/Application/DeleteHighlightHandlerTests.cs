using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class DeleteHighlightHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public DeleteHighlightHandlerTests()
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

    private async Task<(Guid UserId, DocumentChunk Chunk, UserHighlight Highlight)> SeedHighlightAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Highlight Owner"
        };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Storage & Retrieval",
            OriginalTextMarkdown = "LSM-trees vs B-trees excerpt."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentChunkId = chunk.Id,
            SelectedText = "LSM-trees vs B-trees excerpt.",
            Note = "Original reflection note",
            Tags = new List<string> { "storage", "performance" }
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        return (userId, chunk, highlight);
    }

    [Fact]
    public async Task DeleteHighlight_WhenOwned_ShouldSoftDeleteHighlight()
    {
        // Arrange
        var (userId, _, highlight) = await SeedHighlightAsync();
        var handler = new DeleteHighlightHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new DeleteHighlightRequest(highlight.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        var persisted = await _db.UserHighlights.FindAsync(highlight.Id);
        persisted!.IsDeleted.Should().BeTrue();
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteHighlight_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _, _) = await SeedHighlightAsync();
        var handler = new DeleteHighlightHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new DeleteHighlightRequest(Guid.NewGuid(), userId));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task DeleteHighlight_WhenBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var (_, _, highlight) = await SeedHighlightAsync();
        var otherUserId = Guid.NewGuid();
        var otherUser = new User
        {
            Id = otherUserId,
            Email = $"other_{otherUserId}@techdaily.local",
            Name = "Other User"
        };
        await _db.Users.AddAsync(otherUser);
        await _db.SaveChangesAsync();

        var handler = new DeleteHighlightHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new DeleteHighlightRequest(highlight.Id, otherUserId));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task DeleteHighlight_WhenHighlightSoftDeleted_AssociatedFlashcardRemainsActiveAndRetrievableByGetReviewDeck()
    {
        // Arrange: Seed highlight and an active spaced repetition card created from it
        var (userId, _, highlight) = await SeedHighlightAsync();

        var card = SpacedRepetitionCard.CreateFromHighlight(
            userId,
            highlight.Id,
            "What is an LSM-tree?",
            "Log-Structured Merge-tree appending to memory table and flushing to SSTables.",
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var deleteHighlightHandler = new DeleteHighlightHandler(_db);
        var getReviewDeckHandler = new GetReviewDeckHandler(_db);

        // Act: Delete the user highlight
        var deleteResult = await deleteHighlightHandler.ExecuteAsync(new DeleteHighlightRequest(highlight.Id, userId));

        // Assert: Highlight is soft-deleted
        deleteResult.IsSuccess.Should().BeTrue();
        var persistedHighlight = await _db.UserHighlights.FindAsync(highlight.Id);
        persistedHighlight!.IsDeleted.Should().BeTrue();

        // Invariant: Card remains completely active and uncorrupted
        var persistedCard = await _db.SpacedRepetitionCards.FindAsync(card.Id);
        persistedCard.Should().NotBeNull();
        persistedCard!.IsDeleted.Should().BeFalse();
        persistedCard.SourceHighlightId.Should().Be(highlight.Id);
        persistedCard.FrontMarkdown.Should().Be("What is an LSM-tree?");
        persistedCard.BackMarkdown.Should().Be("Log-Structured Merge-tree appending to memory table and flushing to SSTables.");

        // Invariant: Card is still retrieved in user's review deck queue
        var deckResult = await getReviewDeckHandler.ExecuteAsync(new GetReviewDeckRequest(userId));
        deckResult.IsSuccess.Should().BeTrue();
        deckResult.Value.TotalCardsDue.Should().Be(1);
        deckResult.Value.DueCards.Should().ContainSingle(c => c.Id == card.Id);
        var retrievedCard = deckResult.Value.DueCards.First(c => c.Id == card.Id);
        retrievedCard.FrontMarkdown.Should().Be("What is an LSM-tree?");
        retrievedCard.BackMarkdown.Should().Be("Log-Structured Merge-tree appending to memory table and flushing to SSTables.");
    }
}
