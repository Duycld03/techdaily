namespace DeepPace.Application.Interfaces;

public interface IIngestionSourceStorage
{
    Task<string> StoreSourceAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream> OpenSourceStreamAsync(string storageIdentity, CancellationToken cancellationToken = default);
    Task<bool> DeleteSourceAsync(string storageIdentity, CancellationToken cancellationToken = default);
    bool IsStorageAccessible();
}
