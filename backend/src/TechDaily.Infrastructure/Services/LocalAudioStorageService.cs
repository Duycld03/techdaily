using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class LocalAudioStorageService : IAudioStorageService
{
    private readonly string _storageDirectory;
    private readonly ILogger<LocalAudioStorageService> _logger;

    public LocalAudioStorageService(IConfiguration configuration, ILogger<LocalAudioStorageService> logger)
    {
        _logger = logger;
        var configPath = configuration["Storage:AudioPath"];
        _storageDirectory = !string.IsNullOrWhiteSpace(configPath)
            ? configPath
            : Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "storage", "audios");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> SaveAudioAsync(
        Guid drillId,
        Stream audioStream,
        string fileExtension,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"{drillId}{fileExtension}";
        var fullPath = Path.Combine(_storageDirectory, fileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await audioStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Audio successfully saved to {Path}", fullPath);
        return $"/uploads/audios/{fileName}";
    }

    public string GetAudioUrl(string relativePath)
    {
        return relativePath;
    }
}
