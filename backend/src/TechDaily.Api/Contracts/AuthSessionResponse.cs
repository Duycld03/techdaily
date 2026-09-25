namespace TechDaily.Api.Contracts;

/// <summary>
/// Response body for the auth endpoints (<c>register</c>, <c>login</c>, <c>google</c>, <c>refresh</c>).
/// Carries the issued access token and the authenticated user's public profile.
/// </summary>
public record AuthSessionResponse(string Token, AuthUserDto User);

/// <summary>
/// Public user projection returned by the auth endpoints. <see cref="AvatarUrl"/> is null on
/// endpoints that do not expose it (e.g. <c>register</c>).
/// </summary>
public record AuthUserDto(
    Guid Id,
    string Email,
    string Name,
    string PreferredLocale,
    string TargetRole,
    int DailyGoalMinutes,
    string? AvatarUrl
);
