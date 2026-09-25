using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using TechDaily.Infrastructure.Workers;
using TechDaily.Api.Contracts;
using TechDaily.Api.Http;
using TechDaily.Application.Common;

namespace TechDaily.Api.Endpoints;

public record PushSubscriptionKeys(string P256dh, string Auth);

public record SubscribePushRequest(
    string Endpoint,
    PushSubscriptionKeys Keys,
    string? UserAgent = null,
    string? TimeZone = null);

public record UnsubscribePushRequest(string Endpoint);

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this RouteGroupBuilder group)
    {
        // 1. Get VAPID Public Key
        group.MapGet("/push/vapid-public-key", (IWebPushService webPushService) =>
        {
            return Results.Ok(new VapidPublicKeyResponse(webPushService.PublicKey));
        })
        .RequireAuthorization()
        .WithName("GetVapidPublicKey")
        .WithSummary("Get VAPID Public Key")
        .WithDescription("Retrieves the VAPID public key for browser Web Push subscription.")
        .Produces<VapidPublicKeyResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                return new Error("PUSH_SUBSCRIPTION_INVALID", "Endpoint and keys (p256dh, auth) are required.").ToProblem(StatusCodes.Status400BadRequest);
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

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
            {
                var resolvedTz = DailyPushNotificationWorker.ResolveTimeZone(request.TimeZone.Trim());
                user.TimeZone = resolvedTz.Id;
                user.UpdatedAt = DateTime.UtcNow;
            }

            user.IsPushEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new PushAckResponse(true));
        })
        .RequireAuthorization()
        .WithName("SubscribePush")
        .WithSummary("Subscribe Web Push")
        .WithDescription("Registers or updates a browser Web Push subscription for the authenticated user.")
        .Produces<PushAckResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                return new Error("PUSH_ENDPOINT_REQUIRED", "Endpoint is required.").ToProblem(StatusCodes.Status400BadRequest);
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
        .WithSummary("Unsubscribe Web Push")
        .WithDescription("Removes a browser Web Push subscription and disables push if no devices remain.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 4. Send Test Push Notification
        group.MapPost("/push/test", async (
            ClaimsPrincipal userClaims,
            TechDailyDbContext db,
            IWebPushService webPushService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var userId = GetCurrentUserId(userClaims);
            if (!userId.HasValue) return Results.Unauthorized();

            var subscriptions = await db.UserPushSubscriptions
                .Where(s => s.UserId == userId.Value && !s.IsDeleted)
                .ToListAsync(ct);

            if (subscriptions.Count == 0)
            {
                return new Error("PUSH_NO_SUBSCRIPTIONS", "No active push subscriptions found for this device.").ToProblem(StatusCodes.Status400BadRequest);
            }

            var payload = new PushNotificationPayload(
                "TechDaily Test Push 🚀",
                "Web Push notifications are successfully configured and active!",
                "/today",
                "techdaily-test"
            );

            var logger = loggerFactory.CreateLogger("TechDaily.Api.Endpoints.NotificationEndpoints");
            var sentCount = 0;
            var staleSubscriptions = new List<UserPushSubscription>();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var ok = await webPushService.SendNotificationAsync(sub.Endpoint, sub.P256dh, sub.Auth, payload, ct);
                    if (ok)
                    {
                        sentCount++;
                    }
                }
                catch (WebPushSubscriptionExpiredException ex)
                {
                    logger.LogWarning(ex, "Web Push endpoint expired ({Endpoint}). Marking for purge.", sub.Endpoint);
                    staleSubscriptions.Add(sub);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Transient error sending push notification to endpoint {Endpoint}", sub.Endpoint);
                }
            }

            if (staleSubscriptions.Count > 0)
            {
                db.UserPushSubscriptions.RemoveRange(staleSubscriptions);
                await db.SaveChangesAsync(ct);

                var remaining = await db.UserPushSubscriptions
                    .CountAsync(s => s.UserId == userId.Value && !s.IsDeleted, ct);

                if (remaining == 0)
                {
                    var user = await db.Users.FindAsync([userId.Value], ct);
                    if (user != null)
                    {
                        user.IsPushEnabled = false;
                        user.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);
                    }
                }
            }

            if (sentCount == 0 && staleSubscriptions.Count == subscriptions.Count)
            {
                return Results.Problem(
                    detail: "All push notification subscriptions for this device have expired. Please toggle notifications off and on to renew your browser subscription.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "PUSH_SUBSCRIPTION_EXPIRED",
                        ["sent"] = 0,
                        ["total"] = subscriptions.Count,
                        ["stalePurged"] = staleSubscriptions.Count
                    });
            }

            if (sentCount == 0)
            {
                return Results.Problem(
                    detail: "Failed to deliver push notifications.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "PUSH_DELIVERY_FAILED",
                        ["sent"] = 0,
                        ["total"] = subscriptions.Count,
                        ["stalePurged"] = staleSubscriptions.Count
                    });
            }

            return Results.Ok(new PushTestResponse(
                Success: true,
                Sent: sentCount,
                Total: subscriptions.Count,
                StalePurged: staleSubscriptions.Count));
        })
        .RequireAuthorization()
        .WithName("TestPushNotification")
        .WithSummary("Send Test Push")
        .WithDescription("Sends an immediate test push notification and prunes stale subscriptions.")
        .Produces<PushTestResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static Guid? GetCurrentUserId(ClaimsPrincipal claims)
    {
        var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }
}
