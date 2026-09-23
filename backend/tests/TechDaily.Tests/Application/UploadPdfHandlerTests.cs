using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Library.UploadPdf;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class UploadPdfHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public UploadPdfHandlerTests()
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
    public async Task UploadPdf_ShouldCreateBookAndEnqueue_WhenValidPdfProvided()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new UploadPdfValidator();
        var handler = new UploadPdfHandler(_db, mockQueue, validator);

        using var memoryStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 sample content for testing"));
        var request = new UploadPdfRequest(
            FileStream: memoryStream,
            FileName: "architecture-book.pdf",
            FileLength: 1024,
            Title: "Custom Architecture Title",
            Category: Category.SystemDesign,
            Language: "en"
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("Custom Architecture Title");
        result.Value.Book.Status.Should().Be(ProcessingStatus.Processing);

        mockQueue.EnqueuedJobs.Should().HaveCount(1);
        mockQueue.EnqueuedJobs.First().BookId.Should().Be(result.Value.Book.Id);

        var savedBook = await _db.DocumentBooks.FirstOrDefaultAsync(b => b.Id == result.Value.Book.Id);
        savedBook.Should().NotBeNull();
        savedBook!.Status.Should().Be(ProcessingStatus.Processing);
    }

    [Fact]
    public async Task UploadPdf_ShouldReturnInvalidFormat_WhenMagicBytesAreNotPdf()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new UploadPdfValidator();
        var handler = new UploadPdfHandler(_db, mockQueue, validator);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var request = new UploadPdfRequest(
            FileStream: memoryStream,
            FileName: "fake.pdf",
            FileLength: 5,
            Title: "Fake PDF",
            Category: Category.SystemDesign,
            Language: "en"
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_PDF_FORMAT");
    }

    [Fact]
    public async Task UploadPdf_ShouldFailValidation_WhenFileIsNotPdf()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new UploadPdfValidator();
        var handler = new UploadPdfHandler(_db, mockQueue, validator);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadPdfRequest(
            FileStream: memoryStream,
            FileName: "document.docx",
            FileLength: 1024,
            Title: "Test",
            Category: Category.SystemDesign,
            Language: "en"
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    private class MockPdfIngestionQueue : IPdfIngestionQueue
    {
        public List<PdfIngestJob> EnqueuedJobs { get; } = new();

        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
        {
            EnqueuedJobs.Add(job);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<PdfIngestJob> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var job in EnqueuedJobs)
            {
                yield return job;
            }
        }
    }
}
