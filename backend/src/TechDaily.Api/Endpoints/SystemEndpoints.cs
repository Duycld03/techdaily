using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TechDaily.Application.Interfaces;

namespace TechDaily.Api.Endpoints;

public static class SystemEndpoints
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ai-health", async (
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IEmbeddingService embeddingService,
            CancellationToken ct) =>
        {
            var textModel = configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";
            var embeddingModel = configuration["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";
            var apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Results.Json(new
                {
                    status = "unhealthy",
                    textModel = "unhealthy",
                    embeddingModel = "unhealthy",
                    dimension = (int?)null,
                    error = "Gemini API key is not configured.",
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }

            long textLatencyMs = 0;
            string? textError = null;

            var textStopwatch = Stopwatch.StartNew();
            var textTask = Task.Run(async () =>
            {
                try
                {
                    var httpClient = httpClientFactory.CreateClient();
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{textModel}:generateContent";
                    var payload = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = "ping" } } }
                        }
                    };
                    var json = JsonSerializer.Serialize(payload);
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("x-goog-api-key", apiKey);
                    var response = await httpClient.SendAsync(request, ct);
                    textStopwatch.Stop();
                    textLatencyMs = textStopwatch.ElapsedMilliseconds;

                    if (!response.IsSuccessStatusCode)
                    {
                        var err = await response.Content.ReadAsStringAsync(ct);
                        textError = $"Text model {textModel} returned HTTP {(int)response.StatusCode}: {err}";
                    }
                }
                catch (Exception ex)
                {
                    textStopwatch.Stop();
                    textLatencyMs = textStopwatch.ElapsedMilliseconds;
                    textError = $"Text probe failed: {ex.Message}";
                }
            }, ct);

            long embLatencyMs = 0;
            int? returnedDimension = null;
            string? embError = null;

            var embStopwatch = Stopwatch.StartNew();
            var embTask = Task.Run(async () =>
            {
                try
                {
                    var embResult = await embeddingService.GenerateEmbeddingAsync("health check", ct);
                    embStopwatch.Stop();
                    embLatencyMs = embStopwatch.ElapsedMilliseconds;

                    if (!embResult.IsSuccess)
                    {
                        embError = $"Embedding probe failed: {embResult.Error.Message}";
                        return;
                    }

                    var dims = embResult.Value.ToArray().Length;
                    returnedDimension = dims;
                    if (dims != 768)
                    {
                        embError = $"Embedding probe failed: Expected 768 dimensions but received {dims}";
                    }
                }
                catch (Exception ex)
                {
                    embStopwatch.Stop();
                    embLatencyMs = embStopwatch.ElapsedMilliseconds;
                    embError = $"Embedding probe failed: {ex.Message}";
                }
            }, ct);

            await Task.WhenAll(textTask, embTask);

            if (textError != null || embError != null)
            {
                var combinedError = string.Join("; ", new[] { textError, embError }.Where(e => !string.IsNullOrEmpty(e)));
                return Results.Json(new
                {
                    status = "unhealthy",
                    textModel = textError == null ? textModel : "unhealthy",
                    embeddingModel = embError == null ? embeddingModel : "unhealthy",
                    textLatencyMs,
                    embeddingLatencyMs = embLatencyMs,
                    dimension = returnedDimension,
                    error = combinedError,
                    details = new
                    {
                        text = new
                        {
                            model = textModel,
                            status = textError == null ? "healthy" : "unhealthy",
                            latencyMs = textLatencyMs,
                            error = textError
                        },
                        embedding = new
                        {
                            model = embeddingModel,
                            status = embError == null ? "healthy" : "unhealthy",
                            dimension = returnedDimension,
                            latencyMs = embLatencyMs,
                            error = embError
                        }
                    },
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }

            return Results.Ok(new
            {
                status = "healthy",
                textModel = "gemini-3.5-flash-lite",
                textLatencyMs,
                embeddingModel = "gemini-embedding-001",
                embeddingLatencyMs = embLatencyMs,
                dimension = 768,
                details = new
                {
                    text = new
                    {
                        model = textModel,
                        status = "healthy",
                        latencyMs = textLatencyMs
                    },
                    embedding = new
                    {
                        model = embeddingModel,
                        status = "healthy",
                        dimension = 768,
                        latencyMs = embLatencyMs
                    }
                },
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetAiHealth")
        .WithSummary("Probe AI Health")
        .WithDescription("Probes live Google Gemini text generation and embedding endpoints.")
        .AllowAnonymous();

        return group;
    }
}
