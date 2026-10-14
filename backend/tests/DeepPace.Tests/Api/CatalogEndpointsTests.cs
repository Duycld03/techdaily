using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeepPace.Api.Authorization;
using DeepPace.Api.Endpoints;
using DeepPace.Application;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class CatalogEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DeepPaceDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private ConfigurationManager _configuration = null!;

    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _regularUserId = Guid.NewGuid();
    private readonly Guid _deletedAdminUserId = Guid.NewGuid();

    public CatalogEndpointsTests()
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
        // Seed users
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var adminUser = new User { Id = _adminUserId, Email = "admin@example.com", Name = "Admin User", IsDeleted = false };
            var regularUser = new User { Id = _regularUserId, Email = "regular@example.com", Name = "Regular User", IsDeleted = false };
            var deletedAdmin = new User { Id = _deletedAdminUserId, Email = "deleted_admin@example.com", Name = "Deleted Admin", IsDeleted = true };

            await db.Users.AddRangeAsync(adminUser, regularUser, deletedAdmin);
            await db.SaveChangesAsync();
        }

        TestCatalogAuthHandler.CurrentUserId = _adminUserId;
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.ExtraClaims.Clear();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Configure CatalogAdministration allowlist with _adminUserId and _deletedAdminUserId
        builder.Configuration["CatalogAdministration:UserIds:0"] = _adminUserId.ToString();
        builder.Configuration["CatalogAdministration:UserIds:1"] = _deletedAdminUserId.ToString();
        _configuration = builder.Configuration;

        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(_dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddApplicationServices();
        builder.Services.AddScoped<ICatalogAdoptionService, CatalogAdoptionService>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IPdfIngestionQueue, FakePdfQueue>();
        builder.Services.AddScoped<IWebArticleCrawler, FakeArticleCrawler>();

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();

        builder.Services.AddAuthentication(defaultScheme: "Test")
            .AddScheme<AuthenticationSchemeOptions, TestCatalogAuthHandler>("Test", _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CatalogAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CatalogAdminRequirement());
            });
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapOpenApi();
        _app.MapCatalogEndpoints();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        _connection.Dispose();
    }

    [Fact]
    public async Task Capabilities_WhenAnonymous_Returns401Unauthorized()
    {
        TestCatalogAuthHandler.IsAuthenticated = false;
        var response = await _client.GetAsync("/api/v1/catalog/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Capabilities_WhenRegularUser_ReturnsCanManageCatalogFalse()
    {
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _regularUserId;

        var response = await _client.GetAsync("/api/v1/catalog/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CatalogCapabilitiesDto>();
        result.Should().NotBeNull();
        result!.CanManageCatalog.Should().BeFalse();
    }

    [Fact]
    public async Task Capabilities_WhenDeletedUserInAllowlist_ReturnsCanManageCatalogFalse()
    {
        // User is configured in allowlist, but soft-deleted in database
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _deletedAdminUserId;

        var response = await _client.GetAsync("/api/v1/catalog/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CatalogCapabilitiesDto>();
        result.Should().NotBeNull();
        result!.CanManageCatalog.Should().BeFalse();
    }

    [Fact]
    public async Task Capabilities_WhenActiveAdminUser_ReturnsCanManageCatalogTrue()
    {
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _adminUserId;

        var response = await _client.GetAsync("/api/v1/catalog/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CatalogCapabilitiesDto>();
        result.Should().NotBeNull();
        result!.CanManageCatalog.Should().BeTrue();
    }

    [Fact]
    public async Task Capabilities_WhenSpoofedClientRoleSupplied_DoesNotGrantAccess()
    {
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _regularUserId;
        TestCatalogAuthHandler.ExtraClaims.Add(new Claim(ClaimTypes.Role, "Admin"));
        TestCatalogAuthHandler.ExtraClaims.Add(new Claim("role", "administrator"));

        var response = await _client.GetAsync("/api/v1/catalog/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CatalogCapabilitiesDto>();
        result.Should().NotBeNull();
        result!.CanManageCatalog.Should().BeFalse();
    }

    [Fact]
    public async Task AdminRoutes_EnforceDefaultDenyAndActiveUserPolicy()
    {
        // 1. Anonymous -> 401
        TestCatalogAuthHandler.IsAuthenticated = false;
        var anonRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Regular user -> 403 Forbidden
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _regularUserId;
        var regularRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        regularRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Deleted user in allowlist -> 403 Forbidden
        TestCatalogAuthHandler.CurrentUserId = _deletedAdminUserId;
        var deletedRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        deletedRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Active admin -> 200 OK
        TestCatalogAuthHandler.CurrentUserId = _adminUserId;
        var adminRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        adminRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminRoutes_RevocationImmediateWhenRemovedFromConfiguration()
    {
        // Initially active admin has access
        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _adminUserId;

        var initialRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        initialRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Operator revokes admin by removing ID from configuration
        _configuration["CatalogAdministration:UserIds:0"] = Guid.NewGuid().ToString();

        var revokedRes = await _client.GetAsync("/api/v1/admin/catalog/documents");
        revokedRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LearnerAdopt_WhenNewAdoption_Returns201Created_AndWhenDuplicate_Returns200OK()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Adoption Test Catalog Doc",
            Description = "Desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attribution",
            Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 1
        };
        doc.Slices.Add(new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = doc.Id,
            SliceOrder = 1,
            ChapterTitle = "Slice 1",
            OriginalTextMarkdown = "Markdown text",
            SummaryMarkdown = "Summary",
            IsAiFormatted = true
        });

        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _regularUserId;

        // First adoption: 201 Created
        var firstAdoptRes = await _client.PostAsync($"/api/v1/catalog/documents/{doc.Id}/adopt", null);
        firstAdoptRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstResult = await firstAdoptRes.Content.ReadFromJsonAsync<CatalogAdoptionResult>();
        firstResult.Should().NotBeNull();
        firstResult!.AlreadyAdded.Should().BeFalse();
        firstResult.BookId.Should().NotBeEmpty();

        // Second adoption: 200 OK
        var secondAdoptRes = await _client.PostAsync($"/api/v1/catalog/documents/{doc.Id}/adopt", null);
        secondAdoptRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResult = await secondAdoptRes.Content.ReadFromJsonAsync<CatalogAdoptionResult>();
        secondResult.Should().NotBeNull();
        secondResult!.AlreadyAdded.Should().BeTrue();
        secondResult.BookId.Should().Be(firstResult.BookId);
    }

    [Fact]
    public async Task LearnerEndpoints_Return404ForHiddenOrMissingDocument()
    {
        var hiddenDoc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Hidden Catalog Doc",
            Description = "Desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attribution",
            Visibility = CatalogVisibility.Hidden,
            ProcessingStatus = ProcessingStatus.Ready
        };

        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            await db.CatalogDocuments.AddAsync(hiddenDoc);
            await db.SaveChangesAsync();
        }

        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _regularUserId;

        // GET detail -> 404
        var detailRes = await _client.GetAsync($"/api/v1/catalog/documents/{hiddenDoc.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        detailRes.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        // POST adopt -> 404
        var adoptRes = await _client.PostAsync($"/api/v1/catalog/documents/{hiddenDoc.Id}/adopt", null);
        adoptRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        adoptRes.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task AdminPrepare_Returns202Accepted()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Prepare Doc",
            Description = "Desc",
            Category = Category.FrontendWeb,
            AttributionText = "Attr",
            Visibility = CatalogVisibility.Draft,
            SourceType = SourceType.WebDocUrl,
            ProcessingStatus = ProcessingStatus.Pending
        };

        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            await db.CatalogDocuments.AddAsync(doc);
            await db.SaveChangesAsync();
        }

        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _adminUserId;

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/catalog/documents/{doc.Id}/prepare", new AdminPrepareCatalogApiRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task AdminPublish_WhenCriteriaNotMet_Returns409Conflict_WithRfc7807ProblemDetails()
    {
        var draftDoc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Unprepared Draft",
            Description = "Desc",
            Category = Category.FrontendWeb,
            AttributionText = "Attr",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Pending,
            Revision = 1
        };

        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            await db.CatalogDocuments.AddAsync(draftDoc);
            await db.SaveChangesAsync();
        }

        TestCatalogAuthHandler.IsAuthenticated = true;
        TestCatalogAuthHandler.CurrentUserId = _adminUserId;

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/admin/catalog/documents/{draftDoc.Id}/publish",
            new AdminPublishCatalogApiRequest(ExpectedRevision: 1));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
    [Fact]
    public async Task OpenApi_ExposesCatalogContracts_WithoutSecretsOrRoleManagementEndpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();

        // Exposes catalog contracts
        content.Should().Contain("/api/v1/catalog/documents");
        content.Should().Contain("/api/v1/catalog/capabilities");
        content.Should().Contain("/api/v1/admin/catalog/documents");

        // No public role management endpoints exist
        content.Should().NotContain("/api/v1/roles");
        content.Should().NotContain("/api/v1/admin/roles");
        content.Should().NotContain("RoleManagement");

        // No real administrator accounts or secrets leaked
        content.Should().NotContain(_adminUserId.ToString());
    }


    public class FakePdfQueue : IPdfIngestionQueue
    {
        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default) => EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<PdfIngestJob> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    public class FakeArticleCrawler : IWebArticleCrawler
    {
        public Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CrawlArticleResult(
                Title: "Article",
                MarkdownContent: "# Article\n\nContent",
                SourceUrl: url,
                EstimatedWordCount: 50,
                IsPdfDetected: false));
        }
    }

    public class TestCatalogAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static Guid CurrentUserId { get; set; } = Guid.NewGuid();
        public static bool IsAuthenticated { get; set; } = true;
        public static List<Claim> ExtraClaims { get; } = new();

        public TestCatalogAuthHandler(
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, CurrentUserId.ToString()),
                new Claim(ClaimTypes.Email, "catalog-user@techdaily.local")
            };
            claims.AddRange(ExtraClaims);

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
