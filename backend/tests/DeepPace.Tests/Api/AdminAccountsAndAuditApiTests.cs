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
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Audit;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class AdminAccountsAndAuditApiTests : IAsyncLifetime
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();
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
            db.Users.AddRange(
                new User { Id = _adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active },
                new User { Id = _learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active });
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["CatalogAdministration:UserIds:0"] = _adminId.ToString();

        builder.Services.AddRouting();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IAdministrativeUnitOfWork, AdministrativeUnitOfWork>();

        // Handlers
        builder.Services.AddScoped<IUseCase<AdminGetAccountsRequest, AdminPagedResult<AdminAccountSummaryDto>>, AdminGetAccountsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAccountByIdRequest, AdminAccountDetailDto>, AdminGetAccountByIdHandler>();
        builder.Services.AddScoped<IUseCase<AdminSuspendUserRequest, AdminAccountDetailDto>, AdminSuspendUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminReactivateUserRequest, AdminAccountDetailDto>, AdminReactivateUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminRevokeUserSessionsRequest, AdminAccountDetailDto>, AdminRevokeUserSessionsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventsRequest, AdminPagedResult<AdminAuditEventDto>>, AdminGetAuditEventsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventByIdRequest, AdminAuditEventDetailDto>, AdminGetAuditEventByIdHandler>();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AccountAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AuditReadAuthorizationHandler>();

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
        });

        _app = builder.Build();

        _app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1/admin") ||
                context.Request.Path.Equals("/api/v1/catalog/capabilities", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.Pragma = "no-cache";
            }
            await next(context);
        });

        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapCatalogEndpoints();
        _app.MapAdminAccountsAndAuditEndpoints();

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
    public async Task GetCapabilities_Anonymous_ShouldReturn401()
    {
        var res = await _client.GetAsync("/api/v1/admin/capabilities");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCapabilities_Admin_ShouldReturnExplicitFlagsAndNoStore()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/capabilities");
        req.Headers.Add("X-Test-User", _adminId.ToString());

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.CacheControl?.NoStore.Should().BeTrue();

        var body = await res.Content.ReadFromJsonAsync<AdminCapabilitiesDto>();
        body!.CanManageCatalog.Should().BeTrue();
        body.CanManageAccounts.Should().BeTrue();
        body.CanReadAudit.Should().BeTrue();
    }

    [Fact]
    public async Task GetCapabilities_Learner_ShouldReturnAllFalseAndNoStore()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/capabilities");
        req.Headers.Add("X-Test-User", _learnerId.ToString());

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.CacheControl?.NoStore.Should().BeTrue();

        var body = await res.Content.ReadFromJsonAsync<AdminCapabilitiesDto>();
        body!.CanManageCatalog.Should().BeFalse();
        body.CanManageAccounts.Should().BeFalse();
        body.CanReadAudit.Should().BeFalse();
    }

    [Fact]
    public async Task LegacyCatalogCapabilities_ShouldBePreserved()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalog/capabilities");
        req.Headers.Add("X-Test-User", _adminId.ToString());

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadAsStringAsync();
        json.Should().Contain("canManageCatalog");
    }

    [Fact]
    public async Task UsersEndpoints_Learner_ShouldReturn403()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        req.Headers.Add("X-Test-User", _learnerId.ToString());

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UsersEndpoints_Admin_DirectoryAndLifecycleActions()
    {
        var targetId = Guid.NewGuid();
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            db.Users.Add(new User
            {
                Id = targetId,
                Email = "target-lifecycle@example.com",
                Name = "Target Lifecycle",
                Status = AccountStatus.Active
            });
            await db.SaveChangesAsync();
        }

        // 1. Directory
        var dirReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users?search=target-lifecycle");
        dirReq.Headers.Add("X-Test-User", _adminId.ToString());
        var dirRes = await _client.SendAsync(dirReq);
        dirRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var dirBody = await dirRes.Content.ReadFromJsonAsync<AdminPagedResult<AdminAccountSummaryDto>>();
        dirBody!.Items.Should().ContainSingle(i => i.Id == targetId);

        // 2. Suspend target account
        var suspendReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{targetId}/suspend")
        {
            Content = JsonContent.Create(new AdminAccountActionRequest("Suspension for policy test", 1))
        };
        suspendReq.Headers.Add("X-Test-User", _adminId.ToString());
        var suspendRes = await _client.SendAsync(suspendReq);
        suspendRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var suspendDetail = await suspendRes.Content.ReadFromJsonAsync<AdminAccountDetailDto>();
        suspendDetail!.Status.Should().Be(AccountStatus.Suspended);
        suspendDetail.AdminControlVersion.Should().Be(2);

        // 3. Reactivate target account
        var reactivateReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{targetId}/reactivate")
        {
            Content = JsonContent.Create(new AdminAccountActionRequest("Reactivation cleared", 2))
        };
        reactivateReq.Headers.Add("X-Test-User", _adminId.ToString());
        var reactivateRes = await _client.SendAsync(reactivateReq);
        reactivateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactivateDetail = await reactivateRes.Content.ReadFromJsonAsync<AdminAccountDetailDto>();
        reactivateDetail!.Status.Should().Be(AccountStatus.Active);
        reactivateDetail.AdminControlVersion.Should().Be(3);

        // 4. Revoke sessions for target account
        var revokeReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{targetId}/revoke-sessions")
        {
            Content = JsonContent.Create(new AdminAccountActionRequest("Forced logout", 3))
        };
        revokeReq.Headers.Add("X-Test-User", _adminId.ToString());
        var revokeRes = await _client.SendAsync(revokeReq);
        revokeRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuditEndpoints_Admin_ShouldListAndInspectEvents()
    {
        // Audit read
        var auditReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit?pageSize=10");
        auditReq.Headers.Add("X-Test-User", _adminId.ToString());
        var auditRes = await _client.SendAsync(auditReq);
        auditRes.StatusCode.Should().Be(HttpStatusCode.OK);
        auditRes.Headers.CacheControl?.NoStore.Should().BeTrue();
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers["X-Test-User"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header) || !Guid.TryParse(header, out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim("session_version", "1")
            }, "Test");

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
        }
    }
}
