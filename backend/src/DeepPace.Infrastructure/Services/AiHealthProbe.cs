using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Infrastructure.Services;

public class AiHealthProbe(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IEmbeddingService embeddingService,
    TimeProvider timeProvider) : IAiHealthProbe
{
    public async Task<AdminAiProbeDto> ProbeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var textModel = configuration["Gemini:Model"];
        var embeddingModel = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";
        var apiKey = configuration["Gemini:ApiKey"];
        var configurationError = string.IsNullOrWhiteSpace(apiKey) ? "AI_API_KEY_MISSING"
            : string.IsNullOrWhiteSpace(textModel) ? "AI_MODEL_MISSING" : null;
        if (configurationError != null)
        {
            var checkedAt = timeProvider.GetUtcNow();
            return new AdminAiProbeDto(
                new AiServiceObservationDto(textModel, "unavailable", null, checkedAt, configurationError),
                new AiServiceObservationDto(embeddingModel, "unavailable", null, checkedAt, configurationError), checkedAt);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var textTask = ObserveAsync(textModel!, async ct =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(textModel!)}:generateContent");
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                contents = new[] { new { parts = new[] { new { text = "ping" } } } }
            });
            using var client = httpClientFactory.CreateClient("AiHealthProbe");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return (response.IsSuccessStatusCode ? null
                : response.StatusCode == HttpStatusCode.TooManyRequests ? "AI_RATE_LIMITED" : "AI_PROVIDER_ERROR", (int?)null);
        }, linked.Token, cancellationToken);
        var embeddingTask = ObserveAsync(embeddingModel, async ct =>
        {
            var result = await embeddingService.GenerateEmbeddingAsync("health check", ct);
            if (result.IsFailure) return ("AI_PROVIDER_ERROR", (int?)null);
            var dimension = result.Value.ToArray().Length;
            return (dimension == 768 ? null : "EMBEDDING_DIMENSION_MISMATCH", (int?)dimension);
        }, linked.Token, cancellationToken);

        await Task.WhenAll(textTask, embeddingTask);
        cancellationToken.ThrowIfCancellationRequested();
        return new AdminAiProbeDto(await textTask, await embeddingTask, timeProvider.GetUtcNow());
    }

    private async Task<AiServiceObservationDto> ObserveAsync(
        string model,
        Func<CancellationToken, Task<(string? ErrorCode, int? Dimension)>> probe,
        CancellationToken boundedToken,
        CancellationToken requestToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string? errorCode;
        int? dimension = null;
        try
        {
            (errorCode, dimension) = await probe(boundedToken).WaitAsync(boundedToken);
        }
        catch (OperationCanceledException) when (!requestToken.IsCancellationRequested)
        {
            errorCode = "AI_TIMEOUT";
        }
        catch (Exception) when (!requestToken.IsCancellationRequested)
        {
            errorCode = "AI_UNAVAILABLE";
        }
        requestToken.ThrowIfCancellationRequested();
        return new AiServiceObservationDto(model,
            errorCode == null ? "healthy" : errorCode == "AI_TIMEOUT" || errorCode == "AI_UNAVAILABLE" ? "unavailable" : "unhealthy",
            stopwatch.ElapsedMilliseconds, timeProvider.GetUtcNow(), errorCode, dimension);
    }
}
