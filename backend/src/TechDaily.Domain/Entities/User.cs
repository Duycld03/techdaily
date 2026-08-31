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

    // Navigation properties
    public StreakRecord? StreakRecord { get; set; }
    public ICollection<DailyDrill> DailyDrills { get; set; } = new List<DailyDrill>();
    public ICollection<SpacedRepetitionCard> SpacedRepetitionCards { get; set; } = new List<SpacedRepetitionCard>();
    public ICollection<UserHighlight> UserHighlights { get; set; } = new List<UserHighlight>();
}
