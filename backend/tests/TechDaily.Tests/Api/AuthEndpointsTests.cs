using System.Net;
using System.Net.Http.Json;
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

public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private const string Email = "remember@techdaily.local";
    private const string Password = "password123";

    public AuthEndpointsTests()
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
        using (var db = new TechDailyDbContext(_dbOptions))
        {
            await db.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = Email,
                Name = "Remember User",
                PasswordHash = PasswordHasher.HashPassword(Password)
            });
            await db.SaveChangesAsync();
        }

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
        builder.Services.AddSingleton<IEmailSender>(new NoOpEmailSender());
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

    private static string RefreshCookieHeader(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue("login should set a refresh cookie");
        return cookies!.Single(c => c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
    }

    private static string RawRefreshToken(string setCookieHeader)
        => setCookieHeader.Split(';')[0]["refreshToken=".Length..];

    [Fact]
    public async Task Login_WithRememberMeTrue_SetsPersistentRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = Email, password = Password, rememberMe = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshCookieHeader(response).Should().Contain("max-age=2592000");
    }

    [Fact]
    public async Task Login_WithRememberMeOmitted_DefaultsToPersistentRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = Email, password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshCookieHeader(response).Should().Contain("max-age=2592000");
    }

    [Fact]
    public async Task Login_WithRememberMeFalse_SetsSessionRefreshCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = Email, password = Password, rememberMe = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = RefreshCookieHeader(response);
        cookie.Should().NotContainAny("max-age", "expires", "Max-Age", "Expires");
    }

    [Fact]
    public async Task Refresh_OnSessionScopedFamily_ReissuesSessionCookie()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = Email, password = Password, rememberMe = false });
        var rawToken = RawRefreshToken(RefreshCookieHeader(login));

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={rawToken}");
        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = RefreshCookieHeader(response);
        cookie.Should().NotContainAny("max-age", "expires", "Max-Age", "Expires");
    }

    [Fact]
    public async Task Refresh_OnPersistentFamily_ReissuesPersistentCookie()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = Email, password = Password, rememberMe = true });
        var rawToken = RawRefreshToken(RefreshCookieHeader(login));

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={rawToken}");
        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshCookieHeader(response).Should().Contain("max-age=2592000");
    }

    private sealed class NoOpStarterHandbookService : IStarterHandbookService
    {
        public Task ProvisionForUserAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default) => Task.CompletedTask;
    }
}
