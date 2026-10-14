using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Workers;
using DeepPace.Infrastructure.Workers.IngestionDestinations;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class CatalogIngestionDestinationTests : IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly string _connectionString;
    private readonly List<string> _tempFilesToCleanup = new();

    public CatalogIngestionDestinationTests()
    {
        var dbName = "catalog_ingest_" + Guid.NewGuid().ToString("N");
        _connectionString = $"DataSource=file:{dbName}?mode=memory&cache=shared";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        using var initDb = new DeepPaceDbContext(options);
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

    private CatalogDocument CreateTestCatalogDocument(long generation = 1)
    {
        return new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Catalog Test Doc",
            Description = "Catalog Doc Description",
            Category = Category.BackendRuntime,
            SourceType = SourceType.PdfBook,
            Language = "en",
            AttributionText = "Source Attribution",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Pending,
            ProcessingGeneration = generation,
            Revision = 1
        };
    }

    [Fact]
    public async Task CatalogIngestion_ShouldNeverCreatePersonalBooksOrChunks()
    {
        var doc = CreateTestCatalogDocument(generation: 1);
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"test-cat-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        _tempFilesToCleanup.Add(tempPdfPath);

        var fakeExtractor = new FakePdfExtractor(new PdfExtractionResult(
            DocumentTitle: doc.Title,
            TotalPages: 10,
            Slices: new List<ExtractedPdfSlice>
            {
                new(Order: 1, ChapterTitle: "Ch 1", ContentMarkdown: "Content 1", EstimatedReadMinutes: 3, KeyTakeaways: new() { "T1" }),
                new(Order: 2, ChapterTitle: "Ch 2", ContentMarkdown: "Content 2", EstimatedReadMinutes: 4, KeyTakeaways: new() { "T2" })
            }
        ));

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();
        services.AddScoped<PersonalBookDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            tempPdfPath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 1), cts.Token);

        // Wait until Ready
        var deadline = DateTime.UtcNow.AddSeconds(5);
        CatalogDocument? resultDoc = null;
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
            var d = await pollDb.CatalogDocuments.Include(x => x.Slices).FirstOrDefaultAsync(x => x.Id == doc.Id);
            if (d?.ProcessingStatus == ProcessingStatus.Ready)
            {
                resultDoc = d;
                break;
            }
            await Task.Delay(50);
        }

        resultDoc.Should().NotBeNull();
        resultDoc!.ProcessingStatus.Should().Be(ProcessingStatus.Ready);
        resultDoc.Slices.Should().HaveCount(2);

        // Invariant check: ZERO personal books or personal chunks created!
        using (var verifyDb = new DeepPaceDbContext(options))
        {
            var personalBooksCount = await verifyDb.DocumentBooks.CountAsync();
            var personalChunksCount = await verifyDb.DocumentChunks.CountAsync();
            personalBooksCount.Should().Be(0);
            personalChunksCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task StaleWorkerJob_WithOlderGeneration_ShouldBeIgnored()
    {
        var doc = CreateTestCatalogDocument(generation: 2); // DB has generation 2
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"test-stale-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        _tempFilesToCleanup.Add(tempPdfPath);

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => new FakePdfExtractor(new PdfExtractionResult("Title", 1, new())));
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = worker.StartAsync(cts.Token);

        // Enqueue job with stale generation 1 (DB is 2)
        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            tempPdfPath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 1), cts.Token);

        await Task.Delay(200);

        using (var pollDb = new DeepPaceDbContext(options))
        {
            var d = await pollDb.CatalogDocuments.FirstAsync(x => x.Id == doc.Id);
            // Must still be Pending, never touched by stale worker
            d.ProcessingStatus.Should().Be(ProcessingStatus.Pending);
        }
    }

    [Fact]
    public async Task MissingTemporaryFile_SetsRecoverableFailedStatus()
    {
        var doc = CreateTestCatalogDocument(generation: 1);
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var missingFilePath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid():N}.pdf");

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => new FakePdfExtractor(new PdfExtractionResult("Title", 1, new())));
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            missingFilePath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 1), cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        CatalogDocument? failedDoc = null;
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
            var d = await pollDb.CatalogDocuments.FirstAsync(x => x.Id == doc.Id);
            if (d.ProcessingStatus == ProcessingStatus.Failed)
            {
                failedDoc = d;
                break;
            }
            await Task.Delay(50);
        }

        failedDoc.Should().NotBeNull();
        failedDoc!.ProcessingErrorMessage.Should().Contain("Temporary upload file is no longer available");
    }

    [Fact]
    public async Task InitialAiFailure_LeavesDraftInFailedStatus_CannotPublish()
    {
        var doc = CreateTestCatalogDocument(generation: 1);
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"test-aifail-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        _tempFilesToCleanup.Add(tempPdfPath);

        var fakeExtractor = new FakePdfExtractor(new PdfExtractionResult(
            DocumentTitle: doc.Title,
            TotalPages: 5,
            Slices: new List<ExtractedPdfSlice>
            {
                new(Order: 1, ChapterTitle: "Ch 1", ContentMarkdown: "Content 1", EstimatedReadMinutes: 3, KeyTakeaways: new() { "T1" })
            }
        ));

        var failingAi = new FailingAiFormatter();
        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => failingAi);
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            tempPdfPath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 1), cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        CatalogDocument? failedDoc = null;
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
            var d = await pollDb.CatalogDocuments.FirstAsync(x => x.Id == doc.Id);
            if (d.ProcessingStatus == ProcessingStatus.Failed)
            {
                failedDoc = d;
                break;
            }
            await Task.Delay(50);
        }

        failedDoc.Should().NotBeNull();
        failedDoc!.ProcessingStatus.Should().Be(ProcessingStatus.Failed);
        failedDoc.ProcessingErrorMessage.Should().Contain("Initial AI formatting failed");

        // Verify domain rule: cannot publish document in Failed status
        var act = () => failedDoc.Publish(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Ready*");
    }

    [Fact]
    public async Task RetryPreparation_AtomicallyReplacesSlices_NoDuplicates()
    {
        var doc = CreateTestCatalogDocument(generation: 2);
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;

        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            // Pre-seed an older slice attempt
            await db.CatalogSlices.AddAsync(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = 1,
                ChapterTitle = "Old Slice 1",
                OriginalTextMarkdown = "Old content",
                SummaryMarkdown = "Old summary",
                IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"test-retry-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 test"));
        _tempFilesToCleanup.Add(tempPdfPath);

        var fakeExtractor = new FakePdfExtractor(new PdfExtractionResult(
            DocumentTitle: doc.Title,
            TotalPages: 5,
            Slices: new List<ExtractedPdfSlice>
            {
                new(Order: 1, ChapterTitle: "New Ch 1", ContentMarkdown: "New Content 1", EstimatedReadMinutes: 3, KeyTakeaways: new() { "New T1" })
            }
        ));

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _ = worker.StartAsync(cts.Token);

        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            tempPdfPath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 2), cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        CatalogDocument? readyDoc = null;
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
            var d = await pollDb.CatalogDocuments.Include(x => x.Slices).FirstAsync(x => x.Id == doc.Id);
            if (d.ProcessingStatus == ProcessingStatus.Ready)
            {
                readyDoc = d;
                break;
            }
            await Task.Delay(50);
        }

        readyDoc.Should().NotBeNull();
        // Slices must contain exactly 1 slice ("New Ch 1"), old slice was cleanly replaced
        readyDoc!.Slices.Should().HaveCount(1);
        readyDoc.Slices.First().ChapterTitle.Should().Be("New Ch 1");
    }

    [Fact]
    public async Task WorkerStartup_RecoversInterruptedProcessingJobs()
    {
        var doc = CreateTestCatalogDocument(generation: 1);
        doc.ProcessingStatus = ProcessingStatus.Processing;
        doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5); // Expired lease
        doc.ProcessingStatusMessage = "Processing mid-flight...";

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;
        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => new FakePdfExtractor(new PdfExtractionResult("Title", 1, new())));
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);

        using (var pollDb = new DeepPaceDbContext(options))
        {
            var recoveredDoc = await pollDb.CatalogDocuments.FirstAsync(x => x.Id == doc.Id);
            recoveredDoc.ProcessingStatus.Should().Be(ProcessingStatus.Failed);
            recoveredDoc.ProcessingStatusMessage.Should().Contain("Interrupted");
            recoveredDoc.ProcessingErrorMessage.Should().Contain("interrupted");
            recoveredDoc.ProcessingLeaseExpiresAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task DocumentedRetryFlow_WorksWithInterruptedPermittedPdfFixture()
    {
        // 1. Arrange: An interrupted permitted document during server shutdown
        var doc = CreateTestCatalogDocument(generation: 1);
        doc.ProcessingStatus = ProcessingStatus.Processing;
        doc.ProcessingStatusMessage = "Ingesting slice 1...";
        doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-2);

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;
        using (var db = new DeepPaceDbContext(options))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"permitted-fixture-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempPdfPath, System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 permitted literature fixture"));
        _tempFilesToCleanup.Add(tempPdfPath);

        var fakeExtractor = new FakePdfExtractor(new PdfExtractionResult(
            DocumentTitle: doc.Title,
            TotalPages: 15,
            Slices: new List<ExtractedPdfSlice>
            {
                new(Order: 1, ChapterTitle: "Chapter 1: Foundational Principles", ContentMarkdown: "Principles content", EstimatedReadMinutes: 4, KeyTakeaways: new() { "P1" }),
                new(Order: 2, ChapterTitle: "Chapter 2: Implementation Patterns", ContentMarkdown: "Patterns content", EstimatedReadMinutes: 5, KeyTakeaways: new() { "P2" })
            }
        ));

        var queue = new TestQueue();
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => new FakeAiFormatter());
        services.AddScoped<CatalogDocumentDestination>();

        var sp = services.BuildServiceProvider();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        _ = worker.StartAsync(cts.Token);

        // Wait for worker startup recovery to mark the document as Failed / Interrupted
        await Task.Delay(200);

        using (var pollDb = new DeepPaceDbContext(options))
        {
            var d = await pollDb.CatalogDocuments.FirstAsync(x => x.Id == doc.Id);
            d.ProcessingStatus.Should().Be(ProcessingStatus.Failed);
            d.ProcessingStatusMessage.Should().Contain("Interrupted");

            // 2. Act: Admin triggers retry flow (generation bumped to 2)
            d.ProcessingGeneration = 2;
            d.ProcessingStatus = ProcessingStatus.Pending;
            d.ProcessingStatusMessage = "Retry enqueued by administrator";
            await pollDb.SaveChangesAsync();
        }

        // Enqueue retry job with Generation: 2 and replacement/retained temporary PDF
        await queue.EnqueueAsync(new PdfIngestJob(
            doc.Id,
            tempPdfPath,
            doc.Title,
            doc.Category,
            doc.Language,
            IngestionDestinationType.CatalogDocument,
            Generation: 2), cts.Token);

        // 3. Assert: Document reaches Ready with all slices formatted
        var deadline = DateTime.UtcNow.AddSeconds(5);
        CatalogDocument? readyDoc = null;
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
            var d = await pollDb.CatalogDocuments.Include(x => x.Slices).FirstAsync(x => x.Id == doc.Id);
            if (d.ProcessingStatus == ProcessingStatus.Ready)
            {
                readyDoc = d;
                break;
            }
            await Task.Delay(50);
        }

        readyDoc.Should().NotBeNull();
        readyDoc!.ProcessingStatus.Should().Be(ProcessingStatus.Ready);
        readyDoc.ProcessingStatusMessage.Should().Contain("Preparation complete");
        readyDoc.Slices.Should().HaveCount(2);
        readyDoc.Slices.All(s => s.IsAiFormatted).Should().BeTrue();
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
                FormattedMarkdown: $"# {chapterTitle}\n\nAI Formatted Content",
                SummaryMarkdown: $"Summary for {chapterTitle}",
                KeyTakeaways: new List<string> { "Key 1", "Key 2" },
                EstimatedReadMinutes: 4
            )));
        }
    }

    private class FailingAiFormatter : IAiMarkdownFormatter
    {
        public Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
            string rawText,
            string chapterTitle,
            string language = "en",
            Category? category = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AiFormattedSliceResult>.Failure(
                DeepPace.Application.Common.Error.Custom("Ai.ServiceUnavailable", "Model quota exceeded or service error.")));
        }
    }

    private class TestQueue : IPdfIngestionQueue
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
