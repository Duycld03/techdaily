using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;

namespace TechDaily.Infrastructure.Workers;

public class DailyPushNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyPushNotificationWorker> _logger;
    private readonly TimeSpan _checkInterval;

    public DailyPushNotificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyPushNotificationWorker> logger,
        TimeSpan? checkInterval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = checkInterval ?? TimeSpan.FromMinutes(15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyPushNotificationWorker started with interval {Interval}", _checkInterval);
        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await EvaluateAndDispatchNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during DailyPushNotificationWorker execution.");
            }
        }
    }

    public async Task<int> EvaluateAndDispatchNotificationsAsync(
        CancellationToken cancellationToken = default,
        DateTimeOffset? customUtcNow = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TechDailyDbContext>();
        var webPushService = scope.ServiceProvider.GetRequiredService<IWebPushService>();

        var nowUtc = customUtcNow ?? DateTimeOffset.UtcNow;

        var users = await db.Users
            .Include(u => u.PushSubscriptions)
            .Include(u => u.StreakRecord)
            .Where(u => u.IsPushEnabled && u.PushSubscriptions.Any(s => !s.IsDeleted))
            .ToListAsync(cancellationToken);

        if (users.Count == 0) return 0;

        var dispatchedCount = 0;
        var staleSubscriptions = new List<UserPushSubscription>();

        foreach (var user in users)
        {
            var tz = ResolveTimeZone(user.TimeZone);
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);
            var localTime = TimeOnly.FromTimeSpan(localNow.TimeOfDay);
            var localDate = DateOnly.FromDateTime(localNow.DateTime);

            PushNotificationPayload? payloadToDispatch = null;

            // 1. Evaluate Preferred Study Time (15-minute slot)
            var studyTime = user.PreferredStudyTime ?? new TimeOnly(8, 0);
            if (IsTimeWithinSlot(localTime, studyTime))
            {
                var studiedToday = await db.DailyDrills.AnyAsync(
                    d => d.UserId == user.Id && d.ScheduledDate == localDate && d.Status == DrillStatus.Reviewed,
                    cancellationToken);

                var recentlyDispatched = user.PushSubscriptions.Any(
                    s => s.LastDispatchedAt.HasValue && (nowUtc - s.LastDispatchedAt.Value).TotalHours < 12);

                if (!studiedToday && !recentlyDispatched)
                {
                    payloadToDispatch = new PushNotificationPayload(
                        "Daily Study Focus 📚",
                        "Your daily architectural slice and scenario drill are ready!",
                        "/today",
                        "techdaily-study"
                    );
                }
            }

            // 2. Evaluate Streak Alert Time (15-minute slot)
            var streakTime = user.StreakAlertTime ?? new TimeOnly(20, 0);
            if (payloadToDispatch == null && IsTimeWithinSlot(localTime, streakTime))
            {
                var streakCount = user.StreakRecord?.CurrentStreak ?? 0;
                var studiedToday = await db.DailyDrills.AnyAsync(
                    d => d.UserId == user.Id && d.ScheduledDate == localDate && d.Status == DrillStatus.Reviewed,
                    cancellationToken);

                var recentlyDispatched = user.PushSubscriptions.Any(
                    s => s.LastDispatchedAt.HasValue && (nowUtc - s.LastDispatchedAt.Value).TotalHours < 12);

                if (streakCount > 0 && !studiedToday && !recentlyDispatched)
                {
                    payloadToDispatch = new PushNotificationPayload(
                        "Keep Your Streak Alive! 🔥",
                        $"You're on a {streakCount}-day streak. Complete today's drill before midnight to keep it going!",
                        "/today",
                        "techdaily-streak"
                    );
                }
            }

            if (payloadToDispatch == null) continue;

            // Dispatch to user's devices
            foreach (var sub in user.PushSubscriptions.Where(s => !s.IsDeleted))
            {
                try
                {
                    var sent = await webPushService.SendNotificationAsync(
                        sub.Endpoint,
                        sub.P256dh,
                        sub.Auth,
                        payloadToDispatch,
                        cancellationToken);

                    if (sent)
                    {
                        sub.LastDispatchedAt = nowUtc;
                        dispatchedCount++;
                    }
                }
                catch (WebPushSubscriptionExpiredException)
                {
                    _logger.LogInformation("Marking stale push subscription for deletion: {Endpoint}", sub.Endpoint);
                    staleSubscriptions.Add(sub);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification to subscription {Endpoint}", sub.Endpoint);
                }
            }
        }

        // Clean up stale subscriptions
        if (staleSubscriptions.Count > 0)
        {
            var userIdsToCheck = staleSubscriptions.Select(s => s.UserId).Distinct().ToList();
            db.UserPushSubscriptions.RemoveRange(staleSubscriptions);
            await db.SaveChangesAsync(cancellationToken);

            // If user has no subscriptions left, disable push
            foreach (var uid in userIdsToCheck)
            {
                var remaining = await db.UserPushSubscriptions.CountAsync(s => s.UserId == uid, cancellationToken);
                if (remaining == 0)
                {
                    var u = await db.Users.FindAsync([uid], cancellationToken);
                    if (u != null)
                    {
                        u.IsPushEnabled = false;
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return dispatchedCount;
    }

    public static bool IsTimeWithinSlot(TimeOnly current, TimeOnly targetSlot, int slotMinutes = 15)
    {
        var diff = (current.ToTimeSpan() - targetSlot.ToTimeSpan()).TotalMinutes;
        return diff >= 0 && diff < slotMinutes;
    }

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
