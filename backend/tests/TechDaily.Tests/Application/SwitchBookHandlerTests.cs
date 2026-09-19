using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.DailyFocus.SwitchBook;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class SwitchBookHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public SwitchBookHandlerTests()
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

    [Fact]
    public async Task SwitchBook_ShouldActivateNewBook_AndDeactivatePreviousPacer()
    {
        // Arrange
        var user = new User
        {
            Email = "architect@techdaily.io",
            Name = "Senior Architect",
            PasswordHash = "hash123"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        var userId = user.Id;

        var book1 = new DocumentBook
        {
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.SystemDesign,
            Status = ProcessingStatus.Ready,
            TotalChunks = 10
        };

        var book2 = new DocumentBook
        {
            Title = "PostgreSQL 17 Internals",
            Slug = "pg17",
            Category = Category.DatabaseStorage,
            Status = ProcessingStatus.Ready,
            TotalChunks = 15
        };

        await _db.DocumentBooks.AddRangeAsync(book1, book2);

        var chunk1 = new DocumentChunk
        {
            DocumentBookId = book2.Id,
            ChunkOrder = 1,
            ChapterTitle = "MVCC and WAL",
            OriginalTextMarkdown = "WAL context...",
            SummaryMarkdown = "Summary",
            KeyTakeaways = new List<string> { "Takeaway 1" },
            EstimatedReadMinutes = 5
        };
        await _db.DocumentChunks.AddAsync(chunk1);

        var pacer1 = new UserBookPacer
        {
            UserId = userId,
            DocumentBookId = book1.Id,
            CurrentChunkOrder = 3,
            IsActive = true
        };
        await _db.UserBookPacers.AddAsync(pacer1);
        await _db.SaveChangesAsync();

        var validator = new SwitchBookValidator();
        var handler = new SwitchBookHandler(_db, validator);

        // Act
        var result = await handler.ExecuteAsync(new SwitchBookRequest(userId, book2.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BookId.Should().Be(book2.Id);
        result.Value.BookTitle.Should().Be("PostgreSQL 17 Internals");
        result.Value.ChapterTitle.Should().Be("MVCC and WAL");
        result.Value.CurrentChunkOrder.Should().Be(1);
        result.Value.TotalChunks.Should().Be(15);

        var refreshedPacer1 = await _db.UserBookPacers.FirstAsync(p => p.DocumentBookId == book1.Id);
        refreshedPacer1.IsActive.Should().BeFalse();

        var refreshedPacer2 = await _db.UserBookPacers.FirstAsync(p => p.DocumentBookId == book2.Id);
        refreshedPacer2.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchBook_ShouldReturnNotFound_WhenBookIsNotReady()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Title = "Processing Book",
            Slug = "processing-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Processing
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var validator = new SwitchBookValidator();
        var handler = new SwitchBookHandler(_db, validator);

        // Act
        var result = await handler.ExecuteAsync(new SwitchBookRequest(userId, book.Id));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESOURCE_NOT_FOUND");
    }
}
