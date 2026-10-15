namespace DeepPace.Application.Features.Admin.Documents.DTOs;

public record AdminDocumentInventoryItemDto(
    Guid Id,
    string Scope,
    string Title,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerEmail,
    string SourceType,
    string Category,
    string Language,
    string Status,
    string SourceAvailability,
    DateTimeOffset CreatedAt,
    DateTimeOffset ObservedAt);

public record AdminDocumentDetailDto(
    Guid Id,
    string Scope,
    string Title,
    Guid? OwnerUserId,
    string? OwnerName,
    string? OwnerEmail,
    string SourceType,
    string Category,
    string Language,
    string Status,
    string SourceAvailability,
    DateTimeOffset CreatedAt,
    DateTimeOffset ObservedAt,
    int ControlVersion,
    long CurrentAttemptGeneration,
    Guid? CurrentAttemptId,
    bool IsRetryEligible,
    string? IneligibleReasonCode,
    string? IneligibleReason,
    DateTimeOffset? RetentionExpiresAtUtc,
    string? SanitizedSourceUrl);

public record AdminDocumentAttemptItemDto(
    Guid AttemptId,
    Guid TargetDocumentId,
    string Scope,
    string Origin,
    string Stage,
    string State,
    int ProgressPercent,
    DateTimeOffset QueuedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    string? SafeErrorCode,
    string? SafeErrorGuidance);

public record AdminDocumentInventoryQuery(
    string? Search = null,
    string? Scope = null,
    Guid? OwnerId = null,
    string? SourceType = null,
    string? Category = null,
    string? Language = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20);

public record AdminRetryDocumentCommand(
    string Scope,
    Guid DocumentId,
    Guid AttemptId,
    int ExpectedVersion,
    string RequestId,
    string Reason,
    Guid ActorUserId);

public record AdminRetryDocumentResponseDto(
    Guid AttemptId,
    Guid TargetDocumentId,
    string State,
    int ControlVersion);

public record AdminDocumentAttemptsQuery(
    string? State = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int Page = 1,
    int PageSize = 20);
