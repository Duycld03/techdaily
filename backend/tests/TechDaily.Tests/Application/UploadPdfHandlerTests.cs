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
    public async Task UploadPdf_ShouldCreateBookAndChunks_WhenValidPdfProvided()
    {
        // Arrange
        var mockExtractor = new MockPdfExtractor(new PdfExtractionResult(
            DocumentTitle: "Test Architecture PDF",
            TotalPages: 5,
            Slices: new()
            {
                new(1, "Chapter 1: Intro", "# Chapter 1\nIntroductory content.", 2, new() { "Key point 1" }),
                new(2, "Chapter 2: Scaling", "# Chapter 2\nScaling mechanics.", 3, new() { "Key point 2" })
            }
        ));

        var validator = new UploadPdfValidator();
        var handler = new UploadPdfHandler(_db, mockExtractor, validator);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
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
        result.Value.Book.TotalChunks.Should().Be(2);

        var savedBook = await _db.DocumentBooks.Include(b => b.Chunks).FirstOrDefaultAsync(b => b.Id == result.Value.Book.Id);
        savedBook.Should().NotBeNull();
        savedBook!.Chunks.Should().HaveCount(2);
        savedBook.Chunks.First().ChapterTitle.Should().Be("Chapter 1: Intro");
        savedBook.Chunks.First().MicroQuiz.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadPdf_ShouldFailValidation_WhenFileIsNotPdf()
    {
        // Arrange
        var mockExtractor = new MockPdfExtractor(new PdfExtractionResult("Test", 1, new()));
        var validator = new UploadPdfValidator();
        var handler = new UploadPdfHandler(_db, mockExtractor, validator);

        using var memoryStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var request = new UploadPdfRequest(
            FileStream: memoryStream,
            FileName: "document.docx",
            FileLength: 1024,
            Title: "Invalid Doc",
            Category: Category.BackendDotNet
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    private class MockPdfExtractor : IPdfExtractor
    {
        private readonly PdfExtractionResult _result;

        public MockPdfExtractor(PdfExtractionResult result)
        {
            _result = result;
        }

        public Task<PdfExtractionResult> ExtractSlicesAsync(
            Stream pdfStream,
            string? customTitle = null,
            int maxPages = 800,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }
}
