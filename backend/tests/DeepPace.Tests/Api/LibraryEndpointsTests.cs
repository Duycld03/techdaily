using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeepPace.Api.Endpoints;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;
using System.Net.Http.Json;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.SynthesizeAudio;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Tests.Api;

public class LibraryEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DeepPaceDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _userId = Guid.NewGuid();

    public LibraryEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initDb = new DeepPaceDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        TestAuthHandler.UserId = _userId;
        TestAuthHandler.IsAuthenticated = true;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(_dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddApiTestApplicationServices();
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
    public async Task GetBooks_LanguageCombinesOwnerCategorySearchBeforeCountsAndPaging()
    {
        using var db = new DeepPaceDbContext(_dbOptions);
        var otherOwner = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = _userId, Email = "owner@example.test", Name = "Owner" },
            new User { Id = otherOwner, Email = "other@example.test", Name = "Other" });
        var now = DateTimeOffset.UtcNow;
        var matches = Enumerable.Range(1, 3).Select(index => new DocumentBook
        {
            Title = $"Language match {index}", Slug = $"language-match-{index}",
            CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = _userId,
            DeclaredLanguage = "vi", CreatedAt = now.AddMinutes(index)
        }).ToArray();
        db.DocumentBooks.AddRange(matches);
        db.DocumentBooks.AddRange(
            new DocumentBook { Title = "Language match English", Slug = "english", CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = _userId, DeclaredLanguage = "en", CreatedAt = now.AddMinutes(10) },
            new DocumentBook { Title = "Language match other owner", Slug = "other-owner", CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = otherOwner, DeclaredLanguage = "vi" },
            new DocumentBook { Title = "Language match other category", Slug = "other-category", CategoryId = CategoryDefaults.FrontendWebId, CreatedByUserId = _userId, DeclaredLanguage = "vi" },
            new DocumentBook { Title = "Unrelated", Slug = "unrelated", CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = _userId, DeclaredLanguage = "vi" },
            new DocumentBook { Title = "Language match deleted", Slug = "deleted", CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = _userId, DeclaredLanguage = "vi", IsDeleted = true },
            new DocumentBook { Title = "Language match unpublished", Slug = "unpublished", CategoryId = CategoryDefaults.BackendRuntimeId, CreatedByUserId = _userId, DeclaredLanguage = "vi", IsPublished = false });
        await db.SaveChangesAsync();
        var criteria = $"/api/v1/library/books?categoryId={CategoryDefaults.BackendRuntimeId}&search=Language%20match&pageSize=2";

        var response = await _client.GetAsync($"{criteria}&language=%20VI%20&page=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = json.RootElement;
        body.GetProperty("totalCount").GetInt32().Should().Be(3);
        body.GetProperty("totalPages").GetInt32().Should().Be(2);
        body.GetProperty("page").GetInt32().Should().Be(2);
        var book = body.GetProperty("books").EnumerateArray().Should().ContainSingle().Which;
        book.GetProperty("id").GetGuid().Should().Be(matches[0].Id);
        book.GetProperty("language").GetString().Should().Be("vi");

        foreach (var language in new[] { "", "&language=", "&language=%20%20" })
        {
            var cleared = await _client.GetAsync($"{criteria}{language}");
            cleared.StatusCode.Should().Be(HttpStatusCode.OK);
            using var clearedJson = JsonDocument.Parse(await cleared.Content.ReadAsStringAsync());
            clearedJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(4);
            clearedJson.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);
        }
    }

    [Fact]
    public async Task GetBooks_LanguageUsesEditedSlicesInsteadOfStaleDeclaration()
    {
        using var db = new DeepPaceDbContext(_dbOptions);
        db.Users.Add(new User { Id = _userId, Email = "editor@example.test", Name = "Editor" });
        var book = new DocumentBook
        {
            Title = "Edited language", Slug = "edited-language", CategoryId = CategoryDefaults.BackendRuntimeId,
            CreatedByUserId = _userId, DeclaredLanguage = "en",
            Chunks = [new DocumentChunk { ChunkOrder = 1, Language = "en" }]
        };
        db.DocumentBooks.Add(book);
        await db.SaveChangesAsync();
        var updated = await _client.PatchAsJsonAsync($"/api/v1/library/books/{book.Id}", new
        {
            title = book.Title, categoryId = book.CategoryId, language = "vi"
        });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var vietnamese = await _client.GetAsync("/api/v1/library/books?language=vi");
        vietnamese.StatusCode.Should().Be(HttpStatusCode.OK);
        using var viJson = JsonDocument.Parse(await vietnamese.Content.ReadAsStringAsync());
        var returned = viJson.RootElement.GetProperty("books").EnumerateArray().Should().ContainSingle().Which;
        returned.GetProperty("id").GetGuid().Should().Be(book.Id);
        returned.GetProperty("language").GetString().Should().Be("vi");
        var english = await _client.GetAsync("/api/v1/library/books?language=en");
        english.StatusCode.Should().Be(HttpStatusCode.OK);
        using var enJson = JsonDocument.Parse(await english.Content.ReadAsStringAsync());
        enJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        await db.Entry(book).ReloadAsync();
        book.DeclaredLanguage.Should().Be("en");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("en-US")]
    [InlineData("%27%3BSELECT")]
    [InlineData("PRIVATE_LANGUAGE_SENTINEL")]
    [InlineData("en%27%3BPRIVATE_LANGUAGE_SENTINEL")]
    public async Task GetBooks_UnsupportedLanguage_Returns400ProblemDetails(string language)
    {
        var response = await _client.GetAsync($"/api/v1/library/books?language={language}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        json.RootElement.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.GetProperty("code").GetString().Should().Be("Library.InvalidLanguage");
        var body = await response.Content.ReadAsStringAsync();
        foreach (var diagnostic in new[] { "PRIVATE_LANGUAGE_SENTINEL", "SELECT", "System.", "stackTrace", "Npgsql", "Sqlite", "SELECT *", "DocumentBooks" })
            body.Should().NotContain(diagnostic);
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
        using var db = new DeepPaceDbContext(_dbOptions);
        var book = new DocumentBook
        {
            Title = "Audio Test Book",
            Slug = "audio-test-book",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Status = ProcessingStatus.Ready
        };
        await db.DocumentBooks.AddAsync(book);
        var chunk = new DocumentChunk
        {
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Intro",
            OriginalTextMarkdown = "Welcome to DeepPace audio narration.",
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
                new Claim(ClaimTypes.Email, "test@deeppace.test")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
