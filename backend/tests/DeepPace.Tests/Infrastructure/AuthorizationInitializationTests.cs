using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class AuthorizationInitializationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly RefreshTokenService _tokenService;
    private readonly AuthorizationInitializationService _initService;

    public AuthorizationInitializationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        // Ensure roles and permissions seeds exist
        EnsureRoleSeeds();

        _tokenService = new RefreshTokenService(_db);
        _initService = new AuthorizationInitializationService(_db, _tokenService);
    }

    private void EnsureRoleSeeds()
    {
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

        if (!_db.Roles.Any(r => r.Id == RoleDefaults.LearnerRoleId))
        {
            var learnerRole = Role.Create("learner", "Learner", "Nguoi hoc", isProtected: true, id: RoleDefaults.LearnerRoleId);
            _db.Roles.Add(learnerRole);
        }

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Initialize_ValidActiveAndSuspended_ShouldElevateAndPersistReceiptWithoutAuditFabrication()
    {
        var activeUserId = Guid.NewGuid();
        var suspendedUserId = Guid.NewGuid();

        var activeUser = new User
        {
            Id = activeUserId,
            Email = "active@example.com",
            Name = "Active User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };

        var suspendedUser = new User
        {
            Id = suspendedUserId,
            Email = "suspended@example.com",
            Name = "Suspended User",
            Status = AccountStatus.Suspended,
            SuspensionReason = "Policy violation",
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };

        await _db.Users.AddRangeAsync(activeUser, suspendedUser);
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { activeUserId, suspendedUserId });
        res.IsSuccess.Should().BeTrue();

        // Verify receipt
        var receipt = await _db.AuthorizationInitializations.FirstOrDefaultAsync(i => i.Scope == AuthorizationInitialization.DefaultScope);
        receipt.Should().NotBeNull();
        receipt!.ImportedAdminUserIds.Should().BeEquivalentTo(new[] { activeUserId, suspendedUserId });

        // Verify users elevated while preserving status
        var activeElevated = await _db.Users.FindAsync(activeUserId);
        activeElevated!.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        activeElevated.Status.Should().Be(AccountStatus.Active);
        activeElevated.AuthenticationVersion.Should().Be(2);

        var suspendedElevated = await _db.Users.FindAsync(suspendedUserId);
        suspendedElevated!.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        suspendedElevated.Status.Should().Be(AccountStatus.Suspended);
        suspendedElevated.SuspensionReason.Should().Be("Policy violation");

        // Verify NO user-attributed audit events fabricated
        var audits = await _db.AdminAuditEvents.ToListAsync();
        audits.Should().BeEmpty("Bootstrap completion is operational evidence, not a fabricated user-attributed action");
    }

    [Fact]
    public async Task Initialize_EmptyOrMalformedIds_ShouldFail()
    {
        var emptyRes = await _initService.InitializeAsync(Array.Empty<Guid>());
        emptyRes.IsSuccess.Should().BeFalse();
        emptyRes.Error.Code.Should().Be("INIT_EMPTY_ADMIN_SET");

        var malformedRes = await _initService.InitializeAsync(new[] { Guid.NewGuid(), Guid.Empty });
        malformedRes.IsSuccess.Should().BeFalse();
        malformedRes.Error.Code.Should().Be("INIT_INVALID_ADMIN_ID");
    }

    [Fact]
    public async Task Initialize_MissingOrDeletedUser_ShouldFailAndRollbackAll()
    {
        var validId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        var validUser = new User { Id = validId, Email = "valid@example.com", Name = "Valid", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        var deletedUser = new User { Id = deletedId, Email = "deleted@example.com", Name = "Deleted", Status = AccountStatus.Active, IsDeleted = true, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddRangeAsync(validUser, deletedUser);
        await _db.SaveChangesAsync();

        // 1. Missing user
        var missingRes = await _initService.InitializeAsync(new[] { validId, missingId });
        missingRes.IsSuccess.Should().BeFalse();
        missingRes.Error.Code.Should().Be("INIT_USER_NOT_FOUND");

        // Verify valid user was NOT elevated
        var validCheck1 = await _db.Users.FindAsync(validId);
        validCheck1!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);

        // 2. Deleted user
        var deletedRes = await _initService.InitializeAsync(new[] { validId, deletedId });
        deletedRes.IsSuccess.Should().BeFalse();
        deletedRes.Error.Code.Should().Be("INIT_USER_DELETED");

        var validCheck2 = await _db.Users.FindAsync(validId);
        validCheck2!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);

        // Verify no receipt persisted
        var receipt = await _db.AuthorizationInitializations.FirstOrDefaultAsync();
        receipt.Should().BeNull();
    }

    [Fact]
    public async Task Initialize_ZeroActiveAdmins_ShouldFailWithoutElevation()
    {
        var suspendedId = Guid.NewGuid();
        var suspendedUser = new User { Id = suspendedId, Email = "all-susp@example.com", Name = "Suspended Only", Status = AccountStatus.Suspended, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(suspendedUser);
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { suspendedId });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_NO_ACTIVE_ADMIN");

        var check = await _db.Users.FindAsync(suspendedId);
        check!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);

        var receipt = await _db.AuthorizationInitializations.FirstOrDefaultAsync();
        receipt.Should().BeNull();
    }

    [Fact]
    public async Task Initialize_RepeatedInitialization_ShouldRefuseSecondImport()
    {
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        var u1 = new User { Id = admin1Id, Email = "u1@example.com", Name = "U1", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        var u2 = new User { Id = admin2Id, Email = "u2@example.com", Name = "U2", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddRangeAsync(u1, u2);
        await _db.SaveChangesAsync();

        // First initialization succeeds
        var firstRes = await _initService.InitializeAsync(new[] { admin1Id });
        firstRes.IsSuccess.Should().BeTrue();

        // Second initialization refuses
        var secondRes = await _initService.InitializeAsync(new[] { admin2Id });
        secondRes.IsSuccess.Should().BeFalse();
        secondRes.Error.Code.Should().Be("AUTHORIZATION_ALREADY_INITIALIZED");

        // u2 remains Learner
        var u2Check = await _db.Users.FindAsync(admin2Id);
        u2Check!.RoleId.Should().Be(RoleDefaults.LearnerRoleId);
    }

    [Fact]
    public async Task Initialize_RefuseAfterLaterZeroAdminDamage()
    {
        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = "admin-solo@example.com", Name = "Solo", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        var candidateId = Guid.NewGuid();
        var candidate = new User { Id = candidateId, Email = "candidate@example.com", Name = "Candidate", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddRangeAsync(admin, candidate);
        await _db.SaveChangesAsync();

        // 1. Initial initialization succeeds
        var res1 = await _initService.InitializeAsync(new[] { adminId });
        res1.IsSuccess.Should().BeTrue();

        // 2. Later, out-of-band damage suspends or deletes the only admin
        admin.Status = AccountStatus.Suspended;
        await _db.SaveChangesAsync();

        // Active admin count in DB is now 0!
        var activeAdmins = await _db.Users.CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && u.Status == AccountStatus.Active && !u.IsDeleted);
        activeAdmins.Should().Be(0);

        // 3. Attempting re-initialization MUST still refuse because receipt already exists
        var res2 = await _initService.InitializeAsync(new[] { candidateId });
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("AUTHORIZATION_ALREADY_INITIALIZED");
    }

    [Fact]
    public async Task Initialize_WhenAdminHasWrongCaseOrMissingGrants_ShouldFailWithSeedCorrupt()
    {
        var adminUser = new User { Id = Guid.NewGuid(), Email = "cand1@example.com", Name = "Cand", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(adminUser);
        await _db.SaveChangesAsync();

        var catPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageCategories);
        _db.RolePermissions.Remove(catPerm);
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { adminUser.Id });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_SEED_CORRUPT");
    }

    [Fact]
    public async Task Initialize_WhenAdminMissingManageRolesMaskedBySixOtherRows_ShouldFailWithSeedCorrupt()
    {
        var adminUser = new User { Id = Guid.NewGuid(), Email = "cand1b@example.com", Name = "Cand", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(adminUser);
        await _db.SaveChangesAsync();

        var rolesPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageRoles);
        _db.RolePermissions.Remove(rolesPerm);
        await _db.SaveChangesAsync();

        var count = await _db.RolePermissions.CountAsync(p => p.RoleId == RoleDefaults.AdminRoleId);
        count.Should().Be(6);

        var res = await _initService.InitializeAsync(new[] { adminUser.Id });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_SEED_CORRUPT");
    }

    [Fact]
    public async Task Initialize_WhenAdminHasWrongCaseGrant_ShouldFailWithSeedCorrupt()
    {
        var adminUser = new User { Id = Guid.NewGuid(), Email = "cand1c@example.com", Name = "Cand", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(adminUser);
        await _db.SaveChangesAsync();

        var catalogPerm = await _db.RolePermissions.FirstAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageCatalog);
        _db.RolePermissions.Remove(catalogPerm);
        _db.RolePermissions.Add(new RolePermission { RoleId = RoleDefaults.AdminRoleId, PermissionKey = "managecatalog" });
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { adminUser.Id });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_SEED_CORRUPT");
    }

    [Fact]
    public async Task Initialize_WhenAdminHasExtraUnsupportedGrants_ShouldFailWithSeedCorrupt()
    {
        var adminUser = new User { Id = Guid.NewGuid(), Email = "cand2@example.com", Name = "Cand", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(adminUser);
        _db.RolePermissions.Add(new RolePermission { RoleId = RoleDefaults.AdminRoleId, PermissionKey = "SuperAdminPermission" });
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { adminUser.Id });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_SEED_CORRUPT");
    }

    [Fact]
    public async Task Initialize_WhenLearnerHoldsPermissions_ShouldFailWithSeedCorrupt()
    {
        var adminUser = new User { Id = Guid.NewGuid(), Email = "cand3@example.com", Name = "Cand", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId };
        await _db.Users.AddAsync(adminUser);
        _db.RolePermissions.Add(RolePermission.Create(RoleDefaults.LearnerRoleId, PermissionKeys.ReadAudit));
        await _db.SaveChangesAsync();

        var res = await _initService.InitializeAsync(new[] { adminUser.Id });
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("INIT_SEED_CORRUPT");
    }
}
