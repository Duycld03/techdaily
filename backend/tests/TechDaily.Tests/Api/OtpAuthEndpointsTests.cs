using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TechDaily.Api.Endpoints;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Security;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Api;

public class OtpAuthEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _dbOptions;
    private readonly CapturingEmailSender _email = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public OtpAuthEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<TechDailyDbContext>().UseSqlite(_connection).Options;
        using var initDb = new TechDailyDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-signing-secret-key-256bit-minimum",
                ["Jwt:Issuer"] = "TechDaily",
                ["Jwt:Audience"] = "TechDailyUsers"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<TechDailyDbContext>(_ => new TechDailyDbContext(_dbOptions));
        builder.Services.AddScoped<ITechDailyDbContext>(sp => sp.GetRequiredService<TechDailyDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<IStarterHandbookService, NoOpStarterHandbookService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IEmailSender>(_email);
        builder.Services.AddScoped<IOtpService, OtpService>();

        _app = builder.Build();
        _app.MapGroup("/api/v1/auth").MapAuthEndpoints(configuration);

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task SeedUserAsync(string email, string? password, string? googleSubjectId = null)
    {
        await using var db = new TechDailyDbContext(_dbOptions);
        await db.Users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            Name = "Seed User",
            PasswordHash = password == null ? string.Empty : PasswordHasher.HashPassword(password),
            GoogleSubjectId = googleSubjectId
        });
        await db.SaveChangesAsync();
    }

    private async Task<int> UserCountAsync(string email)
    {
        await using var db = new TechDailyDbContext(_dbOptions);
        return await db.Users.CountAsync(u => u.Email == email.ToLowerInvariant());
    }

    private async Task<string?> PasswordHashAsync(string email)
    {
        await using var db = new TechDailyDbContext(_dbOptions);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant());
        return u?.PasswordHash;
    }

    private static string? RawRefreshToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        var cookie = cookies.FirstOrDefault(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
        return cookie?.Split(';')[0]["refreshToken=".Length..];
    }

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
    }

    // --- Registration (task 4.1) ---

    [Fact]
    public async Task Register_ReturnsNoSession_AndEmailsCode_AndCreatesNoUserYet()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "newuser@example.com", password = "password123", name = "New User" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        RawRefreshToken(resp).Should().BeNull("registration step 1 must not issue a session");
        _email.LastCode.Should().NotBeNull();
        (await UserCountAsync("newuser@example.com")).Should().Be(0, "no account exists before verification");
    }

    [Fact]
    public async Task RegisterVerify_WithCorrectCode_CreatesUserAndSession()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "verify@example.com", password = "password123", name = "Verify User" });
        var code = _email.LastCode!;

        var verify = await _client.PostAsJsonAsync("/api/v1/auth/register/verify",
            new { email = "verify@example.com", code });

        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        RawRefreshToken(verify).Should().NotBeNull("verification signs the user in");
        (await UserCountAsync("verify@example.com")).Should().Be(1);
    }

    [Fact]
    public async Task Register_WithExistingEmail_Returns409()
    {
        await SeedUserAsync("taken@example.com", "password123");

        var resp = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "taken@example.com", password = "password123", name = "Dup" });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(resp)).Should().Be("AUTH_EMAIL_EXISTS");
        _email.LastCode.Should().BeNull("no verification email for a duplicate registration");
    }

    [Fact]
    public async Task RegisterVerify_WithWrongCode_Rejected_AndCreatesNoAccount()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "wrongcode@example.com", password = "password123", name = "WC" });
        var correct = _email.LastCode!;
        var wrong = correct == "000000" ? "111111" : "000000";

        var verify = await _client.PostAsJsonAsync("/api/v1/auth/register/verify",
            new { email = "wrongcode@example.com", code = wrong });

        verify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(verify)).Should().Be("AUTH_OTP_INVALID");
        (await UserCountAsync("wrongcode@example.com")).Should().Be(0);
    }

    // --- Forgot / reset (tasks 4.2, 3.1) ---

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns200_AndSendsNothing()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "nobody@example.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        _email.LastCode.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_WithValidCode_UpdatesHash_AndRevokesAllSessions()
    {
        await SeedUserAsync("reset@example.com", "oldpassword1");

        // Two device sessions
        var login1 = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "reset@example.com", password = "oldpassword1" });
        var token1 = RawRefreshToken(login1)!;
        var login2 = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "reset@example.com", password = "oldpassword1" });
        var token2 = RawRefreshToken(login2)!;

        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "reset@example.com" });
        var code = _email.LastCode!;

        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { email = "reset@example.com", code, newPassword = "brandnew123" });
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        // All prior sessions are revoked
        foreach (var token in new[] { token1, token2 })
        {
            var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
            refreshReq.Headers.Add("Cookie", $"refreshToken={token}");
            (await _client.SendAsync(refreshReq)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // New password works; old does not
        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "reset@example.com", password = "brandnew123" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "reset@example.com", password = "oldpassword1" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ForGoogleOnlyAccount_EstablishesPassword()
    {
        await SeedUserAsync("googleuser@example.com", password: null, googleSubjectId: "google-sub-123");

        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "googleuser@example.com" });
        var code = _email.LastCode!;

        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { email = "googleuser@example.com", code, newPassword = "freshpass123" });

        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PasswordHashAsync("googleuser@example.com")).Should().NotBeNullOrEmpty();
        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "googleuser@example.com", password = "freshpass123" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithWrongCode_Rejected_AndPasswordUnchanged()
    {
        await SeedUserAsync("keep@example.com", "keepme12345");

        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "keep@example.com" });
        var correct = _email.LastCode!;
        var wrong = correct == "000000" ? "111111" : "000000";

        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { email = "keep@example.com", code = wrong, newPassword = "newpass12345" });

        reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(reset)).Should().Be("AUTH_OTP_INVALID");
        (await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "keep@example.com", password = "keepme12345" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Resend cooldown (task 4.3) ---

    [Fact]
    public async Task Resend_WithinCooldown_ReturnsCooldownError()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "resend@example.com", password = "password123", name = "Resend" });

        var resend = await _client.PostAsJsonAsync("/api/v1/auth/otp/resend",
            new { email = "resend@example.com", purpose = "EmailVerification" });

        resend.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ErrorCodeAsync(resend)).Should().Be("AUTH_OTP_RESEND_COOLDOWN");
    }

    private sealed class NoOpStarterHandbookService : IStarterHandbookService
    {
        public Task ProvisionForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string? LastCode { get; private set; }
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var m = Regex.Match(htmlBody, "\\d{6}");
            if (m.Success) LastCode = m.Value;
            return Task.CompletedTask;
        }
    }
}
