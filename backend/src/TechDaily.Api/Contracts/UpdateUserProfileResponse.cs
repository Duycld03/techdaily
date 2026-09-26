namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for <c>PUT /api/v1/user/profile</c>: the persisted user profile after update.
/// </summary>
public record UpdateUserProfileResponse(
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
    bool IsPushEnabled
);
