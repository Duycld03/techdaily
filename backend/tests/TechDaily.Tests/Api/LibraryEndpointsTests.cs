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
using System.Net.Http.Json;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.SynthesizeAudio;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

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
        builder.Services.AddScoped<IGoogleCloudTtsService, MockTtsService>();

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

    [Fact]
    public async Task SynthesizeChunkAudio_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        TestAuthHandler.IsAuthenticated = false;
        var response = await _client.PostAsJsonAsync($"/api/v1/library/chunks/{Guid.NewGuid()}/audio", new { voiceId = "vi-VN-Neural2-A" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SynthesizeChunkAudio_WhenChunkNotFound_ShouldReturn404NotFound()
    {
        TestAuthHandler.IsAuthenticated = true;
        var response = await _client.PostAsJsonAsync($"/api/v1/library/chunks/{Guid.NewGuid()}/audio", new { voiceId = "vi-VN-Neural2-A" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SynthesizeChunkAudio_WhenValid_ShouldReturn200OkWithAudioMpeg()
    {
        TestAuthHandler.IsAuthenticated = true;
        using var db = new TechDailyDbContext(_dbOptions);
        var book = new DocumentBook
        {
            Title = "Audio Test Book",
            Slug = "audio-test-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready
        };
        await db.DocumentBooks.AddAsync(book);
        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Intro",
            OriginalTextMarkdown = "Welcome to TechDaily audio narration.",
            SummaryMarkdown = "Summary",
            Language = "vi",
            IsAiFormatted = true
        };
        await db.DocumentChunks.AddAsync(chunk);
        await db.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/library/chunks/{chunk.Id}/audio",
            new { voiceId = "vi-VN-Neural2-A" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("audio/mpeg");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().Equal([0x49, 0x44, 0x33, 0x04]);
    }

    [Fact]
    public async Task GetAudioQuota_WhenAuthenticated_ShouldReturn200OkWithQuotaInfo()
    {
        TestAuthHandler.IsAuthenticated = true;
        var response = await _client.GetAsync("/api/v1/library/audio/quota");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var quota = await response.Content.ReadFromJsonAsync<AudioQuotaResponse>();
        quota.Should().NotBeNull();
        quota!.MonthlyLimit.Should().Be(950_000);
    }

    [Fact]
    public async Task GetAudioQuota_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        TestAuthHandler.IsAuthenticated = false;
        var response = await _client.GetAsync("/api/v1/library/audio/quota");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class MockTtsService : IGoogleCloudTtsService
    {
        public Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(
            string text,
            string voiceId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<GoogleCloudTtsResult>.Success(
                new GoogleCloudTtsResult([0x49, 0x44, 0x33, 0x04], text.Length, 2.0)));
        }
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
