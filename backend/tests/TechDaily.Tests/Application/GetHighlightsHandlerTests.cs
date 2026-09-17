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
    [Fact]
    public async Task GetHighlights_Pagination_ReturnsCorrectPageAndTotalPages()
    {
        // Arrange: seed user and 20 highlights
        var (user, chunk, _) = await SeedHighlightAsync();
        for (int i = 1; i <= 19; i++)
        {
            _db.UserHighlights.Add(new UserHighlight
            {
                UserId = user.Id,
                DocumentChunkId = chunk.Id,
                SelectedText = $"Highlight excerpt {i}",
                Tags = new List<string> { "paging" },
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }
        await _db.SaveChangesAsync();

        // Act: Page 1 with default PageSize (15)
        var resPage1 = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Page: 1, PageSize: 15));
        // Act: Page 2
        var resPage2 = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Page: 2, PageSize: 15));

        // Assert
        resPage1.IsSuccess.Should().BeTrue();
        resPage1.Value.TotalCount.Should().Be(20);
        resPage1.Value.Page.Should().Be(1);
        resPage1.Value.PageSize.Should().Be(15);
        resPage1.Value.TotalPages.Should().Be(2);
        resPage1.Value.Highlights.Should().HaveCount(15);

        resPage2.IsSuccess.Should().BeTrue();
        resPage2.Value.TotalCount.Should().Be(20);
        resPage2.Value.Page.Should().Be(2);
        resPage2.Value.TotalPages.Should().Be(2);
        resPage2.Value.Highlights.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetHighlights_ComputesGlobalTagCounts_OrderedByCountDescendingThenTagAscending()
    {
        // Arrange
        var (user, chunk, _) = await SeedHighlightAsync(); // already has tags: "storage", "lsm"
        _db.UserHighlights.AddRange(
            new UserHighlight
            {
                UserId = user.Id,
                DocumentChunkId = chunk.Id,
                SelectedText = "Text 1",
                Tags = new List<string> { "storage", "indexing" }
            },
            new UserHighlight
            {
                UserId = user.Id,
                DocumentChunkId = chunk.Id,
                SelectedText = "Text 2",
                Tags = new List<string> { "storage", "btree" }
            }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        // "storage" appears 3 times, others ("btree", "indexing", "lsm") appear 1 time each
        result.Value.TagCounts.Should().NotBeEmpty();
        result.Value.TagCounts[0].Tag.Should().Be("storage");
        result.Value.TagCounts[0].Count.Should().Be(3);

        // Count == 1 items are ordered alphabetically: btree, indexing, lsm
        var tieGroup = result.Value.TagCounts.Skip(1).Select(t => t.Tag).ToList();
        tieGroup.Should().Equal("btree", "indexing", "lsm");
    }

    [Fact]
    public async Task GetHighlights_FilterByTag_ReturnsMatchingHighlightsWhilePreservingGlobalTagCounts()
    {
        // Arrange
        var (user, chunk, _) = await SeedHighlightAsync(); // tags: "storage", "lsm"
        _db.UserHighlights.Add(new UserHighlight
        {
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "PostgreSQL indexing text",
            Tags = new List<string> { "database" }
        });
        await _db.SaveChangesAsync();

        // Act: filter by "database"
        var result = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Tag: "database"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Highlights.Should().ContainSingle().Which.SelectedText.Should().Be("PostgreSQL indexing text");
        // Global tag counts should still contain "storage", "lsm", and "database"
        result.Value.TagCounts.Should().Contain(t => t.Tag == "storage");
        result.Value.TagCounts.Should().Contain(t => t.Tag == "lsm");
        result.Value.TagCounts.Should().Contain(t => t.Tag == "database");
    }

    [Fact]
    public async Task GetHighlights_FilterBySearch_MatchesSelectedTextNoteOrBookTitle()
    {
        // Arrange
        var (user, chunk, _) = await SeedHighlightAsync(); // BookTitle = "Designing Data-Intensive Applications", SelectedText = "LSM-Tree...", Note = "Important storage concept"

        // Act 1: Search by note keyword
        var resNote = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Search: "concept"));
        // Act 2: Search by book title keyword
        var resBook = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Search: "intensive"));
        // Act 3: Search with no match
        var resNone = await _handler.ExecuteAsync(new GetHighlightsRequest(user.Id, Search: "nonexistent_token_xyz"));

        // Assert
        resNote.IsSuccess.Should().BeTrue();
        resNote.Value.TotalCount.Should().Be(1);

        resBook.IsSuccess.Should().BeTrue();
        resBook.Value.TotalCount.Should().Be(1);

        resNone.IsSuccess.Should().BeTrue();
        resNone.Value.TotalCount.Should().Be(0);
        resNone.Value.TotalPages.Should().Be(0);
    }
}
