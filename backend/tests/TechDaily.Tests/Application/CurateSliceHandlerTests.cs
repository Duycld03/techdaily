using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.CurateSlice;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class CurateSliceHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public CurateSliceHandlerTests()
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

    private class FakeAiFormatter : IAiMarkdownFormatter
    {
        public int CallCount = 0;
        public int DelayMs = 0;

        public async Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
            string rawText,
            string chapterTitle,
            string language = "en",
            Category? category = null,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            return new AiFormattedSliceResult(
                FormattedMarkdown: $"# {chapterTitle}\n\n> [!NOTE]\n> Curated context.",
                SummaryMarkdown: $"Summary for {chapterTitle}",
                KeyTakeaways: new List<string> { "Takeaway 1", "Takeaway 2" },
                EstimatedReadMinutes: 5,
                ScenarioDrill: new AiScenarioDrillVo(
                    QuestionText: "Scenario drill question?",
                    Options: new List<string> { "A", "B", "C", "D" },
                    CorrectOptionIndex: 1,
                    ExplanationMarkdown: "Option B is correct.",
                    ExpectedKeyPoints: new List<string> { "Key point 1" }
                )
            );
        }
    }

    [Fact]
    public async Task CurateSlice_ShouldCurateAndSaveToDb_WhenChunkNotYetFormatted()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "ASP.NET Core Guide",
            Slug = "aspnet-core-guide",
            Category = Category.BackendDotNet,
            SourceType = SourceType.PdfBook,
            TotalChunks = 1
        };

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookId,
            ChunkOrder = 1,
            ChapterTitle = "Dependency Injection",
            OriginalTextMarkdown = "Raw DI text...",
            SummaryMarkdown = "Raw summary",
            KeyTakeaways = new List<string>(),
            IsAiFormatted = false
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var fakeFormatter = new FakeAiFormatter();
        var handler = new CurateSliceHandler(_db, fakeFormatter, NullLogger<CurateSliceHandler>.Instance);

        // Act
        var result = await handler.ExecuteAsync(new CurateSliceRequest(bookId, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        fakeFormatter.CallCount.Should().Be(1);
        result.Value.Chunk.IsAiFormatted.Should().BeTrue();
        result.Value.Chunk.OriginalTextMarkdown.Should().Contain("> [!NOTE]");

        // Verify entity updated in DB
        var updatedChunk = await _db.DocumentChunks.FirstAsync(c => c.DocumentBookId == bookId && c.ChunkOrder == 1);
        updatedChunk.IsAiFormatted.Should().BeTrue();

        var question = await _db.InterviewQuestions.FirstOrDefaultAsync(q => q.DocumentChunkId == chunk.Id);
        question.Should().NotBeNull();
        question!.QuestionText.Should().Be("Scenario drill question?");
    }

    [Fact]
    public async Task CurateSlice_ShouldNotCallAiFormatter_WhenChunkAlreadyFormatted()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "ASP.NET Core Guide",
            Slug = "aspnet-core-guide-" + Guid.NewGuid(),
            Category = Category.BackendDotNet,
            SourceType = SourceType.PdfBook,
            TotalChunks = 1
        };

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookId,
            ChunkOrder = 1,
            ChapterTitle = "Routing Basics",
            OriginalTextMarkdown = "# Routing Basics\nAlready formatted.",
            SummaryMarkdown = "Already formatted.",
            KeyTakeaways = new List<string> { "Takeaway 1" },
            IsAiFormatted = true
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var fakeFormatter = new FakeAiFormatter();
        var handler = new CurateSliceHandler(_db, fakeFormatter, NullLogger<CurateSliceHandler>.Instance);

        // Act
        var result = await handler.ExecuteAsync(new CurateSliceRequest(bookId, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        fakeFormatter.CallCount.Should().Be(0, "AI formatter should never be invoked when IsAiFormatted is already true");
        result.Value.Chunk.IsAiFormatted.Should().BeTrue();
    }

    [Fact]
    public async Task CurateSlice_ConcurrentCalls_ShouldCallAiFormatterExactlyOnce_DueToLocking()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "ASP.NET Core Concurrency",
            Slug = "aspnet-core-concurrency-" + Guid.NewGuid(),
            Category = Category.BackendDotNet,
            SourceType = SourceType.PdfBook,
            TotalChunks = 1
        };

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookId,
            ChunkOrder = 1,
            ChapterTitle = "Concurrency & Locks",
            OriginalTextMarkdown = "Raw concurrency text...",
            SummaryMarkdown = "Raw summary",
            KeyTakeaways = new List<string>(),
            IsAiFormatted = false
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var fakeFormatter = new FakeAiFormatter { DelayMs = 100 };

        // Create two handler instances simulating concurrent requests from two threads/connections
        var handler1 = new CurateSliceHandler(_db, fakeFormatter, NullLogger<CurateSliceHandler>.Instance);
        var handler2 = new CurateSliceHandler(_db, fakeFormatter, NullLogger<CurateSliceHandler>.Instance);

        // Act: Invoke concurrently
        var task1 = handler1.ExecuteAsync(new CurateSliceRequest(bookId, 1));
        var task2 = handler2.ExecuteAsync(new CurateSliceRequest(bookId, 1));

        var results = await Task.WhenAll(task1, task2);

        // Assert: Both succeed
        results[0].IsSuccess.Should().BeTrue();
        results[1].IsSuccess.Should().BeTrue();

        // AI formatter must have been called EXACTLY ONCE due to double-checked locking!
        fakeFormatter.CallCount.Should().Be(1, "Double-checked locking must prevent redundant AI formatting calls");
    }

    [Fact]
    public async Task CurateSlice_ShouldSaveFormattedMarkdownToOriginalTextMarkdown_WhenBookIsEngineeringCraft()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "Thói Quen Nguyên Tử",
            Slug = "thoi-quen-nguyen-tu",
            Category = Category.EngineeringCraft,
            SourceType = SourceType.PdfBook,
            TotalChunks = 1
        };

        const string verbatimProse = "Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt. Dave Brailsford và triết lý tích lũy lợi ích cận biên 1% mỗi ngày.";

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = bookId,
            ChunkOrder = 1,
            ChapterTitle = "Sức Mạnh Của Những Thay Đổi Nhỏ",
            OriginalTextMarkdown = verbatimProse,
            SummaryMarkdown = "Raw summary",
            KeyTakeaways = new List<string>(),
            IsAiFormatted = false
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.SaveChangesAsync();

        var fakeFormatter = new FakeAiFormatter();
        var handler = new CurateSliceHandler(_db, fakeFormatter, NullLogger<CurateSliceHandler>.Instance);

        // Act
        var result = await handler.ExecuteAsync(new CurateSliceRequest(bookId, 1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Chunk.IsAiFormatted.Should().BeTrue();

        var expectedFormatted = $"# {chunk.ChapterTitle}\n\n> [!NOTE]\n> Curated context.";

        // AI-restored clean formatted markdown is saved to OriginalTextMarkdown across all categories including EngineeringCraft
        result.Value.Chunk.OriginalTextMarkdown.Should().Be(expectedFormatted);
        result.Value.Chunk.OriginalTextMarkdown.Should().Contain("> [!NOTE]");

        // Auxiliary fields must be populated
        result.Value.Chunk.SummaryMarkdown.Should().Contain("Summary for Sức Mạnh Của Những Thay Đổi Nhỏ");
        result.Value.Chunk.KeyTakeaways.Should().HaveCount(2);

        // Database entity verification
        var dbChunk = await _db.DocumentChunks.FirstAsync(c => c.DocumentBookId == bookId && c.ChunkOrder == 1);
        dbChunk.OriginalTextMarkdown.Should().Be(expectedFormatted);
        dbChunk.SummaryMarkdown.Should().Contain("Summary for Sức Mạnh Của Những Thay Đổi Nhỏ");
        dbChunk.IsAiFormatted.Should().BeTrue();
    }
}
