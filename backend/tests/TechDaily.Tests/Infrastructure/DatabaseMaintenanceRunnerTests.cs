using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Maintenance;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class DatabaseMaintenanceRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly FakeEmbeddingService _embeddingService;
    private readonly DatabaseMaintenanceRunner _runner;

    public DatabaseMaintenanceRunnerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _embeddingService = new FakeEmbeddingService();
        _runner = new DatabaseMaintenanceRunner(
            _db,
            _embeddingService,
            NullLogger<DatabaseMaintenanceRunner>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AnalyzeTaintedDataAsync_ShouldIdentifyMockRecordsWithoutModifyingDatabase()
    {
        // Arrange
        await SeedSampleDataAsync();
        var initialTermCount = await _db.TermExplanationCaches.CountAsync();
        var initialInsightCount = await _db.TechInsights.CountAsync();
        var initialQuizCount = await _db.QuizQuestions.CountAsync();
        var initialCardCount = await _db.SpacedRepetitionCards.CountAsync();
        var initialChunkCount = await _db.DocumentChunks.CountAsync();

        // Act
        var report = await _runner.AnalyzeTaintedDataAsync();

        // Assert
        report.TermExplanationCachesTainted.Should().Be(3);
        report.TechInsightsTainted.Should().Be(3);
        report.QuizQuestionsTainted.Should().Be(3);
        report.SpacedRepetitionCardsTainted.Should().Be(2);
        report.UnvectorizedDocumentChunks.Should().Be(2);
        report.TotalTaintedContent.Should().Be(11);

        // Verify zero mutations occurred
        (await _db.TermExplanationCaches.CountAsync()).Should().Be(initialTermCount);
        (await _db.TechInsights.CountAsync()).Should().Be(initialInsightCount);
        (await _db.QuizQuestions.CountAsync()).Should().Be(initialQuizCount);
        (await _db.SpacedRepetitionCards.CountAsync()).Should().Be(initialCardCount);
        (await _db.DocumentChunks.CountAsync()).Should().Be(initialChunkCount);
    }

    [Fact]
    public async Task PurgeTaintedDataAsync_ShouldDeleteTaintedRecordsWhilePreservingCuratedSeedsAndHighlights()
    {
        // Arrange
        var (curatedTermId, curatedInsightId, curatedQuizId, curatedCardId, highlightId) = await SeedSampleDataAsync();

        // Act
        var purgeReport = await _runner.PurgeTaintedDataAsync();
        var postAnalysis = await _runner.AnalyzeTaintedDataAsync();

        // Assert
        purgeReport.TermExplanationCachesPurged.Should().Be(3);
        purgeReport.TechInsightsPurged.Should().Be(3);
        purgeReport.QuizQuestionsPurged.Should().Be(3);
        purgeReport.SpacedRepetitionCardsPurged.Should().Be(2);
        purgeReport.TotalPurged.Should().Be(11);

        postAnalysis.TotalTaintedContent.Should().Be(0);

        // Verify curated items are preserved
        var preservedTerm = await _db.TermExplanationCaches.FindAsync(curatedTermId);
        preservedTerm.Should().NotBeNull();
        preservedTerm!.Term.Should().Be("span");

        var preservedInsight = await _db.TechInsights.FindAsync(curatedInsightId);
        preservedInsight.Should().NotBeNull();
        preservedInsight!.Slug.Should().Be("dotnet-span-zero-alloc-split");

        var preservedQuiz = await _db.QuizQuestions.FindAsync(curatedQuizId);
        preservedQuiz.Should().NotBeNull();
        preservedQuiz!.Topic.Should().Be("Vue3");

        var preservedCard = await _db.SpacedRepetitionCards.FindAsync(curatedCardId);
        preservedCard.Should().NotBeNull();

        // Verify user highlight was preserved even though tainted card derived from highlight was purged
        var preservedHighlight = await _db.UserHighlights.FindAsync(highlightId);
        preservedHighlight.Should().NotBeNull();
        preservedHighlight!.SelectedText.Should().Be("Important paragraph on concurrency");

        // Verify orphaned bookmark and quiz progress were cleanly removed
        (await _db.UserInsightBookmarks.CountAsync()).Should().Be(0);
        (await _db.UserQuizProgresses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BackfillEmbeddingsAsync_ShouldVectorizeChunksInBatchesAndAssertDimensions()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new DocumentBook
        {
            Id = bookId,
            Title = "Test Book",
            Slug = "test-book",
            TotalChunks = 5
        };
        await _db.DocumentBooks.AddAsync(book);

        for (int i = 1; i <= 5; i++)
        {
            await _db.DocumentChunks.AddAsync(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = bookId,
                ChunkOrder = i,
                ChapterTitle = $"Chapter {i}",
                OriginalTextMarkdown = $"Content for chapter {i}",
                SummaryMarkdown = $"Summary {i}",
                Embedding = null
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var backfillReport = await _runner.BackfillEmbeddingsAsync(batchSize: 2);

        // Assert
        backfillReport.TotalVectorized.Should().Be(5);
        backfillReport.TotalBatches.Should().Be(3);
        backfillReport.FailedBatches.Should().Be(0);

        var remainingUnvectorized = await _db.DocumentChunks.CountAsync(c => c.Embedding == null);
        remainingUnvectorized.Should().Be(0);

        var allChunks = await _db.DocumentChunks.ToListAsync();
        allChunks.Should().OnlyContain(c => c.Embedding != null);
        allChunks.ForEach(c => c.Embedding!.ToArray().Length.Should().Be(768));
    }

    [Fact]
    public async Task ReseedCatalogAsync_ShouldInvokeSeedersAndReturnCatalogCount()
    {
        // Act
        var seededCount = await _runner.ReseedCatalogAsync();

        // Assert
        seededCount.Should().BeGreaterThan(0);
        var insights = await _db.TechInsights.ToListAsync();
        insights.Should().NotBeEmpty();
        insights.Should().Contain(i => i.Slug == "dotnet-span-zero-alloc-split");
    }

    private async Task<(Guid curatedTermId, Guid curatedInsightId, Guid curatedQuizId, Guid curatedCardId, Guid highlightId)> SeedSampleDataAsync()
    {
        var dummyVector = new Vector(new float[768]);

        // 1. TermExplanationCaches
        var curatedTerm = new TermExplanationCache
        {
            Id = Guid.NewGuid(),
            Term = "span",
            Category = "DotNet",
            Locale = "en",
            ExplanationText = "Span<T> represents a contiguous region of arbitrary memory.",
            Embedding = dummyVector
        };
        var taintedTerm1 = new TermExplanationCache
        {
            Id = Guid.NewGuid(),
            Term = "kestrel",
            Category = "DotNet",
            Locale = "en",
            ExplanationText = "The term kestrel in DotNet represents a core runtime or architectural mechanism in the ecosystem.",
            Embedding = dummyVector
        };
        var taintedTerm2 = new TermExplanationCache
        {
            Id = Guid.NewGuid(),
            Term = "gc",
            Category = "DotNet",
            Locale = "vi",
            ExplanationText = "Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại của hệ thống.",
            Embedding = dummyVector
        };
        var taintedTerm3 = new TermExplanationCache
        {
            Id = Guid.NewGuid(),
            Term = "postgres",
            Category = "Postgres",
            Locale = "en",
            ExplanationText = "A relational database management system.",
            Embedding = null // Missing embedding
        };
        await _db.TermExplanationCaches.AddRangeAsync(curatedTerm, taintedTerm1, taintedTerm2, taintedTerm3);

        // 2. TechInsights
        var curatedInsight = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "dotnet-span-zero-alloc-split",
            Title = "Why string.Split() Destroys High-Throughput APIs",
            Category = Category.BackendRuntime,
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Bad",
            SolutionSnippet = "Good",
            UnderTheHoodMarkdown = "Under hood",
            BenchmarkStats = "10x faster"
        };
        var taintedInsight1 = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "dotnet-mock-a1b2c3",
            Title = "Some Random Mock Title",
            Category = Category.BackendRuntime,
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Bad",
            SolutionSnippet = "Good",
            UnderTheHoodMarkdown = "Under hood"
        };
        var taintedInsight2 = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "dotnet-memory-alloc",
            Title = "Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool<T>.Shared",
            Category = Category.BackendRuntime,
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Bad",
            SolutionSnippet = "Good",
            UnderTheHoodMarkdown = "Under hood"
        };
        var taintedInsight3 = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "channels-ingestion",
            Title = "Architecting Non-Blocking Request Ingestion with Channels",
            Category = Category.BackendRuntime,
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Bad",
            SolutionSnippet = "Good",
            UnderTheHoodMarkdown = "Under hood"
        };
        await _db.TechInsights.AddRangeAsync(curatedInsight, taintedInsight1, taintedInsight2, taintedInsight3);

        // User & Bookmark on tainted insight
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@techdaily.local",
            Name = "Test User"
        };
        await _db.Users.AddAsync(user);

        var bookmark = new UserInsightBookmark
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            InsightId = taintedInsight1.Id
        };
        await _db.UserInsightBookmarks.AddAsync(bookmark);

        // 3. QuizQuestions
        var curatedQuiz = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Vue3",
            Category = Category.FrontendWeb,
            Level = QuizLevel.Senior,
            QuestionText = "How does shallowRef prevent recursive proxy overhead?",
            ExplanationMarkdown = "### Architectural Breakdown\nshallowRef only wraps the root value.",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0
        };
        var taintedQuiz1 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "ASP.NET",
            Category = Category.BackendRuntime,
            Level = QuizLevel.Senior,
            QuestionText = "[Senior] Question #1 on ASP.NET Core: When addressing memory, what is optimal?",
            ExplanationMarkdown = "### Technical Deep Dive\n- **Optimal Solution:** Use ArrayPool",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0
        };
        var taintedQuiz2 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "ASP.NET",
            Category = Category.BackendRuntime,
            Level = QuizLevel.Senior,
            QuestionText = "[Senior] Câu hỏi #1 về ASP.NET Core: Phương án nào tối ưu?",
            ExplanationMarkdown = "### Phân Tích Kỹ Thuật Chuyên Sâu\n- **Phương án đúng:** Sử dụng ArrayPool",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0
        };
        var taintedQuiz3 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Postgres",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Middle,
            QuestionText = "How do you avoid table bloat?",
            ExplanationMarkdown = "### Technical Deep Dive\n- **Optimal Solution:** Autovacuum",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0
        };
        await _db.QuizQuestions.AddRangeAsync(curatedQuiz, taintedQuiz1, taintedQuiz2, taintedQuiz3);

        var quizProgress = new UserQuizProgress
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuestionId = taintedQuiz1.Id,
            IsMastered = true
        };
        await _db.UserQuizProgresses.AddAsync(quizProgress);

        // 4. SpacedRepetitionCards & UserHighlights
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Mastering C#",
            Slug = "mastering-csharp"
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Concurrency",
            OriginalTextMarkdown = "Concurrency text",
            SummaryMarkdown = "Summary",
            Embedding = dummyVector
        };
        var unvecChunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 2,
            ChapterTitle = "Memory",
            OriginalTextMarkdown = "Memory text",
            SummaryMarkdown = "Summary",
            Embedding = null
        };
        var unvecChunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 3,
            ChapterTitle = "Networking",
            OriginalTextMarkdown = "Networking text",
            SummaryMarkdown = "Summary",
            Embedding = null
        };
        await _db.DocumentChunks.AddRangeAsync(chunk, unvecChunk1, unvecChunk2);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "Important paragraph on concurrency",
            Note = "Remember this for review"
        };
        await _db.UserHighlights.AddAsync(highlight);

        var curatedCard = new SpacedRepetitionCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SourceType = CardSourceType.Topic,
            FrontMarkdown = "What is the difference between Task and ValueTask?",
            BackMarkdown = "Task allocates an object on the managed heap, whereas ValueTask is a struct."
        };
        var taintedCard1 = new SpacedRepetitionCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SourceType = CardSourceType.Highlight,
            SourceHighlightId = highlight.Id,
            FrontMarkdown = "What is the core architectural principle behind: Important paragraph on concurrency",
            BackMarkdown = "Remember this for review\n\n> Important paragraph on concurrency"
        };
        var taintedCard2 = new SpacedRepetitionCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SourceType = CardSourceType.Highlight,
            SourceHighlightId = highlight.Id,
            FrontMarkdown = "Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong Concurrency",
            BackMarkdown = "Note\n\n> Quote"
        };
        await _db.SpacedRepetitionCards.AddRangeAsync(curatedCard, taintedCard1, taintedCard2);

        await _db.SaveChangesAsync();

        return (curatedTerm.Id, curatedInsight.Id, curatedQuiz.Id, curatedCard.Id, highlight.Id);
    }

    private class FakeEmbeddingService : IEmbeddingService
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
}
