using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class AdminAuditEvent
{
    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public string TargetId { get; private set; } = string.Empty;
    public AdminAuditOutcome Outcome { get; private set; }
    public string? Reason { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? CorrelationId { get; private set; }
    public List<string> ChangedFields { get; private set; } = new();
    public string? StatePayload { get; private set; }

    // Parameterless constructor for EF Core
    private AdminAuditEvent() { }

    public static AdminAuditEvent Create(
        Guid actorUserId,
        string action,
        string targetType,
        string targetId,
        AdminAuditOutcome outcome,
        string? reason = null,
        string? errorCode = null,
        string? correlationId = null,
        IEnumerable<string>? changedFields = null,
        string? statePayload = null,
        Guid? id = null,
        DateTimeOffset? occurredAt = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        }
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        }
        if (string.IsNullOrWhiteSpace(targetType))
        {
            throw new ArgumentException("Target type cannot be empty.", nameof(targetType));
        }
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID cannot be empty.", nameof(targetId));
        }

        string? boundedReason = null;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var trimmed = reason.Trim();
            if (trimmed.Length > 500)
            {
                throw new ArgumentException("Reason cannot exceed 500 characters.", nameof(reason));
            }
            if (ContainsSensitivePatterns(trimmed))
            {
                throw new ArgumentException("Reason contains sensitive credential or token patterns.", nameof(reason));
            }
            boundedReason = trimmed;
        }

        return new AdminAuditEvent
        {
            Id = id ?? Guid.NewGuid(),
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
            ActorUserId = actorUserId,
            Action = action.Trim(),
            TargetType = targetType.Trim(),
            TargetId = targetId.Trim(),
            Outcome = outcome,
            Reason = boundedReason,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
            ChangedFields = changedFields?.ToList() ?? new List<string>(),
            StatePayload = string.IsNullOrWhiteSpace(statePayload) ? null : statePayload
        };
    }

    public static bool ContainsSensitivePatterns(string text)
    {
        if (text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("eyJh", StringComparison.OrdinalIgnoreCase) ||
            (text.Contains("password", StringComparison.OrdinalIgnoreCase) && text.Contains(":", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return false;
    }
}
