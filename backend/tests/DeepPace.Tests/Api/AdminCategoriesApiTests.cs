using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Admin.Audit;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Application.Features.Admin.Categories;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.Admin.Categories.Validators;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class AdminCategoriesApiTests : IAsyncLifetime
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

            // Seed Roles with ManageCategories for Admin
            db.Users.AddRange(
                new User { Id = _adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId },
                new User { Id = _learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId }
            );

            db.AuthorizationInitializations.Add(new AuthorizationInitialization
            {
                Scope = AuthorizationInitialization.DefaultScope,
                CompletedAt = DateTimeOffset.UtcNow,
                ImportedAdminUserIds = new List<Guid> { _adminId }
            });

            // EnsureCreated supplies the authoritative UUID category seeds.
            (await db.DocumentCategories.CountAsync()).Should().Be(7);

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
        builder.Services.AddScoped<ICategoryAssignmentGuard, CategoryAssignmentGuard>();

        // Validators
        builder.Services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryValidator>();
        builder.Services.AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryValidator>();
        builder.Services.AddScoped<IValidator<DeleteCategoryRequest>, DeleteCategoryValidator>();
        builder.Services.AddScoped<IValidator<GetAdminCategoriesRequest>, GetAdminCategoriesValidator>();

        // Category Handlers
        builder.Services.AddScoped<IUseCase<CreateCategoryRequest, CategoryDescriptorDto>, CreateCategoryHandler>();
        builder.Services.AddScoped<IUseCase<UpdateCategoryRequest, CategoryDescriptorDto>, UpdateCategoryHandler>();
        builder.Services.AddScoped<IUseCase<DeleteCategoryRequest, bool>, DeleteCategoryHandler>();
        builder.Services.AddScoped<IUseCase<GetAdminCategoriesRequest, AdminPagedResult<AdminCategoryDetailDto>>, GetAdminCategoriesHandler>();
        builder.Services.AddScoped<IUseCase<Guid, AdminCategoryDetailDto>, GetAdminCategoryByIdHandler>();
        builder.Services.AddScoped<IUseCase<GetCategoriesRequest, IReadOnlyList<CategoryDescriptorDto>>, GetCategoriesHandler>();
        // Admin Accounts & Audit Handlers
        builder.Services.AddScoped<IUseCase<AdminGetAccountsRequest, AdminPagedResult<AdminAccountSummaryDto>>, AdminGetAccountsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAccountByIdRequest, AdminAccountDetailDto>, AdminGetAccountByIdHandler>();
        builder.Services.AddScoped<IUseCase<AdminSuspendUserRequest, AdminAccountDetailDto>, AdminSuspendUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminReactivateUserRequest, AdminAccountDetailDto>, AdminReactivateUserHandler>();
        builder.Services.AddScoped<IUseCase<AdminRevokeUserSessionsRequest, AdminAccountDetailDto>, AdminRevokeUserSessionsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventsRequest, AdminPagedResult<AdminAuditEventDto>>, AdminGetAuditEventsHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetAuditEventByIdRequest, AdminAuditEventDetailDto>, AdminGetAuditEventByIdHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetRolesRequest, AdminPagedResult<AdminRoleOptionDto>>, AdminGetRolesHandler>();
        builder.Services.AddScoped<IUseCase<AdminAssignRoleRequest, AdminAccountDetailDto>, AdminAssignRoleHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AccountAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AuditReadAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, RoleAdminAuthorizationHandler>();

        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AdminAuthorizationMiddlewareResultHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, CategoryAdminAuthorizationHandler>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CategoryAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CategoryAdminRequirement());
            });
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

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("CategoryAdminReads", httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
            options.AddPolicy("CategoryAdminWrites", httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
            options.AddPolicy("AdminRolesReadPolicy", httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
            options.AddPolicy("AdminRolesWritePolicy", httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        _app = builder.Build();

        _app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1/admin"))
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.Pragma = "no-cache";
            }
            await next(context);
        });

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseRateLimiter();

        _app.MapAdminCategoriesEndpoints();
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

    private void AuthenticateAsAdmin()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", _adminId.ToString());
    }

    private void AuthenticateAsLearner()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", _learnerId.ToString());
    }

    private void ClearAuthentication()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
    }

    [Fact]
    public async Task GetCategories_Authenticated_ShouldReturn200WithDescriptors()
    {
        AuthenticateAsLearner();
        var response = await _client.GetAsync("/api/v1/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<CategoryDescriptorDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().BeGreaterOrEqualTo(7);
        list.Should().BeInAscendingOrder(c => c.SortOrder);
    }

    [Fact]
    public async Task GetCategories_Anonymous_ShouldReturn401()
    {
        ClearAuthentication();
        var response = await _client.GetAsync("/api/v1/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminCategories_WithCategoryAdmin_ShouldReturn200PagedResult()
    {
        AuthenticateAsAdmin();
        var response = await _client.GetAsync("/api/v1/admin/categories?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();

        var paged = await response.Content.ReadFromJsonAsync<AdminPagedResult<AdminCategoryDetailDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.TotalCount.Should().BeGreaterOrEqualTo(7);
        paged.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminCategories_AsLearner_ShouldReturn403()
    {
        AuthenticateAsLearner();
        var response = await _client.GetAsync("/api/v1/admin/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAdminCategories_Anonymous_ShouldReturn401()
    {
        ClearAuthentication();
        var response = await _client.GetAsync("/api/v1/admin/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_WithValidPayload_ShouldReturn201WithLocationAndDescriptor()
    {
        AuthenticateAsAdmin();
        var input = new CreateCategoryInput(
            Slug: "distributed-tracing",
            NameEn: "Distributed Tracing",
            NameVi: "Truy vet phan tan",
            SortOrder: 80,
            IsActive: true);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<CategoryDescriptorDto>();
        created.Should().NotBeNull();
        created!.Slug.Should().Be("distributed-tracing");
        created.NameEn.Should().Be("Distributed Tracing");
        created.Version.Should().Be(1);
        response.Headers.Location!.ToString().Should().Be($"/api/v1/admin/categories/{created.Id}");
    }

    [Fact]
    public async Task CreateCategory_Anonymous_ShouldReturn401()
    {
        ClearAuthentication();
        var input = new CreateCategoryInput("sec-anon", "Security Anon", "Bao mat", 10, true);
        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_AsLearner_ShouldReturn403()
    {
        AuthenticateAsLearner();
        var input = new CreateCategoryInput("sec-learner", "Security Learner", "Bao mat", 10, true);
        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_DuplicateSlug_ShouldReturn409SlugConflict()
    {
        AuthenticateAsAdmin();
        var input = new CreateCategoryInput("frontend-web", "Frontend Web Duplicate", "Frontend Web Duplicate", 10, true);
        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Category.SlugConflict");
    }

    [Fact]
    public async Task CreateCategory_InvalidSlug_ShouldReturn400()
    {
        AuthenticateAsAdmin();
        var input = new CreateCategoryInput("Invalid_Slug!", "Invalid Slug", "Khong hop le", 10, true);
        var response = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Category.ValidationFailed");
    }

    [Fact]
    public async Task GetAdminCategoryById_Existing_ShouldReturn200Detail()
    {
        AuthenticateAsAdmin();
        var response = await _client.GetAsync($"/api/v1/admin/categories/{CategoryDefaults.FrontendWebId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<AdminCategoryDetailDto>();
        detail.Should().NotBeNull();
        detail!.Category.Slug.Should().Be("frontend-web");
        detail.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminCategoryById_NotFound_ShouldReturn404()
    {
        AuthenticateAsAdmin();
        var randomId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/admin/categories/{randomId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Category.NotFound");
    }

    [Fact]
    public async Task UpdateCategory_Valid_ShouldReturn200()
    {
        AuthenticateAsAdmin();
        var input = new UpdateCategoryInput(
            NameEn: "Frontend & Web Architecture Updated",
            NameVi: "Frontend va Trinh duyet",
            SortOrder: 15,
            IsActive: true,
            ExpectedVersion: 1);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{CategoryDefaults.FrontendWebId}", input);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<CategoryDescriptorDto>();
        updated.Should().NotBeNull();
        updated!.NameEn.Should().Be("Frontend & Web Architecture Updated");
        updated.Version.Should().Be(2);
    }

    [Fact]
    public async Task UpdateCategory_IdenticalPayloadWithStaleVersion_Returns409WithoutAcceptedAudit()
    {
        AuthenticateAsAdmin();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var category = await db.DocumentCategories.AsNoTracking()
            .SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        var input = new UpdateCategoryInput(
            category.NameEn, category.NameVi, category.SortOrder, category.IsActive, 999);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{category.Id}", input);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Category.ConcurrencyConflict");
        (await db.AdminAuditEvents.CountAsync(e => e.Outcome == AdminAuditOutcome.Accepted)).Should().Be(0);
        (await db.AdminAuditEvents.CountAsync(e => e.Outcome == AdminAuditOutcome.Rejected)).Should().Be(1);
    }

    [Fact]
    public async Task UpdateCategory_ValidIdenticalPayload_ReturnsCurrentVersionWithoutAudit()
    {
        AuthenticateAsAdmin();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var category = await db.DocumentCategories.AsNoTracking()
            .SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        var input = new UpdateCategoryInput(
            category.NameEn, category.NameVi, category.SortOrder, category.IsActive, category.ConcurrencyVersion);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{category.Id}", input);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var descriptor = await response.Content.ReadFromJsonAsync<CategoryDescriptorDto>();
        descriptor!.Version.Should().Be(category.ConcurrencyVersion);
        (await db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    public async Task UpdateCategory_AuditWriteUnavailable_ReturnsSafe503WithoutChanges(int expectedVersion)
    {
        AuthenticateAsAdmin();
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """CREATE TRIGGER reject_category_audit BEFORE INSERT ON "AdminAuditEvents" BEGIN SELECT RAISE(ABORT, 'secret-provider-detail'); END;""");
        var input = new UpdateCategoryInput("Must roll back", "Phải hoàn tác", 99, false, expectedVersion);

        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/admin/categories/{CategoryDefaults.FrontendWebId}", input);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("secret-provider-detail");
        var category = await db.DocumentCategories.AsNoTracking()
            .SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        category.NameEn.Should().NotBe("Must roll back");
        category.ConcurrencyVersion.Should().Be(1);
        (await db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateCategory_ConcurrencyConflict_ShouldReturn409ConcurrencyConflict()
    {
        AuthenticateAsAdmin();
        var input = new UpdateCategoryInput(
            NameEn: "Frontend Conflicted",
            NameVi: "Frontend",
            SortOrder: 10,
            IsActive: true,
            ExpectedVersion: 999);

        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{CategoryDefaults.FrontendWebId}", input);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Category.ConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateCategory_NotFound_ShouldReturn404()
    {
        AuthenticateAsAdmin();
        var input = new UpdateCategoryInput("Ghost", "Ma", 1, true, 1);
        var response = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{Guid.NewGuid()}", input);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_Unused_ShouldReturn204()
    {
        AuthenticateAsAdmin();
        // First create a new unused category
        var input = new CreateCategoryInput("deletable-cat", "Deletable Cat", "Xoa duoc", 90, true);
        var createRes = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<CategoryDescriptorDto>();

        // Delete it with expectedVersion: 1
        var delRes = await _client.DeleteAsync($"/api/v1/admin/categories/{created!.Id}?expectedVersion=1");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Subsequent lookup returns 404
        var checkRes = await _client.GetAsync($"/api/v1/admin/categories/{created.Id}");
        checkRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_InUse_ShouldReturn409InUse()
    {
        AuthenticateAsAdmin();
        // Add a DocumentBook referencing DatabaseStorageId
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        db.DocumentBooks.Add(new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Database Internals Book",
            Slug = "db-internals-book",
            CategoryId = CategoryDefaults.DatabaseStorageId,
            SourceType = SourceType.PdfBook,
            TotalChunks = 1
        });
        await db.SaveChangesAsync();

        var delRes = await _client.DeleteAsync($"/api/v1/admin/categories/{CategoryDefaults.DatabaseStorageId}?expectedVersion=1");
        delRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var json = await delRes.Content.ReadAsStringAsync();
        json.Should().Contain("Category.InUse");
    }

    [Fact]
    public async Task DeleteCategory_ConcurrencyConflict_ShouldReturn409ConcurrencyConflict()
    {
        AuthenticateAsAdmin();
        var delRes = await _client.DeleteAsync($"/api/v1/admin/categories/{CategoryDefaults.SystemDesignId}?expectedVersion=999");
        delRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var json = await delRes.Content.ReadAsStringAsync();
        json.Should().Contain("Category.ConcurrencyConflict");
    }

    [Fact]
    public async Task DeleteCategory_NotFound_ShouldReturn404()
    {
        AuthenticateAsAdmin();
        var delRes = await _client.DeleteAsync($"/api/v1/admin/categories/{Guid.NewGuid()}?expectedVersion=1");
        delRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RateLimiting_CategoryAdminWrites_ExceedingLimit_ShouldReturn429()
    {
        AuthenticateAsAdmin();
        // Rate limiter permit limit for CategoryAdminWrites is configured to 3 in this test fixture.
        var post1 = await _client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryInput("rl-1", "RL 1", "RL 1", 1, true));
        var post2 = await _client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryInput("rl-2", "RL 2", "RL 2", 2, true));
        var post3 = await _client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryInput("rl-3", "RL 3", "RL 3", 3, true));
        var post4 = await _client.PostAsJsonAsync("/api/v1/admin/categories", new CreateCategoryInput("rl-4", "RL 4", "RL 4", 4, true));

        post4.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OpenApi_ShouldExposeCategoryEndpointsWithExpectedStatuses()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        // 1. /api/v1/categories
        paths.TryGetProperty("/api/v1/categories", out var catPath).Should().BeTrue();
        var catGet = catPath.GetProperty("get");
        catGet.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();

        // 2. /api/v1/admin/categories
        paths.TryGetProperty("/api/v1/admin/categories", out var adminCatPath).Should().BeTrue();
        var adminGet = adminCatPath.GetProperty("get");
        var adminGetResp = adminGet.GetProperty("responses");
        adminGetResp.TryGetProperty("200", out _).Should().BeTrue();
        adminGetResp.TryGetProperty("400", out _).Should().BeTrue();
        adminGetResp.TryGetProperty("401", out _).Should().BeTrue();
        adminGetResp.TryGetProperty("403", out _).Should().BeTrue();
        adminGetResp.TryGetProperty("429", out _).Should().BeTrue();
        adminGetResp.TryGetProperty("503", out _).Should().BeTrue();

        var adminPost = adminCatPath.GetProperty("post");
        var adminPostResp = adminPost.GetProperty("responses");
        adminPostResp.TryGetProperty("201", out _).Should().BeTrue();
        adminPostResp.TryGetProperty("409", out _).Should().BeTrue();

        // 3. /api/v1/admin/categories/{id}
        paths.TryGetProperty("/api/v1/admin/categories/{id}", out var idPath).Should().BeTrue();
        var idGet = idPath.GetProperty("get");
        idGet.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        idGet.GetProperty("responses").TryGetProperty("404", out _).Should().BeTrue();

        var idPatch = idPath.GetProperty("patch");
        idPatch.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        idPatch.GetProperty("responses").TryGetProperty("409", out _).Should().BeTrue();

        var idDelete = idPath.GetProperty("delete");
        idDelete.GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        idDelete.GetProperty("responses").TryGetProperty("409", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AdminCategories_ShouldRecordAuditEvent_OnCreateAndDeactivate()
    {
        AuthenticateAsAdmin();

        // 1. Creates a category
        var input = new CreateCategoryInput("audit-evt-cat", "Audit Evt Cat", "Kiem toan danh muc", 65, true);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/categories", input);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDescriptorDto>();
        created.Should().NotBeNull();
        var categoryId = created!.Id;

        // Checks that an AdminAuditEvent is persisted in the database with Action = "Category.Create", TargetType = "Category", TargetId = category.Id.ToString()
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var createAudit = await db.AdminAuditEvents.FirstOrDefaultAsync(e => e.TargetId == categoryId.ToString() && e.Action == AdminAuditActions.CategoryCreate);
            createAudit.Should().NotBeNull();
            createAudit!.TargetType.Should().Be("Category");
            createAudit.TargetId.Should().Be(categoryId.ToString());
            createAudit.Action.Should().Be("Category.Create");
        }

        // 2. Updates category metadata/deactivates
        var updateInput = new UpdateCategoryInput("Audit Evt Cat", "Kiem toan danh muc", 65, false, created.Version);
        var updateResponse = await _client.PatchAsJsonAsync($"/api/v1/admin/categories/{categoryId}", updateInput);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Checks that an AdminAuditEvent with Action = "Category.Deactivate" or "Category.UpdateMetadata" is persisted
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var updateAudit = await db.AdminAuditEvents.FirstOrDefaultAsync(e => e.TargetId == categoryId.ToString() && (e.Action == AdminAuditActions.CategoryDeactivate || e.Action == AdminAuditActions.CategoryUpdateMetadata));
            updateAudit.Should().NotBeNull();
            updateAudit!.TargetType.Should().Be("Category");
            updateAudit.TargetId.Should().Be(categoryId.ToString());
            (updateAudit.Action == AdminAuditActions.CategoryDeactivate || updateAudit.Action == AdminAuditActions.CategoryUpdateMetadata).Should().BeTrue();
        }

        // 3. Queries GET /api/v1/admin/audit?targetType=Category with an Admin bearer/user and asserts the events appear in the response
        var auditResponse = await _client.GetAsync("/api/v1/admin/audit?targetType=Category");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await auditResponse.Content.ReadFromJsonAsync<AdminPagedResult<AdminAuditEventDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeEmpty();

        var createEventInAudit = pagedResult.Items.FirstOrDefault(e => e.TargetId == categoryId.ToString() && e.Action == AdminAuditActions.CategoryCreate);
        createEventInAudit.Should().NotBeNull();
        createEventInAudit!.TargetType.Should().Be("Category");

        var deactivateEventInAudit = pagedResult.Items.FirstOrDefault(e => e.TargetId == categoryId.ToString() && (e.Action == AdminAuditActions.CategoryDeactivate || e.Action == AdminAuditActions.CategoryUpdateMetadata));
        deactivateEventInAudit.Should().NotBeNull();
        deactivateEventInAudit!.TargetType.Should().Be("Category");
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
