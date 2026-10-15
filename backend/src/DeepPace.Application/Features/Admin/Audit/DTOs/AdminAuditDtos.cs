using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Audit.DTOs;

public record AdminAuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorUserId,
    string Action,
    string TargetType,
    string TargetId,
    AdminAuditOutcome Outcome,
    string? Reason,
    string? ErrorCode,
    string? CorrelationId);

public record AdminAuditEventDetailDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorUserId,
    string? ActorEmail,
    string Action,
    string TargetType,
    string TargetId,
    AdminAuditOutcome Outcome,
    string? Reason,
    string? ErrorCode,
    string? CorrelationId,
    IReadOnlyList<string> ChangedFields,
    IReadOnlyDictionary<string, object?> StatePayload);
