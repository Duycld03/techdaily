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
                new User { Id = _adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId },
                new User { Id = _learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId });
            db.AuthorizationInitializations.Add(new AuthorizationInitialization
            {
                Scope = AuthorizationInitialization.DefaultScope,
                CompletedAt = DateTimeOffset.UtcNow,
                ImportedAdminUserIds = new List<Guid> { _adminId }
            });
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IAuthorizationReadinessService, AuthorizationReadinessService>();
        builder.Services.AddScoped<IAdministrativeUnitOfWork, AdministrativeUnitOfWork>();

        // Handlers
        builder.Services.AddScoped<IUseCase<AdminGetAccountsRequest, AdminPagedResult<AdminAccountSummaryDto>>, AdminGetAccountsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAccountByIdRequest, AdminAccountDetailDto>, AdminGetAccountByIdHandler>();
        builder.Services.AddScoped<IUseCase<AdminSuspendUserRequest, AdminAccountDetailDto>, AdminSuspendUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminReactivateUserRequest, AdminAccountDetailDto>, AdminReactivateUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminRevokeUserSessionsRequest, AdminAccountDetailDto>, AdminRevokeUserSessionsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventsRequest, AdminPagedResult<AdminAuditEventDto>>, AdminGetAuditEventsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventByIdRequest, AdminAuditEventDetailDto>, AdminGetAuditEventByIdHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetRolesRequest, AdminPagedResult<AdminRoleOptionDto>>, AdminGetRolesHandler>();
        builder.Services.AddScoped<IUseCase<AdminAssignRoleRequest, AdminAccountDetailDto>, AdminAssignRoleHandler>();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AccountAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AuditReadAuthorizationHandler>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AdminAuthorizationMiddlewareResultHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, RoleAdminAuthorizationHandler>();

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
            options.AddPolicy("RoleAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RoleAdminRequirement());
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
        _app.MapOpenApi();

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
        body.CanManageDocumentOperations.Should().BeTrue();
        body.CanViewAdminDashboard.Should().BeTrue();
        body.CanManageRoles.Should().BeTrue();
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
        body.CanManageDocumentOperations.Should().BeFalse();
        body.CanViewAdminDashboard.Should().BeFalse();
        body.CanManageRoles.Should().BeFalse();
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
    public async Task RolesEndpoint_PolicyEnforcement_ShouldEnforceRoleAdminConjunction()
    {
        // 1. Anonymous -> 401
        var anonRes = await _client.GetAsync("/api/v1/admin/roles");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Learner -> 403
        var learnerReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/roles");
        learnerReq.Headers.Add("X-Test-User", _learnerId.ToString());
        var learnerRes = await _client.SendAsync(learnerReq);
        learnerRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. User with ManageRoles but lacking ManageAccounts -> 403 (conjunctive failure)
        var rolesOnlyUserId = Guid.NewGuid();
        var rolesOnlyRoleId = Guid.NewGuid();
        await using (var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options))
        {
            var role = Role.Create("roles-only", "Roles Only", "Chi roles", isProtected: false, id: rolesOnlyRoleId);
            db.Roles.Add(role);
            db.RolePermissions.Add(RolePermission.Create(rolesOnlyRoleId, PermissionKeys.ManageRoles));
            db.Users.Add(new User { Id = rolesOnlyUserId, Email = "rolesonly@example.com", Name = "Roles Only", RoleId = rolesOnlyRoleId, Status = AccountStatus.Active });
            await db.SaveChangesAsync();
        }

        var rolesOnlyReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/roles");
        rolesOnlyReq.Headers.Add("X-Test-User", rolesOnlyUserId.ToString());
        var rolesOnlyRes = await _client.SendAsync(rolesOnlyReq);
        rolesOnlyRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "ManageRoles alone cannot satisfy RoleAdmin without ManageAccounts");

        // 4. Admin (has both ManageAccounts and ManageRoles) -> 200 OK
        var adminReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/roles");
        adminReq.Headers.Add("X-Test-User", _adminId.ToString());
        var adminRes = await _client.SendAsync(adminReq);
        adminRes.StatusCode.Should().Be(HttpStatusCode.OK);
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
        // 1. Role manager lacking ReadAudit is forbidden
        var roleManagerOnlyId = Guid.NewGuid();
        var roleManagerRoleId = Guid.NewGuid();
        await using (var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options))
        {
            var role = Role.Create("role-mgr-no-audit", "Role Manager", "Quan ly vai tro", isProtected: false, id: roleManagerRoleId);
            db.Roles.Add(role);
            db.RolePermissions.AddRange(
                RolePermission.Create(roleManagerRoleId, PermissionKeys.ManageAccounts),
                RolePermission.Create(roleManagerRoleId, PermissionKeys.ManageRoles)
            );
            db.Users.Add(new User { Id = roleManagerOnlyId, Email = "rolemgr@example.com", Name = "Role Manager", RoleId = roleManagerRoleId, Status = AccountStatus.Active });
            await db.SaveChangesAsync();
        }

        var roleMgrReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit");
        roleMgrReq.Headers.Add("X-Test-User", roleManagerOnlyId.ToString());
        var roleMgrRes = await _client.SendAsync(roleMgrReq);
        roleMgrRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "ManageRoles permission must NOT grant ReadAudit authority");

        // 2. Audit read with Admin
        var auditReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit?pageSize=10");
        auditReq.Headers.Add("X-Test-User", _adminId.ToString());
        var auditRes = await _client.SendAsync(auditReq);
        auditRes.StatusCode.Should().Be(HttpStatusCode.OK);
        auditRes.Headers.CacheControl?.NoStore.Should().BeTrue();

        // 3. Filter by User.AssignRole action
        var filterReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/audit?action={AdminAuditActions.UserAssignRole}&pageSize=10");
        filterReq.Headers.Add("X-Test-User", _adminId.ToString());
        var filterRes = await _client.SendAsync(filterReq);
        filterRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await filterRes.Content.ReadFromJsonAsync<AdminPagedResult<AdminAuditEventDto>>();
        page.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRole_HttpIntegration_ContractsMatrix()
    {
        var targetId = Guid.NewGuid();
        await using (var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options))
        {
            db.Users.Add(new User
            {
                Id = targetId,
                Email = "candidate@example.com",
                Name = "Candidate",
                RoleId = RoleDefaults.LearnerRoleId,
                AdminControlVersion = 1,
                AuthenticationVersion = 1,
                Status = AccountStatus.Active
            });
            await db.SaveChangesAsync();
        }

        // 1. Anonymous -> 401
        var anonReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "Promotion", 1))
        };
        var anonRes = await _client.SendAsync(anonReq);
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Learner -> 403
        var learnerReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "Promotion", 1))
        };
        learnerReq.Headers.Add("X-Test-User", _learnerId.ToString());
        var learnerRes = await _client.SendAsync(learnerReq);
        learnerRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Bad request: empty reason -> 400
        var emptyReasonReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "", 1))
        };
        emptyReasonReq.Headers.Add("X-Test-User", _adminId.ToString());
        var emptyReasonRes = await _client.SendAsync(emptyReasonReq);
        emptyReasonRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 4. Missing target user -> 404
        var missingTargetReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{Guid.NewGuid()}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "Promotion", 1))
        };
        missingTargetReq.Headers.Add("X-Test-User", _adminId.ToString());
        var missingTargetRes = await _client.SendAsync(missingTargetReq);
        missingTargetRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 5. Missing role -> 404
        var missingRoleReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(Guid.NewGuid(), "Promotion", 1))
        };
        missingRoleReq.Headers.Add("X-Test-User", _adminId.ToString());
        var missingRoleRes = await _client.SendAsync(missingRoleReq);
        missingRoleRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 6. Self-role assignment -> 409 ACCOUNT_SELF_ROLE_CHANGE_BLOCKED
        var selfReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{_adminId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.LearnerRoleId, "Self demotion", 1))
        };
        selfReq.Headers.Add("X-Test-User", _adminId.ToString());
        var selfRes = await _client.SendAsync(selfReq);
        selfRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var selfBody = await selfRes.Content.ReadAsStringAsync();
        selfBody.Should().Contain("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED");

        // 7. Unchanged role -> 409 ACCOUNT_ROLE_UNCHANGED
        var sameRoleReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.LearnerRoleId, "Same role", 1))
        };
        sameRoleReq.Headers.Add("X-Test-User", _adminId.ToString());
        var sameRoleRes = await _client.SendAsync(sameRoleReq);
        sameRoleRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var sameRoleBody = await sameRoleRes.Content.ReadAsStringAsync();
        sameRoleBody.Should().Contain("ACCOUNT_ROLE_UNCHANGED");

        // 8. Concurrency version mismatch -> 409 ACCOUNT_CONCURRENCY_CONFLICT
        var staleReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "Promotion", 99))
        };
        staleReq.Headers.Add("X-Test-User", _adminId.ToString());
        var staleRes = await _client.SendAsync(staleReq);
        staleRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var staleBody = await staleRes.Content.ReadAsStringAsync();
        staleBody.Should().Contain("ACCOUNT_CONCURRENCY_CONFLICT");

        // 9. Valid role assignment -> 200 OK
        var validReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new AdminAssignRoleActionRequest(RoleDefaults.AdminRoleId, "Promotion to Admin", 1))
        };
        validReq.Headers.Add("X-Test-User", _adminId.ToString());
        var validRes = await _client.SendAsync(validReq);
        validRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await validRes.Content.ReadFromJsonAsync<AdminAccountDetailDto>();
        detail.Should().NotBeNull();
        detail!.Role.Id.Should().Be(RoleDefaults.AdminRoleId);
        detail.Role.Key.Should().Be("admin");
        detail.AdminControlVersion.Should().Be(2);
    }

    [Fact]
    public async Task OpenApi_ShouldExposeRoleEndpointsAndStatusCodes()
    {
        var json = await _client.GetStringAsync("/openapi/v1.json");
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        // 1. GET /api/v1/admin/roles
        paths.TryGetProperty("/api/v1/admin/roles", out var rolesPath).Should().BeTrue();
        var rolesGet = rolesPath.GetProperty("get");
        var rolesResponses = rolesGet.GetProperty("responses");
        rolesResponses.TryGetProperty("200", out _).Should().BeTrue();
        rolesResponses.TryGetProperty("400", out _).Should().BeTrue();
        rolesResponses.TryGetProperty("401", out _).Should().BeTrue();
        rolesResponses.TryGetProperty("403", out _).Should().BeTrue();
        rolesResponses.TryGetProperty("429", out _).Should().BeTrue();
        rolesResponses.TryGetProperty("503", out _).Should().BeTrue();

        // 2. PUT /api/v1/admin/users/{id}/role
        paths.TryGetProperty("/api/v1/admin/users/{id}/role", out var roleAssignPath).Should().BeTrue();
        var roleAssignPut = roleAssignPath.GetProperty("put");
        var assignResponses = roleAssignPut.GetProperty("responses");
        assignResponses.TryGetProperty("200", out _).Should().BeTrue();
        assignResponses.TryGetProperty("400", out _).Should().BeTrue();
        assignResponses.TryGetProperty("401", out _).Should().BeTrue();
        assignResponses.TryGetProperty("403", out _).Should().BeTrue();
        assignResponses.TryGetProperty("404", out _).Should().BeTrue();
        assignResponses.TryGetProperty("409", out _).Should().BeTrue();
        assignResponses.TryGetProperty("429", out _).Should().BeTrue();
        assignResponses.TryGetProperty("503", out _).Should().BeTrue();
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
