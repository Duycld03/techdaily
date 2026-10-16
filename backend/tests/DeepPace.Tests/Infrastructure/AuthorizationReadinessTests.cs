using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class AuthorizationReadinessTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly AuthorizationReadinessService _readinessService;

    public AuthorizationReadinessTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        _readinessService = new AuthorizationReadinessService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private void SeedRolesAndPermissions()
    {
        if (!_db.Roles.Any(r => r.Id == RoleDefaults.LearnerRoleId))
        {
            var learnerRole = Role.Create("learner", "Learner", "Nguoi hoc", isProtected: true, id: RoleDefaults.LearnerRoleId);
            _db.Roles.Add(learnerRole);
        }

        if (!_db.Roles.Any(r => r.Id == RoleDefaults.AdminRoleId))
        {
            var adminRole = Role.Create("admin", "Administrator", "Quan tri vien", isProtected: true, id: RoleDefaults.AdminRoleId);
            _db.Roles.Add(adminRole);

            _db.RolePermissions.AddRange(
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ViewAdminDashboard),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageCatalog),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageAccounts),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ReadAudit),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageDocumentOperations),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageRoles),
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageCategories)
            );
        }

        _db.SaveChanges();
    }

    [Fact]
    public async Task Readiness_WhenRolesMissingOrUnprotected_FailsReadiness()
    {
        // Clear roles to simulate unseeded/missing schema
        _db.RolePermissions.RemoveRange(_db.RolePermissions);
        _db.Roles.RemoveRange(_db.Roles);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.HasRequiredSchemaAndRoles.Should().BeFalse();
        status.FailureReason.Should().Contain("missing or unprotected");
    }

    [Fact]
    public async Task Readiness_WhenAdminMissingPermissions_FailsReadiness()
    {
        // Remove 3 permissions so fewer remain
        var perms = await _db.RolePermissions.Where(p => p.RoleId == RoleDefaults.AdminRoleId).Take(3).ToListAsync();
        _db.RolePermissions.RemoveRange(perms);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role protected permissions do not match");
    }

    [Fact]
    public async Task Readiness_WhenAdminHasWrongCaseOrInvalidKey_FailsReadiness()
    {
        var catalogPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageCatalog);
        _db.RolePermissions.Remove(catalogPerm);
        // Add lowercase wrong-case key directly simulating out-of-band DB insertion
        _db.RolePermissions.Add(new RolePermission { RoleId = RoleDefaults.AdminRoleId, PermissionKey = "managecatalog" });
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role protected permissions do not match");
    }

    [Fact]
    public async Task Readiness_WhenAdminHasExtraUnsupportedGrants_FailsReadiness()
    {
        // Add an 8th unsupported grant directly simulating out-of-band DB insertion
        _db.RolePermissions.Add(new RolePermission { RoleId = RoleDefaults.AdminRoleId, PermissionKey = "SuperAdminPermission" });
        await _db.SaveChangesAsync();
        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role protected permissions do not match");
    }

    [Fact]
    public async Task Readiness_WhenAdminMissingManageCategories_FailsReadiness()
    {
        var catPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageCategories);
        _db.RolePermissions.Remove(catPerm);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role protected permissions do not match");
    }

    [Fact]
    public async Task Readiness_WhenAdminMissingManageRolesMaskedBySixOtherRows_FailsReadiness()
    {
        var rolesPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageRoles);
        _db.RolePermissions.Remove(rolesPerm);
        await _db.SaveChangesAsync();

        var count = await _db.RolePermissions.CountAsync(p => p.RoleId == RoleDefaults.AdminRoleId);
        count.Should().Be(6);

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role protected permissions do not match");
    }

    [Fact]
    public async Task Readiness_WhenLearnerHoldsPermissions_FailsReadiness()
    {
        _db.RolePermissions.Add(RolePermission.Create(RoleDefaults.LearnerRoleId, PermissionKeys.ReadAudit));
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Learner role must not hold any permissions");
    }
    [Fact]
    public async Task Readiness_WhenValidFreshSchemaAwaitingInitialization_IsReadyButNotInitialized()
    {
        SeedRolesAndPermissions();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeTrue();
        status.HasRequiredSchemaAndRoles.Should().BeTrue();
        status.IsInitialized.Should().BeFalse("Fresh schema has not executed operator initialization");
    }

    [Fact]
    public async Task Readiness_WhenUnusedCategoryDeleted_RemainsReady()
    {
        var category = await _db.DocumentCategories.FirstAsync();
        category.Delete(category.ConcurrencyVersion);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();

        status.IsReady.Should().BeTrue();
        status.HasRequiredSchemaAndRoles.Should().BeTrue();
    }

    [Fact]
    public async Task Readiness_WhenCategoryTableIsEmpty_RemainsReady()
    {
        await _db.DocumentCategories.IgnoreQueryFilters().ExecuteDeleteAsync();

        var status = await _readinessService.GetReadinessAsync();

        status.IsReady.Should().BeTrue();
        status.HasRequiredSchemaAndRoles.Should().BeTrue();
    }

    [Fact]
    public async Task Readiness_WhenCategorySchemaIsMissing_FailsReadiness()
    {
        await _db.Database.ExecuteSqlRawAsync("DROP TABLE \"DocumentCategories\"");

        var status = await _readinessService.GetReadinessAsync();

        status.IsReady.Should().BeFalse();
        status.HasRequiredSchemaAndRoles.Should().BeFalse();
    }

    [Fact]
    public async Task Readiness_WhenReceiptSchemaIsMissing_FailsReadiness()
    {
        SeedRolesAndPermissions();
        await _db.Database.ExecuteSqlRawAsync("DROP TABLE \"AuthorizationInitializations\"");

        var status = await _readinessService.GetReadinessAsync();

        status.IsReady.Should().BeFalse();
        status.HasRequiredSchemaAndRoles.Should().BeFalse();
        status.IsInitialized.Should().BeFalse();
        status.FailureReason.Should().Contain("Required authorization initialization receipt schema is missing or invalid");
    }

    [Fact]
    public async Task Readiness_WhenReceiptExists_IsReadyAndInitialized()
    {
        SeedRolesAndPermissions();
        _db.AuthorizationInitializations.Add(new AuthorizationInitialization
        {
            Scope = AuthorizationInitialization.DefaultScope,
            CompletedAt = DateTimeOffset.UtcNow,
            ImportedAdminUserIds = new List<Guid> { Guid.NewGuid() }
        });
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();

        status.IsReady.Should().BeTrue();
        status.HasRequiredSchemaAndRoles.Should().BeTrue();
        status.IsInitialized.Should().BeTrue();
        status.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Capabilities_WhenAwaitingInitialization_ReadinessReportsUninitialized()
    {
        SeedRolesAndPermissions();

        var learnerId = Guid.NewGuid();
        var learner = new User
        {
            Id = learnerId,
            Email = "learner@example.com",
            Name = "Learner",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId
        };
        await _db.Users.AddAsync(learner);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeTrue();
        status.HasRequiredSchemaAndRoles.Should().BeTrue();
        status.IsInitialized.Should().BeFalse("Privileged authorization must remain uninitialized until operator command runs");
    }

    [Fact]
    public async Task Capabilities_WhenInitialized_ResolvesNormally()
    {
        SeedRolesAndPermissions();

        // Persist receipt
        _db.AuthorizationInitializations.Add(new AuthorizationInitialization
        {
            Scope = AuthorizationInitialization.DefaultScope,
            CompletedAt = DateTimeOffset.UtcNow,
            ImportedAdminUserIds = new List<Guid>()
        });

        var adminId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId
        };

        var learner = new User
        {
            Id = learnerId,
            Email = "learner@example.com",
            Name = "Learner",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId
        };

        await _db.Users.AddRangeAsync(admin, learner);
        await _db.SaveChangesAsync();

        var permResolver = new AdminPermissionResolver(_db);
        var catalogAdminService = new CatalogAdminService(permResolver);

        var adminAuth = await catalogAdminService.CheckCatalogAdminAsync(adminId);
        adminAuth.IsAuthorized.Should().BeTrue();

        var learnerAuth = await catalogAdminService.CheckCatalogAdminAsync(learnerId);
        learnerAuth.IsAuthorized.Should().BeFalse();
        learnerAuth.IsDenied.Should().BeTrue();
    }
}
