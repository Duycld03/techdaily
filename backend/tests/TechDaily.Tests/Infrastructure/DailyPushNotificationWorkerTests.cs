using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using TechDaily.Infrastructure.Workers;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class DailyPushNotificationWorkerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly MockWebPushService _mockWebPushService;

    public DailyPushNotificationWorkerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var initDb = CreateDbContext())
        {
            initDb.Database.EnsureCreated();
        }

        _mockWebPushService = new MockWebPushService();

        var services = new ServiceCollection();
        services.AddDbContext<TechDailyDbContext>(opts => opts.UseSqlite(_connection));
        services.AddSingleton<IWebPushService>(_mockWebPushService);
        _serviceProvider = services.BuildServiceProvider();
    }

    private TechDailyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new TechDailyDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void IsTimeWithinSlot_ShouldReturnExpectedResults()
    {
        var targetSlot = new TimeOnly(8, 0);

        // Exactly on time
        DailyPushNotificationWorker.IsTimeWithinSlot(new TimeOnly(8, 0), targetSlot).Should().BeTrue();

        // 7 minutes after (within 15 min slot)
        DailyPushNotificationWorker.IsTimeWithinSlot(new TimeOnly(8, 7), targetSlot).Should().BeTrue();

        // 14 minutes after (within 15 min slot)
        DailyPushNotificationWorker.IsTimeWithinSlot(new TimeOnly(8, 14), targetSlot).Should().BeTrue();

        // 15 minutes after (next slot, not in window)
        DailyPushNotificationWorker.IsTimeWithinSlot(new TimeOnly(8, 15), targetSlot).Should().BeFalse();

        // Before target time
        DailyPushNotificationWorker.IsTimeWithinSlot(new TimeOnly(7, 59), targetSlot).Should().BeFalse();
    }

    [Fact]
    public void ResolveTimeZone_ValidAndInvalid_ShouldResolveOrFallbackToUtc()
    {
        DailyPushNotificationWorker.ResolveTimeZone("UTC").Id.Should().Be(TimeZoneInfo.Utc.Id);
        DailyPushNotificationWorker.ResolveTimeZone("Invalid/Timezone/String_XYZ").Id.Should().Be(TimeZoneInfo.Utc.Id);
        DailyPushNotificationWorker.ResolveTimeZone(null).Id.Should().Be(TimeZoneInfo.Utc.Id);
    }

    [Fact]
    public async Task EvaluateAndDispatch_MorningStudyTimeMatch_ShouldDispatchNotification()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        using (var db = CreateDbContext())
        {
            var user = new User
            {
                Id = userId,
                Email = "study@test.com",
                Name = "Morning Learner",
                TimeZone = "UTC",
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            await db.Users.AddAsync(user);

            var sub = new UserPushSubscription
            {
                Id = subId,
                UserId = userId,
                Endpoint = "https://push.example.com/sub-1",
                P256dh = "test-p256dh",
                Auth = "test-auth",
                CreatedAt = DateTime.UtcNow
            };
            await db.UserPushSubscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
        }

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new DailyPushNotificationWorker(scopeFactory, NullLogger<DailyPushNotificationWorker>.Instance);

        // Simulate 08:05 UTC (matching 08:00 slot)
        var simulatedUtc = new DateTimeOffset(2026, 9, 16, 8, 5, 0, TimeSpan.Zero);

        // Act
        var dispatched = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, simulatedUtc);

        // Assert
        dispatched.Should().Be(1);
        _mockWebPushService.DispatchedPayloads.Should().HaveCount(1);
        _mockWebPushService.DispatchedPayloads[0].Title.Should().Be("Daily Study Focus 📚");

        using (var verifyDb = CreateDbContext())
        {
            var updatedSub = await verifyDb.UserPushSubscriptions.FindAsync(subId);
            updatedSub!.LastDispatchedAt.Should().Be(simulatedUtc);
        }
    }

    [Fact]
    public async Task EvaluateAndDispatch_EveningStreakAlert_ShouldDispatchWhenStreakActiveAndNotStudied()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        using (var db = CreateDbContext())
        {
            var user = new User
            {
                Id = userId,
                Email = "streak@test.com",
                Name = "Streak Saver",
                TimeZone = "UTC",
                PreferredStudyTime = new TimeOnly(8, 0),
                StreakAlertTime = new TimeOnly(20, 0),
                IsPushEnabled = true
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var streak = StreakRecord.Create(userId);
            streak.RecordCompletion(new DateOnly(2026, 9, 15));
            await db.StreakRecords.AddAsync(streak);

            var sub = new UserPushSubscription
            {
                Id = subId,
                UserId = userId,
                Endpoint = "https://push.example.com/sub-2",
                P256dh = "test-p256dh",
                Auth = "test-auth",
                CreatedAt = DateTime.UtcNow
            };
            await db.UserPushSubscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
        }

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new DailyPushNotificationWorker(scopeFactory, NullLogger<DailyPushNotificationWorker>.Instance);

        // Simulate 20:05 UTC (matching 20:00 streak alert slot)
        var simulatedUtc = new DateTimeOffset(2026, 9, 16, 20, 5, 0, TimeSpan.Zero);

        // Act
        var dispatched = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, simulatedUtc);

        // Assert
        dispatched.Should().Be(1);
        _mockWebPushService.DispatchedPayloads.Should().HaveCount(1);
        _mockWebPushService.DispatchedPayloads[0].Title.Should().Be("Keep Your Streak Alive! 🔥");
        _mockWebPushService.DispatchedPayloads[0].Body.Should().Contain("1-day streak");
    }

    [Fact]
    public async Task EvaluateAndDispatch_UserAlreadyStudiedToday_ShouldNotDispatchNotification()
    {
        var userId = Guid.NewGuid();

        using (var db = CreateDbContext())
        {
            var user = new User
            {
                Id = userId,
                Email = "completed@test.com",
                Name = "Diligent Learner",
                TimeZone = "UTC",
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            await db.Users.AddAsync(user);

            var topic = new Topic
            {
                Id = Guid.NewGuid(),
                Title = "Kafka Internals",
                Slug = "kafka-internals",
                Category = Category.SystemDesign,
                Difficulty = Difficulty.Senior,
                DayOrder = 10
            };
            await db.Topics.AddAsync(topic);

            var question = new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                TopicId = topic.Id,
                QuestionText = "How does partition replication work in Kafka?",
                Options = ["Leader-Follower ISR", "Paxos", "Raft", "2PC"],
                CorrectOptionIndex = 0,
                ExplanationMarkdown = "Kafka uses Leader-Follower ISR.",
                Difficulty = Difficulty.Senior
            };
            await db.InterviewQuestions.AddAsync(question);

            var today = new DateOnly(2026, 9, 16);
            var completedDrill = new DailyDrill
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestionId = question.Id,
                ScheduledDate = today,
                Status = DrillStatus.Reviewed,
                AttemptCount = 1
            };
            await db.DailyDrills.AddAsync(completedDrill);

            var sub = new UserPushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = "https://push.example.com/sub-3",
                P256dh = "test-p256dh",
                Auth = "test-auth",
                CreatedAt = DateTime.UtcNow
            };
            await db.UserPushSubscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
        }

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new DailyPushNotificationWorker(scopeFactory, NullLogger<DailyPushNotificationWorker>.Instance);

        // Simulate 08:05 UTC (matching study slot)
        var simulatedUtc = new DateTimeOffset(2026, 9, 16, 8, 5, 0, TimeSpan.Zero);

        // Act
        var dispatched = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, simulatedUtc);

        // Assert
        dispatched.Should().Be(0);
        _mockWebPushService.DispatchedPayloads.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAndDispatch_ExpiredEndpoint_ShouldDeleteStaleSubscriptionAndDisablePush()
    {
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var expiredEndpoint = "https://push.example.com/expired-endpoint";

        using (var db = CreateDbContext())
        {
            var user = new User
            {
                Id = userId,
                Email = "expired@test.com",
                Name = "Expired Device User",
                TimeZone = "UTC",
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            await db.Users.AddAsync(user);

            var sub = new UserPushSubscription
            {
                Id = subId,
                UserId = userId,

                Endpoint = expiredEndpoint,
                P256dh = "test-p256dh",
                Auth = "test-auth",
                CreatedAt = DateTime.UtcNow
            };
            await db.UserPushSubscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
        }

        // Configure mock to throw WebPushSubscriptionExpiredException
        _mockWebPushService.ExpiredEndpoints.Add(expiredEndpoint);

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new DailyPushNotificationWorker(scopeFactory, NullLogger<DailyPushNotificationWorker>.Instance);

        var simulatedUtc = new DateTimeOffset(2026, 9, 16, 8, 5, 0, TimeSpan.Zero);

        // Act
        var dispatched = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, simulatedUtc);

        // Assert
        dispatched.Should().Be(0);

        using (var verifyDb = CreateDbContext())
        {
            var remainingSubs = await verifyDb.UserPushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
            remainingSubs.Should().BeEmpty();

            var updatedUser = await verifyDb.Users.FindAsync(userId);
            updatedUser!.IsPushEnabled.Should().BeFalse();
        }
    }
    [Fact]
    public async Task EvaluateAndDispatch_MultipleTimezones_ShouldMatchRespectiveWindows()
    {
        var userUtcPlus7Id = Guid.NewGuid();
        var userUtcMinus5Id = Guid.NewGuid();
        var userUtc0Id = Guid.NewGuid();

        using (var db = CreateDbContext())
        {
            var userUtcPlus7 = new User
            {
                Id = userUtcPlus7Id,
                Email = "vietnam@test.com",
                Name = "VN Dev",
                TimeZone = "Asia/Ho_Chi_Minh", // UTC+7
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            var sub1 = new UserPushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userUtcPlus7Id,
                Endpoint = "https://push.example.com/vn",
                P256dh = "p256dh",
                Auth = "auth",
                CreatedAt = DateTime.UtcNow
            };

            var userUtcMinus5 = new User
            {
                Id = userUtcMinus5Id,
                Email = "newyork@test.com",
                Name = "NY Dev",
                TimeZone = "America/New_York", // UTC-5 / UTC-4
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            var sub2 = new UserPushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userUtcMinus5Id,
                Endpoint = "https://push.example.com/ny",
                P256dh = "p256dh",
                Auth = "auth",
                CreatedAt = DateTime.UtcNow
            };

            var userUtc0 = new User
            {
                Id = userUtc0Id,
                Email = "london@test.com",
                Name = "London Dev",
                TimeZone = "UTC", // UTC+0
                PreferredStudyTime = new TimeOnly(8, 0),
                IsPushEnabled = true
            };
            var sub3 = new UserPushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userUtc0Id,
                Endpoint = "https://push.example.com/london",
                P256dh = "p256dh",
                Auth = "auth",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.AddRangeAsync(userUtcPlus7, userUtcMinus5, userUtc0);
            await db.UserPushSubscriptions.AddRangeAsync(sub1, sub2, sub3);
            await db.SaveChangesAsync();
        }

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var worker = new DailyPushNotificationWorker(scopeFactory, NullLogger<DailyPushNotificationWorker>.Instance);

        // At 01:05 UTC:
        // UTC+7 local time is 08:05 (matches 08:00 slot)
        // UTC local time is 01:05 (no match)
        // NY local time is 21:05 previous day (no match)
        var utc0105 = new DateTimeOffset(2026, 9, 16, 1, 5, 0, TimeSpan.Zero);
        var dispatched0105 = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, utc0105);

        dispatched0105.Should().Be(1);
        _mockWebPushService.DispatchedPayloads.Should().HaveCount(1);
        _mockWebPushService.DispatchedPayloads[0].Title.Should().Be("Daily Study Focus 📚");

        _mockWebPushService.DispatchedPayloads.Clear();

        // At 08:05 UTC:
        // UTC local time is 08:05 (matches 08:00 slot)
        var utc0805 = new DateTimeOffset(2026, 9, 16, 8, 5, 0, TimeSpan.Zero);
        var dispatched0805 = await worker.EvaluateAndDispatchNotificationsAsync(CancellationToken.None, utc0805);

        dispatched0805.Should().Be(1);
        _mockWebPushService.DispatchedPayloads.Should().HaveCount(1);
    }
}

public class MockWebPushService : IWebPushService
{
    public string PublicKey => "mock-public-key";
    public List<PushNotificationPayload> DispatchedPayloads { get; } = new();
    public HashSet<string> ExpiredEndpoints { get; } = new();

    public Task<bool> SendNotificationAsync(
        string endpoint,
        string p256dh,
        string auth,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (ExpiredEndpoints.Contains(endpoint))
        {
            throw new WebPushSubscriptionExpiredException(endpoint, "410 Gone");
        }

        DispatchedPayloads.Add(payload);
        return Task.FromResult(true);
    }
}
