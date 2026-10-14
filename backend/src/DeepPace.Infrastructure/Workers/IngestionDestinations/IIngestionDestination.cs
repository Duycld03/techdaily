using DeepPace.Application.Interfaces;

namespace DeepPace.Infrastructure.Workers.IngestionDestinations;

public interface IIngestionDestination
{
    IngestionDestinationType DestinationType { get; }

    Task<bool> CanProcessJobAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);

    Task OnProcessingStartedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);

    Task OnProgressUpdatedAsync(PdfIngestJob job, int percentage, string message, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);

    Task OnMissingFileAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);

    Task OnNoSlicesExtractedAsync(PdfIngestJob job, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);

    Task<bool> PersistAndFormatSlicesAsync(
        PdfIngestJob job,
        PdfExtractionResult extractionResult,
        IDeepPaceDbContext dbContext,
        IAiMarkdownFormatter? aiFormatter,
        IEmbeddingService? embeddingService,
        CancellationToken cancellationToken);

    Task OnFailedAsync(PdfIngestJob job, string errorMessage, IDeepPaceDbContext dbContext, CancellationToken cancellationToken);
}
