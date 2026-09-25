using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechDaily.Api.Contracts;
using TechDaily.Api.Http;
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

            return Results.Ok(new UserProfileResponse(
                User: new UserProfileDto(
                    Id: user.Id,
                    Email: user.Email,
                    Name: user.Name,
                    AvatarUrl: user.AvatarUrl,
                    PreferredLocale: user.PreferredLocale,
                    TargetRole: user.TargetRole,
                    DailyGoalMinutes: user.DailyGoalMinutes,
                    TelegramChatId: user.TelegramChatId,
                    PreferredStudyTime: user.PreferredStudyTime?.ToString("HH:mm"),
                    StreakAlertTime: user.StreakAlertTime?.ToString("HH:mm"),
                    TimeZone: user.TimeZone,
                    IsPushEnabled: user.IsPushEnabled,
                    HasPassword: !string.IsNullOrEmpty(user.PasswordHash),
                    IsGoogleLinked: !string.IsNullOrEmpty(user.GoogleSubjectId)),
                Stats: new ProfileStatsDto(
                    CurrentStreak: user.StreakRecord?.CurrentStreak ?? 0,
                    LongestStreak: user.StreakRecord?.LongestStreak ?? 0,
                    FreezeCreditsRemaining: user.StreakRecord?.FreezeCreditsRemaining ?? 2,
                    TotalDrillsCompleted: totalDrills,
                    AverageScore: avgScore,
                    TotalCardsInDeck: cardsCount,
                    TotalHighlightsSaved: highlightsCount,
                    MemberSince: user.CreatedAt)));
        })
        .WithName("GetUserProfile")
        .WithSummary("Get User Profile")
        .WithDescription("Fetches current authenticated user profile and aggregated learning statistics.")
        .Produces<UserProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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

            return Results.Ok(new UpdateUserProfileResponse(
                Id: user.Id,
                Email: user.Email,
                Name: user.Name,
                AvatarUrl: user.AvatarUrl,
                PreferredLocale: user.PreferredLocale,
                TargetRole: user.TargetRole,
                DailyGoalMinutes: user.DailyGoalMinutes,
                TelegramChatId: user.TelegramChatId,
                PreferredStudyTime: user.PreferredStudyTime?.ToString("HH:mm"),
                StreakAlertTime: user.StreakAlertTime?.ToString("HH:mm"),
                TimeZone: user.TimeZone,
                IsPushEnabled: user.IsPushEnabled));
        })
        .WithName("UpdateUserProfile")
        .WithSummary("Update User Profile")
        .WithDescription("Updates current authenticated user profile metadata.")
        .Produces<UpdateUserProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                return Error.NewPasswordTooShort.ToProblem(StatusCodes.Status400BadRequest);
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
                    return Error.CurrentPasswordIncorrect.ToProblem(StatusCodes.Status400BadRequest);
                }
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new MessageResponse("Password updated successfully."));
        })
        .WithName("ChangePassword")
        .WithSummary("Change Password")
        .WithDescription("Changes or sets password for current authenticated user account.")
        .Produces<MessageResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
