using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.ExportBookMarkdown;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class ExportBookMarkdownHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly ExportBookMarkdownHandler _handler;

    public ExportBookMarkdownHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _handler = new ExportBookMarkdownHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ExportBookMarkdown_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Act
        var result = await _handler.ExecuteAsync(new ExportBookMarkdownRequest(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task ExportBookMarkdown_ShouldGenerateMarkdownWithFrontmatterAndChapters_WhenBookExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "designing-data-intensive-applications",
            AuthorOrSourceUrl = "Martin Kleppmann",
            Category = Category.SystemDesign,
            TotalChunks = 2
        };

        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Reliable, Scalable, and Maintainable Applications",
            SummaryMarkdown = "Core concepts of distributed reliability.",
            KeyTakeaways = new List<string> { "Faults vs Failures", "SLA metrics" }
        };

        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 2,
            ChapterTitle = "Data Models and Query Languages",
            SummaryMarkdown = "Relational vs Document models.",
            KeyTakeaways = new List<string> { "Impedance mismatch", "Schema-on-read" }
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddRangeAsync(chunk1, chunk2);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new ExportBookMarkdownRequest(book.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.FileName.Should().Be("designing-data-intensive-applications-notes.md");
        response.MarkdownContent.Should().Contain("book: \"Designing Data-Intensive Applications\"");
        response.MarkdownContent.Should().Contain("author: \"Martin Kleppmann\"");
        response.MarkdownContent.Should().Contain("total_chapters: 2");
        response.MarkdownContent.Should().Contain("tags: [techdaily, architecture, notes]");
        response.MarkdownContent.Should().Contain("# Designing Data-Intensive Applications");
        response.MarkdownContent.Should().Contain("## Chapter 1: Reliable, Scalable, and Maintainable Applications");
        response.MarkdownContent.Should().Contain("### Executive Summary");
        response.MarkdownContent.Should().Contain("Core concepts of distributed reliability.");
        response.MarkdownContent.Should().Contain("### Key Takeaways");
        response.MarkdownContent.Should().Contain("- Faults vs Failures");
        response.MarkdownContent.Should().Contain("## Chapter 2: Data Models and Query Languages");
    }

    [Fact]
    public async Task ExportBookMarkdown_ShouldIncludeUserHighlightsAndPersonalNotes_WhenHighlightsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "architect@techdaily.io",
            Name = "Senior Architect",
            PasswordHash = "hash"
        };
        var otherUser = new User
        {
            Id = otherUserId,
            Email = "other@techdaily.io",
            Name = "Other Engineer",
            PasswordHash = "hash"
        };
        await _db.Users.AddRangeAsync(user, otherUser);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Database Internals",
            Slug = "database-internals",
            AuthorOrSourceUrl = "Alex Petrov",
            Category = Category.DatabaseStorage,
            TotalChunks = 1
        };

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Storage Engines",
            SummaryMarkdown = "B-Trees and LSM Trees overview.",
            KeyTakeaways = new List<string> { "B-Tree node balancing" }
        };

        var userHighlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentChunkId = chunk.Id,
            SelectedText = "LSM-Trees append writes sequentially to WAL.",
            Note = "Converts random I/O to sequential writes.",
            Tags = new List<string> { "storage", "durability" }
        };

        var otherUserHighlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            DocumentChunkId = chunk.Id,
            SelectedText = "Other user private quote.",
            Note = "Other user note.",
            Tags = new List<string> { "private" }
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.UserHighlights.AddRangeAsync(userHighlight, otherUserHighlight);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new ExportBookMarkdownRequest(book.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var content = result.Value.MarkdownContent;
        result.Value.FileName.Should().Be("database-internals-notes.md");

        // Should include requesting user's highlight and note
        content.Should().Contain("> \"LSM-Trees append writes sequentially to WAL.\"");
        content.Should().Contain("> **Personal Note:** Converts random I/O to sequential writes.");
        content.Should().Contain("`#storage`");
        content.Should().Contain("`#durability`");
        content.Should().Contain("total_highlights: 1");

        // Should NOT include other user's highlight
        content.Should().NotContain("Other user private quote");
        content.Should().NotContain("Other user note");
    }
}
