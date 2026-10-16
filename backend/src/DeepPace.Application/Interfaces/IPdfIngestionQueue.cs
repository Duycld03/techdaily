namespace DeepPace.Application.Interfaces;

public enum IngestionDestinationType
{
    PersonalBook = 0,
    CatalogDocument = 1
}

public record PdfIngestJob(
    Guid TargetId,
    string TempFilePath,
    string DocumentTitle,
    Guid CategoryId,
    string Language = "en",
    IngestionDestinationType DestinationType = IngestionDestinationType.PersonalBook,
    long Generation = 0)
{
    public Guid BookId => TargetId;
}

public interface IPdfIngestionQueue
{
    ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default);
}
