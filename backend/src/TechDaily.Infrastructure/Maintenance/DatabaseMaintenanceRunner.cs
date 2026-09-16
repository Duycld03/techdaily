using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Pgvector;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;

namespace TechDaily.Infrastructure.Maintenance;

public record MaintenanceReport(
    int TermExplanationCachesTainted,
    int TechInsightsTainted,
    int QuizQuestionsTainted,
    int SpacedRepetitionCardsTainted,
    int UnvectorizedDocumentChunks)
{
    public int TotalTaintedContent => TermExplanationCachesTainted + TechInsightsTainted + QuizQuestionsTainted + SpacedRepetitionCardsTainted;
}

public record PurgeReport(
    int TermExplanationCachesPurged,
    int TechInsightsPurged,
    int QuizQuestionsPurged,
    int SpacedRepetitionCardsPurged)
{
    public int TotalPurged => TermExplanationCachesPurged + TechInsightsPurged + QuizQuestionsPurged + SpacedRepetitionCardsPurged;
}

public record BackfillReport(
    int TotalVectorized,
    int TotalBatches = 0,
    int FailedBatches = 0)
{
    public static implicit operator int(BackfillReport report) => report.TotalVectorized;
}

public class DatabaseMaintenanceRunner
{
    private static readonly Regex MockSlugRegex = new(@"-[0-9a-f]{6}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TechDailyDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DatabaseMaintenanceRunner> _logger;

    public DatabaseMaintenanceRunner(
        TechDailyDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<DatabaseMaintenanceRunner> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes the database for tainted fallback records and unvectorized document chunks without mutating any data.
    /// </summary>
    public async Task<MaintenanceReport> AnalyzeTaintedDataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting diagnostic analysis of database content tables...");

        // 1. TermExplanationCaches: boilerplate explanations or missing embeddings
        var taintedTerms = await _dbContext.TermExplanationCaches
            .IgnoreQueryFilters()
            .CountAsync(t => t.ExplanationText.Contains("represents a core runtime or architectural mechanism")
                          || t.ExplanationText.Contains("Khái niệm kỹ thuật quan trọng")
                          || t.Embedding == null, ct);

        // 2. TechInsights: mock slugs ending in -[0-9a-f]{6} or specific mock pool titles
        var insights = await _dbContext.TechInsights
            .IgnoreQueryFilters()
            .Select(i => new { i.Slug, i.Title })
            .ToListAsync(ct);

        var taintedInsights = insights.Count(i =>
            MockSlugRegex.IsMatch(i.Slug)
            || i.Title.StartsWith("Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool", StringComparison.OrdinalIgnoreCase)
            || i.Title.StartsWith("Architecting Non-Blocking Request Ingestion", StringComparison.OrdinalIgnoreCase));

        // 3. QuizQuestions: mock senior question prefixes or technical deep dive markers
        var taintedQuizzes = await _dbContext.QuizQuestions
            .IgnoreQueryFilters()
            .CountAsync(q => q.QuestionText.StartsWith("[Senior] Question #")
                          || q.QuestionText.StartsWith("[Senior] Câu hỏi #")
                          || q.ExplanationMarkdown.Contains("### Technical Deep Dive"), ct);

        // 4. SpacedRepetitionCards: highlight-sourced boilerplate active recall cards
        var taintedCards = await _dbContext.SpacedRepetitionCards
            .IgnoreQueryFilters()
            .CountAsync(c => c.SourceType == CardSourceType.Highlight
                          && ((c.FrontMarkdown != null && c.FrontMarkdown.StartsWith("What is the core architectural principle behind:"))
                           || (c.FrontMarkdown != null && c.FrontMarkdown.StartsWith("Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong"))), ct);

        // 5. DocumentChunks (Unvectorized)
        var unvectorizedChunks = await _dbContext.DocumentChunks
            .IgnoreQueryFilters()
            .CountAsync(c => c.Embedding == null, ct);

        var report = new MaintenanceReport(
            TermExplanationCachesTainted: taintedTerms,
            TechInsightsTainted: taintedInsights,
            QuizQuestionsTainted: taintedQuizzes,
            SpacedRepetitionCardsTainted: taintedCards,
            UnvectorizedDocumentChunks: unvectorizedChunks);

        _logger.LogInformation(
            "Diagnostic Analysis Complete: TermExplanationCaches={Terms}, TechInsights={Insights}, QuizQuestions={Quizzes}, SpacedRepetitionCards={Cards}, UnvectorizedChunks={Chunks}",
            report.TermExplanationCachesTainted,
            report.TechInsightsTainted,
            report.QuizQuestionsTainted,
            report.SpacedRepetitionCardsTainted,
            report.UnvectorizedDocumentChunks);

        return report;
    }

    /// <summary>
    /// Atomically purges tainted fallback records across all 4 content tables within a transaction.
    /// Safely cascades to user bookmarks and quiz progresses, and resets source IDs on cards.
    /// </summary>
    public async Task<PurgeReport> PurgeTaintedDataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting atomic transactional purge of tainted fallback data...");

        var isRelational = _dbContext.Database.IsRelational();
        IDbContextTransaction? tx = null;
        if (isRelational)
        {
            tx = await _dbContext.Database.BeginTransactionAsync(ct);
        }

        try
        {
            // 1. TermExplanationCaches
            var termsToDelete = await _dbContext.TermExplanationCaches
                .IgnoreQueryFilters()
                .Where(t => t.ExplanationText.Contains("represents a core runtime or architectural mechanism")
                         || t.ExplanationText.Contains("Khái niệm kỹ thuật quan trọng")
                         || t.Embedding == null)
                .ToListAsync(ct);
            _dbContext.TermExplanationCaches.RemoveRange(termsToDelete);
            var termsPurged = termsToDelete.Count;

            // 2. TechInsights (and cascade bookmarks)
            var allInsights = await _dbContext.TechInsights
                .IgnoreQueryFilters()
                .ToListAsync(ct);
            var insightsToDelete = allInsights.Where(i =>
                MockSlugRegex.IsMatch(i.Slug)
                || i.Title.StartsWith("Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool", StringComparison.OrdinalIgnoreCase)
                || i.Title.StartsWith("Architecting Non-Blocking Request Ingestion", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            var insightIds = insightsToDelete.Select(i => i.Id).ToList();
            var bookmarksToDelete = await _dbContext.UserInsightBookmarks
                .IgnoreQueryFilters()
                .Where(b => insightIds.Contains(b.InsightId))
                .ToListAsync(ct);
            _dbContext.UserInsightBookmarks.RemoveRange(bookmarksToDelete);
            _dbContext.TechInsights.RemoveRange(insightsToDelete);
            var insightsPurged = insightsToDelete.Count;

            // 3. QuizQuestions (cascade progress; clear source id on cards)
            var quizzesToDelete = await _dbContext.QuizQuestions
                .IgnoreQueryFilters()
                .Where(q => q.QuestionText.StartsWith("[Senior] Question #")
                         || q.QuestionText.StartsWith("[Senior] Câu hỏi #")
                         || q.ExplanationMarkdown.Contains("### Technical Deep Dive"))
                .ToListAsync(ct);

            var quizIds = quizzesToDelete.Select(q => q.Id).ToList();
            var progressToDelete = await _dbContext.UserQuizProgresses
                .IgnoreQueryFilters()
                .Where(p => quizIds.Contains(p.QuestionId))
                .ToListAsync(ct);
            _dbContext.UserQuizProgresses.RemoveRange(progressToDelete);

            var cardsReferencingQuizzes = await _dbContext.SpacedRepetitionCards
                .IgnoreQueryFilters()
                .Where(c => c.SourceQuizQuestionId.HasValue && quizIds.Contains(c.SourceQuizQuestionId.Value))
                .ToListAsync(ct);
            foreach (var card in cardsReferencingQuizzes)
            {
                card.SourceQuizQuestionId = null;
            }

            _dbContext.QuizQuestions.RemoveRange(quizzesToDelete);
            var quizzesPurged = quizzesToDelete.Count;

            // 4. SpacedRepetitionCards (preserving user highlights)
            var cardsToDelete = await _dbContext.SpacedRepetitionCards
                .IgnoreQueryFilters()
                .Where(c => c.SourceType == CardSourceType.Highlight
                         && ((c.FrontMarkdown != null && c.FrontMarkdown.StartsWith("What is the core architectural principle behind:"))
                          || (c.FrontMarkdown != null && c.FrontMarkdown.StartsWith("Nguyên lý kiến trúc cốt lõi đằng sau trích dẫn trong"))))
                .ToListAsync(ct);
            _dbContext.SpacedRepetitionCards.RemoveRange(cardsToDelete);
            var cardsPurged = cardsToDelete.Count;

            await _dbContext.SaveChangesAsync(ct);

            if (tx != null)
            {
                await tx.CommitAsync(ct);
            }

            var report = new PurgeReport(
                TermExplanationCachesPurged: termsPurged,
                TechInsightsPurged: insightsPurged,
                QuizQuestionsPurged: quizzesPurged,
                SpacedRepetitionCardsPurged: cardsPurged);

            _logger.LogInformation(
                "Purge Transaction Committed: TermExplanationCaches={Terms}, TechInsights={Insights}, QuizQuestions={Quizzes}, SpacedRepetitionCards={Cards}",
                report.TermExplanationCachesPurged,
                report.TechInsightsPurged,
                report.QuizQuestionsPurged,
                report.SpacedRepetitionCardsPurged);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed during database purge. Rolling back changes.");
            if (tx != null)
            {
                await tx.RollbackAsync(ct);
            }
            throw;
        }
        finally
        {
            if (tx != null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Invokes TechInsightsSeeder and CurriculumSeeder to restore curated content.
    /// </summary>
    public async Task<int> ReseedCatalogAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Reseeding curated catalog items (TechInsights & 30-Day Curriculum)...");

        await TechInsightsSeeder.SeedAsync(_dbContext);
        await CurriculumSeeder.SeedAsync(_dbContext);

        var insightCount = await _dbContext.TechInsights.IgnoreQueryFilters().CountAsync(ct);
        _logger.LogInformation("Reseed completed successfully. Total curated TechInsights in catalog: {Count}", insightCount);

        return insightCount;
    }

    /// <summary>
    /// Batches through DocumentChunks where Embedding is null, calls IEmbeddingService with gemini-embedding-001 (768-D),
    /// asserts vector dimensions, assigns embeddings, and saves changes.
    /// </summary>
    public async Task<BackfillReport> BackfillEmbeddingsAsync(int batchSize = 25, CancellationToken ct = default)
    {
        if (batchSize < 1) batchSize = 25;

        _logger.LogInformation("Starting batched vector embedding backfill (batchSize={BatchSize})...", batchSize);

        int totalVectorized = 0;
        int totalBatches = 0;
        int failedBatches = 0;

        while (!ct.IsCancellationRequested)
        {
            var unvectorizedChunks = await _dbContext.DocumentChunks
                .IgnoreQueryFilters()
                .Where(c => c.Embedding == null)
                .OrderBy(c => c.DocumentBookId)
                .ThenBy(c => c.ChunkOrder)
                .Take(batchSize)
                .ToListAsync(ct);

            if (unvectorizedChunks.Count == 0)
            {
                _logger.LogInformation("Zero unvectorized DocumentChunks remaining.");
                break;
            }

            totalBatches++;
            _logger.LogInformation("Processing backfill batch #{BatchNumber} with {Count} chunks...", totalBatches, unvectorizedChunks.Count);

            var texts = unvectorizedChunks.Select(c =>
                string.IsNullOrWhiteSpace(c.SummaryMarkdown)
                    ? $"{c.ChapterTitle}: {c.OriginalTextMarkdown[..Math.Min(1000, c.OriginalTextMarkdown.Length)]}"
                    : $"{c.ChapterTitle}: {c.SummaryMarkdown}"
            ).ToList();

            Application.Common.Result<List<Vector>>? embResult = null;
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                embResult = await _embeddingService.GenerateBatchEmbeddingsAsync(texts, ct);
                if (embResult.IsSuccess && embResult.Value != null && embResult.Value.Count == unvectorizedChunks.Count)
                {
                    break;
                }

                var errMsg = embResult?.Error.Message ?? "Count mismatch";
                _logger.LogWarning("Batch #{BatchNumber} attempt {Attempt}/{MaxAttempts} failed: {Error}. Retrying...",
                    totalBatches, attempt, maxAttempts, errMsg);

                if (attempt < maxAttempts)
                {
                    var isRateLimit = errMsg.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                                   || errMsg.Contains("429")
                                   || errMsg.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
                                   || errMsg.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase);

                    TimeSpan delay;
                    if (isRateLimit)
                    {
                        var retryMatch = Regex.Match(errMsg, @"retry in ([\d\.]+)s", RegexOptions.IgnoreCase);
                        if (retryMatch.Success && double.TryParse(retryMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var sec))
                        {
                            delay = TimeSpan.FromSeconds(Math.Ceiling(sec) + 1);
                        }
                        else
                        {
                            delay = TimeSpan.FromSeconds(25);
                        }
                        _logger.LogInformation("Rate limit detected on Batch #{BatchNumber}. Backing off for {Seconds:F1}s before retry...",
                            totalBatches, delay.TotalSeconds);
                    }
                    else
                    {
                        delay = TimeSpan.FromMilliseconds(attempt * 500);
                    }

                    await Task.Delay(delay, ct);
                }
            }

            if (embResult == null || !embResult.IsSuccess || embResult.Value == null || embResult.Value.Count != unvectorizedChunks.Count)
            {
                failedBatches++;
                _logger.LogError("Batch #{BatchNumber} permanently failed after {MaxAttempts} attempts: {Error}",
                    totalBatches, maxAttempts, embResult?.Error.Message ?? "Vector count mismatch or null result");
                break;
            }
            var vectors = embResult.Value;
            for (int i = 0; i < unvectorizedChunks.Count; i++)
            {
                var vector = vectors[i];
                var dims = vector?.ToArray().Length ?? 0;
                if (vector == null || dims != 768)
                {
                    throw new InvalidOperationException(
                        $"Returned embedding vector for chunk {unvectorizedChunks[i].Id} has invalid dimension {dims}; expected 768.");
                }
                unvectorizedChunks[i].Embedding = vector;
            }

            await _dbContext.SaveChangesAsync(ct);
            totalVectorized += unvectorizedChunks.Count;
            _logger.LogInformation("Batch #{BatchNumber} saved. Total vectorized so far: {Total}", totalBatches, totalVectorized);

            if (unvectorizedChunks.Count < batchSize)
            {
                break;
            }

            // Sleep 200ms between batches to respect Gemini rate limits
            await Task.Delay(200, ct);
        }

        var report = new BackfillReport(totalVectorized, totalBatches, failedBatches);
        _logger.LogInformation("Embedding backfill finished: TotalVectorized={Total}, TotalBatches={Batches}, FailedBatches={Failed}",
            report.TotalVectorized, report.TotalBatches, report.FailedBatches);

        return report;
    }
}
