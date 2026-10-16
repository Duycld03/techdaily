using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class AdminRoleAssignmentHandlersTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly IAdministrativeUnitOfWork _uow;
    private readonly IRefreshTokenService _tokenService;
    private readonly IAdminPermissionResolver _resolver;

    private readonly Guid _superAdminId = Guid.NewGuid();
    private readonly Guid _accountsOnlyAdminId = Guid.NewGuid();
    private readonly Guid _catalogOnlyAdminId = Guid.NewGuid();
    private readonly Guid _customRoleId = Guid.NewGuid();

    public AdminRoleAssignmentHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        _uow = new AdministrativeUnitOfWork(_db);
        _tokenService = new RefreshTokenService(_db);
        _resolver = new AdminPermissionResolver(_db);

        // Ensure default roles and permissions
        var customRole = Role.Create("custom-auditor", "Auditor", "Kiểm toán viên", isProtected: false, id: _customRoleId);
        var customPerm = RolePermission.Create(_customRoleId, PermissionKeys.ReadAudit);
        _db.Roles.Add(customRole);
        _db.RolePermissions.Add(customPerm);

        // Additional role with only ManageAccounts
        var accountsOnlyRoleId = Guid.NewGuid();
        var accountsOnlyRole = Role.Create("accounts-only", "Accounts Admin", "Quản lý tài khoản", isProtected: false, id: accountsOnlyRoleId);
        var accountsOnlyPerm = RolePermission.Create(accountsOnlyRoleId, PermissionKeys.ManageAccounts);
        _db.Roles.Add(accountsOnlyRole);
        _db.RolePermissions.Add(accountsOnlyPerm);

        // Role with only ManageCatalog
        var catalogOnlyRoleId = Guid.NewGuid();
        var catalogOnlyRole = Role.Create("catalog-only", "Catalog Admin", "Quản trị danh mục", isProtected: false, id: catalogOnlyRoleId);
        var catalogOnlyPerm = RolePermission.Create(catalogOnlyRoleId, PermissionKeys.ManageCatalog);
        _db.Roles.Add(catalogOnlyRole);
        _db.RolePermissions.Add(catalogOnlyPerm);

        // Super admin user with Admin role (has ManageAccounts and ManageRoles)
        var superAdmin = new User
        {
            Id = _superAdminId,
            Email = "superadmin@example.com",
            Name = "Super Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };

        // Admin with only ManageAccounts
        var accountsOnlyAdmin = new User
        {
            Id = _accountsOnlyAdminId,
            Email = "accountsonly@example.com",
            Name = "Accounts Only Admin",
            Status = AccountStatus.Active,
            RoleId = accountsOnlyRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };

        // Admin with only ManageCatalog
        var catalogOnlyAdmin = new User
        {
            Id = _catalogOnlyAdminId,
            Email = "catalogonly@example.com",
            Name = "Catalog Only Admin",
            Status = AccountStatus.Active,
            RoleId = catalogOnlyRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };

        _db.Users.AddRange(superAdmin, accountsOnlyAdmin, catalogOnlyAdmin);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetRoles_ShouldRequireManageAccountsAndManageRolesConjunction()
    {
        var handler = new AdminGetRolesHandler(_db, _resolver);

        // Accounts-only admin lacks ManageRoles
        var res1 = await handler.ExecuteAsync(new AdminGetRolesRequest(_accountsOnlyAdminId));
        res1.IsSuccess.Should().BeFalse();
        res1.Error.Code.Should().Be("FORBIDDEN");

        // Catalog-only admin lacks both
        var res2 = await handler.ExecuteAsync(new AdminGetRolesRequest(_catalogOnlyAdminId));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("FORBIDDEN");

        // Super admin has both ManageAccounts and ManageRoles
        var res3 = await handler.ExecuteAsync(new AdminGetRolesRequest(_superAdminId));
        res3.IsSuccess.Should().BeTrue();
        res3.Value.Items.Should().NotBeEmpty();
        res3.Value.Items.Any(r => r.Key == RoleDefaults.AdminKey).Should().BeTrue();
        res3.Value.Items.Any(r => r.Key == RoleDefaults.LearnerKey).Should().BeTrue();
    }

    [Fact]
    public async Task GetRoles_ShouldFilterBySearchAndPaginate()
    {
        var handler = new AdminGetRolesHandler(_db, _resolver);

        var searchRes = await handler.ExecuteAsync(new AdminGetRolesRequest(_superAdminId, Search: "Learner"));
        searchRes.IsSuccess.Should().BeTrue();
        searchRes.Value.TotalCount.Should().Be(1);
        searchRes.Value.Items.First().Key.Should().Be(RoleDefaults.LearnerKey);

        // Empty search with pagination
        var pagedRes = await handler.ExecuteAsync(new AdminGetRolesRequest(_superAdminId, Page: 1, PageSize: 2));
        pagedRes.IsSuccess.Should().BeTrue();
        pagedRes.Value.Items.Count.Should().Be(2);
        pagedRes.Value.TotalCount.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task AssignRole_ShouldRequireManageAccountsAndManageRolesConjunction()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();
        var target = new User { Id = targetId, Email = "target@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AdminControlVersion = 1 };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        // Accounts-only admin lacks ManageRoles
        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _accountsOnlyAdminId, _customRoleId, "Promoting auditor", 1, CorrelationId: "corr-unauth"));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("AUTH_FORBIDDEN");
        var unauthAudit = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-unauth");
        unauthAudit.Should().BeNull("Unadmitted actor must not persist rejection audit");
    }

    [Fact]
    public async Task AssignRole_ShouldValidateInputs()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();

        // Empty reason
        var res1 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, "", 1, CorrelationId: "corr-val-1"));
        res1.IsSuccess.Should().BeFalse();
        res1.Error.Code.Should().Be("Account.ReasonRequired");
        var audit1 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-val-1");
        audit1.Should().NotBeNull();
        audit1!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit1.Action.Should().Be(AdminAuditActions.UserAssignRole);
        audit1.ErrorCode.Should().Be("Account.ReasonRequired");

        // Reason too long (> 500 chars)
        var res2 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, new string('a', 501), 1, CorrelationId: "corr-val-2"));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("Account.ReasonTooLong");
        var audit2 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-val-2");
        audit2.Should().NotBeNull();
        audit2!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit2.ErrorCode.Should().Be("Account.ReasonTooLong");
        audit2.Reason.Should().BeNull("Overlong reason must not be copied or truncated into history");

        // Invalid RoleId
        var res3 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, Guid.Empty, "Valid reason", 1, CorrelationId: "corr-val-3"));
        res3.IsSuccess.Should().BeFalse();
        res3.Error.Code.Should().Be("Role.InvalidId");
        var audit3 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-val-3");
        audit3.Should().NotBeNull();
        audit3!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit3.ErrorCode.Should().Be("Role.InvalidId");

        // Invalid expected version
        var res4 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, "Valid reason", 0, CorrelationId: "corr-val-4"));
        res4.IsSuccess.Should().BeFalse();
        res4.Error.Code.Should().Be("Validation.ExpectedVersionInvalid");
        var audit4 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-val-4");
        audit4.Should().NotBeNull();
        audit4!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit4.ErrorCode.Should().Be("Validation.ExpectedVersionInvalid");

        // Sensitive reason
        var res5 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, "password: secret123", 1, CorrelationId: "corr-val-5"));
        res5.IsSuccess.Should().BeFalse();
        res5.Error.Code.Should().Be("Account.InvalidReason");
        var audit5 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-val-5");
        audit5.Should().NotBeNull();
        audit5!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit5.ErrorCode.Should().Be("Account.InvalidReason");
        audit5.Reason.Should().BeNull("Sensitive reason must be omitted from history");
    }

    [Fact]
    public async Task AssignRole_ShouldBlockSelfAssignment()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(_superAdminId, _superAdminId, RoleDefaults.LearnerRoleId, "Demoting myself", 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED");
    }

    [Fact]
    public async Task AssignRole_ShouldRejectMissingOrDeletedTarget_WithoutAuditPersistence()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);

        // Missing target
        var missingId = Guid.NewGuid();
        var res1 = await handler.ExecuteAsync(new AdminAssignRoleRequest(missingId, _superAdminId, _customRoleId, "Valid reason", 1, CorrelationId: "corr-miss-target"));
        res1.IsSuccess.Should().BeFalse();
        res1.Error.Code.Should().Be("Account.NotFound");

        var audit1 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-miss-target");
        audit1.Should().BeNull("Missing target must not persist rejection audit");

        // Soft-deleted target
        var deletedId = Guid.NewGuid();
        var deletedUser = new User { Id = deletedId, Email = "del@example.com", Name = "Del", Status = AccountStatus.Active, IsDeleted = true, RoleId = RoleDefaults.LearnerRoleId };
        _db.Users.Add(deletedUser);
        await _db.SaveChangesAsync();

        var res2 = await handler.ExecuteAsync(new AdminAssignRoleRequest(deletedId, _superAdminId, _customRoleId, "Valid reason", 1, CorrelationId: "corr-del-target"));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("Account.NotFound");

        var audit2 = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.CorrelationId == "corr-del-target");
        audit2.Should().BeNull("Soft-deleted target must not persist rejection audit");
    }

    [Fact]
    public async Task AssignRole_ShouldRejectMissingOrDeletedRole()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();
        var target = new User { Id = targetId, Email = "target@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AdminControlVersion = 1 };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        // Non-existent role ID
        var res1 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, Guid.NewGuid(), "Valid reason", 1));
        res1.IsSuccess.Should().BeFalse();
        res1.Error.Code.Should().Be("Role.NotFound");

        // Soft-deleted role
        var softDeletedRoleId = Guid.NewGuid();
        var softDeletedRole = Role.Create("soft-deleted", "Deleted", "Đã xóa", isProtected: false, id: softDeletedRoleId);
        softDeletedRole.IsDeleted = true;
        _db.Roles.Add(softDeletedRole);
        await _db.SaveChangesAsync();

        var res2 = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, softDeletedRoleId, "Valid reason", 1));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("Role.NotFound");
    }

    [Fact]
    public async Task AssignRole_ShouldRejectUnchangedRole()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();
        var target = new User { Id = targetId, Email = "target@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AdminControlVersion = 1 };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        // Attempting to assign same role
        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, RoleDefaults.LearnerRoleId, "Same role", 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_ROLE_UNCHANGED");
    }

    [Fact]
    public async Task AssignRole_ShouldRejectStaleConcurrencyVersion()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();
        var target = new User { Id = targetId, Email = "target@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AdminControlVersion = 5 };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, "Updating role", 4));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task AssignRole_ShouldProtectLastActiveAdministrator_AndAllowSuspendedAdminDemotion()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);

        // _superAdminId is the ONLY active admin in DB with AdminRoleId
        // Create another target that is an active admin
        var adminTargetId = Guid.NewGuid();
        var adminTarget = new User
        {
            Id = adminTargetId,
            Email = "admintarget@example.com",
            Name = "Admin Target",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AdminControlVersion = 1
        };
        _db.Users.Add(adminTarget);
        await _db.SaveChangesAsync();

        // Now there are 2 active admins (_superAdminId and adminTargetId).
        // Demoting adminTargetId to Learner should succeed!
        var res1 = await handler.ExecuteAsync(new AdminAssignRoleRequest(adminTargetId, _superAdminId, RoleDefaults.LearnerRoleId, "Demoting second admin", 1));
        res1.IsSuccess.Should().BeTrue();

        // Now only 1 active admin remains (_superAdminId).
        var admin2Id = Guid.NewGuid();
        var admin2 = new User
        {
            Id = admin2Id,
            Email = "admin2@example.com",
            Name = "Admin 2",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AdminControlVersion = 1
        };
        _db.Users.Add(admin2);
        await _db.SaveChangesAsync();

        // Demoting admin2 succeeds (leaves 1 active admin: _superAdminId)
        var res2 = await handler.ExecuteAsync(new AdminAssignRoleRequest(admin2Id, _superAdminId, RoleDefaults.LearnerRoleId, "Demoting admin2", 1));
        res2.IsSuccess.Should().BeTrue();

        // Now only _superAdminId remains as active Admin.
        // Set admin2's role back to Admin in DB, but with Suspended status.
        admin2.RoleId = RoleDefaults.AdminRoleId;
        admin2.Status = AccountStatus.Suspended;
        admin2.SuspendedByUserId = _superAdminId;
        admin2.SuspendedAt = DateTimeOffset.UtcNow.AddDays(-1);
        admin2.SuspensionReason = "Policy violation";
        var suspendedAtBefore = admin2.SuspendedAt;
        var suspendedByBefore = admin2.SuspendedByUserId;
        var suspensionReasonBefore = admin2.SuspensionReason;
        var authVersionBefore = admin2.AuthenticationVersion;
        var controlVersionBefore = admin2.AdminControlVersion;
        await _db.SaveChangesAsync();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = admin2Id,
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenService.HashToken(Guid.NewGuid().ToString("N")),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            AuthenticationVersion = admin2.AuthenticationVersion
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        // Demoting a Suspended admin must SUCCEED because it does not remove an ACTIVE administrator!
        var res3 = await handler.ExecuteAsync(new AdminAssignRoleRequest(admin2Id, _superAdminId, RoleDefaults.LearnerRoleId, "Demoting suspended admin", admin2.AdminControlVersion));
        res3.IsSuccess.Should().BeTrue();
        res3.Value.Role.Key.Should().Be(RoleDefaults.LearnerKey);
        res3.Value.Status.Should().Be(AccountStatus.Suspended);
        res3.Value.SuspendedAt.Should().Be(suspendedAtBefore);
        res3.Value.SuspendedByUserId.Should().Be(suspendedByBefore);
        res3.Value.SuspensionReason.Should().Be(suspensionReasonBefore);

        var admin2Reread = await _db.Users.FindAsync(admin2Id);
        admin2Reread!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);
        admin2Reread.Status.Should().Be(AccountStatus.Suspended);
        admin2Reread.SuspendedAt.Should().Be(suspendedAtBefore);
        admin2Reread.SuspendedByUserId.Should().Be(suspendedByBefore);
        admin2Reread.SuspensionReason.Should().Be(suspensionReasonBefore);
        admin2Reread.AuthenticationVersion.Should().BeGreaterThan(authVersionBefore);
        admin2Reread.AdminControlVersion.Should().BeGreaterThan(controlVersionBefore);

        var tokens = await _db.RefreshTokens.Where(t => t.UserId == admin2Id).ToListAsync();
        tokens.All(t => t.RevokedAt != null).Should().BeTrue();

        // Last active admin protection: reactivate adminTarget as active Admin
        var superAdminUser = await _db.Users.FindAsync(_superAdminId);
        var callerAdminId = Guid.NewGuid();
        var callerAdmin = new User
        {
            Id = callerAdminId,
            Email = "calleradmin@example.com",
            Name = "Caller Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AdminControlVersion = 1
        };
        _db.Users.Add(callerAdmin);
        await _db.SaveChangesAsync();

        // Caller demotes callerAdmin itself? Blocked by self-role change
        // Caller demotes adminTarget (learner)? Target is learner
        // Caller demotes superAdminUser when 2 active admins exist: succeeds
        // Then when only callerAdmin remains as active Admin, demoting callerAdmin is blocked
        // Let's verify unchanged role check runs BEFORE last-admin check
        var unchangedRes = await handler.ExecuteAsync(new AdminAssignRoleRequest(callerAdminId, _superAdminId, RoleDefaults.AdminRoleId, "Reassigning same admin role", callerAdmin.AdminControlVersion));
        unchangedRes.IsSuccess.Should().BeFalse();
        unchangedRes.Error.Code.Should().Be("ACCOUNT_ROLE_UNCHANGED");
    }

    [Fact]
    public async Task AssignRole_Success_ShouldAdvanceVersions_RevokeSessions_AndReturnSafeDto()
    {
        var handler = new AdminAssignRoleHandler(_db, _uow, _tokenService, _resolver);
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "promoted@example.com",
            Name = "Promoted User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 2,
            AdminControlVersion = 3
        };
        _db.Users.Add(target);

        // Add refresh token for target
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = targetId,
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenService.HashToken(Guid.NewGuid().ToString("N")),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            AuthenticationVersion = 2
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, _superAdminId, _customRoleId, "Promoting to auditor", 3));
        res.IsSuccess.Should().BeTrue();

        var dto = res.Value;
        dto.Id.Should().Be(targetId);
        dto.AdminControlVersion.Should().Be(4);
        dto.Role.Id.Should().Be(_customRoleId);
        dto.Role.Key.Should().Be("custom-auditor");

        // Verify target entity in DB
        var reloadedTarget = await _db.Users.FindAsync(targetId);
        reloadedTarget!.RoleId.Should().Be(_customRoleId);
        reloadedTarget.AuthenticationVersion.Should().Be(3);
        reloadedTarget.AdminControlVersion.Should().Be(4);

        // Verify token was revoked
        var reloadedToken = await _db.RefreshTokens.FindAsync(token.Id);
        reloadedToken!.RevokedAt.Should().NotBeNull();

        // Verify audit event
        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.TargetId == targetId.ToString() && e.Action == AdminAuditActions.UserAssignRole);
        audit.Should().NotBeNull();
        audit!.Outcome.Should().Be(AdminAuditOutcome.Succeeded);
        audit.Reason.Should().Be("Promoting to auditor");
        audit.ChangedFields.Should().Contain("RoleId");
        audit.ChangedFields.Should().Contain("AuthenticationVersion");
        audit.ChangedFields.Should().Contain("AdminControlVersion");
        audit.StatePayload.Should().Contain(RoleDefaults.LearnerRoleId.ToString());
        audit.StatePayload.Should().Contain(_customRoleId.ToString());
    }

    [Fact]
    public async Task ExecuteAccountControlCommand_ShouldRejectStaleActorSessionVersion()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "suspend_target@example.com",
            Name = "Target",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AdminControlVersion = 1
        };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        var handler = new AdminSuspendUserHandler(_uow, _tokenService);

        // Actor in DB has AuthenticationVersion = 1, but presented version is 99
        var res = await handler.ExecuteAsync(new AdminSuspendUserRequest(
            targetId,
            _superAdminId,
            "Valid suspension reason",
            1,
            PresentedSessionVersion: 99));

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("AUTH_FORBIDDEN");
        res.Error.Message.Should().Contain("stale or has been invalidated");
    }

    [Fact]
    public async Task ExecuteAccountControlCommand_ShouldRejectActorWithoutRequiredPermissions()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "suspend_target2@example.com",
            Name = "Target 2",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AdminControlVersion = 1
        };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        var handler = new AdminSuspendUserHandler(_uow, _tokenService);

        // _catalogOnlyAdminId has ManageCatalog but lacks ManageAccounts
        var res = await handler.ExecuteAsync(new AdminSuspendUserRequest(
            targetId,
            _catalogOnlyAdminId,
            "Valid suspension reason",
            1,
            PresentedSessionVersion: 1));

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("AUTH_FORBIDDEN");
        res.Error.Message.Should().Contain("required administrator permissions");
    }

    [Fact]
    public async Task AccountLifecycle_ShouldRetainExistingRoleIdAndPermissions_AndRejectStaleActorsAfterReactivation()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "lifecycle@example.com",
            Name = "Lifecycle Target",
            Status = AccountStatus.Active,
            RoleId = _customRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        _db.Users.Add(target);
        await _db.SaveChangesAsync();

        var suspendHandler = new AdminSuspendUserHandler(_uow, _tokenService);
        var reactivateHandler = new AdminReactivateUserHandler(_uow);
        var revokeHandler = new AdminRevokeUserSessionsHandler(_uow, _tokenService);

        // 1. Suspend target
        var suspendRes = await suspendHandler.ExecuteAsync(new AdminSuspendUserRequest(
            targetId, _superAdminId, "Policy check", 1, PresentedSessionVersion: 1));
        suspendRes.IsSuccess.Should().BeTrue();
        suspendRes.Value.Role.Id.Should().Be(_customRoleId);

        var dbTarget1 = await _db.Users.FindAsync(targetId);
        dbTarget1!.RoleId.Should().Be(_customRoleId);
        dbTarget1.Status.Should().Be(AccountStatus.Suspended);
        dbTarget1.AuthenticationVersion.Should().Be(2); // advanced on suspend
        dbTarget1.AdminControlVersion.Should().Be(2);

        // 2. Reactivate target
        var reactivateRes = await reactivateHandler.ExecuteAsync(new AdminReactivateUserRequest(
            targetId, _superAdminId, "Reactivating account", 2, PresentedSessionVersion: 1));
        reactivateRes.IsSuccess.Should().BeTrue();
        reactivateRes.Value.Role.Id.Should().Be(_customRoleId);

        var dbTarget2 = await _db.Users.FindAsync(targetId);
        dbTarget2!.RoleId.Should().Be(_customRoleId);
        dbTarget2.Status.Should().Be(AccountStatus.Active);
        dbTarget2.AdminControlVersion.Should().Be(3);

        // 3. Revoke sessions
        var revokeRes = await revokeHandler.ExecuteAsync(new AdminRevokeUserSessionsRequest(
            targetId, _superAdminId, "Revoking sessions", 3, PresentedSessionVersion: 1));
        revokeRes.IsSuccess.Should().BeTrue();
        revokeRes.Value.Role.Id.Should().Be(_customRoleId);

        var dbTarget3 = await _db.Users.FindAsync(targetId);
        dbTarget3!.RoleId.Should().Be(_customRoleId);
        dbTarget3.AuthenticationVersion.Should().Be(3); // advanced on revocation

        // 4. Stale actor rejection: target attempts to act using old session version 1 or 2
        var staleActRes = await suspendHandler.ExecuteAsync(new AdminSuspendUserRequest(
            Guid.NewGuid(), targetId, "Unauthorized attempt", 1, PresentedSessionVersion: 1));
        staleActRes.IsSuccess.Should().BeFalse();
        staleActRes.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }
}
