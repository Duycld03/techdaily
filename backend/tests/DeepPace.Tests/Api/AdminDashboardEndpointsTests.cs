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
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Features.AdminDashboard.GetOverview;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class AdminDashboardEndpointsTests : IAsyncLifetime
{
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _otherAdmin = Guid.NewGuid();
    private readonly Guid _learner = Guid.NewGuid();
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly OverviewHandler _overview = new();
    private readonly StatusService _status = new();
    private readonly ProbeService _probe = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        await using (var db = new DeepPaceDbContext(dbOptions))
        {
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(new User { Id = _admin, Email = "admin@example.test", RoleId = RoleDefaults.AdminRoleId },
                new User { Id = _otherAdmin, Email = "other-admin@example.test", RoleId = RoleDefaults.AdminRoleId },
                new User { Id = _learner, Email = "learner@example.test", RoleId = RoleDefaults.LearnerRoleId });
            await db.SaveChangesAsync();
        }
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AdminAuthorizationMiddlewareResultHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ViewAdminDashboardAuthorizationHandler>();
        builder.Services.AddSingleton<IUseCase<GetAdminDashboardOverviewRequest, AdminDashboardOverviewDto>>(_overview);
        builder.Services.AddSingleton<IAdminSystemStatusService>(_status);
        builder.Services.AddSingleton<IAiHealthProbe>(_probe);
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, DashboardTestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy("ViewAdminDashboard", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ViewAdminDashboardRequirement());
        }));
        builder.Services.AddAdminDashboardRateLimiting();
        _app = builder.Build();
        _app.UseAdminDashboardNoStore();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseRateLimiter();
        _app.MapOpenApi();
        _app.MapAdminDashboardEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [Theory]
    [InlineData("GET", "overview")]
    [InlineData("GET", "status")]
    [InlineData("POST", "ai-probe")]
    public async Task ProtectedEndpoints_RejectAnonymousAndLearnersBeforeExecutingServices(string method, string endpoint)
    {
        var anonymous = await SendAsync(method, endpoint, null);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        anonymous.Headers.CacheControl!.NoStore.Should().BeTrue();
        var learner = await SendAsync(method, endpoint, _learner);
        learner.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _overview.Calls.Should().Be(0);
        _status.Calls.Should().Be(0);
        _probe.Calls.Should().Be(0);
    }

    [Fact]
    public async Task OverviewAndStatus_DoNotStartAiProbesAndReturnDeclaredFieldsWithoutPrivateData()
    {
        var overview = await SendAsync("GET", "overview", _admin);
        overview.StatusCode.Should().Be(HttpStatusCode.OK);
        overview.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await overview.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("accountCount", "personalDocuments", "catalogDocuments", "observedAt");
        json.Should().NotContain("email").And.NotContain("token").And.NotContain("originalText");
        var status = await SendAsync("GET", "status", _admin);
        status.StatusCode.Should().Be(HttpStatusCode.OK);
        _overview.Calls.Should().Be(1);
        _status.Calls.Should().Be(1);
        _probe.Calls.Should().Be(0);
    }

    [Fact]
    public async Task AiProbe_RateLimitPreventsDuplicatesAndIsPartitionedByAdministrator()
    {
        (await SendAsync("POST", "ai-probe", _admin)).StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicate = await SendAsync("POST", "ai-probe", _admin);
        duplicate.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await duplicate.Content.ReadAsStringAsync()).Should().Contain("RATE_LIMITED");
        _probe.Calls.Should().Be(1);
        (await SendAsync("POST", "ai-probe", _otherAdmin)).StatusCode.Should().Be(HttpStatusCode.OK);
        _probe.Calls.Should().Be(2);
    }

    [Fact]
    public async Task AiFailure_Returns503WithSanitizedPartialObservations()
    {
        _probe.Fail = true;
        var response = await SendAsync("POST", "ai-probe", _admin);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<AdminAiProbeDto>();
        body!.TextGeneration.ErrorCode.Should().Be("AI_TIMEOUT");
        body.Embeddings.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task OpenApi_ListsProtectedContractsAndBoundedProbeBehavior()
    {
        var json = await _client.GetStringAsync("/openapi/v1.json");
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        paths.GetProperty("/api/v1/admin/dashboard/overview").GetProperty("get")
            .GetProperty("responses").GetProperty("403").ValueKind.Should().Be(JsonValueKind.Object);
        paths.GetProperty("/api/v1/admin/dashboard/ai-probe").GetProperty("post")
            .GetProperty("responses").GetProperty("429").ValueKind.Should().Be(JsonValueKind.Object);
        json.Should().Contain("15-second").And.NotContain("ApiKey").And.NotContain("PasswordHash");
    }

    [Fact]
    public async Task CatalogOnlyRole_CannotProbeOrAccessOverview_ShouldReturn403WithoutCallingServices()
    {
        var catalogOnlyUser = Guid.NewGuid();
        var catalogRoleId = Guid.NewGuid();
        await using (var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options))
        {
            var role = Role.Create("cat-only", "Catalog Only", "Chi catalog", isProtected: false, id: catalogRoleId);
            db.Roles.Add(role);
            db.RolePermissions.Add(RolePermission.Create(catalogRoleId, PermissionKeys.ManageCatalog));
            db.Users.Add(new User { Id = catalogOnlyUser, Email = "cat@example.test", RoleId = catalogRoleId, Status = AccountStatus.Active });
            await db.SaveChangesAsync();
        }

        var initialProbeCalls = _probe.Calls;
        var initialOverviewCalls = _overview.Calls;

        var overviewRes = await SendAsync("GET", "overview", catalogOnlyUser);
        overviewRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var probeRes = await SendAsync("POST", "ai-probe", catalogOnlyUser);
        probeRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _probe.Calls.Should().Be(initialProbeCalls, "No external AI probe may be dispatched for catalog-only role");
        _overview.Calls.Should().Be(initialOverviewCalls);
    }

    [Fact]
    public async Task DashboardOnlyRole_IsAdmittedIndependently()
    {
        var dashboardOnlyUser = Guid.NewGuid();
        var dashboardRoleId = Guid.NewGuid();
        await using (var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options))
        {
            var role = Role.Create("dash-only", "Dashboard Only", "Chi dashboard", isProtected: false, id: dashboardRoleId);
            db.Roles.Add(role);
            db.RolePermissions.Add(RolePermission.Create(dashboardRoleId, PermissionKeys.ViewAdminDashboard));
            db.Users.Add(new User { Id = dashboardOnlyUser, Email = "dash@example.test", RoleId = dashboardRoleId, Status = AccountStatus.Active });
            await db.SaveChangesAsync();
        }

        var overviewRes = await SendAsync("GET", "overview", dashboardOnlyUser);
        overviewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusRes = await SendAsync("GET", "status", dashboardOnlyUser);
        statusRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> SendAsync(string method, string endpoint, Guid? user)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/admin/dashboard/" + endpoint);
        if (user.HasValue) request.Headers.Add("X-Test-User", user.Value.ToString());
        return _client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class OverviewHandler : IUseCase<GetAdminDashboardOverviewRequest, AdminDashboardOverviewDto>
    {
        public int Calls { get; private set; }
        public Task<Result<AdminDashboardOverviewDto>> ExecuteAsync(GetAdminDashboardOverviewRequest request, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult<Result<AdminDashboardOverviewDto>>(new AdminDashboardOverviewDto(3,
                new DocumentProcessingCountsDto(0, 0, 2, 0),
                new AdminCatalogCountsDto(1, 0, 1, 0, new DocumentProcessingCountsDto(0, 0, 1, 0)), DateTimeOffset.UtcNow));
        }
    }
    private sealed class StatusService : IAdminSystemStatusService
    {
        public int Calls { get; private set; }
        public Task<AdminDashboardStatusDto> ObserveAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new AdminDashboardStatusDto(new ServiceObservationDto("healthy", DateTimeOffset.UtcNow, 1), DateTimeOffset.UtcNow));
        }
    }
    private sealed class ProbeService : IAiHealthProbe
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public Task<AdminAiProbeDto> ProbeAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(new AdminAiProbeDto(
                new AiServiceObservationDto("gemini-3.6-flash", Fail ? "unavailable" : "healthy", 2, DateTimeOffset.UtcNow, Fail ? "AI_TIMEOUT" : null),
                new AiServiceObservationDto("gemini-embedding-001", "healthy", 1, DateTimeOffset.UtcNow, Dimension: 768), DateTimeOffset.UtcNow));
        }
    }

    private sealed class DashboardTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-User"], out var user)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
