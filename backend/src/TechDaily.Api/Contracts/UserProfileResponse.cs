namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>GET /api/v1/user/profile</c>: the authenticated user's profile plus
/// aggregated learning statistics.
/// </summary>
public record UserProfileResponse(UserProfileDto User, ProfileStatsDto Stats);

/// <summary>
/// Authenticated user's profile projection returned by <c>GET /api/v1/user/profile</c>.
/// </summary>
public record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string PreferredLocale,
    string TargetRole,
    int DailyGoalMinutes,
    string? PreferredStudyTime,
    string? StreakAlertTime,
    string TimeZone,
    bool IsPushEnabled,
    bool HasPassword,
    bool IsGoogleLinked
);

/// <summary>
/// Aggregated learning statistics returned by <c>GET /api/v1/user/profile</c>.
/// </summary>
public record ProfileStatsDto(
    int CurrentStreak,
    int LongestStreak,
    int FreezeCreditsRemaining,
    int TotalDrillsCompleted,
    double AverageScore,
    int TotalCardsInDeck,
    int TotalHighlightsSaved,
    DateTimeOffset MemberSince
);
