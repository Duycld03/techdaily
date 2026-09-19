using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly bool _useOfflineMock;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]
               ?? configuration["GEMINI_API_KEY"]
               ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
               ?? string.Empty;
        _model = configuration["Gemini:EmbeddingModel"]
              ?? configuration["GEMINI_EMBEDDING_MODEL"]
              ?? Environment.GetEnvironmentVariable("GEMINI_EMBEDDING_MODEL")
              ?? "gemini-embedding-001";
        _useOfflineMock = configuration.GetValue<bool>("Gemini:UseOfflineMock");
    }

    public async Task<Result<Vector>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Error.Custom("Embedding.EmptyInput", "Text for embedding generation cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            if (_useOfflineMock)
            {
                _logger.LogWarning("Gemini API key is not configured. Returning deterministic mock vector for testing.");
                return GenerateDeterministicMockVector(text);
            }
            return Result<Vector>.Failure(Error.Custom("Embedding.MissingApiKey", "Gemini API key is not configured."));
        }

        try
        {
            var cleanText = text.Length > 2000 ? text[..2000] : text;
            var requestBody = new
            {
                model = $"models/{_model}",
                content = new
                {
                    parts = new[] { new { text = cleanText } }
                },
                outputDimensionality = 768
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:embedContent";
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("embedding", out var embeddingProp) &&
                    embeddingProp.TryGetProperty("values", out var valuesProp))
                {
                    var floats = new float[768];
                    var idx = 0;
                    foreach (var val in valuesProp.EnumerateArray())
                    {
                        if (idx < 768)
                        {
                            floats[idx++] = val.GetSingle();
                        }
                    }
                    return new Vector(floats);
                }
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Gemini Embedding API returned status {Status}: {Error}.", response.StatusCode, errBody);
            if (_useOfflineMock)
            {
                _logger.LogWarning("Using offline mock vector due to API error.");
                return GenerateDeterministicMockVector(text);
            }
            return Result<Vector>.Failure(Error.Custom("Embedding.ApiError", $"Gemini Embedding API returned {response.StatusCode}: {errBody}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to call Gemini Embedding API.");
            if (_useOfflineMock)
            {
                _logger.LogWarning("Using offline mock vector due to exception.");
                return GenerateDeterministicMockVector(text);
            }
            return Result<Vector>.Failure(Error.Custom("Embedding.Exception", ex.Message));
        }
    }

    public async Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(
        List<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return new List<Vector>();
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            if (_useOfflineMock)
            {
                _logger.LogWarning("Gemini API key is not configured. Returning deterministic mock vectors.");
                return texts.Select(GenerateDeterministicMockVector).ToList();
            }
            return Result<List<Vector>>.Failure(Error.Custom("Embedding.MissingApiKey", "Gemini API key is not configured."));
        }

        try
        {
            var requestsPayload = texts.Select(t => new
            {
                model = $"models/{_model}",
                content = new
                {
                    parts = new[] { new { text = t.Length > 2000 ? t[..2000] : t } }
                },
                outputDimensionality = 768
            }).ToList();

            var requestBody = new { requests = requestsPayload };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:batchEmbedContents";
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("embeddings", out var embeddingsProp))
                {
                    var results = new List<Vector>();
                    foreach (var emb in embeddingsProp.EnumerateArray())
                    {
                        if (emb.TryGetProperty("values", out var valuesProp))
                        {
                            var floats = new float[768];
                            var idx = 0;
                            foreach (var val in valuesProp.EnumerateArray())
                            {
                                if (idx < 768)
                                {
                                    floats[idx++] = val.GetSingle();
                                }
                            }
                            results.Add(new Vector(floats));
                        }
                    }

                    if (results.Count == texts.Count)
                    {
                        return results;
                    }
                }
            }

            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini Batch Embedding API failed with status {Status}: {Error}", response.StatusCode, errBody);
            if (_useOfflineMock)
            {
                _logger.LogWarning("Using offline mock fallback for batch embeddings.");
                return texts.Select(GenerateDeterministicMockVector).ToList();
            }
            return Result<List<Vector>>.Failure(Error.Custom("Embedding.ApiError", $"Gemini Batch Embedding API returned {response.StatusCode}: {errBody}"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Gemini Batch Embedding API exception.");
            if (_useOfflineMock)
            {
                _logger.LogWarning("Using offline mock fallback for batch embeddings.");
                return texts.Select(GenerateDeterministicMockVector).ToList();
            }
            return Result<List<Vector>>.Failure(Error.Custom("Embedding.Exception", ex.Message));
        }
    }
    private static Vector GenerateDeterministicMockVector(string text)
    {
        var floats = new float[768];
        var seed = 0;
        foreach (var ch in text)
        {
            seed = (seed * 31) ^ ch;
        }

        var rng = new Random(seed);
        double sumSq = 0;
        for (var i = 0; i < 768; i++)
        {
            var val = (float)(rng.NextDouble() * 2.0 - 1.0);
            floats[i] = val;
            sumSq += val * val;
        }

        // Normalize to unit vector so cosine similarity calculations work realistically
        var norm = (float)Math.Sqrt(sumSq);
        if (norm > 0)
        {
            for (var i = 0; i < 768; i++)
            {
                floats[i] /= norm;
            }
        }

        return new Vector(floats);
    }
}
