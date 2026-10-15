using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeepPace.Api.Authorization;
using DeepPace.Api.Endpoints;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Documents;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class AdminDocumentOperationsApiTests : IAsyncLifetime
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();
    private readonly Guid _attemptId = Guid.NewGuid();
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        await using (var db = new DeepPaceDbContext(dbOptions))
        {
            await db.Database.EnsureCreatedAsync();

            var admin = new User { Id = _adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active };
            var learner = new User { Id = _learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active };
            var book = new DocumentBook
            {
                Id = _bookId,
                Title = "Kafka Architecture",
                CreatedByUserId = _learnerId,
                Status = ProcessingStatus.Failed,
                IngestionControlVersion = 2,
                IsDeleted = false
            };
            var source = IngestionSource.Create(IngestionSourceKind.Pdf, "sources/k.pdf", 4096, "hash", _learnerId, "en", DateTimeOffset.UtcNow);
            var attempt = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, _bookId, IngestionAttemptOrigin.UserUpload, "req-init", 1, sourceId: source.Id);
            attempt.Claim("tok", DateTimeOffset.UtcNow.AddMinutes(15), DateTimeOffset.UtcNow);
            attempt.Fail("tok", "Err", "Failed", DateTimeOffset.UtcNow.AddMinutes(5));

            book.AssignAttempt(attempt.Id);

            db.Users.AddRange(admin, learner);
            db.DocumentBooks.Add(book);
            db.IngestionSources.Add(source);
            db.IngestionAttempts.Add(attempt);
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["CatalogAdministration:UserIds:0"] = _adminId.ToString();

        builder.Services.AddRouting();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        builder.Services.AddScoped<IAdministrativeUnitOfWork, AdministrativeUnitOfWork>();
        builder.Services.AddSingleton<IDocumentMutationGuard, DocumentMutationGuard>();
        builder.Services.AddSingleton<IIngestionSourceStorage, LocalFileIngestionSourceStorage>();
        builder.Services.AddScoped<IIngestionQuotaService, IngestionQuotaService>();
        builder.Services.AddScoped<IIngestionClaimService, IngestionClaimService>();

        builder.Services.AddScoped<IUseCase<AdminDocumentInventoryQuery, AdminPagedResult<AdminDocumentInventoryItemDto>>, AdminGetDocumentInventoryHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetDocumentDetailRequest, AdminDocumentDetailDto>, AdminGetDocumentDetailHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetDocumentAttemptsRequest, AdminPagedResult<AdminDocumentAttemptItemDto>>, AdminGetDocumentAttemptsHandler>();
        builder.Services.AddScoped<IUseCase<AdminRetryDocumentCommand, AdminRetryDocumentResponseDto>, AdminRetryDocumentHandler>();
        builder.Services.AddScoped<IUseCase<ReplacePersonalDocumentSourceCommand, SourceReplacementResponseDto>, ReplacePersonalDocumentSourceHandler>();
        builder.Services.AddScoped<IUseCase<ReplaceCatalogDocumentSourceCommand, SourceReplacementResponseDto>, ReplaceCatalogDocumentSourceHandler>();

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AccountAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AuditReadAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, DocumentOperationsAdminAuthorizationHandler>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CatalogAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CatalogAdminRequirement());
            });
            options.AddPolicy("AccountAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AccountAdminRequirement());
            });
            options.AddPolicy("AuditRead", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AuditReadRequirement());
            });
            options.AddPolicy("DocumentOperationsAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new DocumentOperationsAdminRequirement());
            });
        });

        builder.Services.AddAuthentication("TestAuth")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", _ => { });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapAdminDocumentOperationsEndpoints();
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
    public async Task GetInventory_AsAdmin_Returns200WithItems()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminPagedResult<AdminDocumentInventoryItemDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
        body.Items[0].Title.Should().Be("Kafka Architecture");
    }

    [Fact]
    public async Task GetInventory_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInventory_AsLearner_Returns403()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _learnerId.ToString());

        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDetail_AsAdmin_Returns200()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync($"/api/v1/admin/documents/personal/{_bookId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminDocumentDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(_bookId);
        body.IsRetryEligible.Should().BeTrue();
    }

    [Fact]
    public async Task GetAttempts_AsAdmin_Returns200()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync($"/api/v1/admin/documents/personal/{_bookId}/attempts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminPagedResult<AdminDocumentAttemptItemDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Retry_AsAdmin_Eligible_Returns202Accepted()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        // Get current attempt id from db
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var attempt = await db.IngestionAttempts.FirstAsync();
        var book = await db.DocumentBooks.FirstAsync();

        var payload = new AdminRetryPayload(
            AttemptId: attempt.Id,
            ExpectedVersion: book.IngestionControlVersion,
            RequestId: "api-retry-req-1",
            Reason: "Retry from API test");

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/documents/personal/{_bookId}/retry", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<AdminRetryDocumentResponseDto>();
        body.Should().NotBeNull();
        body!.TargetDocumentId.Should().Be(_bookId);
        body.State.Should().Be("Queued");
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdValues) ||
                !Guid.TryParse(userIdValues.FirstOrDefault(), out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim("session_version", "1")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
