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

    [Fact]
    public async Task PdfIngestionWorker_ShouldPreserveVerbatimSource_WhenFormatterAddsRestoredContent()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var owner = new User { Email = "pdf-source@example.test", Name = "PDF reader" };
        var category = DocumentCategory.Create("novel-pdf-source", "Novel PDF source", "Nguồn PDF mới");
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "Thói Quen Nguyên Tử",
            Slug = "thoi-quen-nguyen-tu-" + Guid.NewGuid().ToString().Substring(0, 6),
            CategoryId = category.Id,
            CreatedByUserId = owner.Id,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = "https://example.com/atomic-habits.pdf",
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            TotalChunks = 0
        };

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        using (var setupDb = new DeepPaceDbContext(options))
        {
            setupDb.Users.Add(owner);
            setupDb.DocumentCategories.Add(category);
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
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IPdfExtractor>(_ => fakeExtractor);
        services.AddScoped<IAiMarkdownFormatter>(_ => fakeFormatter);
        services.AddScoped<ILookAheadBufferService>(_ => null!);
        services.AddScoped<IAuthorizationReadinessService>(_ => new FakeReadinessService());
        var serviceProvider = services.BuildServiceProvider();

        var worker = new PdfIngestionWorker(queue, serviceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var workerTask = worker.StartAsync(cts.Token);

        // Act: Enqueue ingestion job
        await queue.EnqueueAsync(new PdfIngestJob(bookId, tempPdfPath, book.Title, category.Id, "vi"), cts.Token);

        // Wait until processed
        DocumentChunk? chunk = null;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            using var pollDb = new DeepPaceDbContext(options);
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
        chunk!.OriginalTextMarkdown.Should().Be(verbatimOriginal, "formatting must not replace verbatim source text");
        chunk.SummaryMarkdown.Should().Be("Summary for Phần Giới Thiệu");
        chunk.IsAiFormatted.Should().BeTrue();
        using var verificationDb = new DeepPaceDbContext(options);
        var persistedBook = await verificationDb.DocumentBooks.AsNoTracking().SingleAsync(b => b.Id == bookId);
        persistedBook.CategoryId.Should().Be(category.Id);
        persistedBook.CreatedByUserId.Should().Be(owner.Id);
        persistedBook.Status.Should().Be(ProcessingStatus.Ready);
    }

    [Fact]
    public async Task CatalogPdfIngestion_PreservesVerbatimSource_WhenFormatterAddsRestoredContent()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;
        await using var db = new DeepPaceDbContext(options);
        var category = DocumentCategory.Create("novel-catalog-pdf", "Novel catalog PDF", "PDF danh mục mới");
        var document = new CatalogDocument
        {
            Title = "Catalog source",
            CategoryId = category.Id,
            SourceType = SourceType.PdfBook,
            Language = "vi",
            AttributionText = "Original author",
            ProcessingGeneration = 1
        };
        db.DocumentCategories.Add(category);
        db.CatalogDocuments.Add(document);
        await db.SaveChangesAsync();
        const string source = "  Nội dung gốc phải được giữ nguyên.\r\n\r\nDòng thứ hai với dấu tiếng Việt.\n";
        var extraction = new PdfExtractionResult(document.Title, 1,
            [new ExtractedPdfSlice(1, "Chapter", source, 2, ["Original takeaway"])]);
        var job = new PdfIngestJob(document.Id, string.Empty, document.Title, category.Id, "vi",
            IngestionDestinationType.CatalogDocument, Generation: 1);

        var persisted = await new CatalogDocumentDestination().PersistAndFormatSlicesAsync(
            job, extraction, db, new FakeAiFormatter(), null, CancellationToken.None);

        persisted.Should().BeTrue();
        db.ChangeTracker.Clear();
        var slice = await db.CatalogSlices.SingleAsync(s => s.CatalogDocumentId == document.Id);
        slice.OriginalTextMarkdown.Should().Be(source);
        slice.SummaryMarkdown.Should().Be("Summary for Chapter");
        slice.IsAiFormatted.Should().BeTrue();
        var savedDocument = await db.CatalogDocuments.SingleAsync(d => d.Id == document.Id);
        savedDocument.CategoryId.Should().Be(category.Id);
        savedDocument.ProcessingStatus.Should().Be(ProcessingStatus.Ready);
        savedDocument.Visibility.Should().Be(CatalogVisibility.Draft);
    }

    [Fact]
    public async Task PdfIngestionWorker_WhenReadinessServiceNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        // Note: IAuthorizationReadinessService is deliberately NOT registered
        var sp = services.BuildServiceProvider();
        var queue = new PdfIngestionQueue();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        var act = async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }
        };
        await act.Should().ThrowAsync<InvalidOperationException>("Bypassing readiness without a registered service is prohibited");
    }

    [Fact]
    public async Task PdfIngestionWorker_WhenReadinessFails_ThrowsInvalidOperationExceptionAndStopsStartup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        var unreadyStatus = new AuthorizationReadinessStatus(IsReady: false, IsDatabaseConnected: true, HasRequiredSchemaAndRoles: false, IsInitialized: false, FailureReason: "Admin role protected permissions do not match");
        services.AddScoped<IAuthorizationReadinessService>(_ => new FakeReadinessService(unreadyStatus));
        var sp = services.BuildServiceProvider();
        var queue = new PdfIngestionQueue();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        var act = async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            if (worker.ExecuteTask != null)
            {
                await worker.ExecuteTask;
            }
        };
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*required authorization schema or seeds not ready*");
    }

    [Fact]
    public async Task PdfIngestionWorker_WhenSchemaAwaitingInitialization_PermitsLearnerProcessing()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        // Valid schema awaiting initialization: IsReady = true, HasRequiredSchemaAndRoles = true, IsInitialized = false
        var awaitingInitStatus = new AuthorizationReadinessStatus(IsReady: true, IsDatabaseConnected: true, HasRequiredSchemaAndRoles: true, IsInitialized: false, FailureReason: null);
        services.AddScoped<IAuthorizationReadinessService>(_ => new FakeReadinessService(awaitingInitStatus));
        var sp = services.BuildServiceProvider();
        var queue = new PdfIngestionQueue();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        // Should start cleanly without throwing
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PdfIngestionWorker_StartupRecovery_PreservesQueuedAttemptsAndMarksRunningInterrupted()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connectionString).Options;
        var queuedAttemptId = Guid.NewGuid();
        var runningAttemptId = Guid.NewGuid();

        using (var setupDb = new DeepPaceDbContext(options))
        {
            var category = DocumentCategory.Create("rec-cat-" + Guid.NewGuid().ToString("N")[..6], "Recovery Cat", "DanhMuc");
            setupDb.DocumentCategories.Add(category);

            var queuedAttempt = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, Guid.NewGuid(), IngestionAttemptOrigin.UserUpload, "req-queued", 1);
            var runningAttempt = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, Guid.NewGuid(), IngestionAttemptOrigin.UserUpload, "req-running", 1);
            runningAttempt.Claim("token-claim", DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow);
            queuedAttemptId = queuedAttempt.Id;
            runningAttemptId = runningAttempt.Id;

            setupDb.IngestionAttempts.AddRange(queuedAttempt, runningAttempt);
            await setupDb.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IAuthorizationReadinessService>(_ => new FakeReadinessService());
        var sp = services.BuildServiceProvider();
        var queue = new PdfIngestionQueue();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            using var checkDb = new DeepPaceDbContext(options);
            var att = await checkDb.IngestionAttempts.FindAsync(runningAttemptId);
            if (att?.State == IngestionAttemptState.Interrupted) break;
            await Task.Delay(50);
        }

        await worker.StopAsync(CancellationToken.None);
        using (var verifyDb = new DeepPaceDbContext(options))
        {
            var queued = await verifyDb.IngestionAttempts.FindAsync(queuedAttemptId);
            queued!.State.Should().Be(IngestionAttemptState.Queued, "Queued attempt must be preserved across restart");

            var running = await verifyDb.IngestionAttempts.FindAsync(runningAttemptId);
            running!.State.Should().Be(IngestionAttemptState.Interrupted, "Running attempt must be marked Interrupted on startup recovery");
        }
    }

    [Fact]
    public async Task PdfIngestionWorker_WhenShutdownCancelled_StopsNormally()
    {
        var services = new ServiceCollection();
        services.AddDbContext<DeepPaceDbContext>(o => o.UseSqlite(_connectionString));
        services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        services.AddScoped<IAuthorizationReadinessService>(_ => new FakeReadinessService());
        var sp = services.BuildServiceProvider();
        var queue = new PdfIngestionQueue();
        var worker = new PdfIngestionWorker(queue, sp.GetRequiredService<IServiceScopeFactory>(), NullLogger<PdfIngestionWorker>.Instance);

        using var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    private class FakeReadinessService : IAuthorizationReadinessService
    {
        private readonly AuthorizationReadinessStatus _status;
        public FakeReadinessService(AuthorizationReadinessStatus? status = null)
        {
            _status = status ?? new AuthorizationReadinessStatus(true, true, true, false, null);
        }

        public Task<AuthorizationReadinessStatus> GetReadinessAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_status);
        }
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
