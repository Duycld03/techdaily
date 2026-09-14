using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Infrastructure.Workers;

public class PdfIngestionWorker : BackgroundService
{
    private readonly IPdfIngestionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PdfIngestionWorker> _logger;

    public PdfIngestionWorker(
        IPdfIngestionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PdfIngestionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PdfIngestionWorker started listening for PDF ingestion jobs.");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing PDF ingestion for book {BookId} ({Title})", job.BookId, job.DocumentTitle);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();
                var pdfExtractor = scope.ServiceProvider.GetRequiredService<IPdfExtractor>();
                var lookAheadService = scope.ServiceProvider.GetService<ILookAheadBufferService>();
                var aiFormatter = scope.ServiceProvider.GetService<IAiMarkdownFormatter>();

                var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.BookId, stoppingToken);
                if (book == null)
                {
                    _logger.LogWarning("Book {BookId} not found in database. Skipping job.", job.BookId);
                    continue;
                }

                if (!File.Exists(job.TempFilePath))
                {
                    book.Status = ProcessingStatus.Failed;
                    book.ErrorMessage = "Temporary file not found.";
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                book.Status = ProcessingStatus.Processing;
                book.ProgressPercentage = 5;
                book.StatusMessage = "Analyzing document structure and bookmarks...";
                await dbContext.SaveChangesAsync(stoppingToken);

                var progress = new Progress<PdfExtractionProgress>(p =>
                {
                    _ = UpdateProgressAsync(job.BookId, p);
                });

                PdfExtractionResult result;
                await using (var fileStream = new FileStream(job.TempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
                {
                    result = await pdfExtractor.ExtractSlicesAsync(
                        fileStream,
                        job.DocumentTitle,
                        maxPages: 10000,
                        progress: progress,
                        cancellationToken: stoppingToken);
                }

                if (result.Slices.Count == 0)
                {
                    book.Status = ProcessingStatus.Failed;
                    book.ErrorMessage = "No readable text or chapters could be extracted from this PDF.";
                    await dbContext.SaveChangesAsync(stoppingToken);
                    continue;
                }

                book.StatusMessage = "Persisting chapters and slices...";
                book.ProgressPercentage = 8;
                await dbContext.SaveChangesAsync(stoppingToken);

                var chunks = new List<DocumentChunk>();
                foreach (var slice in result.Slices)
                {
                    var summaryText = SanitizeSummary(slice.ContentMarkdown, slice.ChapterTitle);

                    var chunk = new DocumentChunk
                    {
                        DocumentBookId = book.Id,
                        ChunkOrder = slice.Order,
                        ChapterTitle = slice.ChapterTitle,
                        OriginalTextMarkdown = slice.ContentMarkdown,
                        SummaryMarkdown = summaryText,
                        KeyTakeaways = slice.KeyTakeaways,
                        Language = job.Language,
                        EstimatedReadMinutes = slice.EstimatedReadMinutes,
                        IsAiFormatted = false
                    };
                    chunks.Add(chunk);
                }

                await dbContext.DocumentChunks.AddRangeAsync(chunks, stoppingToken);
                book.TotalChunks = chunks.Count;
                await dbContext.SaveChangesAsync(stoppingToken);

                // Phase 1: Rapid Availability - AI format initial slices 1..3
                int initialCount = Math.Min(3, chunks.Count);
                if (aiFormatter != null)
                {
                    for (int i = 0; i < initialCount; i++)
                    {
                        var chunk = chunks[i];
                        try
                        {
                            book.StatusMessage = $"AI is curating initial slice {i + 1}/{chunks.Count}...";
                            await dbContext.SaveChangesAsync(stoppingToken);

                            var aiResult = await aiFormatter.FormatSliceAsync(
                                chunk.OriginalTextMarkdown,
                                chunk.ChapterTitle,
                                chunk.Language,
                                stoppingToken);

                            if (aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                            {
                                chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                                chunk.SummaryMarkdown = aiResult.Value.SummaryMarkdown;
                                chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
                                chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                                chunk.IsAiFormatted = true;

                                if (aiResult.Value.ScenarioDrill != null)
                                {
                                    var drill = aiResult.Value.ScenarioDrill;
                                    chunk.MicroQuiz = new Domain.ValueObjects.MicroQuizVo
                                    {
                                        Question = drill.QuestionText,
                                        Options = drill.Options,
                                        AnswerIndex = drill.CorrectOptionIndex,
                                        Explanation = drill.ExplanationMarkdown
                                    };

                                    var question = new Domain.Entities.InterviewQuestion
                                    {
                                        DocumentChunkId = chunk.Id,
                                        QuestionText = drill.QuestionText,
                                        Options = drill.Options,
                                        CorrectOptionIndex = drill.CorrectOptionIndex,
                                        ExplanationMarkdown = drill.ExplanationMarkdown,
                                        ExpectedKeyPoints = drill.ExpectedKeyPoints,
                                        ModelAnswerMarkdown = drill.ExplanationMarkdown,
                                        Difficulty = Domain.Enums.Difficulty.Senior
                                    };
                                    await dbContext.InterviewQuestions.AddAsync(question, stoppingToken);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to format initial slice {Order} with AI for book {BookId}", chunk.ChunkOrder, book.Id);
                        }
                    }
                }

                book.ProgressPercentage = 100;
                book.Status = ProcessingStatus.Ready;
                book.StatusMessage = "Ready for reading";
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully ingested book {BookId}: {TotalChunks} slices extracted, initial {InitialCount} slices AI-curated. Book is Ready.", book.Id, chunks.Count, initialCount);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing PDF ingestion for book {BookId}", job.BookId);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();
                    var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.BookId, CancellationToken.None);
                    if (book != null)
                    {
                        book.Status = ProcessingStatus.Failed;
                        book.ErrorMessage = ex.Message;
                        await dbContext.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to update book status to Failed for book {BookId}", job.BookId);
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(job.TempFilePath))
                    {
                        File.Delete(job.TempFilePath);
                        _logger.LogInformation("Cleaned up temp file {TempPath}", job.TempFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file {TempPath}", job.TempFilePath);
                }
            }
        }
    }

    private async Task UpdateProgressAsync(Guid bookId, PdfExtractionProgress p)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();
            var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == bookId);
            if (book != null && book.Status == ProcessingStatus.Processing)
            {
                var pct = p.TotalPages > 0 ? (int)Math.Round((double)p.ProcessedPages / p.TotalPages * 100) : 5;
                book.ProgressPercentage = Math.Clamp(pct, 5, 89);
                book.StatusMessage = p.CurrentStep;
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Minor error updating progress for book {BookId}", bookId);
        }
    }

    private static string SanitizeSummary(string contentMarkdown, string chapterTitle)
    {
        if (string.IsNullOrWhiteSpace(contentMarkdown)) return chapterTitle;

        // 1. Strip leading and inline markdown headings (# Heading, ## Subheading, ### 07/30/2025)
        var cleaned = Regex.Replace(contentMarkdown, @"(?m)^\s*#+\s+[^\n\r]*$", "").Trim();

        // 2. Strip code blocks from summary
        cleaned = Regex.Replace(cleaned, @"(?s)```.*?```", "").Trim();

        // 3. Strip pre-release disclaimer boilerplate
        cleaned = Regex.Replace(cleaned, @"(?i)(?:Important\s+)?This information relates to a pre-release product[^.\n]*\.[^.\n]*\.(?:\s*For the current release[^.\n]*\.)?", "").Trim();

        // 4. Collapse multiple whitespace and newlines
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 15)
        {
            return chapterTitle;
        }

        return cleaned.Length > 280 ? cleaned.Substring(0, 277) + "..." : cleaned;
    }
}
