using DeepPace.Domain.Enums;

namespace DeepPace.Application.Interfaces;

public record PdfIngestJob(
    Guid BookId,
    string TempFilePath,
    string DocumentTitle,
    Category Category,
    string Language = "en");

public interface IPdfIngestionQueue
{
    ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default);
}
