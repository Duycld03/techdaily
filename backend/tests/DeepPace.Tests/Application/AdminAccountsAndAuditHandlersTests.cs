using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Interfaces;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Audit;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class AdminAccountsAndAuditHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly AdministrativeUnitOfWork _uow;
    private readonly RefreshTokenService _tokenService;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public AdminAccountsAndAuditHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();


        _uow = new AdministrativeUnitOfWork(_db, options);
        _tokenService = new RefreshTokenService(_db);

        // Seed the administrator
        var admin = new User
        {
            Id = _adminUserId,
            Email = "admin@example.com",
            Name = "Admin User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId
        };
        _db.Users.Add(admin);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetAccounts_ShouldExcludeSoftDeletedAndPendingRegistrations()
    {
        var activeUser = new User { Id = Guid.NewGuid(), Email = "active@example.com", Name = "Active", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        var suspendedUser = new User { Id = Guid.NewGuid(), Email = "suspended@example.com", Name = "Suspended", Status = AccountStatus.Suspended, RoleId = RoleDefaults.LearnerRoleId };
        var deletedUser = new User { Id = Guid.NewGuid(), Email = "deleted@example.com", Name = "Deleted", Status = AccountStatus.Active, IsDeleted = true, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddRangeAsync(activeUser, suspendedUser, deletedUser);

        // Add a pending OTP registration that has no User row yet
        var pendingOtp = new EmailOtp
        {
            Email = "pending@example.com",
            CodeHash = "hash123",
            Purpose = OtpPurpose.EmailVerification,
            PendingName = "Pending Person",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        await _db.EmailOtps.AddAsync(pendingOtp);
        await _db.SaveChangesAsync();

        var handler = new AdminGetAccountsHandler(_db);

        // Filter by Suspended
        var suspendedResult = await handler.ExecuteAsync(new AdminGetAccountsRequest(Status: AccountStatus.Suspended));
        suspendedResult.IsSuccess.Should().BeTrue();
        suspendedResult.Value.Items.Should().ContainSingle(u => u.Email == "suspended@example.com");

        // Filter all non-deleted
        var allResult = await handler.ExecuteAsync(new AdminGetAccountsRequest());
        allResult.IsSuccess.Should().BeTrue();
        allResult.Value.Items.Should().NotContain(u => u.Email == "deleted@example.com");
        allResult.Value.Items.Should().NotContain(u => u.Email == "pending@example.com");
    }

    [Fact]
    public async Task GetAccountById_ShouldExcludeSensitiveCredentials()
    {
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@example.com",
            Name = "Target User",
            PasswordHash = "argon2id$v=19$m=65536,t=3,p=1$secret_hash",
            GoogleSubjectId = "google_sub_12345",
            Status = AccountStatus.Active
        };
        await _db.Users.AddAsync(targetUser);
        await _db.SaveChangesAsync();

        var handler = new AdminGetAccountByIdHandler(_db);
        var result = await handler.ExecuteAsync(new AdminGetAccountByIdRequest(targetUser.Id));

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Email.Should().Be("target@example.com");
        dto.HasPassword.Should().BeTrue();
        dto.HasGoogleAuth.Should().BeTrue();
        // The DTO has no PasswordHash or GoogleSubjectId properties!
    }

    [Fact]
    public async Task SuspendUser_Validation_BlankOrOverlongReason_ShouldFail()
    {
        var target = new User { Id = Guid.NewGuid(), Email = "t@example.com", Name = "T", Status = AccountStatus.Active };
        await _db.Users.AddAsync(target);
        await _db.SaveChangesAsync();

        var handler = new AdminSuspendUserHandler(_uow, _tokenService);

        var blankRes = await handler.ExecuteAsync(new AdminSuspendUserRequest(target.Id, _adminUserId, "  ", 1));
        blankRes.IsSuccess.Should().BeFalse();
        blankRes.Error.Code.Should().Be("Account.ReasonRequired");

        var longRes = await handler.ExecuteAsync(new AdminSuspendUserRequest(target.Id, _adminUserId, new string('a', 501), 1));
        longRes.IsSuccess.Should().BeFalse();
        longRes.Error.Code.Should().Be("Account.ReasonTooLong");
    }

    [Fact]
    public async Task SuspendUser_SelfTarget_ShouldFailAndRecordRejection()
    {
        var handler = new AdminSuspendUserHandler(_uow, _tokenService);

        var res = await handler.ExecuteAsync(new AdminSuspendUserRequest(_adminUserId, _adminUserId, "Attempting self suspension", 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_SELF_SUSPENSION_BLOCKED");

        var rejectedAudit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.ErrorCode == "ACCOUNT_SELF_SUSPENSION_BLOCKED");
        rejectedAudit.Should().NotBeNull();
        rejectedAudit!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
    }

    [Fact]
    public async Task AuditEvents_TimeRangeFilter_InvalidRange_ShouldReturnError()
    {
        var handler = new AdminGetAuditEventsHandler(_db);

        var from = DateTimeOffset.UtcNow;
        var to = from.AddHours(-1); // Inverted range

        var res = await handler.ExecuteAsync(new AdminGetAuditEventsRequest(From: from, To: to));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("Audit.InvalidTimeRange");
    }

    [Fact]
    public async Task AuditEvents_UTCBoundaries_ShouldFilterCorrectly()
    {
        var t1 = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 10, 3, 10, 0, 0, TimeSpan.Zero);

        var e1 = AdminAuditEvent.Create(_adminUserId, AdminAuditActions.UserSuspend, "User", "1", AdminAuditOutcome.Succeeded, occurredAt: t1);
        var e2 = AdminAuditEvent.Create(_adminUserId, AdminAuditActions.UserReactivate, "User", "1", AdminAuditOutcome.Succeeded, occurredAt: t2);
        var e3 = AdminAuditEvent.Create(_adminUserId, AdminAuditActions.UserRevokeSessions, "User", "1", AdminAuditOutcome.Succeeded, occurredAt: t3);

        await _db.AdminAuditEvents.AddRangeAsync(e1, e2, e3);
        await _db.SaveChangesAsync();

        var handler = new AdminGetAuditEventsHandler(_db);

        // From t1 inclusive to t3 exclusive -> should return e1 and e2
        var res = await handler.ExecuteAsync(new AdminGetAuditEventsRequest(From: t1, To: t3));
        res.IsSuccess.Should().BeTrue();
        res.Value.Items.Should().HaveCount(2);
        res.Value.Items.Select(i => i.Id).Should().Contain(new[] { e1.Id, e2.Id });
    }

    [Fact]
    public async Task AccountControl_RejectionAfterStagedMutations_ShouldRollbackMutationsAndPersistOnlyRejectedHistory()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "target-reject@example.com",
            Name = "Target User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(target);
        await _db.SaveChangesAsync();

        var res = await _uow.ExecuteAccountControlCommandAsync<bool>(
            _adminUserId,
            1,
            new[] { AdminPermission.ManageAccounts },
            targetId,
            1,
            AdminAuditActions.UserSuspend,
            "corr-rej-staged",
            preTargetValidation: null,
            command: async (act, tgt, role) =>
            {
                tgt.Status = AccountStatus.Suspended;
                tgt.AdminControlVersion = 99;
                return DeepPace.Application.Common.Error.Custom("BUSINESS_RULE_FAILED", "Simulated business rule failure");
            });

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("BUSINESS_RULE_FAILED");

        // Verify target state was rolled back completely
        var targetReread = await _db.Users.FindAsync(targetId);
        targetReread!.Status.Should().Be(AccountStatus.Active, "Staged mutations must be rolled back on command failure");
        targetReread.AdminControlVersion.Should().Be(1);

        // Verify only rejected audit history survives
        var audits = await _db.AdminAuditEvents.Where(e => e.CorrelationId == "corr-rej-staged").ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audits[0].ErrorCode.Should().Be("BUSINESS_RULE_FAILED");
    }

    [Fact]
    public async Task AccountControl_RejectionPersistenceFailure_ShouldReturnUnavailableWithoutSilentFailure()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "target-fail-rej@example.com",
            Name = "Target User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(target);
        await _db.SaveChangesAsync();

        // Use an invalid options builder to simulate failure during clean context rejection recording
        var brokenOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite("DataSource=file::memory:?cache=shared;Mode=ReadOnly")
            .Options;
        var brokenUow = new AdministrativeUnitOfWork(_db, brokenOptions);

        var res = await brokenUow.ExecuteAccountControlCommandAsync<bool>(
            _adminUserId,
            1,
            new[] { AdminPermission.ManageAccounts },
            targetId,
            1,
            AdminAuditActions.UserSuspend,
            "corr-broken",
            preTargetValidation: null,
            command: async (act, tgt, role) =>
            {
                return DeepPace.Application.Common.Error.Custom("FAIL", "Rejection trigger");
            });

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE", "Audit failure must return unavailable and not fail silently");
    }

    [Fact]
    public async Task AccountControl_DeniedOrStaleActor_ShouldNotPersistAuditEvents()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "target-unauth@example.com",
            Name = "Target User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(target);
        await _db.SaveChangesAsync();

        // 1. Stale session version
        var staleRes = await _uow.ExecuteAccountControlCommandAsync<bool>(
            _adminUserId,
            presentedSessionVersion: 999, // Stale!
            new[] { AdminPermission.ManageAccounts },
            targetId,
            expectedVersion: 1,
            AdminAuditActions.UserSuspend,
            "corr-stale-actor",
            preTargetValidation: null,
            command: (act, tgt, role) => Task.FromResult(Result<(bool, AdminAuditEvent)>.Success((true, AdminAuditEvent.Create(_adminUserId, AdminAuditActions.UserSuspend, "User", targetId.ToString(), AdminAuditOutcome.Succeeded)))));

        staleRes.IsSuccess.Should().BeFalse();
        staleRes.Error.Code.Should().Be("AUTH_FORBIDDEN");

        var staleAudits = await _db.AdminAuditEvents.Where(e => e.CorrelationId == "corr-stale-actor").ToListAsync();
        staleAudits.Should().BeEmpty("Stale actor must not produce any audit records");

        // 2. Inactive actor
        var inactiveAdminId = Guid.NewGuid();
        var inactiveAdmin = new User
        {
            Id = inactiveAdminId,
            Email = "inactive-admin@example.com",
            Name = "Inactive Admin",
            Status = AccountStatus.Suspended,
            RoleId = RoleDefaults.AdminRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(inactiveAdmin);
        await _db.SaveChangesAsync();

        var inactiveRes = await _uow.ExecuteAccountControlCommandAsync<bool>(
            inactiveAdminId,
            presentedSessionVersion: 1,
            new[] { AdminPermission.ManageAccounts },
            targetId,
            expectedVersion: 1,
            AdminAuditActions.UserSuspend,
            "corr-inactive-actor",
            preTargetValidation: null,
            command: (act, tgt, role) => Task.FromResult(Result<(bool, AdminAuditEvent)>.Success((true, AdminAuditEvent.Create(inactiveAdminId, AdminAuditActions.UserSuspend, "User", targetId.ToString(), AdminAuditOutcome.Succeeded)))));

        inactiveRes.IsSuccess.Should().BeFalse();
        inactiveRes.Error.Code.Should().Be("AUTH_FORBIDDEN");

        var inactiveAudits = await _db.AdminAuditEvents.Where(e => e.CorrelationId == "corr-inactive-actor").ToListAsync();
        inactiveAudits.Should().BeEmpty("Inactive actor must not produce any audit records");
    }

    [Fact]
    public async Task AccountControl_MissingTarget_ShouldNotPersistAuditEvents()
    {
        var nonExistentTargetId = Guid.NewGuid();

        var res = await _uow.ExecuteAccountControlCommandAsync<bool>(
            _adminUserId,
            presentedSessionVersion: 1,
            new[] { AdminPermission.ManageAccounts },
            nonExistentTargetId,
            expectedVersion: 1,
            AdminAuditActions.UserSuspend,
            "corr-missing-target",
            preTargetValidation: null,
            command: (act, tgt, role) => Task.FromResult(Result<(bool, AdminAuditEvent)>.Success((true, AdminAuditEvent.Create(_adminUserId, AdminAuditActions.UserSuspend, "User", nonExistentTargetId.ToString(), AdminAuditOutcome.Succeeded)))));

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("Account.NotFound");

        var audits = await _db.AdminAuditEvents.Where(e => e.CorrelationId == "corr-missing-target").ToListAsync();
        audits.Should().BeEmpty("Missing target account must not produce any audit records");
    }

    [Fact]
    public async Task AccountControl_SuccessfulMutation_ShouldCommitAtomicallyWithAuditEvent()
    {
        var targetId = Guid.NewGuid();
        var target = new User
        {
            Id = targetId,
            Email = "target-success@example.com",
            Name = "Target User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(target);
        await _db.SaveChangesAsync();

        var res = await _uow.ExecuteAccountControlCommandAsync<bool>(
            _adminUserId,
            presentedSessionVersion: 1,
            new[] { AdminPermission.ManageAccounts },
            targetId,
            expectedVersion: 1,
            AdminAuditActions.UserSuspend,
            "corr-atomic-success",
            preTargetValidation: null,
            command: async (act, tgt, role) =>
            {
                tgt.Status = AccountStatus.Suspended;
                tgt.AdminControlVersion++;
                var audit = AdminAuditEvent.Create(
                    actorUserId: act.Id,
                    action: AdminAuditActions.UserSuspend,
                    targetType: "User",
                    targetId: tgt.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    correlationId: "corr-atomic-success");
                return Result<(bool, AdminAuditEvent)>.Success((true, audit));
            });

        res.IsSuccess.Should().BeTrue();

        // Verify target mutation committed
        var targetReread = await _db.Users.FindAsync(targetId);
        targetReread!.Status.Should().Be(AccountStatus.Suspended);
        targetReread.AdminControlVersion.Should().Be(2);

        // Verify succeeded audit committed
        var audits = await _db.AdminAuditEvents.Where(e => e.CorrelationId == "corr-atomic-success").ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].Outcome.Should().Be(AdminAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task AccountActions_OnAdminTarget_ShouldPreserveAdminRoleDescriptor_RatherThanLearnerFallback()
    {
        var admin2Id = Guid.NewGuid();
        var admin2 = new User
        {
            Id = admin2Id,
            Email = "admin2@example.com",
            Name = "Second Administrator",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(admin2);
        await _db.SaveChangesAsync();

        var suspendHandler = new AdminSuspendUserHandler(_uow, _tokenService);
        var reactivateHandler = new AdminReactivateUserHandler(_uow);
        var revokeHandler = new AdminRevokeUserSessionsHandler(_uow, _tokenService);

        // Suspend
        var suspendRes = await suspendHandler.ExecuteAsync(new AdminSuspendUserRequest(
            admin2Id, _adminUserId, "Temporary security review", 1, PresentedSessionVersion: 1));
        suspendRes.IsSuccess.Should().BeTrue();
        suspendRes.Value.Role.Key.Should().Be(RoleDefaults.AdminKey, "Response role descriptor must be Admin, not synthetic Learner");
        suspendRes.Value.Role.Id.Should().Be(RoleDefaults.AdminRoleId);

        // Reactivate
        var reactivateRes = await reactivateHandler.ExecuteAsync(new AdminReactivateUserRequest(
            admin2Id, _adminUserId, "Review complete", 2, PresentedSessionVersion: 1));
        reactivateRes.IsSuccess.Should().BeTrue();
        reactivateRes.Value.Role.Key.Should().Be(RoleDefaults.AdminKey, "Response role descriptor must be Admin, not synthetic Learner");
        reactivateRes.Value.Role.Id.Should().Be(RoleDefaults.AdminRoleId);

        // Revoke sessions
        var revokeRes = await revokeHandler.ExecuteAsync(new AdminRevokeUserSessionsRequest(
            admin2Id, _adminUserId, "Credential rotation", 3, PresentedSessionVersion: 1));
        revokeRes.IsSuccess.Should().BeTrue();
        revokeRes.Value.Role.Key.Should().Be(RoleDefaults.AdminKey, "Response role descriptor must be Admin, not synthetic Learner");
        revokeRes.Value.Role.Id.Should().Be(RoleDefaults.AdminRoleId);
    }

    [Fact]
    public async Task AccountActions_WhenTargetRoleMissing_ShouldFailAndNeverReturnFabricatedRole()
    {
        var tempRole = Role.Create("temp-broken-role", "Temp", "Temp", isProtected: false);
        await _db.Roles.AddAsync(tempRole);
        await _db.SaveChangesAsync();

        var brokenTargetId = Guid.NewGuid();
        var brokenTarget = new User
        {
            Id = brokenTargetId,
            Email = "broken-role@example.com",
            Name = "Broken Role User",
            Status = AccountStatus.Active,
            RoleId = tempRole.Id,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await _db.Users.AddAsync(brokenTarget);
        await _db.SaveChangesAsync();

        tempRole.IsDeleted = true;
        await _db.SaveChangesAsync();
        var suspendHandler = new AdminSuspendUserHandler(_uow, _tokenService);
        var suspendRes = await suspendHandler.ExecuteAsync(new AdminSuspendUserRequest(
            brokenTargetId, _adminUserId, "Suspending corrupt account", 1, PresentedSessionVersion: 1));

        suspendRes.IsSuccess.Should().BeFalse();
        suspendRes.Error.Code.Should().Be("Role.NotFound");

        var getByIdHandler = new AdminGetAccountByIdHandler(_db);
        var getByIdRes = await getByIdHandler.ExecuteAsync(new AdminGetAccountByIdRequest(brokenTargetId));
        getByIdRes.IsSuccess.Should().BeFalse();
        getByIdRes.Error.Code.Should().Be("Account.RoleUnavailable");
    }
}
