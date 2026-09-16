using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class UserPushSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTimeOffset? LastDispatchedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
