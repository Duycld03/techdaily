using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.AskBook;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class AskBookHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly FakeEmbeddingService _fakeEmbeddingService;
    private readonly FakeBookQAService _fakeBookQAService;
    private readonly AskBookValidator _validator;

    public AskBookHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _fakeEmbeddingService = new FakeEmbeddingService();
        _fakeBookQAService = new FakeBookQAService();
        _validator = new AskBookValidator();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AskBook_WithInvalidShortQuestion_ShouldFailValidation()
    {
        // Arrange
        var handler = new AskBookHandler(_db, _fakeEmbeddingService, _fakeBookQAService, _validator);
        var request = new AskBookRequest(Guid.NewGuid(), "hi"); // Less than 3 chars

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task AskBook_WithQuestionExceeding300Chars_ShouldFailValidation()
    {
        // Arrange
        var handler = new AskBookHandler(_db, _fakeEmbeddingService, _fakeBookQAService, _validator);
        var request = new AskBookRequest(Guid.NewGuid(), new string('a', 301)); // Exceeds 300 chars

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task AskBook_WhenBookDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = new AskBookHandler(_db, _fakeEmbeddingService, _fakeBookQAService, _validator);
        var request = new AskBookRequest(Guid.NewGuid(), "How does write skew happen?");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(Error.NotFound.Code);
    }

    [Fact]
    public async Task AskBook_WhenBookExists_ShouldCallBookQAServiceAndReturnGroundedAnswerWithCitations()
    {
        // Arrange
        var book = new DocumentBook
        {
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage,
            Status = ProcessingStatus.Ready,
            TotalChunks = 2
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk1 = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Reliability, Scalability, and Maintainability",
            SummaryMarkdown = "Summary of chapter 1.",
            OriginalTextMarkdown = "Full text of chapter 1."
        };
        await _db.DocumentChunks.AddAsync(chunk1);
        await _db.SaveChangesAsync();

        _fakeBookQAService.AnswerToReturn = "Reliability means making systems work correctly even when things go wrong.";

        var handler = new AskBookHandler(_db, _fakeEmbeddingService, _fakeBookQAService, _validator);
        var request = new AskBookRequest(book.Id, "What is reliability?", chunk1.Id);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AnswerMarkdown.Should().Contain("Reliability means");
        result.Value.Citations.Should().NotBeEmpty();
        result.Value.Citations.First().ChapterTitle.Should().Be("Reliability, Scalability, and Maintainability");
    }

    public class FakeEmbeddingService : IEmbeddingService
    {
        public Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Result<Vector>>(new Vector(new float[768]));
        }

        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            var list = texts.Select(_ => new Vector(new float[768])).ToList();
            return Task.FromResult<Result<List<Vector>>>(list);
        }
    }

    public class FakeBookQAService : IBookQAService
    {
        public string AnswerToReturn { get; set; } = "Default fake answer.";

        public Task<Result<string>> AnswerQuestionAsync(
            string bookTitle,
            string question,
            List<(int ChunkOrder, string ChapterTitle, string Text)> contexts,
            string locale = "en",
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Result<string>>(AnswerToReturn);
        }
    }
}
