using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Workers.IngestionDestinations;

namespace DeepPace.Infrastructure.Workers;

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

        // Verify database and schema readiness before admitting work (mandatory readiness; fail loud to stop host)
        using (var readyScope = _scopeFactory.CreateScope())
        {
            var readiness = readyScope.ServiceProvider.GetRequiredService<IAuthorizationReadinessService>();
            var status = await readiness.GetReadinessAsync(stoppingToken);
            if (!status.IsReady || !status.HasRequiredSchemaAndRoles)
            {
                _logger.LogError("PdfIngestionWorker startup failed: required authorization schema or seeds not ready ({Reason})", status.FailureReason);
                throw new InvalidOperationException($"PdfIngestionWorker startup failed: required authorization schema or seeds not ready ({status.FailureReason})");
            }
        }

        // Interrupted-work recovery on worker startup
        await RecoverInterruptedJobsAsync(stoppingToken);

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing PDF ingestion for target {TargetId} ({Title}) [Destination: {Destination}]",
                job.TargetId, job.DocumentTitle, job.DestinationType);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDeepPaceDbContext>();
            var pdfExtractor = scope.ServiceProvider.GetRequiredService<IPdfExtractor>();
            var aiFormatter = scope.ServiceProvider.GetService<IAiMarkdownFormatter>();
            var embeddingService = scope.ServiceProvider.GetService<IEmbeddingService>();
            try
            {
                var destination = ResolveDestination(scope.ServiceProvider, job.DestinationType);
                if (!await destination.CanProcessJobAsync(job, dbContext, stoppingToken))
                {
                    continue;
                }

                if (!File.Exists(job.TempFilePath))
                {
                    _logger.LogWarning("Temporary file {TempPath} not found for job {TargetId}", job.TempFilePath, job.TargetId);
                    await destination.OnMissingFileAsync(job, dbContext, stoppingToken);
                    continue;
                }

                await destination.OnProcessingStartedAsync(job, dbContext, stoppingToken);

                var progress = new Progress<PdfExtractionProgress>(p =>
                {
                    var pct = p.TotalPages > 0 ? (int)Math.Round((double)p.ProcessedPages / p.TotalPages * 100) : 5;
                    _ = destination.OnProgressUpdatedAsync(job, pct, p.CurrentStep, dbContext, CancellationToken.None);
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
                    _logger.LogWarning("No slices extracted from PDF for job {TargetId}", job.TargetId);
                    await destination.OnNoSlicesExtractedAsync(job, dbContext, stoppingToken);
                    continue;
                }

                await destination.PersistAndFormatSlicesAsync(
                    job,
                    result,
                    dbContext,
                    aiFormatter,
                    embeddingService,
                    stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing PDF ingestion for job {TargetId}", job.TargetId);
                try
                {
                    using var errScope = _scopeFactory.CreateScope();
                    var errDb = errScope.ServiceProvider.GetRequiredService<IDeepPaceDbContext>();
                    var errDestination = ResolveDestination(errScope.ServiceProvider, job.DestinationType);
                    await errDestination.OnFailedAsync(job, ex.Message, errDb, CancellationToken.None);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to persist failure status for job {TargetId}", job.TargetId);
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

    private static IIngestionDestination ResolveDestination(IServiceProvider sp, IngestionDestinationType type)
    {
        return type switch
        {
            IngestionDestinationType.CatalogDocument =>
                sp.GetService<CatalogDocumentDestination>()
                ?? new CatalogDocumentDestination(
                    sp.GetService<ILogger<CatalogDocumentDestination>>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CatalogDocumentDestination>.Instance),
            _ =>
                sp.GetService<PersonalBookDestination>()
                ?? new PersonalBookDestination(
                    sp.GetService<ILogger<PersonalBookDestination>>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PersonalBookDestination>.Instance)
        };
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDeepPaceDbContext>();

            // 1. Recover Catalog Documents
            var interruptedCatalogDocs = await dbContext.CatalogDocuments
                .Where(d => d.ProcessingStatus == ProcessingStatus.Processing)
                .ToListAsync(cancellationToken);

            foreach (var doc in interruptedCatalogDocs)
            {
                doc.ProcessingStatus = ProcessingStatus.Failed;
                doc.ProcessingStatusMessage = "Interrupted - retry available";
                doc.ProcessingErrorMessage = "Preparation was interrupted by system shutdown or worker restart. Please retry preparation.";
                doc.ProcessingLeaseExpiresAt = null;
            }

            // 2. Recover Personal Books
            var interruptedBooks = await dbContext.DocumentBooks
                .Where(b => b.Status == ProcessingStatus.Processing)
                .ToListAsync(cancellationToken);

            foreach (var book in interruptedBooks)
            {
                book.Status = ProcessingStatus.Failed;
                book.StatusMessage = "Interrupted by system restart";
                book.ErrorMessage = "Document processing was interrupted by a server restart. Please import or retry.";
            }

            // 3. Recover Ingestion Attempts
            var interruptedAttempts = await dbContext.IngestionAttempts
                .Where(a => a.State == IngestionAttemptState.Running)
                .ToListAsync(cancellationToken);

            foreach (var attempt in interruptedAttempts)
            {
                attempt.MarkInterrupted(DateTimeOffset.UtcNow);
            }

            if (interruptedCatalogDocs.Count > 0 || interruptedBooks.Count > 0 || interruptedAttempts.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Startup recovery completed: {CatalogCount} catalog docs, {BookCount} personal books, {AttemptCount} attempts marked recoverable.",
                    interruptedCatalogDocs.Count, interruptedBooks.Count, interruptedAttempts.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown cancellation during startup recovery
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed during interrupted job recovery on startup.");
        }
    }
}
