using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Workers;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class PdfIngestionWorkerTests : IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly string _connectionString;
    private readonly List<string> _tempFilesToCleanup = new();

    public PdfIngestionWorkerTests()
    {
        var dbName = "pdf_worker_" + Guid.NewGuid().ToString("N");
        _connectionString = $"DataSource=file:{dbName}?mode=memory&cache=shared";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        using var initDb = new TechDailyDbContext(options);
        initDb.Database.EnsureCreated();
    }

    public void Dispose()
    {
        foreach (var file in _tempFilesToCleanup)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }
        _keepAliveConnection.Dispose();
    }

    [Fact]
    public async Task PdfIngestionWorker_ShouldPreserveVerbatimProse_WhenBookIsEngineeringCraft()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "Thói Quen Nguyên Tử",
            Slug = "thoi-quen-nguyen-tu-" + Guid.NewGuid().ToString().Substring(0, 6),
            Category = Category.EngineeringCraft,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = "https://example.com/atomic-habits.pdf",
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            TotalChunks = 0
        };

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        using (var setupDb = new TechDailyDbContext(options))
        {
            await setupDb.DocumentBooks.AddAsync(book);
            await setupDb.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"test-pdf-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 mock pdf"));
        _tempFilesToCleanup.Add(tempPdfPath);

        const string verbatimOriginal = "Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt. Tôi gãy xương mũi, rạn hộp sọ và tụ máu mắt.";

        var fakeExtractor = new FakePdfExtractor(new PdfExtractionResult(
            DocumentTitle: "Thói Quen Nguyên Tử",
            TotalPages: 112,
            Slices: new List<ExtractedPdfSlice>
            {
                new(
                    Order: 1,
                    ChapterTitle: "Phần Giới Thiệu",
                    ContentMarkdown: verbatimOriginal,
                    EstimatedReadMinutes: 4,
                    KeyTakeaways: new List<string> { "Takeaway from extraction" }
                )
            }
        ));

        var fakeFormatter = new FakeAiFormatter();
        var queue = new PdfIngestionQueue();

        var services = new ServiceCollection();
        services.AddDbContext<TechDailyDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<ITechDailyDbContext>(sp => sp.GetRequiredService<TechDailyDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => fakeFormatter);
        services.AddScoped<ILookAheadBufferService>(_ => null!);
        var serviceProvider = services.BuildServiceProvider();

        var worker = new PdfIngestionWorker(queue, serviceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var workerTask = worker.StartAsync(cts.Token);

        // Act: Enqueue ingestion job
        await queue.EnqueueAsync(new PdfIngestJob(bookId, tempPdfPath, book.Title, Category.EngineeringCraft, "vi"), cts.Token);

        // Wait until processed
        DocumentChunk? chunk = null;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new TechDailyDbContext(options);
            var dbBook = await pollDb.DocumentBooks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (dbBook?.Status == ProcessingStatus.Ready || dbBook?.Status == ProcessingStatus.Failed)
            {
                chunk = await pollDb.DocumentChunks.AsNoTracking().FirstOrDefaultAsync(c => c.DocumentBookId == bookId && c.ChunkOrder == 1);
                if (chunk != null && chunk.IsAiFormatted) break;
            }
            await Task.Delay(100);
        }

        await worker.StopAsync(CancellationToken.None);

        // Assert
        chunk.Should().NotBeNull();
        chunk!.OriginalTextMarkdown.Should().Be(verbatimOriginal, "OriginalTextMarkdown must remain 100% verbatim for Category.EngineeringCraft!");
        chunk.OriginalTextMarkdown.Should().NotContain("AI Condensed Summary");
        chunk.SummaryMarkdown.Should().Contain("Summary for Phần Giới Thiệu");
        chunk.IsAiFormatted.Should().BeTrue();
    }

    private class FakePdfExtractor : IPdfExtractor
    {
        private readonly PdfExtractionResult _result;
        public FakePdfExtractor(PdfExtractionResult result) => _result = result;

        public Task<PdfExtractionResult> ExtractSlicesAsync(
            Stream pdfStream,
            string? customTitle = null,
            int maxPages = int.MaxValue,
            IProgress<PdfExtractionProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private class FakeAiFormatter : IAiMarkdownFormatter
    {
        public Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
            string rawText,
            string chapterTitle,
            string language = "en",
            Category? category = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AiFormattedSliceResult>.Success(new AiFormattedSliceResult(
                FormattedMarkdown: $"# {chapterTitle}\n\nAI Condensed Summary",
                SummaryMarkdown: $"Summary for {chapterTitle}",
                KeyTakeaways: new List<string> { "Key Habit 1", "Key Habit 2" },
                EstimatedReadMinutes: 4
            )));
        }
    }

    private class PdfIngestionQueue : IPdfIngestionQueue
    {
        private readonly System.Threading.Channels.Channel<PdfIngestJob> _channel =
            System.Threading.Channels.Channel.CreateUnbounded<PdfIngestJob>();

        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
