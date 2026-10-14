namespace DeepPace.Application.Features.AdminDashboard.DTOs;

public record DocumentProcessingCountsDto(long Pending, long Processing, long Ready, long Failed)
{
    public long Total => Pending + Processing + Ready + Failed;
}

public record AdminCatalogCountsDto(
    long Total, long Draft, long Published, long Hidden, DocumentProcessingCountsDto Processing);

public record AdminDashboardOverviewDto(
    long AccountCount,
    DocumentProcessingCountsDto PersonalDocuments,
    AdminCatalogCountsDto CatalogDocuments,
    DateTimeOffset ObservedAt);

public record ServiceObservationDto(
    string Status, DateTimeOffset CheckedAt, long? LatencyMs, string? ErrorCode = null);

public record AdminDashboardStatusDto(ServiceObservationDto Database, DateTimeOffset ObservedAt);

public record AiServiceObservationDto(
    string? Model,
    string Status,
    long? LatencyMs,
    DateTimeOffset CheckedAt,
    string? ErrorCode = null,
    int? Dimension = null);

public record AdminAiProbeDto(
    AiServiceObservationDto TextGeneration,
    AiServiceObservationDto Embeddings,
    DateTimeOffset CheckedAt)
{
    public bool IsHealthy => TextGeneration.Status == "healthy" && Embeddings.Status == "healthy";
}
