using System.Net;
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
using TechDaily.Application;
using TechDaily.Application.Interfaces;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Api;

public class LibraryEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _userId = Guid.NewGuid();

    public LibraryEndpointsTests()
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
        TestAuthHandler.UserId = _userId;
        TestAuthHandler.IsAuthenticated = true;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<ITechDailyDbContext>(_ => new TechDailyDbContext(_dbOptions));
        builder.Services.AddApplicationServices();

        builder.Services.AddAuthentication(defaultScheme: "Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapLibraryEndpoints();

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
    public async Task GetBooks_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.GetAsync("/api/v1/library/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBookById_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.GetAsync($"/api/v1/library/books/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBookStatus_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.GetAsync($"/api/v1/library/books/{Guid.NewGuid()}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBookSlice_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.GetAsync($"/api/v1/library/books/{Guid.NewGuid()}/slices/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurateSlice_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.PostAsync($"/api/v1/library/books/{Guid.NewGuid()}/slices/1/curate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBooks_WhenAuthenticated_ShouldReturn200Ok()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.UserId = _userId;

        // Act
        var response = await _client.GetAsync("/api/v1/library/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static Guid UserId { get; set; } = Guid.NewGuid();
        public static bool IsAuthenticated { get; set; } = true;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

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
