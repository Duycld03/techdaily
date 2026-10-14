using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Workers.IngestionDestinations;

public class CatalogDocumentDestination : IIngestionDestination
{
    private readonly ILogger<CatalogDocumentDestination> _logger;

    public CatalogDocumentDestination(ILogger<CatalogDocumentDestination>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CatalogDocumentDestination>.Instance;
    }

    public IngestionDestinationType DestinationType => IngestionDestinationType.CatalogDocument;

    public async Task<bool> CanProcessJobAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc == null)
        {
            _logger.LogWarning("CatalogDocument {Id} not found in database. Skipping job.", job.TargetId);
            return false;
        }

        if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
        {
            _logger.LogWarning("CatalogDocument {Id} generation mismatch (DB: {DbGen}, Job: {JobGen}). Stale worker run cancelled.",
                job.TargetId, doc.ProcessingGeneration, job.Generation);
            return false;
        }

        return true;
    }

    public async Task OnProcessingStartedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc != null)
        {
            if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
            {
                _logger.LogWarning("CatalogDocument {Id} generation mismatch on start. Ignoring.", job.TargetId);
                return;
            }

            doc.ProcessingStatus = ProcessingStatus.Processing;
            doc.ProcessingStatusMessage = "Analyzing document structure and extracting slices...";
            doc.ProcessingErrorMessage = null;
            doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnProgressUpdatedAsync(PdfIngestJob job, int percentage, string message, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc != null && doc.ProcessingStatus == ProcessingStatus.Processing)
        {
            if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
            {
                return;
            }

            doc.ProcessingStatusMessage = message;
            doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnMissingFileAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc != null)
        {
            if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
            {
                return;
            }

            doc.ProcessingStatus = ProcessingStatus.Failed;
            doc.ProcessingErrorMessage = "Temporary upload file is no longer available. Please upload the PDF again to retry preparation.";
            doc.ProcessingStatusMessage = "Failed: Missing temporary file";
            doc.ProcessingLeaseExpiresAt = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task OnNoSlicesExtractedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc != null)
        {
            if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
            {
                return;
            }

            doc.ProcessingStatus = ProcessingStatus.Failed;
            doc.ProcessingErrorMessage = "No readable text or chapters could be extracted from this PDF.";
            doc.ProcessingStatusMessage = "Failed: No readable chapters extracted";
            doc.ProcessingLeaseExpiresAt = null;
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
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc == null) return false;

        if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
        {
            _logger.LogWarning("CatalogDocument {Id} generation mismatch before slice persistence. Aborting.", job.TargetId);
            return false;
        }

        // Clean replacement of existing slices to prevent duplicate slices across retries
        var existingSlices = await dbContext.CatalogSlices
            .Where(s => s.CatalogDocumentId == doc.Id)
            .ToListAsync(cancellationToken);
        if (existingSlices.Count > 0)
        {
            dbContext.CatalogSlices.RemoveRange(existingSlices);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var catalogSlices = new List<CatalogSlice>();
        foreach (var slice in extractionResult.Slices)
        {
            var summaryText = SanitizeSummary(slice.ContentMarkdown, slice.ChapterTitle);

            var catalogSlice = new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = slice.Order,
                ChapterTitle = slice.ChapterTitle,
                OriginalTextMarkdown = slice.ContentMarkdown,
                SummaryMarkdown = summaryText,
                KeyTakeaways = slice.KeyTakeaways,
                Language = job.Language,
                EstimatedReadMinutes = slice.EstimatedReadMinutes,
                IsAiFormatted = false
            };
            catalogSlices.Add(catalogSlice);
        }

        await dbContext.CatalogSlices.AddRangeAsync(catalogSlices, cancellationToken);
        doc.TotalSlices = catalogSlices.Count;
        doc.EstimatedReadMinutes = catalogSlices.Sum(s => s.EstimatedReadMinutes);
        doc.ProcessingStatusMessage = "Initial slices persisted. Running initial AI formatting...";
        doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Publication readiness requirement: First min(3, N) slices must be successfully formatted
        int initialCount = Math.Min(3, catalogSlices.Count);
        if (aiFormatter == null)
        {
            doc.ProcessingStatus = ProcessingStatus.Failed;
            doc.ProcessingErrorMessage = "AI Markdown Formatter is not available or model is unconfigured.";
            doc.ProcessingStatusMessage = "Failed: Missing AI configuration";
            doc.ProcessingLeaseExpiresAt = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        for (int i = 0; i < initialCount; i++)
        {
            var slice = catalogSlices[i];
            doc.ProcessingStatusMessage = $"AI formatting initial slice {i + 1}/{initialCount}...";
            doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
            await dbContext.SaveChangesAsync(cancellationToken);

            Result<AiFormattedSliceResult> aiResult;
            try
            {
                aiResult = await aiFormatter.FormatSliceAsync(
                    slice.OriginalTextMarkdown,
                    slice.ChapterTitle,
                    slice.Language,
                    doc.Category,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                aiResult = DeepPace.Application.Common.Error.Custom("Ai.Exception", ex.Message);
            }

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Value?.FormattedMarkdown))
            {
                var errorMsg = aiResult.Error?.Message ?? "AI returned empty formatted content";
                _logger.LogError("Initial AI formatting failed for catalog slice {Order} of document {DocId}: {Error}",
                    slice.SliceOrder, doc.Id, errorMsg);

                doc.ProcessingStatus = ProcessingStatus.Failed;
                doc.ProcessingErrorMessage = $"Initial AI formatting failed for slice {slice.SliceOrder}: {errorMsg}";
                doc.ProcessingStatusMessage = "Failed during initial slice AI formatting";
                doc.ProcessingLeaseExpiresAt = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                return false;
            }

            var val = aiResult.Value!;
            if (doc.Category != Category.EngineeringCraft)
            {
                slice.OriginalTextMarkdown = val.FormattedMarkdown;
            }

            slice.SummaryMarkdown = !string.IsNullOrWhiteSpace(val.SummaryMarkdown)
                ? val.SummaryMarkdown
                : val.FormattedMarkdown;
            slice.KeyTakeaways = val.KeyTakeaways;
            slice.EstimatedReadMinutes = val.EstimatedReadMinutes;
            slice.IsAiFormatted = true;
        }

        if (embeddingService != null && catalogSlices.Count > 0)
        {
            try
            {
                var initialToEmbed = catalogSlices.Take(initialCount).ToList();
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
                _logger.LogWarning(embEx, "Failed to generate embeddings for initial slices of catalog doc {DocId}", doc.Id);
            }
        }

        // Check generation again before marking ready
        if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
        {
            _logger.LogWarning("CatalogDocument {Id} generation changed during processing. Aborting completion.", job.TargetId);
            return false;
        }

        doc.ProcessingStatus = ProcessingStatus.Ready;
        doc.ProcessingStatusMessage = "Preparation complete. Ready for review.";
        doc.ProcessingErrorMessage = null;
        doc.ProcessingLeaseExpiresAt = null;
        doc.InvalidateReviewDueToContentChange();
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully prepared catalog document {DocId}: {TotalSlices} slices created, {InitialCount} AI-formatted.",
            doc.Id, catalogSlices.Count, initialCount);
        return true;
    }

    public async Task OnFailedAsync(PdfIngestJob job, string errorMessage, IDeepPaceDbContext dbContext, CancellationToken cancellationToken)
    {
        var doc = await dbContext.CatalogDocuments.FirstOrDefaultAsync(d => d.Id == job.TargetId, cancellationToken);
        if (doc != null)
        {
            if (job.Generation > 0 && doc.ProcessingGeneration != job.Generation)
            {
                return;
            }

            doc.ProcessingStatus = ProcessingStatus.Failed;
            doc.ProcessingErrorMessage = errorMessage;
            doc.ProcessingStatusMessage = "Preparation failed";
            doc.ProcessingLeaseExpiresAt = null;
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
