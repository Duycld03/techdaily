using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? GoogleSubjectId { get; set; }
    public string? PasswordHash { get; set; }
    public string PreferredLocale { get; set; } = "en"; // "en" or "vi"
    public string TargetRole { get; set; } = "Deep Work & Focus Practitioner";
    public int DailyGoalMinutes { get; set; } = 10;
    public TimeOnly? PreferredStudyTime { get; set; } = new TimeOnly(8, 0);
    public TimeOnly? StreakAlertTime { get; set; } = new TimeOnly(20, 0);
    public string TimeZone { get; set; } = "UTC";
    public bool IsPushEnabled { get; set; } = false;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public Guid RoleId { get; set; } = RoleDefaults.LearnerRoleId;
    public int AuthenticationVersion { get; set; } = 1;
    public int AdminControlVersion { get; set; } = 1;
    public Guid? SuspendedByUserId { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }

    // Navigation properties
    public Role? Role { get; set; }
    public StreakRecord? StreakRecord { get; set; }
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
    public ICollection<SpacedRepetitionCard> SpacedRepetitionCards { get; set; } = new List<SpacedRepetitionCard>();
    public ICollection<UserHighlight> UserHighlights { get; set; } = new List<UserHighlight>();
    public ICollection<UserQuizProgress> QuizProgresses { get; set; } = new List<UserQuizProgress>();
    public ICollection<UserBookPacer> BookPacers { get; set; } = new List<UserBookPacer>();
    public ICollection<UserPushSubscription> PushSubscriptions { get; set; } = new List<UserPushSubscription>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public void Suspend(Guid actorUserId, string reason, DateTimeOffset? now = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        }
        if (actorUserId == Id)
        {
            throw new InvalidOperationException("An administrator cannot suspend their own account.");
        }
        if (Status == AccountStatus.Suspended)
        {
            throw new InvalidOperationException("Account is already suspended.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Suspension reason cannot be empty.", nameof(reason));
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Suspension reason cannot exceed 500 characters.", nameof(reason));
        }

        Status = AccountStatus.Suspended;
        SuspendedByUserId = actorUserId;
        SuspendedAt = now ?? DateTimeOffset.UtcNow;
        SuspensionReason = trimmed;
        AuthenticationVersion++;
        AdminControlVersion++;
        MarkUpdated();
    }

    public void Reactivate(Guid actorUserId, string reason, DateTimeOffset? now = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        }
        if (Status != AccountStatus.Suspended)
        {
            throw new InvalidOperationException("Account is not suspended.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reactivation reason cannot be empty.", nameof(reason));
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Reactivation reason cannot exceed 500 characters.", nameof(reason));
        }

        Status = AccountStatus.Active;
        SuspendedByUserId = null;
        SuspendedAt = null;
        SuspensionReason = null;
        AdminControlVersion++;
        MarkUpdated();
    }

    public void RevokeAllSessions(Guid actorUserId, string reason, DateTimeOffset? now = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        }
        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Cannot revoke sessions for an inactive account.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Revocation reason cannot be empty.", nameof(reason));
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Revocation reason cannot exceed 500 characters.", nameof(reason));
        }

        AuthenticationVersion++;
        AdminControlVersion++;
        MarkUpdated();
    }

    public void AssignRole(Guid actorUserId, Guid newRoleId, string reason, DateTimeOffset? now = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));
        }
        if (actorUserId == Id)
        {
            throw new InvalidOperationException("An administrator cannot change their own role.");
        }
        if (newRoleId == Guid.Empty)
        {
            throw new ArgumentException("New role ID cannot be empty.", nameof(newRoleId));
        }
        if (newRoleId == RoleId)
        {
            throw new InvalidOperationException("User already has the specified role.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Role assignment reason cannot be empty.", nameof(reason));
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Role assignment reason cannot exceed 500 characters.", nameof(reason));
        }

        RoleId = newRoleId;
        AuthenticationVersion++;
        AdminControlVersion++;
        MarkUpdated();
    }
}
