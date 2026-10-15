using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Interfaces;

namespace DeepPace.Infrastructure.Services;

public class LocalFileIngestionSourceStorage : IIngestionSourceStorage
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileIngestionSourceStorage> _logger;

    public LocalFileIngestionSourceStorage(
        IConfiguration configuration,
        ILogger<LocalFileIngestionSourceStorage> logger)
    {
        _logger = logger;
        _storageRoot = configuration["Ingestion:SourceStoragePath"]
            ?? configuration["Ingestion__SourceStoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage", "sources");

        try
        {
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ingestion source storage directory at {Path}", _storageRoot);
        }
    }

    public bool IsStorageAccessible()
    {
        try
        {
            if (!Directory.Exists(_storageRoot))
            {
                return false;
            }

            var testFile = Path.Combine(_storageRoot, $".probe_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "probe");
            File.Delete(testFile);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage probe failed at {Path}", _storageRoot);
            return false;
        }
    }

    public async Task<string> StoreSourceAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!IsStorageAccessible())
        {
            throw new InvalidOperationException($"Ingestion source storage is not accessible at root '{_storageRoot}'.");
        }

        var safeExtension = Path.GetExtension(fileName);
        var opaqueFileName = $"{Guid.NewGuid():N}{safeExtension}";
        var destinationPath = Path.Combine(_storageRoot, opaqueFileName);

        // Canonical path check to prevent directory traversal
        var fullPath = Path.GetFullPath(destinationPath);
        var canonicalRoot = Path.GetFullPath(_storageRoot);
        if (!fullPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Illegal path traversal detected in source storage.");
        }

        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        return opaqueFileName;
    }

    public Task<Stream> OpenSourceStreamAsync(string storageIdentity, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveAndValidatePath(storageIdentity);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Source file was not found at identity '{storageIdentity}'.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> DeleteSourceAsync(string storageIdentity, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = ResolveAndValidatePath(storageIdentity);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to physically delete ingestion source '{Identity}'", storageIdentity);
            return Task.FromResult(false);
        }
    }

    private string ResolveAndValidatePath(string storageIdentity)
    {
        if (string.IsNullOrWhiteSpace(storageIdentity))
        {
            throw new ArgumentException("Storage identity cannot be empty.", nameof(storageIdentity));
        }

        // Must be a simple file name without subdirectories or separators
        var cleanName = Path.GetFileName(storageIdentity);
        var targetPath = Path.Combine(_storageRoot, cleanName);
        var fullPath = Path.GetFullPath(targetPath);
        var canonicalRoot = Path.GetFullPath(_storageRoot);

        if (!fullPath.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path traversal or escape attempt detected.");
        }

        return fullPath;
    }
}
