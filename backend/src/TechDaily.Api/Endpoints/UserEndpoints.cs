using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Security;

namespace TechDaily.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        // Get Current Authenticated User Profile with Real-time Learning Analytics
        group.MapGet("/profile", async (
            ClaimsPrincipal userClaims,
            TechDailyDbContext db) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users
                .Include(u => u.StreakRecord)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            var drills = await db.DailyDrills
                .Where(d => d.UserId == user.Id && d.Status == TechDaily.Domain.Enums.DrillStatus.Reviewed)
                .ToListAsync();

            var cardsCount = await db.SpacedRepetitionCards
                .CountAsync(c => c.UserId == user.Id && !c.IsDeleted);

            var highlightsCount = await db.UserHighlights
                .CountAsync(h => h.UserId == user.Id && !h.IsDeleted);

            var totalDrills = drills.Count;
            var avgScore = totalDrills > 0 
                ? Math.Round(drills.Average(d => d.Score ?? 0), 1) 
                : 0.0;

            return Results.Ok(new
            {
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.AvatarUrl,
                    user.PreferredLocale,
                    user.TargetRole,
                    user.DailyGoalMinutes,
                    user.TelegramChatId,
                    preferredStudyTime = user.PreferredStudyTime?.ToString("HH:mm"),
                    streakAlertTime = user.StreakAlertTime?.ToString("HH:mm"),
                    user.TimeZone,
                    user.IsPushEnabled,
                    hasPassword = !string.IsNullOrEmpty(user.PasswordHash),
                    isGoogleLinked = !string.IsNullOrEmpty(user.GoogleSubjectId)
                },
                stats = new
                {
                    currentStreak = user.StreakRecord?.CurrentStreak ?? 0,
                    longestStreak = user.StreakRecord?.LongestStreak ?? 0,
                    freezeCreditsRemaining = user.StreakRecord?.FreezeCreditsRemaining ?? 2,
                    totalDrillsCompleted = totalDrills,
                    averageScore = avgScore,
                    totalCardsInDeck = cardsCount,
                    totalHighlightsSaved = highlightsCount,
                    memberSince = user.CreatedAt
                }
            });
        })
        .WithName("GetUserProfile")
        .WithSummary("Get User Profile")
        .WithDescription("Fetches current authenticated user profile and aggregated learning statistics.");

        // Update User Profile
        group.MapPut("/profile", async (
            [FromBody] UpdateProfileRequest request,
            ClaimsPrincipal userClaims,
            TechDailyDbContext db) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name.Trim();

            if (request.AvatarUrl != null)
                user.AvatarUrl = request.AvatarUrl.Trim();

            if (!string.IsNullOrWhiteSpace(request.PreferredLocale))
                user.PreferredLocale = request.PreferredLocale.Trim();

            if (!string.IsNullOrWhiteSpace(request.TargetRole))
                user.TargetRole = request.TargetRole.Trim();

            if (request.DailyGoalMinutes.HasValue && request.DailyGoalMinutes.Value > 0)
                user.DailyGoalMinutes = request.DailyGoalMinutes.Value;

            if (request.TelegramChatId.HasValue)
                user.TelegramChatId = request.TelegramChatId.Value;

            if (!string.IsNullOrWhiteSpace(request.PreferredStudyTime) && TimeOnly.TryParse(request.PreferredStudyTime, out var studyTime))
                user.PreferredStudyTime = studyTime;

            if (!string.IsNullOrWhiteSpace(request.StreakAlertTime) && TimeOnly.TryParse(request.StreakAlertTime, out var streakTime))
                user.StreakAlertTime = streakTime;

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
                user.TimeZone = request.TimeZone.Trim();

            if (request.IsPushEnabled.HasValue)
                user.IsPushEnabled = request.IsPushEnabled.Value;

            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.AvatarUrl,
                user.PreferredLocale,
                user.TargetRole,
                user.DailyGoalMinutes,
                user.TelegramChatId,
                preferredStudyTime = user.PreferredStudyTime?.ToString("HH:mm"),
                streakAlertTime = user.StreakAlertTime?.ToString("HH:mm"),
                user.TimeZone,
                user.IsPushEnabled
            });
        })
        .WithName("UpdateUserProfile")
        .WithSummary("Update User Profile")
        .WithDescription("Updates current authenticated user profile metadata.");

        // Change Password
        group.MapPut("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            ClaimsPrincipal userClaims,
            TechDailyDbContext db) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { code = Error.NewPasswordTooShort.Code, error = Error.NewPasswordTooShort.Message });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            // If user has existing password, verify current password
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) || !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return Results.BadRequest(new { code = Error.CurrentPasswordIncorrect.Code, error = Error.CurrentPasswordIncorrect.Message });
                }
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Password updated successfully." });
        })
        .WithName("ChangePassword")
        .WithSummary("Change Password")
        .WithDescription("Changes or sets password for current authenticated user account.");

        return group;
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal claims)
    {
        var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var guid))
        {
            return guid;
        }

        return null;
    }
}

public record UpdateProfileRequest(
    string? Name,
    string? AvatarUrl,
    string? PreferredLocale,
    string? TargetRole,
    int? DailyGoalMinutes,
    long? TelegramChatId,
    string? PreferredStudyTime,
    string? StreakAlertTime,
    string? TimeZone,
    bool? IsPushEnabled
);

public record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword
);
