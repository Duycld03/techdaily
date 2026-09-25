using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechDaily.Api.Endpoints;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Api;

public class NotificationEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _dbOptions;
    private readonly MockWebPushService _mockWebPushService = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _userId = Guid.NewGuid();
    public NotificationEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initDb = new TechDailyDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<TechDailyDbContext>(_ => new TechDailyDbContext(_dbOptions));
        builder.Services.AddSingleton<IWebPushService>(_mockWebPushService);
        builder.Services.AddAuthentication(defaultScheme: "Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGroup("/api/v1/notifications")
            .MapNotificationEndpoints();

        await _app.StartAsync();
        _client = _app.GetTestClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        TestAuthHandler.UserId = _userId;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _connection.Dispose();
    }

    private TechDailyDbContext CreateDbContext() => new TechDailyDbContext(_dbOptions);

    [Fact]
    public async Task SubscribePush_WithValidTimeZone_UpdatesUserTimeZone()
    {
        // Arrange
        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "learner@techdaily.local",
                Name = "Learner",
                TimeZone = "UTC",
                IsPushEnabled = false
            });
            await db.SaveChangesAsync();
        }

        var request = new SubscribePushRequest(
            Endpoint: "https://push.example.com/sub/valid-tz",
            Keys: new PushSubscriptionKeys("p256dh-key-123", "auth-secret-123"),
            UserAgent: "Mozilla/5.0 (X11; Linux x86_64)",
            TimeZone: "Asia/Ho_Chi_Minh"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/push/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var db = CreateDbContext())
        {
            var updatedUser = await db.Users
                .Include(u => u.PushSubscriptions)
                .FirstAsync(u => u.Id == _userId);

            updatedUser.TimeZone.Should().Be("Asia/Ho_Chi_Minh");
            updatedUser.IsPushEnabled.Should().BeTrue();
            updatedUser.PushSubscriptions.Should().ContainSingle(s =>
                s.Endpoint == "https://push.example.com/sub/valid-tz" &&
                s.P256dh == "p256dh-key-123" &&
                s.Auth == "auth-secret-123" &&
                s.UserAgent == "Mozilla/5.0 (X11; Linux x86_64)");
        }
    }

    [Fact]
    public async Task SubscribePush_WithInvalidTimeZone_FallsBackSafelyToUtc()
    {
        // Arrange
        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "invalidtz@techdaily.local",
                Name = "Learner Invalid",
                TimeZone = "Asia/Tokyo",
                IsPushEnabled = false
            });
            await db.SaveChangesAsync();
        }

        var request = new SubscribePushRequest(
            Endpoint: "https://push.example.com/sub/invalid-tz",
            Keys: new PushSubscriptionKeys("p256dh-key-456", "auth-secret-456"),
            UserAgent: "Mozilla/5.0 Brave/1.0",
            TimeZone: "Invalid/Zone"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/push/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var db = CreateDbContext())
        {
            var updatedUser = await db.Users
                .Include(u => u.PushSubscriptions)
                .FirstAsync(u => u.Id == _userId);

            updatedUser.TimeZone.Should().Be("UTC");
            updatedUser.IsPushEnabled.Should().BeTrue();
            updatedUser.PushSubscriptions.Should().ContainSingle(s =>
                s.Endpoint == "https://push.example.com/sub/invalid-tz");
        }
    }

    [Fact]
    public async Task SubscribePush_WithNullTimeZone_PreservesExistingUserTimeZone()
    {
        // Arrange
        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "nulltz@techdaily.local",
                Name = "Learner Null TZ",
                TimeZone = "Asia/Tokyo",
                IsPushEnabled = false
            });
            await db.SaveChangesAsync();
        }

        var request = new SubscribePushRequest(
            Endpoint: "https://push.example.com/sub/null-tz",
            Keys: new PushSubscriptionKeys("p256dh-key-789", "auth-secret-789"),
            UserAgent: "Mozilla/5.0 Firefox/120.0",
            TimeZone: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/push/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var db = CreateDbContext())
        {
            var updatedUser = await db.Users
                .Include(u => u.PushSubscriptions)
                .FirstAsync(u => u.Id == _userId);

            updatedUser.TimeZone.Should().Be("Asia/Tokyo");
            updatedUser.IsPushEnabled.Should().BeTrue();
            updatedUser.PushSubscriptions.Should().ContainSingle(s =>
                s.Endpoint == "https://push.example.com/sub/null-tz");
        }
    }

    [Fact]
    public async Task SubscribePush_WithExistingSubscription_UpdatesKeysAndTimeZone()
    {
        // Arrange
        using (var db = CreateDbContext())
        {
            var user = new User
            {
                Id = _userId,
                Email = "update@techdaily.local",
                Name = "Learner Update",
                TimeZone = "America/New_York",
                IsPushEnabled = true
            };
            user.PushSubscriptions.Add(new UserPushSubscription
            {
                UserId = _userId,
                Endpoint = "https://push.example.com/sub/existing",
                P256dh = "old-p256dh",
                Auth = "old-auth",
                UserAgent = "OldAgent/1.0",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var request = new SubscribePushRequest(
            Endpoint: "https://push.example.com/sub/existing",
            Keys: new PushSubscriptionKeys("new-p256dh", "new-auth"),
            UserAgent: "NewAgent/2.0",
            TimeZone: "Asia/Ho_Chi_Minh"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/push/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var db = CreateDbContext())
        {
            var updatedUser = await db.Users
                .Include(u => u.PushSubscriptions)
                .FirstAsync(u => u.Id == _userId);

            updatedUser.TimeZone.Should().Be("Asia/Ho_Chi_Minh");
            updatedUser.PushSubscriptions.Should().HaveCount(1);
            var sub = updatedUser.PushSubscriptions.First();
            sub.P256dh.Should().Be("new-p256dh");
            sub.Auth.Should().Be("new-auth");
            sub.UserAgent.Should().Be("NewAgent/2.0");
        }
    }

    [Fact]
    public async Task SubscribePush_MissingRequiredFields_ReturnsBadRequest()
    {
        // Act - missing endpoint
        var request = new SubscribePushRequest(
            Endpoint: "",
            Keys: new PushSubscriptionKeys("key", "auth")
        );
        var response = await _client.PostAsJsonAsync("/api/v1/notifications/push/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TestPushNotification_WithValidSubscription_DispatchesAndReturnsOk()
    {
        // Arrange
        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "valid-sub@techdaily.local",
                Name = "Valid User",
                IsPushEnabled = true
            });
            db.UserPushSubscriptions.Add(new UserPushSubscription
            {
                UserId = _userId,
                Endpoint = "https://push.example.com/sub/valid",
                P256dh = "valid-p256dh",
                Auth = "valid-auth"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync("/api/v1/notifications/push/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TestPushSuccessResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Sent.Should().Be(1);
        body.Total.Should().Be(1);
        body.StalePurged.Should().Be(0);
    }

    [Fact]
    public async Task TestPushNotification_WithExpiredSubscription_PurgesStaleAndReturnsExpired()
    {
        // Arrange
        const string expiredEndpoint = "https://push.example.com/sub/expired";
        _mockWebPushService.ExpiredEndpoints.Add(expiredEndpoint);

        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "expired-sub@techdaily.local",
                Name = "Expired User",
                IsPushEnabled = true
            });
            db.UserPushSubscriptions.Add(new UserPushSubscription
            {
                UserId = _userId,
                Endpoint = expiredEndpoint,
                P256dh = "expired-p256dh",
                Auth = "expired-auth"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync("/api/v1/notifications/push/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<TestPushErrorResponse>();
        body.Should().NotBeNull();
        body!.Code.Should().Be("PUSH_SUBSCRIPTION_EXPIRED");
        body.Sent.Should().Be(0);
        body.Total.Should().Be(1);
        body.StalePurged.Should().Be(1);

        using (var db = CreateDbContext())
        {
            var remainingSubs = await db.UserPushSubscriptions
                .Where(s => s.UserId == _userId)
                .ToListAsync();
            remainingSubs.Should().BeEmpty();

            var user = await db.Users.FirstAsync(u => u.Id == _userId);
            user.IsPushEnabled.Should().BeFalse();
        }
    }

    [Fact]
    public async Task TestPushNotification_WithMixedSubscriptions_DeliversActiveAndPurgesExpired()
    {
        // Arrange
        const string activeEndpoint = "https://push.example.com/sub/active";
        const string expiredEndpoint = "https://push.example.com/sub/expired";
        _mockWebPushService.ExpiredEndpoints.Add(expiredEndpoint);

        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "mixed-sub@techdaily.local",
                Name = "Mixed User",
                IsPushEnabled = true
            });
            db.UserPushSubscriptions.AddRange(
                new UserPushSubscription
                {
                    UserId = _userId,
                    Endpoint = activeEndpoint,
                    P256dh = "active-p256dh",
                    Auth = "active-auth"
                },
                new UserPushSubscription
                {
                    UserId = _userId,
                    Endpoint = expiredEndpoint,
                    P256dh = "expired-p256dh",
                    Auth = "expired-auth"
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync("/api/v1/notifications/push/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TestPushSuccessResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Sent.Should().Be(1);
        body.Total.Should().Be(2);
        body.StalePurged.Should().Be(1);

        using (var db = CreateDbContext())
        {
            var remainingSubs = await db.UserPushSubscriptions
                .Where(s => s.UserId == _userId)
                .ToListAsync();
            remainingSubs.Should().ContainSingle(s => s.Endpoint == activeEndpoint);

            var user = await db.Users.FirstAsync(u => u.Id == _userId);
            user.IsPushEnabled.Should().BeTrue();
        }
    }

    [Fact]
    public async Task TestPushNotification_WithNoSubscriptions_ReturnsBadRequest()
    {
        // Arrange - user exists but has no push subscriptions
        using (var db = CreateDbContext())
        {
            db.Users.Add(new User
            {
                Id = _userId,
                Email = "no-sub@techdaily.local",
                Name = "No Sub User",
                IsPushEnabled = false
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync("/api/v1/notifications/push/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<TestPushErrorResponse>();
        body.Should().NotBeNull();
        body!.Code.Should().Be("PUSH_NO_SUBSCRIPTIONS");
        body.Detail.Should().Be("No active push subscriptions found for this device.");
        body.Sent.Should().BeNull();
    }

    private record TestPushSuccessResponse(bool Success, int Sent, int Total, int StalePurged);
    private record TestPushErrorResponse(string? Detail, string? Code, int? Sent, int? Total, int? StalePurged);

    private class MockWebPushService : IWebPushService
    {
        public string PublicKey => "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U";
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

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static Guid UserId { get; set; } = Guid.NewGuid();

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Email, "test@techdaily.local")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
