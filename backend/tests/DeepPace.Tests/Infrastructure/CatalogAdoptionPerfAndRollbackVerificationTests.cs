using Npgsql;
using System.Data.Common;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;
using DeepPace.Application.Features.Catalog.AdoptDocument;

namespace DeepPace.Tests.Infrastructure;

public class CatalogAdoptionPerfAndRollbackVerificationTests
{
    private static string PostgresConnStr => Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
        ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION must identify an isolated PostgreSQL test database.");
    private static readonly NpgsqlDataSource DataSource;
    private readonly ITestOutputHelper _output;

    static CatalogAdoptionPerfAndRollbackVerificationTests()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(PostgresConnStr);
        dataSourceBuilder.UseVector();
        DataSource = dataSourceBuilder.Build();
    }

    public CatalogAdoptionPerfAndRollbackVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static DbContextOptions<DeepPaceDbContext> CreatePostgresOptions(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseNpgsql(DataSource, o => o.UseVector());
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return builder.Options;
    }

    private static bool IsPostgresAvailable()
    {
        try
        {
            using var db = new DeepPaceDbContext(CreatePostgresOptions());
            return db.Database.CanConnect();
        }
        catch
        {
            return false;
        }
    }

    private static Pgvector.Vector CreateSampleEmbedding(int seed, int dimensions = 768)
    {
        var vector = new float[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (float)Math.Sin(seed * 1000 + i);
        }
        return new Pgvector.Vector(vector);
    }

    private async Task<(CatalogDocument Document, User Learner)> SeedRepresentativeCatalogDocumentAsync(
        DeepPaceDbContext db,
        int sliceCount = 20)
    {
        var learner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"perf-learner-{Guid.NewGuid():N}@example.com",
            Name = "Perf Test Learner",
            IsDeleted = false
        };
        await db.Users.AddAsync(learner);

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = $"Representative System Architecture Handbook {Guid.NewGuid():N}",
            Description = "Comprehensive microservices, distributed systems, and consensus guide.",
            CategoryId = CategoryDefaults.SystemDesignId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Distributed Systems Council 2026",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = sliceCount,
            Revision = 1
        };

        for (int i = 1; i <= sliceCount; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}: High Throughput Stream Processing Part {i}",
                OriginalTextMarkdown = string.Join("\n\n", Enumerable.Range(1, 15).Select(p =>
                    $"Paragraph {p}: Distributed log-structured append-only architectures ensure predictable read and write performance under high concurrency in slice {i}. Detailed operational semantics and replication factor trade-offs apply.")),
                SummaryMarkdown = $"Summary for slice {i}: Explores log-structured persistence and replication mechanics.",
                KeyTakeaways = new()
                {
                    $"Key insight 1 for slice {i}: Log appending avoids random disk I/O.",
                    $"Key insight 2 for slice {i}: Consensus leases prevent split-brain conditions.",
                    $"Key insight 3 for slice {i}: Zero-copy network transport minimizes kernel context switches."
                },
                EstimatedReadMinutes = 4,
                IsAiFormatted = true,
                Language = "en",
                Embedding = CreateSampleEmbedding(i, 768)
            });
        }

        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Publish(adminId);

        await db.CatalogDocuments.AddAsync(doc);
        await db.SaveChangesAsync();

        return (doc, learner);
    }


    [Fact]
    public async Task MeasureAdoptionPerformanceAndStorage_RepresentativeDocumentWithEmbeddings()
    {
        if (!IsPostgresAvailable()) return;

        const int SliceCount = 25;
        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, learner) = await SeedRepresentativeCatalogDocumentAsync(db, SliceCount);

        var adoptionService = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(db));

        // Warm up DB connection and execute timed adoption
        var sw = Stopwatch.StartNew();
        var result = await adoptionService.AdoptAsync(doc.Id, learner.Id);
        sw.Stop();

        var primaryDurationMs = sw.Elapsed.TotalMilliseconds;
        _output.WriteLine($"=== Primary Adoption Run ===");
        _output.WriteLine($"CatalogDocumentId: {doc.Id}");
        _output.WriteLine($"LearnerId: {learner.Id}");
        _output.WriteLine($"Slices with 768-d embeddings: {SliceCount}");
        _output.WriteLine($"Adoption Duration: {primaryDurationMs:F2} ms");

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyAdded.Should().BeFalse();
        var personalBookId = result.Value.BookId;

        // Verify committed data in PostgreSQL
        var committedBook = await db.DocumentBooks
            .Include(b => b.Chunks)
            .FirstOrDefaultAsync(b => b.Id == personalBookId);

        committedBook.Should().NotBeNull();
        committedBook!.CreatedByUserId.Should().Be(learner.Id);
        committedBook.SourceCatalogDocumentId.Should().Be(doc.Id);
        committedBook.Chunks.Count.Should().Be(SliceCount);
        committedBook.Chunks.Should().OnlyContain(c => c.Embedding != null);

        // Storage analysis from PostgreSQL engine in an isolated connection
        int bookRowBytes;
        long totalChunksBytes, totalEmbeddingBytes, totalMarkdownBytes;
        using (var rawConn = new Npgsql.NpgsqlConnection(PostgresConnStr))
        {
            await rawConn.OpenAsync();
            await using var cmd = rawConn.CreateCommand();
            cmd.CommandText = $@"
                SELECT 
                    pg_column_size(b.*) AS book_row_bytes,
                    (SELECT sum(pg_column_size(c.*)) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_chunks_bytes,
                    (SELECT sum(pg_column_size(c.""Embedding"")) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_embedding_bytes,
                    (SELECT sum(octet_length(c.""OriginalTextMarkdown"")) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_markdown_text_bytes
                FROM ""DocumentBooks"" b
                WHERE b.""Id"" = '{personalBookId}';";

            await using var reader = await cmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();

            bookRowBytes = reader.GetInt32(0);
            totalChunksBytes = reader.GetInt64(1);
            totalEmbeddingBytes = reader.GetInt64(2);
            totalMarkdownBytes = reader.GetInt64(3);
        }

        _output.WriteLine($"=== Storage Analysis ===");
        _output.WriteLine($"DocumentBook row size: {bookRowBytes:N0} bytes");
        _output.WriteLine($"Total DocumentChunks storage: {totalChunksBytes:N0} bytes ({totalChunksBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Total Embedding storage: {totalEmbeddingBytes:N0} bytes ({totalEmbeddingBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Total Markdown text storage: {totalMarkdownBytes:N0} bytes ({totalMarkdownBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Average storage per slice: {totalChunksBytes / (double)SliceCount:F2} bytes");

        // Execute repeated benchmark across 5 distinct learners to gather statistical distribution
        var iterations = 5;
        var durations = new List<double>();

        for (int i = 0; i < iterations; i++)
        {
            var nextLearner = new User
            {
                Id = Guid.NewGuid(),
                Email = $"bench-learner-{i}-{Guid.NewGuid():N}@example.com",
                Name = $"Bench Learner {i}",
                IsDeleted = false
            };
            await db.Users.AddAsync(nextLearner);
            await db.SaveChangesAsync();

            var loopSw = Stopwatch.StartNew();
            var loopRes = await adoptionService.AdoptAsync(doc.Id, nextLearner.Id);
            loopSw.Stop();

            loopRes.IsSuccess.Should().BeTrue();
            durations.Add(loopSw.Elapsed.TotalMilliseconds);
        }

        _output.WriteLine($"=== Benchmark Results ({iterations} runs on {SliceCount}-slice doc) ===");
        _output.WriteLine($"Min Duration: {durations.Min():F2} ms");
        _output.WriteLine($"Average Duration: {durations.Average():F2} ms");
        _output.WriteLine($"Max Duration: {durations.Max():F2} ms");

        durations.Min().Should().BeLessThan(500, "Adoption of 25 slices with vector embeddings must complete under 500ms");
        durations.Average().Should().BeLessThan(1500, "Adoption benchmark average must stay within bounded latency during full suite concurrency");
    }

    [Fact]
    public async Task CancellationRollback_WhenTokenCanceledPreExecution_LeavesZeroOrphans()
    {
        if (!IsPostgresAvailable()) return;

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, user) = await SeedRepresentativeCatalogDocumentAsync(db, sliceCount: 10);

        var service = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(db));

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Already cancelled

        var act = async () => await service.AdoptAsync(doc.Id, user.Id, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Query database via fresh context
        using var verifyDb = new DeepPaceDbContext(CreatePostgresOptions());
        var bookCount = await verifyDb.DocumentBooks
            .CountAsync(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id);
        var chunkCount = await verifyDb.DocumentChunks
            .CountAsync(c => c.DocumentBook.CreatedByUserId == user.Id && c.DocumentBook.SourceCatalogDocumentId == doc.Id);

        _output.WriteLine($"Pre-execution cancellation: BookCount = {bookCount}, ChunkCount = {chunkCount}");
        bookCount.Should().Be(0);
        chunkCount.Should().Be(0);
    }

    [Fact]
    public async Task CancellationRollback_WhenTokenCanceledMidSave_RollsBackTransactionCompletely()
    {
        if (!IsPostgresAvailable()) return;

        using var seedDb = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, user) = await SeedRepresentativeCatalogDocumentAsync(seedDb, sliceCount: 15);

        using var cts = new CancellationTokenSource();

        // Intercept save changes to cancel the token precisely when EF Core begins saving the entity graph
        var interceptor = new MidSaveCancellingInterceptor(cts);
        var optionsWithInterceptor = CreatePostgresOptions(interceptor);

        using var db = new DeepPaceDbContext(optionsWithInterceptor);
        var service = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(db));

        Func<Task> act = async () => await service.AdoptAsync(doc.Id, user.Id, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify zero orphan rows in PostgreSQL
        using var verifyDb = new DeepPaceDbContext(CreatePostgresOptions());
        var orphanBooks = await verifyDb.DocumentBooks
            .Where(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        var orphanChunks = await verifyDb.DocumentChunks
            .Where(c => c.DocumentBook.CreatedByUserId == user.Id && c.DocumentBook.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        _output.WriteLine($"Mid-save cancellation verified: OrphanBooks = {orphanBooks.Count}, OrphanChunks = {orphanChunks.Count}");
        orphanBooks.Should().BeEmpty();
        orphanChunks.Should().BeEmpty();
    }

    [Fact]
    public async Task BehavioralVerification_ZeroInvocationsToAiAndCrawlerDuringAdoption()
    {
        if (!IsPostgresAvailable()) return;

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, learner) = await SeedRepresentativeCatalogDocumentAsync(db, sliceCount: 10);

        var geminiSpy = new GeminiAiSpy();
        var embeddingSpy = new EmbeddingSpy();
        var crawlerSpy = new WebArticleCrawlerSpy();

        var adoptionService = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(db));
        var handler = new AdoptCatalogDocumentHandler(adoptionService);

        var sw = Stopwatch.StartNew();
        var result = await handler.ExecuteAsync(new AdoptCatalogDocumentRequest(doc.Id, learner.Id));
        sw.Stop();

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyAdded.Should().BeFalse();

        _output.WriteLine($"Adoption completed in {sw.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Gemini AI invocation count: {geminiSpy.InvocationCount}");
        _output.WriteLine($"Embedding Service invocation count: {embeddingSpy.InvocationCount}");
        _output.WriteLine($"Web Article Crawler invocation count: {crawlerSpy.InvocationCount}");

        geminiSpy.InvocationCount.Should().Be(0, "Adoption must NOT call Gemini AI");
        embeddingSpy.InvocationCount.Should().Be(0, "Adoption must reuse existing catalog embeddings without re-embedding");
        crawlerSpy.InvocationCount.Should().Be(0, "Adoption must NOT invoke web crawlers");
    }

    [Fact]
    public async Task FineGrainedAdoptionStageBreakdown_50SlicesWithEmbeddings()
    {
        if (!IsPostgresAvailable()) return;

        const int SliceCount = 50;
        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, learner) = await SeedRepresentativeCatalogDocumentAsync(db, SliceCount);

        var swTotal = Stopwatch.StartNew();

        // Phase 1: Begin transaction & SELECT FOR UPDATE with Slices
        var swPhase1 = Stopwatch.StartNew();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
        var lockedDoc = await db.CatalogDocuments
            .FromSqlInterpolated($"SELECT * FROM \"CatalogDocuments\" WHERE \"Id\" = {doc.Id} FOR UPDATE")
            .Include(d => d.Slices)
            .FirstOrDefaultAsync();
        swPhase1.Stop();

        lockedDoc.Should().NotBeNull();
        lockedDoc!.Slices.Count.Should().Be(SliceCount);

        // Phase 2: In-memory snapshot entity creation (including 50 vector allocations)
        var swPhase2 = Stopwatch.StartNew();
        var uniqueSlug = $"breakdown-slug-{learner.Id:N}";
        var book = DocumentBook.CreateFromCatalogSnapshot(lockedDoc, learner.Id, uniqueSlug);
        swPhase2.Stop();

        book.Chunks.Count.Should().Be(SliceCount);

        // Phase 3: EF Core Add & SaveChangesAsync (SQL INSERT of book + 50 chunks into PostgreSQL)
        var swPhase3 = Stopwatch.StartNew();
        await db.DocumentBooks.AddAsync(book);
        await db.SaveChangesAsync();
        swPhase3.Stop();

        // Phase 4: Transaction commit
        var swPhase4 = Stopwatch.StartNew();
        await transaction.CommitAsync();
        swPhase4.Stop();

        swTotal.Stop();

        _output.WriteLine($"=== Fine-Grained Timing Breakdown (50 Slices, 768-d Vector Each) ===");
        _output.WriteLine($"Phase 1 (Row Lock SELECT FOR UPDATE + Include 50 Slices): {swPhase1.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Phase 2 (In-Memory Entity Graph Snapshot Creation):       {swPhase2.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Phase 3 (PostgreSQL Batch INSERT Book + 50 Chunks):      {swPhase3.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Phase 4 (Transaction CommitAsync):                         {swPhase4.Elapsed.TotalMilliseconds:F2} ms");
        _output.WriteLine($"Total Roundtrip Transaction Duration:                     {swTotal.Elapsed.TotalMilliseconds:F2} ms");

        // Storage measurement for 50-slice document
        int bookRowBytes;
        long totalChunksBytes, totalEmbeddingBytes, totalMarkdownBytes;
        using (var rawConn = new Npgsql.NpgsqlConnection(PostgresConnStr))
        {
            await rawConn.OpenAsync();
            await using var cmd = rawConn.CreateCommand();
            cmd.CommandText = $@"
                SELECT 
                    pg_column_size(b.*) AS book_row_bytes,
                    (SELECT sum(pg_column_size(c.*)) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_chunks_bytes,
                    (SELECT sum(pg_column_size(c.""Embedding"")) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_embedding_bytes,
                    (SELECT sum(octet_length(c.""OriginalTextMarkdown"")) FROM ""DocumentChunks"" c WHERE c.""DocumentBookId"" = b.""Id"") AS total_markdown_text_bytes
                FROM ""DocumentBooks"" b
                WHERE b.""Id"" = '{book.Id}';";

            await using var reader = await cmd.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();

            bookRowBytes = reader.GetInt32(0);
            totalChunksBytes = reader.GetInt64(1);
            totalEmbeddingBytes = reader.GetInt64(2);
            totalMarkdownBytes = reader.GetInt64(3);
        }

        _output.WriteLine($"=== Storage Metrics (50 Slices) ===");
        _output.WriteLine($"DocumentBook row size: {bookRowBytes:N0} bytes");
        _output.WriteLine($"Total DocumentChunks storage: {totalChunksBytes:N0} bytes ({totalChunksBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Total Embedding storage: {totalEmbeddingBytes:N0} bytes ({totalEmbeddingBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Total Markdown text storage: {totalMarkdownBytes:N0} bytes ({totalMarkdownBytes / 1024.0:F2} KB)");
        _output.WriteLine($"Average storage per slice: {totalChunksBytes / (double)SliceCount:F2} bytes");

        swTotal.ElapsedMilliseconds.Should().BeLessThan(3000, "50-slice adoption must complete under 3 seconds even under test concurrency load");
    }

    [Fact]
    public async Task CancellationRollback_WhenTokenCanceledDuringCommandExecution_RollsBackCompletely()
    {
        if (!IsPostgresAvailable()) return;

        using var seedDb = new DeepPaceDbContext(CreatePostgresOptions());
        var (doc, user) = await SeedRepresentativeCatalogDocumentAsync(seedDb, sliceCount: 20);

        using var cts = new CancellationTokenSource();

        // Intercept DbCommand execution to cancel the token precisely when SQL command is sent to Postgres
        var interceptor = new MidCommandCancellingInterceptor(cts);
        var optionsWithInterceptor = CreatePostgresOptions(interceptor);

        using var db = new DeepPaceDbContext(optionsWithInterceptor);
        var service = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(db));

        Func<Task> act = async () => await service.AdoptAsync(doc.Id, user.Id, cts.Token);
        await act.Should().ThrowAsync<Exception>();

        // Verify zero orphan rows in PostgreSQL
        using var verifyDb = new DeepPaceDbContext(CreatePostgresOptions());
        var orphanBooks = await verifyDb.DocumentBooks
            .Where(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        var orphanChunks = await verifyDb.DocumentChunks
            .Where(c => c.DocumentBook.CreatedByUserId == user.Id && c.DocumentBook.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        _output.WriteLine($"Mid-command cancellation verified: OrphanBooks = {orphanBooks.Count}, OrphanChunks = {orphanChunks.Count}");
        orphanBooks.Should().BeEmpty();
        orphanChunks.Should().BeEmpty();
    }

    private class MidCommandCancellingInterceptor : DbCommandInterceptor
    {
        private readonly CancellationTokenSource _cts;

        public MidCommandCancellingInterceptor(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("DocumentBooks", StringComparison.OrdinalIgnoreCase) ||
                command.CommandText.Contains("DocumentChunks", StringComparison.OrdinalIgnoreCase))
            {
                _cts.Cancel();
            }
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("DocumentBooks", StringComparison.OrdinalIgnoreCase) ||
                command.CommandText.Contains("DocumentChunks", StringComparison.OrdinalIgnoreCase))
            {
                _cts.Cancel();
            }
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private class GeminiAiSpy : IGeminiAiService
    {
        public int InvocationCount { get; private set; }

        public Task<DeepPace.Application.Common.Result<(string Front, string Back)>> SynthesizeActiveRecallCardAsync(
            string quote,
            string? note,
            string chapterTitle,
            string locale = "en",
            CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult(DeepPace.Application.Common.Result<(string Front, string Back)>.Success(("Q", "A")));
        }
    }

    private class EmbeddingSpy : IEmbeddingService
    {
        public int InvocationCount { get; private set; }

        public Task<DeepPace.Application.Common.Result<Pgvector.Vector>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(DeepPace.Application.Common.Result<Pgvector.Vector>.Success(new Pgvector.Vector(new float[768])));
        }

        public Task<DeepPace.Application.Common.Result<List<Pgvector.Vector>>> GenerateBatchEmbeddingsAsync(
            List<string> texts,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(DeepPace.Application.Common.Result<List<Pgvector.Vector>>.Success(new List<Pgvector.Vector>()));
        }
    }

    private class WebArticleCrawlerSpy : IWebArticleCrawler
    {
        public int InvocationCount { get; private set; }

        public Task<CrawlArticleResult> CrawlUrlAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new CrawlArticleResult("Title", url, "# Content", 100));
        }
    }

    private class MidSaveCancellingInterceptor : SaveChangesInterceptor
    {
        private readonly CancellationTokenSource _cts;

        public MidSaveCancellingInterceptor(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            // Trigger cancellation while inside the transaction, before SQL commits
            _cts.Cancel();
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
