using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBookSlice;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetBookByIdAndSliceHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public GetBookByIdAndSliceHandlerTests()
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
    public async Task GetBookById_ShouldReturnLightweightTOC_WithoutHeavyMarkdown()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "ASP.NET Core Architecture",
            Slug = "aspnet-core-architecture",
            Category = Category.BackendDotNet,
            Status = ProcessingStatus.Ready,
            TotalChunks = 2
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk1 = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1: Middleware",
            OriginalTextMarkdown = "# Heavy markdown content exceeding several kilobytes...",
            SummaryMarkdown = "Middleware summary",
            EstimatedReadMinutes = 4,
            IsAiFormatted = true
        };
        var chunk2 = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 2,
            ChapterTitle = "Chapter 2: Dependency Injection",
            OriginalTextMarkdown = "# Another heavy markdown content block...",
            SummaryMarkdown = "DI summary",
            EstimatedReadMinutes = 6,
            IsAiFormatted = false
        };
        await _db.DocumentChunks.AddRangeAsync(chunk1, chunk2);
        await _db.SaveChangesAsync();

        var handler = new GetBookByIdHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetBookByIdRequest(book.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("ASP.NET Core Architecture");
        result.Value.Book.Chunks.Should().HaveCount(2);

        // Lightweight TOC check: Titles and Orders present, heavy markdown empty
        var firstTOC = result.Value.Book.Chunks[0];
        firstTOC.ChunkOrder.Should().Be(1);
        firstTOC.ChapterTitle.Should().Be("Chapter 1: Middleware");
        firstTOC.EstimatedReadMinutes.Should().Be(4);
        firstTOC.IsAiFormatted.Should().BeTrue();
        firstTOC.OriginalTextMarkdown.Should().BeEmpty();
        firstTOC.SummaryMarkdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookById_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var handler = new GetBookByIdHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetBookByIdRequest(Guid.NewGuid()));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task GetBookSlice_ShouldReturnFullMarkdown_WhenSliceExists()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Vue 3 Internals",
            Slug = "vue-3-internals",
            Category = Category.FrontendWeb,
            Status = ProcessingStatus.Ready,
            TotalChunks = 1
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Reactivity Engine",
            OriginalTextMarkdown = "# Deep Dive into Proxy and Reflect",
            SummaryMarkdown = "Proxy summary",
            KeyTakeaways = new List<string> { "Takeaway 1", "Takeaway 2" },
            EstimatedReadMinutes = 5,
            IsAiFormatted = true
        };
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var handler = new GetBookSliceHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetBookSliceRequest(book.Id, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slice.ChapterTitle.Should().Be("Reactivity Engine");
        result.Value.Slice.OriginalTextMarkdown.Should().Be("# Deep Dive into Proxy and Reflect");
        result.Value.Slice.SummaryMarkdown.Should().Be("Proxy summary");
        result.Value.Slice.KeyTakeaways.Should().Contain("Takeaway 1");
    }

    [Fact]
    public async Task GetBookSlice_ShouldReturnNotFound_WhenSliceDoesNotExist()
    {
        // Arrange
        var handler = new GetBookSliceHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetBookSliceRequest(Guid.NewGuid(), 99));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RESOURCE_NOT_FOUND");
    }
}
