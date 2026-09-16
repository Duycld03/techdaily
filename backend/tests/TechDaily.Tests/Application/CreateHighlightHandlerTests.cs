using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class CreateHighlightHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly CreateHighlightValidator _validator;

    public CreateHighlightHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _validator = new CreateHighlightValidator();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(Guid UserId, Guid DocumentChunkId)> SeedUserAndChunkAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid():N}@test.com",
            Name = "Test User"
        };
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = $"ddia-{Guid.NewGuid():N}"
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Storage and Retrieval",
            OriginalTextMarkdown = "Sample content for testing highlights."
        };

        await _db.Users.AddAsync(user);
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        return (user.Id, chunk.Id);
    }

    [Fact]
    public async Task CreateHighlight_WhenNew_ShouldCreateAndReturnHighlight()
    {
        // Arrange
        var (userId, chunkId) = await SeedUserAndChunkAsync();
        var handler = new CreateHighlightHandler(_db, _validator);

        var request = new CreateHighlightRequest(
            userId,
            chunkId,
            "  Event-driven architecture decouples producers and consumers.  ",
            "Core concept",
            new List<string> { "architecture", "eda" });

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlight.Should().NotBeNull();
        result.Value.Highlight.Id.Should().NotBeEmpty();
        result.Value.Highlight.DocumentChunkId.Should().Be(chunkId);
        result.Value.Highlight.SelectedText.Should().Be("Event-driven architecture decouples producers and consumers.");
        result.Value.Highlight.Note.Should().Be("Core concept");
        result.Value.Highlight.Tags.Should().BeEquivalentTo(new[] { "architecture", "eda" });

        var inDb = await _db.UserHighlights.FirstOrDefaultAsync(h => h.Id == result.Value.Highlight.Id);
        inDb.Should().NotBeNull();
        inDb!.SelectedText.Should().Be("Event-driven architecture decouples producers and consumers.");
    }

    [Fact]
    public async Task CreateHighlight_WhenDuplicateExists_ShouldReturnExistingHighlightWithSameId()
    {
        // Arrange
        var (userId, chunkId) = await SeedUserAndChunkAsync();
        var selectedText = "PostgreSQL MVCC uses vacuuming to reclaim dead tuples.";

        var existingHighlight = new UserHighlight
        {
            UserId = userId,
            DocumentChunkId = chunkId,
            SelectedText = selectedText,
            Note = "Original note",
            Tags = new List<string> { "postgres" }
        };
        await _db.UserHighlights.AddAsync(existingHighlight);
        await _db.SaveChangesAsync();

        var handler = new CreateHighlightHandler(_db, _validator);

        // Act - Request with surrounding whitespace in selectedText
        var request = new CreateHighlightRequest(
            userId,
            chunkId,
            $"   {selectedText}   ");

        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlight.Id.Should().Be(existingHighlight.Id);
        result.Value.Highlight.SelectedText.Should().Be(selectedText);

        // Verify no duplicate record was created in the database
        var totalHighlights = await _db.UserHighlights
            .CountAsync(h => h.UserId == userId && h.DocumentChunkId == chunkId);
        totalHighlights.Should().Be(1);
    }

    [Fact]
    public async Task CreateHighlight_WhenDuplicateFoundWithNewNoteAndTags_ShouldUpdateExistingHighlightAndMergeTags()
    {
        // Arrange
        var (userId, chunkId) = await SeedUserAndChunkAsync();
        var selectedText = "Raft consensus requires a majority quorum of (n/2)+1 nodes.";

        var existingHighlight = new UserHighlight
        {
            UserId = userId,
            DocumentChunkId = chunkId,
            SelectedText = selectedText,
            Note = "Initial draft note",
            Tags = new List<string> { "distributed-systems", "consensus" }
        };
        await _db.UserHighlights.AddAsync(existingHighlight);
        await _db.SaveChangesAsync();

        var handler = new CreateHighlightHandler(_db, _validator);

        // Act - Update note and add new tags (with overlapping tag in different case)
        var request = new CreateHighlightRequest(
            userId,
            chunkId,
            selectedText,
            Note: "Refined note for interview prep",
            Tags: new List<string> { "CONSENSUS", "raft", "fault-tolerance" });

        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlight.Id.Should().Be(existingHighlight.Id);
        result.Value.Highlight.Note.Should().Be("Refined note for interview prep");
        result.Value.Highlight.Tags.Should().Contain(new[] { "distributed-systems", "consensus", "raft", "fault-tolerance" });

        // Verify DB persistence
        var updatedDb = await _db.UserHighlights.FindAsync(existingHighlight.Id);
        updatedDb.Should().NotBeNull();
        updatedDb!.Note.Should().Be("Refined note for interview prep");
        updatedDb.Tags.Should().Contain(new[] { "distributed-systems", "consensus", "raft", "fault-tolerance" });
        updatedDb.UpdatedAt.Should().NotBeNull();

        var totalHighlights = await _db.UserHighlights
            .CountAsync(h => h.UserId == userId && h.DocumentChunkId == chunkId);
        totalHighlights.Should().Be(1);
    }

    [Fact]
    public async Task CreateHighlight_WhenMatchingHighlightIsDeleted_ShouldCreateNewHighlight()
    {
        // Arrange
        var (userId, chunkId) = await SeedUserAndChunkAsync();
        var selectedText = "Soft-deleted highlight text";

        var deletedHighlight = new UserHighlight
        {
            UserId = userId,
            DocumentChunkId = chunkId,
            SelectedText = selectedText,
            Note = "Old deleted note",
            IsDeleted = true
        };
        await _db.UserHighlights.AddAsync(deletedHighlight);
        await _db.SaveChangesAsync();

        var handler = new CreateHighlightHandler(_db, _validator);

        // Act
        var request = new CreateHighlightRequest(userId, chunkId, selectedText, "Active note");
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlight.Id.Should().NotBe(deletedHighlight.Id);

        var activeHighlights = await _db.UserHighlights
            .CountAsync(h => h.UserId == userId && h.DocumentChunkId == chunkId && !h.IsDeleted);
        activeHighlights.Should().Be(1);
    }
}
