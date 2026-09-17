using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Library.ImportRemotePdf;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class ImportRemotePdfHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly List<string> _tempFilesToCleanup = new();

    public ImportRemotePdfHandlerTests()
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
        foreach (var file in _tempFilesToCleanup)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
            }
            catch { }
        }
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ImportRemotePdf_ShouldDownloadStreamAndEnqueue_WhenValid()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new ImportRemotePdfValidator();

        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Test PDF Content"))
            };
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            resp.Content.Headers.ContentLength = 25;
            return resp;
        });

        using var httpClient = new HttpClient(fakeHandler);
        var handler = new ImportRemotePdfHandler(httpClient, _db, mockQueue, validator);

        var request = new ImportRemotePdfRequest(
            PdfUrl: "https://example.com/books/atomic-habits.pdf",
            Title: "Thói Quen Nguyên Tử",
            Category: Category.EngineeringCraft,
            Language: "vi"
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("Thói Quen Nguyên Tử");
        result.Value.Book.Category.Should().Be(Category.EngineeringCraft);
        result.Value.Book.SourceType.Should().Be(SourceType.PdfBook);
        result.Value.Book.AuthorOrSourceUrl.Should().Be("https://example.com/books/atomic-habits.pdf");
        result.Value.Book.Status.Should().Be(ProcessingStatus.Processing);

        mockQueue.EnqueuedJobs.Should().HaveCount(1);
        var job = mockQueue.EnqueuedJobs.First();
        job.BookId.Should().Be(result.Value.Book.Id);
        job.DocumentTitle.Should().Be("Thói Quen Nguyên Tử");
        job.Category.Should().Be(Category.EngineeringCraft);
        job.Language.Should().Be("vi");

        _tempFilesToCleanup.Add(job.TempFilePath);
        File.Exists(job.TempFilePath).Should().BeTrue();
        var fileBytes = await File.ReadAllBytesAsync(job.TempFilePath);
        System.Text.Encoding.UTF8.GetString(fileBytes).Should().Contain("%PDF-1.4 Test PDF Content");

        var dbBook = await _db.DocumentBooks.FirstOrDefaultAsync(b => b.Id == result.Value.Book.Id);
        dbBook.Should().NotBeNull();
        dbBook!.Category.Should().Be(Category.EngineeringCraft);
    }

    [Theory]
    [InlineData("http://127.0.0.1/sensitive-data.pdf")]
    [InlineData("http://localhost:5000/internal-report.pdf")]
    [InlineData("http://10.0.0.1/private-book.pdf")]
    [InlineData("http://192.168.1.1/router-manual.pdf")]
    [InlineData("http://backend:5000/metrics.pdf")]
    public async Task ImportRemotePdf_ShouldRejectSsrf_WhenUrlPointsToInternalOrPrivateHost(string internalUrl)
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new ImportRemotePdfValidator();
        using var httpClient = new HttpClient();
        var handler = new ImportRemotePdfHandler(httpClient, _db, mockQueue, validator);

        var request = new ImportRemotePdfRequest(
            PdfUrl: internalUrl,
            Title: "Restricted Document",
            Category: Category.BackendDotNet
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Security.SsrfBlocked");
        mockQueue.EnqueuedJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportRemotePdf_ShouldReject_WhenContentLengthHeaderExceeds350Mb()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new ImportRemotePdfValidator();

        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[100])
            };
            resp.Content.Headers.ContentLength = 400_000_000; // 400 MB
            return resp;
        });

        using var httpClient = new HttpClient(fakeHandler);
        var handler = new ImportRemotePdfHandler(httpClient, _db, mockQueue, validator);

        var request = new ImportRemotePdfRequest(
            PdfUrl: "https://example.com/huge-dataset.pdf",
            Title: "Giant PDF Document",
            Category: Category.DatabaseStorage
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RemotePdf.FileTooLarge");
        result.Error.Message.Should().Contain("350 MB");
        mockQueue.EnqueuedJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportRemotePdf_ShouldReject_WhenStreamedContentExceeds350Mb()
    {
        // Arrange: Stream that generates slightly more than 367,001,600 bytes without Content-Length
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new ImportRemotePdfValidator();

        var fakeHandler = new FakeHttpMessageHandler(req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new InfiniteStream(maxBytes: 368_000_000))
            };
            // No Content-Length header to test streaming guardrail
            return resp;
        });

        using var httpClient = new HttpClient(fakeHandler);
        var handler = new ImportRemotePdfHandler(httpClient, _db, mockQueue, validator);

        var request = new ImportRemotePdfRequest(
            PdfUrl: "https://example.com/unbounded-stream.pdf",
            Title: "Unbounded Stream Document",
            Category: Category.SystemDesign
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RemotePdf.FileTooLarge");
        mockQueue.EnqueuedJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportRemotePdf_ShouldFailValidation_WhenRequestIsInvalid()
    {
        // Arrange
        var mockQueue = new MockPdfIngestionQueue();
        var validator = new ImportRemotePdfValidator();
        using var httpClient = new HttpClient();
        var handler = new ImportRemotePdfHandler(httpClient, _db, mockQueue, validator);

        var request = new ImportRemotePdfRequest(
            PdfUrl: "not-a-valid-url",
            Title: "",
            Category: (Category)999
        );

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    private class InfiniteStream : Stream
    {
        private readonly long _maxBytes;
        private long _bytesRead = 0;

        public InfiniteStream(long maxBytes) => _maxBytes = maxBytes;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _maxBytes;
        public override long Position { get => _bytesRead; set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long remaining = _maxBytes - _bytesRead;
            if (remaining <= 0) return 0;
            int toRead = (int)Math.Min(count, remaining);
            Array.Clear(buffer, offset, toRead);
            _bytesRead += toRead;
            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
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
            foreach (var job in EnqueuedJobs)
            {
                yield return job;
            }
            await Task.CompletedTask;
        }
    }
}
