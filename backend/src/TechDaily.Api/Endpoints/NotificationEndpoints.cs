using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;

namespace TechDaily.Api.Endpoints;

public record PushSubscriptionKeys(string P256dh, string Auth);

public record SubscribePushRequest(
    string Endpoint,
    PushSubscriptionKeys Keys,
    string? UserAgent
);

public record UnsubscribePushRequest(string Endpoint);

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder group)
    {
        // 1. Get VAPID Public Key
        group.MapGet("/push/vapid-public-key", (IWebPushService webPushService) =>
        {
            return Results.Ok(new
            {
                publicKey = webPushService.PublicKey
            });
        })
        .RequireAuthorization()
        .WithName("GetVapidPublicKey")
        .WithSummary("Retrieves the VAPID public key for browser Web Push subscription.");

        // 2. Subscribe Device to Web Push
        group.MapPost("/push/subscribe", async (
            [FromBody] SubscribePushRequest request,
            ClaimsPrincipal userClaims,
            TechDailyDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Endpoint) ||
                string.IsNullOrWhiteSpace(request.Keys?.P256dh) ||
                string.IsNullOrWhiteSpace(request.Keys?.Auth))
            {
                return Results.BadRequest(new { error = "Endpoint and keys (p256dh, auth) are required." });
            }

            var user = await db.Users
                .Include(u => u.PushSubscriptions)
                .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

            if (user == null) return Results.Unauthorized();

            var existing = user.PushSubscriptions
                .FirstOrDefault(s => s.Endpoint == request.Endpoint.Trim());

            if (existing != null)
            {
                existing.P256dh = request.Keys.P256dh.Trim();
                existing.Auth = request.Keys.Auth.Trim();
                existing.UserAgent = request.UserAgent;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newSub = new UserPushSubscription
                {
                    UserId = user.Id,
                    Endpoint = request.Endpoint.Trim(),
                    P256dh = request.Keys.P256dh.Trim(),
                    Auth = request.Keys.Auth.Trim(),
                    UserAgent = request.UserAgent,
                    CreatedAt = DateTime.UtcNow
                };
                await db.UserPushSubscriptions.AddAsync(newSub, ct);
            }

            user.IsPushEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { success = true });
        })
        .RequireAuthorization()
        .WithName("SubscribePush")
        .WithSummary("Registers or updates a browser Web Push subscription for the authenticated user.");

        // 3. Unsubscribe Device from Web Push
        group.MapPost("/push/unsubscribe", async (
            [FromBody] UnsubscribePushRequest request,
            ClaimsPrincipal userClaims,
            TechDailyDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Endpoint))
            {
                return Results.BadRequest(new { error = "Endpoint is required." });
            }

            var sub = await db.UserPushSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Endpoint == request.Endpoint.Trim(), ct);

            if (sub != null)
            {
                db.UserPushSubscriptions.Remove(sub);
            }

            // Check if user has any remaining subscriptions
            var remainingCount = await db.UserPushSubscriptions
                .CountAsync(s => s.UserId == userId.Value && s.Endpoint != request.Endpoint.Trim(), ct);

            if (remainingCount == 0)
            {
                var user = await db.Users.FindAsync([userId.Value], ct);
                if (user != null)
                {
                    user.IsPushEnabled = false;
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("UnsubscribePush")
        .WithSummary("Removes a browser Web Push subscription and disables push if no devices remain.");

        // 4. Send Test Push Notification
        group.MapPost("/push/test", async (
            ClaimsPrincipal userClaims,
            TechDailyDbContext db,
            IWebPushService webPushService,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue) return Results.Unauthorized();

            var subscriptions = await db.UserPushSubscriptions
                .Where(s => s.UserId == userId.Value)
                .ToListAsync(ct);

            if (subscriptions.Count == 0)
            {
                return Results.BadRequest(new { error = "No active push subscriptions found for this device." });
            }

            var payload = new PushNotificationPayload(
                "TechDaily Test Push 🚀",
                "Web Push notifications are successfully configured and active!",
                "/today",
                "techdaily-test"
            );

            var sentCount = 0;
            foreach (var sub in subscriptions)
            {
                var ok = await webPushService.SendNotificationAsync(sub.Endpoint, sub.P256dh, sub.Auth, payload, ct);
                if (ok) sentCount++;
            }

            return Results.Ok(new { success = true, sent = sentCount, total = subscriptions.Count });
        })
        .RequireAuthorization()
        .WithName("TestPushNotification")
        .WithSummary("Sends an immediate test push notification to all registered devices of the current user.");

        return group;
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal claims)
    {
        var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}
