using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Notes.UpdateHighlight;
using TechDaily.Domain.Enums;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class UpdateHighlightHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly UpdateHighlightValidator _validator;

    public UpdateHighlightHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _validator = new UpdateHighlightValidator();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(Guid UserId, Guid DocumentChunkId, UserHighlight Highlight)> SeedHighlightAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Senior Engineer"
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
            ChapterTitle = "Storage and Retrieval",
            OriginalTextMarkdown = "LSM-trees and B-trees explained in detail."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentChunkId = chunk.Id,
            SelectedText = "LSM-trees and B-trees",
            Note = "Original reflection",
            Tags = new List<string> { "storage" }
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        return (userId, chunk.Id, highlight);
    }

    [Fact]
    public async Task Validator_ValidRequest_ShouldPassValidation()
    {
        var request = new UpdateHighlightRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A concise note",
            new List<string> { "database", "internals" });

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EmptyHighlightIdOrUserId_ShouldFail()
    {
        var req1 = new UpdateHighlightRequest(Guid.Empty, Guid.NewGuid(), "Note");
        var req2 = new UpdateHighlightRequest(Guid.NewGuid(), Guid.Empty, "Note");

        var res1 = await _validator.ValidateAsync(req1);
        var res2 = await _validator.ValidateAsync(req2);

        res1.IsValid.Should().BeFalse();
        res2.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_NoteExceeding2000Characters_ShouldFail()
    {
        var longNote = new string('a', 2001);
        var request = new UpdateHighlightRequest(Guid.NewGuid(), Guid.NewGuid(), longNote);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    [Fact]
    public async Task Validator_MoreThan10Tags_ShouldFail()
    {
        var tags = Enumerable.Range(1, 11).Select(i => $"tag{i}").ToList();
        var request = new UpdateHighlightRequest(Guid.NewGuid(), Guid.NewGuid(), "Note", tags);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tags");
    }

    [Fact]
    public async Task Validator_TagExceeding50Characters_ShouldFail()
    {
        var longTag = new string('x', 51);
        var request = new UpdateHighlightRequest(Guid.NewGuid(), Guid.NewGuid(), "Note", new List<string> { longTag });

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith("Tags"));
    }

    [Fact]
    public async Task UpdateHighlight_WhenOwned_ShouldUpdateNoteAndTags()
    {
        // Arrange
        var (userId, _, highlight) = await SeedHighlightAsync();
        var handler = new UpdateHighlightHandler(_db, _validator);

        var request = new UpdateHighlightRequest(
            highlight.Id,
            userId,
            "Updated deep-dive note on LSM compaction",
            new List<string> { "storage", "compaction", "performance" });

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Highlight.Note.Should().Be("Updated deep-dive note on LSM compaction");
        result.Value.Highlight.Tags.Should().ContainInOrder("storage", "compaction", "performance");
        result.Value.Highlight.BookTitle.Should().Be("Designing Data-Intensive Applications");
        result.Value.Highlight.ChapterTitle.Should().Be("Storage and Retrieval");

        // Verify DB persistence
        var persisted = await _db.UserHighlights.FindAsync(highlight.Id);
        persisted!.Note.Should().Be("Updated deep-dive note on LSM compaction");
        persisted.Tags.Should().ContainInOrder("storage", "compaction", "performance");
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateHighlight_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _, _) = await SeedHighlightAsync();
        var handler = new UpdateHighlightHandler(_db, _validator);

        var request = new UpdateHighlightRequest(Guid.NewGuid(), userId, "Some note");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TechDaily.Application.Common.Error.NotFound);
    }

    [Fact]
    public async Task UpdateHighlight_WhenBelongsToAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var (_, _, highlight) = await SeedHighlightAsync();
        var otherUserId = Guid.NewGuid();
        var handler = new UpdateHighlightHandler(_db, _validator);

        var request = new UpdateHighlightRequest(highlight.Id, otherUserId, "Malicious overwrite");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TechDaily.Application.Common.Error.NotFound);
    }

    [Fact]
    public async Task UpdateHighlight_WhenSoftDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var (userId, _, highlight) = await SeedHighlightAsync();
        highlight.SoftDelete();
        await _db.SaveChangesAsync();

        var handler = new UpdateHighlightHandler(_db, _validator);
        var request = new UpdateHighlightRequest(highlight.Id, userId, "Trying to edit deleted");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TechDaily.Application.Common.Error.NotFound);
    }

    [Fact]
    public async Task UpdateHighlight_WhenValidationFails_ShouldReturnValidationError()
    {
        // Arrange
        var (userId, _, highlight) = await SeedHighlightAsync();
        var handler = new UpdateHighlightHandler(_db, _validator);
        var request = new UpdateHighlightRequest(highlight.Id, userId, new string('z', 2005));

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }
}
