using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class AdminPermissionResolverTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public AdminPermissionResolverTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }


    [Fact]
    public async Task GetCapabilities_EmptyGuid_ShouldReturnAllFalse()
    {
        var resolver = new AdminPermissionResolver(_db);

        var result = await resolver.GetCapabilitiesAsync(Guid.Empty);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
        result.Value.CanReadAudit.Should().BeFalse();
        result.Value.CanManageDocumentOperations.Should().BeFalse();
        result.Value.CanViewAdminDashboard.Should().BeFalse();
        result.Value.CanManageRoles.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_NonExistentUser_ShouldReturnAllFalse()
    {
        var resolver = new AdminPermissionResolver(_db);

        var result = await resolver.GetCapabilitiesAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
        result.Value.CanReadAudit.Should().BeFalse();
        result.Value.CanManageDocumentOperations.Should().BeFalse();
        result.Value.CanViewAdminDashboard.Should().BeFalse();
        result.Value.CanManageRoles.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_LearnerUser_ShouldReturnAllFalse()
    {
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

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(learnerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
        result.Value.CanReadAudit.Should().BeFalse();
        result.Value.CanManageDocumentOperations.Should().BeFalse();
        result.Value.CanViewAdminDashboard.Should().BeFalse();
        result.Value.CanManageRoles.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_ActiveAdminUser_ShouldReturnAllTrue()
    {
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId
        };
        await _db.Users.AddAsync(admin);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(adminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeTrue();
        result.Value.CanManageAccounts.Should().BeTrue();
        result.Value.CanReadAudit.Should().BeTrue();
        result.Value.CanManageDocumentOperations.Should().BeTrue();
        result.Value.CanViewAdminDashboard.Should().BeTrue();
        result.Value.CanManageRoles.Should().BeTrue();
    }

    [Fact]
    public async Task GetCapabilities_SuspendedAdmin_ShouldReturnAllFalse()
    {
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Suspended Admin",
            Status = AccountStatus.Suspended,
            RoleId = RoleDefaults.AdminRoleId
        };
        await _db.Users.AddAsync(admin);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(adminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
        result.Value.CanReadAudit.Should().BeFalse();
        result.Value.CanManageDocumentOperations.Should().BeFalse();
        result.Value.CanViewAdminDashboard.Should().BeFalse();
        result.Value.CanManageRoles.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_SoftDeletedAdmin_ShouldReturnAllFalse()
    {
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Deleted Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            IsDeleted = true
        };
        await _db.Users.AddAsync(admin);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(adminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_PartialRole_CatalogOnly_ShouldReturnOnlyCatalogManagement()
    {
        var catalogRoleId = Guid.NewGuid();
        var catalogRole = Role.Create("catalog-manager", "Catalog Manager", "Quản lý mục lục", false, catalogRoleId);
        await _db.Roles.AddAsync(catalogRole);
        await _db.RolePermissions.AddAsync(RolePermission.Create(catalogRoleId, PermissionKeys.ManageCatalog));

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "catalog@example.com",
            Name = "Catalog Manager",
            Status = AccountStatus.Active,
            RoleId = catalogRoleId
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageCatalog.Should().BeTrue();
        result.Value.CanManageAccounts.Should().BeFalse();
        result.Value.CanReadAudit.Should().BeFalse();
        result.Value.CanManageDocumentOperations.Should().BeFalse();
        result.Value.CanViewAdminDashboard.Should().BeFalse();
        result.Value.CanManageRoles.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_ManageRolesWithoutManageAccounts_ShouldNotGrantEffectiveCanManageRoles()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("roles-only", "Roles Only", "Chỉ vai trò", false, roleId);
        await _db.Roles.AddAsync(role);
        await _db.RolePermissions.AddAsync(RolePermission.Create(roleId, PermissionKeys.ManageRoles));

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "rolesonly@example.com",
            Name = "Roles Only User",
            Status = AccountStatus.Active,
            RoleId = roleId
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        // Conjunction rule: canManageRoles requires BOTH ManageRoles AND ManageAccounts
        result.Value.CanManageRoles.Should().BeFalse();
        result.Value.CanManageAccounts.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_ManageRolesWithManageAccounts_ShouldGrantEffectiveCanManageRoles()
    {
        var roleId = Guid.NewGuid();
        var role = Role.Create("account-roles-admin", "Account and Roles Admin", "Quản trị tài khoản và vai trò", false, roleId);
        await _db.Roles.AddAsync(role);
        await _db.RolePermissions.AddRangeAsync(
            RolePermission.Create(roleId, PermissionKeys.ManageRoles),
            RolePermission.Create(roleId, PermissionKeys.ManageAccounts));

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "superadmin@example.com",
            Name = "Super Admin",
            Status = AccountStatus.Active,
            RoleId = roleId
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);
        var result = await resolver.GetCapabilitiesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanManageRoles.Should().BeTrue();
        result.Value.CanManageAccounts.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermission_ReturnsTypedAuthorityResult()
    {
        var adminId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();

        await _db.Users.AddRangeAsync(
            new User { Id = adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId },
            new User { Id = learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId });
        await _db.SaveChangesAsync();

        var resolver = new AdminPermissionResolver(_db);

        var adminResult = await resolver.HasPermissionAsync(adminId, AdminPermission.ManageAccounts);
        adminResult.IsAuthorized.Should().BeTrue();
        adminResult.IsDenied.Should().BeFalse();
        adminResult.IsUnavailable.Should().BeFalse();

        var learnerResult = await resolver.HasPermissionAsync(learnerId, AdminPermission.ManageAccounts);
        learnerResult.IsAuthorized.Should().BeFalse();
        learnerResult.IsDenied.Should().BeTrue();
        learnerResult.IsUnavailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_DatabaseFailure_ShouldReturnUnavailableResult()
    {
        var resolver = new AdminPermissionResolver(_db);

        // Force database connection closure to simulate store failure
        _connection.Close();

        var result = await resolver.GetCapabilitiesAsync(Guid.NewGuid());
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authority.Unavailable");

        var authResult = await resolver.HasPermissionAsync(Guid.NewGuid(), AdminPermission.ManageCatalog);
        authResult.IsUnavailable.Should().BeTrue();
        authResult.IsAuthorized.Should().BeFalse();
    }
}
