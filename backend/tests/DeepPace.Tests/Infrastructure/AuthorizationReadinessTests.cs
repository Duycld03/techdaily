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
                RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageRoles)
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
        // Remove 3 permissions so fewer than 6 remain
        var perms = await _db.RolePermissions.Where(p => p.RoleId == RoleDefaults.AdminRoleId).Take(3).ToListAsync();
        _db.RolePermissions.RemoveRange(perms);
        await _db.SaveChangesAsync();

        var status = await _readinessService.GetReadinessAsync();
        status.IsReady.Should().BeFalse();
        status.FailureReason.Should().Contain("Admin role requires at least 6 protected permissions");
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
