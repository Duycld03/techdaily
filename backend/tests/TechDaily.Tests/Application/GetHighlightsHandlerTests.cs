using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Notes.GetHighlights;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetHighlightsHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly GetHighlightsHandler _handler;

    public GetHighlightsHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _handler = new GetHighlightsHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(User User, DocumentChunk Chunk, UserHighlight Highlight)> SeedHighlightAsync(string? email = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? $"dev_{Guid.NewGuid():N}@techdaily.local",
            Name = "Highlight User"
        };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = $"ddia-{Guid.NewGuid():N}",
            Category = Category.DatabaseStorage
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Storage and Retrieval",
            OriginalTextMarkdown = "LSM-Tree storage architecture."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "LSM-Tree storage architecture.",
            Note = "Important storage concept",
            Tags = new List<string> { "storage", "lsm" }
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        return (user, chunk, highlight);
    }

    [Fact]
    public async Task GetHighlights_WhenNoCardCreated_ReturnsHasFlashcardFalse()
    {
        // Arrange
        var (user, _, highlight) = await SeedHighlightAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlights.Should().ContainSingle();
        var item = result.Value.Highlights.First();
        item.Id.Should().Be(highlight.Id);
        item.HasFlashcard.Should().BeFalse();
    }

    [Fact]
    public async Task GetHighlights_WhenCardLinkedToHighlight_ReturnsHasFlashcardTrue()
    {
        // Arrange
        var (user, _, highlight) = await SeedHighlightAsync();

        var card = SpacedRepetitionCard.CreateFromHighlight(
            user.Id,
            highlight.Id,
            "What is an LSM-tree?",
            "A log-structured merge-tree.",
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlights.Should().ContainSingle();
        var item = result.Value.Highlights.First();
        item.Id.Should().Be(highlight.Id);
        item.HasFlashcard.Should().BeTrue();
    }

    [Fact]
    public async Task GetHighlights_EnforcesUserIsolation_DoesNotMarkCardFromAnotherUser()
    {
        // Arrange
        var (userA, _, highlightA) = await SeedHighlightAsync("userA@techdaily.local");
        var (userB, _, _) = await SeedHighlightAsync("userB@techdaily.local");

        // User B has a card referencing highlightA.Id, but belonging to userB
        var cardOfUserB = SpacedRepetitionCard.CreateFromHighlight(
            userB.Id,
            highlightA.Id,
            "User B Card Front",
            "User B Card Back",
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _db.SpacedRepetitionCards.AddAsync(cardOfUserB);
        await _db.SaveChangesAsync();

        // Act: User A queries their highlights
        var resultUserA = await _handler.ExecuteAsync(new GetHighlightsRequest(userA.Id));

        // Assert: User A's highlight has HasFlashcard = false because the card belongs to User B
        resultUserA.IsSuccess.Should().BeTrue();
        resultUserA.Value.Highlights.Should().ContainSingle();
        resultUserA.Value.Highlights.First().HasFlashcard.Should().BeFalse();

        // Act: User B queries their highlights
        var resultUserB = await _handler.ExecuteAsync(new GetHighlightsRequest(userB.Id));

        // Assert: User B's result only contains User B's highlights
        resultUserB.IsSuccess.Should().BeTrue();
        resultUserB.Value.Highlights.Should().NotContain(h => h.Id == highlightA.Id);
    }
}
