namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for the <c>GET /health</c> liveness probe. <see cref="Error"/> is populated
/// only on the unhealthy (503) path.
/// </summary>
public record HealthStatusResponse(string Status, string Database, string? Error, DateTime Timestamp);
