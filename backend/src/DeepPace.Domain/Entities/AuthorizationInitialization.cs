namespace DeepPace.Domain.Entities;

public class AuthorizationInitialization
{
    public const string DefaultScope = "database-roles-v1";

    public string Scope { get; set; } = DefaultScope;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Guid> ImportedAdminUserIds { get; set; } = new();

    public AuthorizationInitialization() { }

    public static AuthorizationInitialization Create(
        IEnumerable<Guid> adminUserIds,
        DateTimeOffset? completedAt = null,
        string scope = DefaultScope)
    {
        var sorted = adminUserIds.Distinct().OrderBy(id => id).ToList();
        return new AuthorizationInitialization
        {
            Scope = scope,
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow,
            ImportedAdminUserIds = sorted
        };
    }
}
