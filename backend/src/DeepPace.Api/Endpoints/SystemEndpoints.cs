using DeepPace.Api.Contracts;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Endpoints;

public static class SystemEndpoints
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ai-health", async (IAiHealthProbe probe, CancellationToken ct) =>
        {
            var observation = await probe.ProbeAsync(ct);
            var text = observation.TextGeneration;
            var embedding = observation.Embeddings;
            var configurationMissing = text.ErrorCode is "AI_API_KEY_MISSING" or "AI_MODEL_MISSING";
            var textError = DescribeFailure(text, true);
            var embeddingError = DescribeFailure(embedding, false);
            var error = configurationMissing ? textError
                : string.Join("; ", new[] { textError, embeddingError }.Where(message => message != null));
            var response = new AiHealthResponse(
                observation.IsHealthy ? "healthy" : "unhealthy",
                text.Status == "healthy" ? text.Model! : "unhealthy", text.LatencyMs ?? 0,
                embedding.Status == "healthy" ? embedding.Model! : "unhealthy", embedding.LatencyMs ?? 0,
                embedding.Dimension ?? 0,
                configurationMissing ? null : new
                {
                    text = new { model = text.Model, status = text.Status == "healthy" ? "healthy" : "unhealthy", latencyMs = text.LatencyMs ?? 0, error = textError },
                    embedding = new { model = embedding.Model, status = embedding.Status == "healthy" ? "healthy" : "unhealthy", dimension = embedding.Dimension, latencyMs = embedding.LatencyMs ?? 0, error = embeddingError }
                },
                observation.CheckedAt.UtcDateTime,
                observation.IsHealthy ? null : error);
            return Results.Json(response, statusCode: observation.IsHealthy ? 200 : 503);
        })
        .WithName("GetAiHealth")
        .WithSummary("Probe AI Health")
        .WithDescription("Probes live Google Gemini text generation and embedding endpoints.")
        .AllowAnonymous()
        .Produces<AiHealthResponse>(StatusCodes.Status200OK)
        .Produces<AiHealthResponse>(StatusCodes.Status503ServiceUnavailable);
        return group;
    }

    private static string? DescribeFailure(AiServiceObservationDto observation, bool isText) => observation.ErrorCode switch
    {
        null => null,
        "AI_API_KEY_MISSING" => "Gemini API key is not configured.",
        "AI_MODEL_MISSING" => "Gemini model is not configured.",
        "EMBEDDING_DIMENSION_MISMATCH" => $"Embedding probe failed: Expected 768 dimensions but received {observation.Dimension}",
        "AI_TIMEOUT" => isText ? "Text model probe timed out." : "Embedding probe timed out.",
        _ => isText ? "Text model probe failed." : "Embedding probe failed."
    };
}
