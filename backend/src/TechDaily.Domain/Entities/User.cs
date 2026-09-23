using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? GoogleSubjectId { get; set; }
    public string? PasswordHash { get; set; }
    public long? TelegramChatId { get; set; }
    public string PreferredLocale { get; set; } = "en"; // "en" or "vi"
    public string TargetRole { get; set; } = "Senior Engineer";
    public int DailyGoalMinutes { get; set; } = 10;
    public TimeOnly? PreferredStudyTime { get; set; } = new TimeOnly(8, 0);
    public TimeOnly? StreakAlertTime { get; set; } = new TimeOnly(20, 0);
    public string TimeZone { get; set; } = "UTC";
    public bool IsPushEnabled { get; set; } = false;

    // Navigation properties
    public StreakRecord? StreakRecord { get; set; }
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
    public ICollection<SpacedRepetitionCard> SpacedRepetitionCards { get; set; } = new List<SpacedRepetitionCard>();
    public ICollection<UserHighlight> UserHighlights { get; set; } = new List<UserHighlight>();
    public ICollection<UserQuizProgress> QuizProgresses { get; set; } = new List<UserQuizProgress>();
    public ICollection<UserBookPacer> BookPacers { get; set; } = new List<UserBookPacer>();
    public ICollection<UserPushSubscription> PushSubscriptions { get; set; } = new List<UserPushSubscription>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
