using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DeleteBook;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class DeleteBookHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public DeleteBookHandlerTests()
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
    public async Task DeleteBook_ShouldSoftDeleteBookAndItsChunks()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "Architecture Handbook",
            Slug = "architecture-handbook",
            Category = Category.SystemDesign,
            SourceType = SourceType.MarkdownSeries,
            TotalChunks = 1
        };

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookId,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1: Scalability",
            OriginalTextMarkdown = "# Scalability\nHigh throughput design.",
            SummaryMarkdown = "High throughput.",
            KeyTakeaways = new() { "Horizontal scaling" },
            MicroQuiz = new MicroQuizVo
            {
                Question = "What is horizontal scaling?",
                Options = new() { "Adding more nodes", "Upgrading CPU", "Using SQLite", "None" },
                AnswerIndex = 0,
                Explanation = "Adding nodes is horizontal scaling."
            }
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var handler = new DeleteBookHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new DeleteBookRequest(bookId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();

        // Query with ignore query filters to verify soft-delete
        var deletedBook = await _db.DocumentBooks.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == bookId);
        deletedBook.Should().NotBeNull();
        deletedBook!.IsDeleted.Should().BeTrue();

        var deletedChunk = await _db.DocumentChunks.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.DocumentBookId == bookId);
        deletedChunk.Should().NotBeNull();
        deletedChunk!.IsDeleted.Should().BeTrue();

        // Standard query should return null (filtered out)
        var queryBook = await _db.DocumentBooks.FirstOrDefaultAsync(b => b.Id == bookId);
        queryBook.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBook_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var handler = new DeleteBookHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new DeleteBookRequest(Guid.NewGuid()));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }
}
