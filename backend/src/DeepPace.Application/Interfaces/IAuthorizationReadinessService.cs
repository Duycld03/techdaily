namespace DeepPace.Application.Interfaces;

public interface IAuthorizationReadinessService
{
    Task<AuthorizationReadinessStatus> GetReadinessAsync(CancellationToken ct = default);
}

public record AuthorizationReadinessStatus(
    bool IsReady,
    bool IsDatabaseConnected,
    bool HasRequiredSchemaAndRoles,
    bool IsInitialized,
    string? FailureReason = null);
