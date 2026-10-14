using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Workers.IngestionDestinations;

public class PersonalBookDestination : IIngestionDestination
{
    private readonly ILogger<PersonalBookDestination> _logger;

    public PersonalBookDestination(ILogger<PersonalBookDestination>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PersonalBookDestination>.Instance;
    }

    public IngestionDestinationType DestinationType => IngestionDestinationType.PersonalBook;

    public async Task<bool> CanProcessJobAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book == null)
        {
            _logger.LogWarning("Book {BookId} not found in database. Skipping personal ingestion job.", job.TargetId);
            return false;
        }

        return true;
    }

    public async Task OnProcessingStartedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book != null)
        {
            book.Status = ProcessingStatus.Processing;
            book.ProgressPercentage = 5;
            book.StatusMessage = "Analyzing document structure and bookmarks...";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnProgressUpdatedAsync(PdfIngestJob job, int percentage, string message, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book != null && book.Status == ProcessingStatus.Processing)
        {
            book.ProgressPercentage = Math.Clamp(percentage, 5, 89);
            book.StatusMessage = message;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnMissingFileAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book != null)
        {
            book.Status = ProcessingStatus.Failed;
            book.ErrorMessage = "Temporary file not found.";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnNoSlicesExtractedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book != null)
        {
            book.Status = ProcessingStatus.Failed;
            book.ErrorMessage = "No readable text or chapters could be extracted from this PDF.";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> PersistAndFormatSlicesAsync(
        PdfIngestJob job,
        PdfExtractionResult extractionResult,
        IDeepPaceDbContext dbContext,
        IAiMarkdownFormatter? aiFormatter,
        IEmbeddingService? embeddingService,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book == null) return false;

        book.StatusMessage = "Persisting chapters and slices...";
        book.ProgressPercentage = 8;
        await dbContext.SaveChangesAsync(cancellationToken);

        var chunks = new List<DocumentChunk>();
        foreach (var slice in extractionResult.Slices)
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

        await dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        book.TotalChunks = chunks.Count;
        await dbContext.SaveChangesAsync(cancellationToken);

        int initialCount = Math.Min(3, chunks.Count);
        if (aiFormatter != null)
        {
            for (int i = 0; i < initialCount; i++)
            {
                var chunk = chunks[i];
                try
                {
                    book.StatusMessage = $"AI is curating initial slice {i + 1}/{chunks.Count}...";
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var aiResult = await aiFormatter.FormatSliceAsync(
                        chunk.OriginalTextMarkdown,
                        chunk.ChapterTitle,
                        chunk.Language,
                        book.Category,
                        cancellationToken);

                    if (aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                    {
                        if (book.Category != Category.EngineeringCraft)
                        {
                            chunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                        }

                        chunk.SummaryMarkdown = !string.IsNullOrWhiteSpace(aiResult.Value.SummaryMarkdown)
                            ? aiResult.Value.SummaryMarkdown
                            : aiResult.Value.FormattedMarkdown;
                        chunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
                        chunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                        chunk.IsAiFormatted = true;

                        if (aiResult.Value.ScenarioDrill != null)
                        {
                            var drill = aiResult.Value.ScenarioDrill;
                            var question = new InterviewQuestion
                            {
                                DocumentChunkId = chunk.Id,
                                QuestionText = drill.QuestionText,
                                Options = drill.Options,
                                CorrectOptionIndex = drill.CorrectOptionIndex,
                                ExplanationMarkdown = drill.ExplanationMarkdown,
                                ExpectedKeyPoints = drill.ExpectedKeyPoints,
                                ModelAnswerMarkdown = drill.ExplanationMarkdown,
                                Difficulty = Difficulty.Senior
                            };
                            await dbContext.InterviewQuestions.AddAsync(question, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to format initial slice {Order} with AI for personal book {BookId}", chunk.ChunkOrder, book.Id);
                }
            }
        }

        if (embeddingService != null && chunks.Count > 0)
        {
            try
            {
                var initialToEmbed = chunks.Take(initialCount).ToList();
                var texts = initialToEmbed.Select(c => $"{c.ChapterTitle}: {c.SummaryMarkdown}").ToList();
                var embResult = await embeddingService.GenerateBatchEmbeddingsAsync(texts, cancellationToken);
                if (embResult.IsSuccess && embResult.Value.Count == initialToEmbed.Count)
                {
                    for (int i = 0; i < initialToEmbed.Count; i++)
                    {
                        initialToEmbed[i].Embedding = embResult.Value[i];
                    }
                }
            }
            catch (Exception embEx)
            {
                _logger.LogWarning(embEx, "Failed to generate embeddings for initial slices of personal book {BookId}", book.Id);
            }
        }

        book.ProgressPercentage = 100;
        book.Status = ProcessingStatus.Ready;
        book.StatusMessage = "Ready for reading";
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully ingested personal book {BookId}: {TotalChunks} slices extracted.", book.Id, chunks.Count);
        return true;
    }

    public async Task OnFailedAsync(PdfIngestJob job, string errorMessage, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var book = await dbContext.DocumentBooks.FirstOrDefaultAsync(b => b.Id == job.TargetId, cancellationToken);
        if (book != null)
        {
            book.Status = ProcessingStatus.Failed;
            book.ErrorMessage = errorMessage;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string SanitizeSummary(string contentMarkdown, string chapterTitle)
    {
        if (string.IsNullOrWhiteSpace(contentMarkdown)) return chapterTitle;

        var cleaned = Regex.Replace(contentMarkdown, @"(?m)^\s*#+\s+[^\n\r]*$", "").Trim();
        cleaned = Regex.Replace(cleaned, @"(?s)```.*?```", "").Trim();
        cleaned = Regex.Replace(cleaned, @"(?i)(?:Important\s+)?This information relates to a pre-release product[^.\n]*\.[^.\n]*\.(?:\s*For the current release[^.\n]*\.)?", "").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 15)
        {
            return chapterTitle;
        }

        return cleaned.Length > 280 ? cleaned.Substring(0, 277) + "..." : cleaned;
    }
}
