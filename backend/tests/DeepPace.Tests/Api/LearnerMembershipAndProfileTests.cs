using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DeepPace.Api.Contracts;
using DeepPace.Api.Endpoints;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Security;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class LearnerMembershipAndProfileTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private DbContextOptions<DeepPaceDbContext> _dbOptions = null!;

    private readonly FakeEmailSender _emailSender = new();
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _learnerUserId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using (var db = new DeepPaceDbContext(_dbOptions))
        {
            await db.Database.EnsureCreatedAsync();

            var adminUser = new User
            {
                Id = _adminUserId,
                Email = "admin@example.com",
                Name = "Admin User",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = AccountStatus.Active,
                RoleId = RoleDefaults.AdminRoleId
            };

            var learnerUser = new User
            {
                Id = _learnerUserId,
                Email = "learner@example.com",
                Name = "Learner User",
                PasswordHash = PasswordHasher.HashPassword("Password123!"),
                Status = AccountStatus.Active,
                RoleId = RoleDefaults.LearnerRoleId
            };

            await db.Users.AddRangeAsync(adminUser, learnerUser);
            await db.StreakRecords.AddRangeAsync(StreakRecord.Create(_adminUserId), StreakRecord.Create(_learnerUserId));
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var config = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "this-is-a-very-long-test-secret-key-32-bytes-long!",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test"
        };
        builder.Configuration.AddInMemoryCollection(config);

        builder.Services.AddRouting();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(_dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<IOtpService, OtpService>();
        builder.Services.AddSingleton<IEmailSender>(_emailSender);

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestProfileAuthHandler>("Test", _ => { });

        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();

        var authGroup = _app.MapGroup("/api/v1/auth");
        authGroup.MapAuthEndpoints(builder.Configuration);

        var userGroup = _app.MapGroup("/api/v1/user").RequireAuthorization();
        userGroup.MapUserEndpoints();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task RegisterVerify_WhenDefaultRoleMissing_ShouldFail503AndCreateNoUser()
    {
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = "newbie@example.com", password = "Password123!", name = "Newbie" });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var code = _emailSender.LastCode!;

        await using (var db = new DeepPaceDbContext(_dbOptions))
        {
            // Temporarily mark Learner role deleted
            var learnerRole = await db.Roles.FindAsync(RoleDefaults.LearnerRoleId);
            learnerRole!.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var res = await _client.PostAsJsonAsync("/api/v1/auth/register/verify", new VerifyRegistrationRequest("newbie@example.com", code));
        res.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        // Verify no user was created
        await using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "newbie@example.com");
            user.Should().BeNull();

            // Restore Learner role for subsequent tests
            var learnerRole = await db.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == RoleDefaults.LearnerRoleId);
            learnerRole!.IsDeleted = false;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task RegisterVerify_Success_ShouldAssignLearnerRoleAndReturnRoleDescriptor()
    {
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = "validuser@example.com", password = "Password123!", name = "Valid User" });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var code = _emailSender.LastCode!;

        var res = await _client.PostAsJsonAsync("/api/v1/auth/register/verify", new VerifyRegistrationRequest("validuser@example.com", code));
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<AuthSessionResponse>();
        body.Should().NotBeNull();
        body!.User.Role.Should().NotBeNull();
        body.User.Role.Id.Should().Be(RoleDefaults.LearnerRoleId);
        body.User.Role.Key.Should().Be(RoleDefaults.LearnerKey);

        // Verify database state
        await using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "validuser@example.com");
            user.Should().NotBeNull();
            user!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);
        }
    }

    [Fact]
    public async Task Profile_ShouldReturnRoleDescriptorAndNeverLeakPrivatePermissions()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/user/profile");
        req.Headers.Add("X-Test-User", _adminUserId.ToString());

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<UserProfileResponse>();
        body.Should().NotBeNull();
        body!.User.Role.Should().NotBeNull();
        body.User.Role.Id.Should().Be(RoleDefaults.AdminRoleId);
        body.User.Role.Key.Should().Be(RoleDefaults.AdminKey);

        // Verify JSON string does not contain permission keys
        var rawJson = await res.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("ManageCatalog");
        rawJson.Should().NotContain("ManageAccounts");
        rawJson.Should().NotContain("PermissionKeys");
    }

    [Fact]
    public async Task Login_ShouldReturnCorrectRoleDescriptorForBothAdminAndLearner()
    {
        // Admin login
        var adminRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@example.com", "Password123!"));
        adminRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminBody = await adminRes.Content.ReadFromJsonAsync<AuthSessionResponse>();
        adminBody!.User.Role.Id.Should().Be(RoleDefaults.AdminRoleId);
        adminBody.User.Role.Key.Should().Be(RoleDefaults.AdminKey);

        // Learner login
        var learnerRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("learner@example.com", "Password123!"));
        learnerRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var learnerBody = await learnerRes.Content.ReadFromJsonAsync<AuthSessionResponse>();
        learnerBody!.User.Role.Id.Should().Be(RoleDefaults.LearnerRoleId);
        learnerBody.User.Role.Key.Should().Be(RoleDefaults.LearnerKey);
    }

    private class FakeEmailSender : IEmailSender
    {
        public string? LastCode { get; private set; }

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var match = System.Text.RegularExpressions.Regex.Match(htmlBody, @"\b\d{6}\b");
            if (match.Success)
            {
                LastCode = match.Value;
            }
            return Task.CompletedTask;
        }
    }

    private class TestProfileAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestProfileAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userIdHeader = Request.Headers["X-Test-User"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim("session_version", "1")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
