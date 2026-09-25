namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>GET /api/v1/system/ai-health</c>: the outcome of probing the live
/// Gemini text-generation and embedding endpoints. <see cref="Details"/> carries the per-model
/// breakdown as a dynamic object.
/// </summary>
public record AiHealthResponse(
    string Status,
    string TextModel,
    long TextLatencyMs,
    string EmbeddingModel,
    long EmbeddingLatencyMs,
    int Dimension,
    object? Details,
    DateTime Timestamp,
    string? Error = null);
