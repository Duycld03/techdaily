using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class GoogleCloudTtsService : IGoogleCloudTtsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GoogleCloudTtsService> _logger;

    public GoogleCloudTtsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleCloudTtsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Google:TtsApiKey"]
               ?? configuration["GOOGLE_TTS_API_KEY"]
               ?? configuration["Google__TtsApiKey"]
               ?? Environment.GetEnvironmentVariable("Google__TtsApiKey")
               ?? string.Empty;
    }

    public async Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(
        string text,
        string voiceId,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<GoogleCloudTtsResult>.Failure(Error.Custom("GOOGLE_TTS_EMPTY_TEXT", "Text to synthesize cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Google Cloud TTS API key is not configured.");
            return Result<GoogleCloudTtsResult>.Failure(Error.GoogleTtsNotConfigured);
        }

        var resolvedLanguageCode = ResolveLanguageCode(voiceId, languageCode);
        var subBatches = PartitionTextIntoSubBatches(text, 4000);

        if (subBatches.Count <= 1)
        {
            return await SynthesizeSingleBatchAsync(text, voiceId, resolvedLanguageCode, cancellationToken);
        }

        var concatenatedAudio = new List<byte>();
        var totalChars = 0;
        var totalDuration = 0.0;

        foreach (var subBatch in subBatches)
        {
            var batchResult = await SynthesizeSingleBatchAsync(subBatch, voiceId, resolvedLanguageCode, cancellationToken);
            if (batchResult.IsFailure)
            {
                return batchResult.Error;
            }

            concatenatedAudio.AddRange(batchResult.Value.AudioBytes);
            totalChars += batchResult.Value.CharacterCount;
            totalDuration += batchResult.Value.DurationSeconds;
        }

        return Result<GoogleCloudTtsResult>.Success(new GoogleCloudTtsResult(
            concatenatedAudio.ToArray(),
            text.Length,
            Math.Round(totalDuration, 1)));
    }

    private async Task<Result<GoogleCloudTtsResult>> SynthesizeSingleBatchAsync(
        string text,
        string voiceId,
        string resolvedLanguageCode,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            input = new { text },
            voice = new
            {
                languageCode = resolvedLanguageCode,
                name = voiceId
            },
            audioConfig = new
            {
                audioEncoding = "MP3"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var endpoint = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={Uri.EscapeDataString(_apiKey)}";

        try
        {
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Google Cloud TTS failed with status {StatusCode}: {ErrorDetail}", response.StatusCode, errorDetail);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return Result<GoogleCloudTtsResult>.Failure(Error.AudioQuotaExhausted);
                }

                return Result<GoogleCloudTtsResult>.Failure(Error.Custom("GOOGLE_TTS_FAILED", $"Google Cloud TTS request failed with status {response.StatusCode}."));
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("audioContent", out var audioContentProp) ||
                string.IsNullOrWhiteSpace(audioContentProp.GetString()))
            {
                _logger.LogError("Google Cloud TTS response did not contain valid audioContent.");
                return Result<GoogleCloudTtsResult>.Failure(Error.Custom("GOOGLE_TTS_EMPTY_AUDIO", "Google Cloud TTS response did not contain audio data."));
            }

            var audioBytes = Convert.FromBase64String(audioContentProp.GetString()!);
            var characterCount = text.Length;
            var durationSeconds = Math.Max(1.0, Math.Round(text.Length / 15.0, 1));

            return Result<GoogleCloudTtsResult>.Success(new GoogleCloudTtsResult(audioBytes, characterCount, durationSeconds));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while synthesizing audio via Google Cloud TTS.");
            return Result<GoogleCloudTtsResult>.Failure(Error.GoogleTtsFailed);
        }
    }

    public static List<string> PartitionTextIntoSubBatches(string text, int maxBytesPerBatch = 4000)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        if (Encoding.UTF8.GetByteCount(text) <= maxBytesPerBatch)
        {
            return new List<string> { text };
        }

        var rawSentences = Regex.Split(text, @"(?<=[.!?\n\r])\s+");
        var batches = new List<string>();
        var currentBatch = new StringBuilder();

        foreach (var sentence in rawSentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrEmpty(trimmedSentence))
            {
                continue;
            }

            var sentenceBytes = Encoding.UTF8.GetByteCount(trimmedSentence);
            if (sentenceBytes > maxBytesPerBatch)
            {
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString().Trim());
                    currentBatch.Clear();
                }

                var words = trimmedSentence.Split(' ');
                var wordBatch = new StringBuilder();
                foreach (var word in words)
                {
                    var wordToAdd = wordBatch.Length == 0 ? word : " " + word;
                    if (Encoding.UTF8.GetByteCount(wordBatch.ToString() + wordToAdd) > maxBytesPerBatch)
                    {
                        if (wordBatch.Length > 0)
                        {
                            batches.Add(wordBatch.ToString().Trim());
                            wordBatch.Clear();
                        }
                    }
                    wordBatch.Append(wordBatch.Length == 0 ? word : " " + word);
                }
                if (wordBatch.Length > 0)
                {
                    batches.Add(wordBatch.ToString().Trim());
                }
                continue;
            }

            var separator = currentBatch.Length == 0 ? "" : " ";
            var prospectiveBytes = Encoding.UTF8.GetByteCount(currentBatch.ToString() + separator + trimmedSentence);

            if (prospectiveBytes <= maxBytesPerBatch)
            {
                currentBatch.Append(separator).Append(trimmedSentence);
            }
            else
            {
                if (currentBatch.Length > 0)
                {
                    batches.Add(currentBatch.ToString().Trim());
                    currentBatch.Clear();
                }
                currentBatch.Append(trimmedSentence);
            }
        }

        if (currentBatch.Length > 0)
        {
            batches.Add(currentBatch.ToString().Trim());
        }

        return batches;
    }

    private static string ResolveLanguageCode(string voiceId, string? languageCode)
    {
        if (!string.IsNullOrWhiteSpace(voiceId))
        {
            var parts = voiceId.Split('-');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}-{parts[1]}";
            }
        }

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            if (languageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))
            {
                return "vi-VN";
            }
            if (languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
            {
                return "en-US";
            }
            return languageCode;
        }

        return "en-US";
    }
}
