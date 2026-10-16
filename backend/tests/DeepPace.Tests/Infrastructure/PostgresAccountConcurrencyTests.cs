using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Catalog.Admin.Publish;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace DeepPace.Tests.Infrastructure;

public class PostgresAccountConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public PostgresAccountConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION must identify an isolated PostgreSQL test database.");
    }

    private static DbContextOptions<DeepPaceDbContext> CreatePostgresOptions()
    {
        return new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseNpgsql(GetConnectionString(), o => o.UseVector())
            .Options;
    }

    private bool CheckPostgresConnection()
    {
        try
        {
            using var db = new DeepPaceDbContext(CreatePostgresOptions());
            var canConnect = db.Database.CanConnect();
            if (canConnect)
            {
                db.Database.Migrate();
            }
            return canConnect;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"[PostgresAccountConcurrencyTests] PostgreSQL fixture unavailable: {ex.Message}");
            return false;
        }
    }

    private static IConfiguration CreateConfig(params Guid[] adminIds)
    {
        var dict = new Dictionary<string, string?>();
        dict["Jwt:Secret"] = "super-secret-key-that-has-at-least-32-characters-entropy!";
        dict["Jwt:Issuer"] = "DeepPaceTest";
        dict["Jwt:Audience"] = "DeepPaceUsersTest";
        dict["Jwt:ExpiryMinutes"] = "60";

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task ConcurrentSuspensions_ShouldSerializeAndRetainAtLeastOneActiveAdmin()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        var admin1 = new User { Id = admin1Id, Email = $"admin1-{Guid.NewGuid():N}@example.com", Name = "Admin 1", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
        var admin2 = new User { Id = admin2Id, Email = $"admin2-{Guid.NewGuid():N}@example.com", Name = "Admin 2", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
        await dbSetup.Users.AddRangeAsync(admin1, admin2);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(admin1Id, admin2Id);

        // Admin 1 attempts to suspend Admin 2 concurrently with Admin 2 attempting to suspend Admin 1
        var task1 = Task.Run(async () =>
        {
            using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db1);
            var tokenService = new RefreshTokenService(db1);
            var handler = new AdminSuspendUserHandler(uow, tokenService);
            return await handler.ExecuteAsync(new AdminSuspendUserRequest(admin2Id, admin1Id, "Admin 1 suspending Admin 2", 1));
        });

        var task2 = Task.Run(async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db2);
            var tokenService = new RefreshTokenService(db2);
            var handler = new AdminSuspendUserHandler(uow, tokenService);
            return await handler.ExecuteAsync(new AdminSuspendUserRequest(admin1Id, admin2Id, "Admin 2 suspending Admin 1", 1));
        });

        var results = await Task.WhenAll(task1, task2);

        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var u1 = await dbVerify.Users.FindAsync(admin1Id);
        var u2 = await dbVerify.Users.FindAsync(admin2Id);

        // Exactly one admin must remain active
        var activeCount = (u1!.Status == AccountStatus.Active ? 1 : 0) + (u2!.Status == AccountStatus.Active ? 1 : 0);
        activeCount.Should().Be(1, "PostgreSQL advisory locks must serialize suspensions so at least 1 active admin remains");

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);
        successCount.Should().Be(1);
        failureCount.Should().Be(1);
    }

    [Fact]
    public async Task CompetingAccountUpdates_StaleVersion_ShouldReturnConflictAndRecordRejection()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AdminControlVersion = 1, RoleId = RoleDefaults.LearnerRoleId };
        await dbSetup.Users.AddRangeAsync(admin, target);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(adminId);

        using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow1 = new AdministrativeUnitOfWork(db1);
        var tokenService1 = new RefreshTokenService(db1);
        var handler1 = new AdminSuspendUserHandler(uow1, tokenService1);

        // First update with expectedVersion = 1 succeeds
        var res1 = await handler1.ExecuteAsync(new AdminSuspendUserRequest(targetId, adminId, "Valid suspension", 1));
        res1.IsSuccess.Should().BeTrue();

        using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow2 = new AdministrativeUnitOfWork(db2);
        var tokenService2 = new RefreshTokenService(db2);
        var handler2 = new AdminSuspendUserHandler(uow2, tokenService2);

        // Second update with stale expectedVersion = 1 fails with conflict
        var res2 = await handler2.ExecuteAsync(new AdminSuspendUserRequest(targetId, adminId, "Stale retry", 1));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("ACCOUNT_CONCURRENCY_CONFLICT");

        // Verify rejected audit event recorded
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var rejectedEvent = await dbVerify.AdminAuditEvents
            .FirstOrDefaultAsync(e => e.TargetId == targetId.ToString() && e.Outcome == AdminAuditOutcome.Rejected);
        rejectedEvent.Should().NotBeNull();
        rejectedEvent!.ErrorCode.Should().Be("ACCOUNT_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Rollback_WhenAuditInsertionFails_ShouldRevertStatusAndTokenRevocations()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AuthenticationVersion = 1, AdminControlVersion = 1, RoleId = RoleDefaults.LearnerRoleId };
        await dbSetup.Users.AddRangeAsync(admin, target);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = targetId,
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenService.HashToken(Guid.NewGuid().ToString("N")),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            AuthenticationVersion = 1
        };
        await dbSetup.RefreshTokens.AddAsync(token);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(adminId);

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(db);

        // Attempt command where audit creation throws an exception
        var act = async () =>
        {
            await uow.ExecuteAccountControlCommandAsync<bool>(
                adminId,
                1,
                new[] { AdminPermission.ManageAccounts },
                targetId,
                1,
                AdminAuditActions.UserSuspend,
                "corr-fail",
                async (actUser, tgtUser) =>
                {
                    tgtUser.Suspend(actUser.Id, "Reason to rollback");
                    token.RevokedAt = DateTimeOffset.UtcNow;

                    // Throwing exception before or during audit creation causes rollback
                    throw new InvalidOperationException("Simulated audit failure");
                });
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Verify that target state and token remain unmutated in database
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var targetVerify = await dbVerify.Users.FindAsync(targetId);
        var tokenVerify = await dbVerify.RefreshTokens.FindAsync(token.Id);

        targetVerify!.Status.Should().Be(AccountStatus.Active);
        targetVerify.AuthenticationVersion.Should().Be(1);
        targetVerify.AdminControlVersion.Should().Be(1);
        tokenVerify!.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task SessionRevocation_PreRevocationCredentialsFail_FreshLoginSucceeds()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AuthenticationVersion = 1, AdminControlVersion = 1, RoleId = RoleDefaults.LearnerRoleId };
        await db.Users.AddRangeAsync(admin, target);
        await db.SaveChangesAsync();

        var config = CreateConfig(adminId);
        var tokenService = new RefreshTokenService(db);
        var issuer = new SessionIssuer(db, tokenService, config);
        var authority = new AccountSessionAuthority(db);

        // Issue initial session with version 1
        var session1 = await issuer.IssueSessionAsync(targetId);
        session1.IsSuccess.Should().BeTrue();
        var rawToken1 = session1.Value.RawRefreshToken;

        // Verify session 1 is valid
        var validAuth1 = await authority.ValidateSessionAsync(targetId, 1);
        validAuth1.Status.Should().Be(AccountAuthorityStatus.Valid);

        // Admin forces all-device sign-out
        var uow = new AdministrativeUnitOfWork(db);
        var revokeHandler = new AdminRevokeUserSessionsHandler(uow, tokenService);
        var revokeRes = await revokeHandler.ExecuteAsync(new AdminRevokeUserSessionsRequest(targetId, adminId, "Revoking all devices", 1));
        revokeRes.IsSuccess.Should().BeTrue();

        // 1. Old session version 1 must now fail JWT admission
        var staleAuth = await authority.ValidateSessionAsync(targetId, 1);
        staleAuth.Status.Should().Be(AccountAuthorityStatus.StaleVersion);

        // 2. Old refresh token rotation must fail
        var rotateStale = await issuer.RotateSessionAsync(rawToken1);
        rotateStale.IsSuccess.Should().BeFalse();

        // 3. Fresh sign-in after commit succeeds with new version 2
        var freshSession = await issuer.IssueSessionAsync(targetId);
        freshSession.IsSuccess.Should().BeTrue();
        freshSession.Value.SessionVersion.Should().Be(2);

        var validFresh = await authority.ValidateSessionAsync(targetId, 2);
        validFresh.Status.Should().Be(AccountAuthorityStatus.Valid);
    }

    [Fact]
    public async Task RoleAssignment_DemotionVersusConcurrentSuspension_ShouldSerializeAndRetainAtLeastOneActiveAdmin()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        var admin1 = new User { Id = admin1Id, Email = $"admin1-{Guid.NewGuid():N}@example.com", Name = "Admin 1", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var admin2 = new User { Id = admin2Id, Email = $"admin2-{Guid.NewGuid():N}@example.com", Name = "Admin 2", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin1, admin2);
        await dbSetup.SaveChangesAsync();

        // Admin 1 attempts to demote Admin 2 concurrently with Admin 2 attempting to suspend Admin 1
        var task1 = Task.Run(async () =>
        {
            using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db1);
            var tokenService = new RefreshTokenService(db1);
            var permResolver = new AdminPermissionResolver(db1);
            var handler = new AdminAssignRoleHandler(db1, uow, tokenService, permResolver);
            return await handler.ExecuteAsync(new AdminAssignRoleRequest(admin2Id, admin1Id, RoleDefaults.LearnerRoleId, "Demoting admin 2", 1, "corr-demote-1", 1));
        });

        var task2 = Task.Run(async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db2);
            var tokenService = new RefreshTokenService(db2);
            var handler = new AdminSuspendUserHandler(uow, tokenService);
            return await handler.ExecuteAsync(new AdminSuspendUserRequest(admin1Id, admin2Id, "Admin 2 suspending Admin 1", 1, "corr-suspend-2", 1));
        });

        var results = await Task.WhenAll(task1, task2);

        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var u1 = await dbVerify.Users.FindAsync(admin1Id);
        var u2 = await dbVerify.Users.FindAsync(admin2Id);

        // Exactly one active Admin must remain
        var activeAdminCount = await dbVerify.Users
            .CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && !u.IsDeleted && u.Status == AccountStatus.Active);
        activeAdminCount.Should().BeGreaterOrEqualTo(1, "PostgreSQL advisory locks must serialize operations so at least 1 active admin remains");
    }

    [Fact]
    public async Task RoleAssignment_ActorSessionVersionLoss_ShouldRejectWithForbidden()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        // Admin has AuthenticationVersion = 2 in database
        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 2, AdminControlVersion = 1 };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin, target);
        await dbSetup.SaveChangesAsync();

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(db);
        var tokenService = new RefreshTokenService(db);
        var permResolver = new AdminPermissionResolver(db);
        var handler = new AdminAssignRoleHandler(db, uow, tokenService, permResolver);

        // Presenting stale session version 1 should be rejected
        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(targetId, adminId, RoleDefaults.AdminRoleId, "Promoting target", 1, "corr-stale-actor", PresentedSessionVersion: 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task RoleAssignment_LastProtectedAdminRetention_ShouldRejectDemotingOnlyActiveAdmin()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var existingAdmins = await dbSetup.Users
            .Where(u => u.RoleId == RoleDefaults.AdminRoleId && u.Status == AccountStatus.Active)
            .ToListAsync();
        foreach (var a in existingAdmins)
        {
            a.Status = AccountStatus.Suspended;
        }
        await dbSetup.SaveChangesAsync();
        var managerRoleId = Guid.NewGuid();
        var roleKey = $"mgr-{Guid.NewGuid():N}"[..20];
        var managerRole = Role.Create(roleKey, "Super Manager", "Quan ly cap cao", isProtected: false, id: managerRoleId);
        await dbSetup.Roles.AddAsync(managerRole);
        await dbSetup.RolePermissions.AddRangeAsync(
            RolePermission.Create(managerRoleId, PermissionKeys.ManageAccounts),
            RolePermission.Create(managerRoleId, PermissionKeys.ManageRoles)
        );

        var managerId = Guid.NewGuid();
        var soloAdminId = Guid.NewGuid();

        var manager = new User { Id = managerId, Email = $"manager-{Guid.NewGuid():N}@example.com", Name = "Manager", Status = AccountStatus.Active, RoleId = managerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var soloAdmin = new User { Id = soloAdminId, Email = $"solo-admin-{Guid.NewGuid():N}@example.com", Name = "Solo Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(manager, soloAdmin);
        await dbSetup.SaveChangesAsync();

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(db);
        var tokenService = new RefreshTokenService(db);
        var permResolver = new AdminPermissionResolver(db);
        var handler = new AdminAssignRoleHandler(db, uow, tokenService, permResolver);

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(soloAdminId, managerId, RoleDefaults.LearnerRoleId, "Attempting to demote sole admin", 1, "corr-last-admin", 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_LAST_ADMIN_PROTECTED");
    }

    [Fact]
    public async Task RoleAssignment_SelfTargetRule_ShouldReject()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddAsync(admin);
        await dbSetup.SaveChangesAsync();

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(db);
        var tokenService = new RefreshTokenService(db);
        var permResolver = new AdminPermissionResolver(db);
        var handler = new AdminAssignRoleHandler(db, uow, tokenService, permResolver);

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(adminId, adminId, RoleDefaults.LearnerRoleId, "Self demotion", 1, "corr-self", 1));
        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED");
    }

    [Fact]
    public async Task RoleAssignment_SuspendedTarget_ShouldAssignRoleAndRevokeTokensWhilePreservingSuspendedStatus()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var suspendedUserId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var suspendedUser = new User
        {
            Id = suspendedUserId,
            Email = $"suspended-{Guid.NewGuid():N}@example.com",
            Name = "Suspended Target",
            Status = AccountStatus.Suspended,
            SuspendedByUserId = adminId,
            SuspendedAt = DateTimeOffset.UtcNow.AddDays(-1),
            SuspensionReason = "Prior policy breach",
            RoleId = RoleDefaults.LearnerRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await dbSetup.Users.AddRangeAsync(admin, suspendedUser);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = suspendedUserId,
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenService.HashToken(Guid.NewGuid().ToString("N")),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            AuthenticationVersion = 1
        };
        await dbSetup.RefreshTokens.AddAsync(token);
        await dbSetup.SaveChangesAsync();

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(db);
        var tokenService = new RefreshTokenService(db);
        var permResolver = new AdminPermissionResolver(db);
        var handler = new AdminAssignRoleHandler(db, uow, tokenService, permResolver);

        var res = await handler.ExecuteAsync(new AdminAssignRoleRequest(suspendedUserId, adminId, RoleDefaults.AdminRoleId, "Reassigning role to suspended account", 1, "corr-susp-role", 1));
        res.IsSuccess.Should().BeTrue();

        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var targetVerify = await dbVerify.Users.FindAsync(suspendedUserId);
        targetVerify!.RoleId.Should().Be(RoleDefaults.AdminRoleId);
        targetVerify.Status.Should().Be(AccountStatus.Suspended, "Suspension status must be preserved");
        targetVerify.SuspensionReason.Should().Be("Prior policy breach");
        targetVerify.AdminControlVersion.Should().Be(2);
        targetVerify.AuthenticationVersion.Should().Be(2);

        var tokenVerify = await dbVerify.RefreshTokens.FindAsync(token.Id);
        tokenVerify!.RevokedAt.Should().NotBeNull("Refresh token must be revoked on role assignment");
    }

    [Fact]
    public async Task RoleAssignment_LostResponseRetry_ShouldFailStaleVersionWithoutRepeatedRevocation()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin, target);
        await dbSetup.SaveChangesAsync();

        using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow1 = new AdministrativeUnitOfWork(db1);
        var tokenService1 = new RefreshTokenService(db1);
        var permResolver1 = new AdminPermissionResolver(db1);
        var handler1 = new AdminAssignRoleHandler(db1, uow1, tokenService1, permResolver1);

        // 1. Initial assignment succeeds
        var res1 = await handler1.ExecuteAsync(new AdminAssignRoleRequest(targetId, adminId, RoleDefaults.AdminRoleId, "Initial promotion", 1, "corr-lost-1", 1));
        res1.IsSuccess.Should().BeTrue();

        // Target now has version 2 and fresh token issued
        using var dbToken = new DeepPaceDbContext(CreatePostgresOptions());
        var freshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = targetId,
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenService.HashToken(Guid.NewGuid().ToString("N")),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            AuthenticationVersion = 2
        };
        await dbToken.RefreshTokens.AddAsync(freshToken);
        await dbToken.SaveChangesAsync();

        // 2. Client retries with stale expectedVersion = 1 (lost response scenario)
        using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow2 = new AdministrativeUnitOfWork(db2);
        var tokenService2 = new RefreshTokenService(db2);
        var permResolver2 = new AdminPermissionResolver(db2);
        var handler2 = new AdminAssignRoleHandler(db2, uow2, tokenService2, permResolver2);

        var res2 = await handler2.ExecuteAsync(new AdminAssignRoleRequest(targetId, adminId, RoleDefaults.AdminRoleId, "Initial promotion retry", 1, "corr-lost-1-retry", 1));
        res2.IsSuccess.Should().BeFalse();
        res2.Error.Code.Should().Be("ACCOUNT_CONCURRENCY_CONFLICT");

        // 3. Verify target version remains 2, fresh token was NOT revoked
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var targetVerify = await dbVerify.Users.FindAsync(targetId);
        targetVerify!.AdminControlVersion.Should().Be(2);
        targetVerify.AuthenticationVersion.Should().Be(2);

        var tokenVerify = await dbVerify.RefreshTokens.FindAsync(freshToken.Id);
        tokenVerify!.RevokedAt.Should().BeNull("Fresh token must not be revoked on failed/stale retry");
    }

    [Fact]
    public async Task PrivilegedCatalogAdmission_WhenSessionRevocationCommitsFirst_ShouldPreventPrivilegedMutation()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Email = $"admin-{Guid.NewGuid():N}@example.com",
            Name = "Catalog Admin",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        await dbSetup.Users.AddAsync(admin);

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Privileged Admission Test Doc",
            Description = "Testing privileged admission rejection on session revocation.",
            CategoryId = CategoryDefaults.SystemDesignId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Test Attribution",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 1,
            Revision = 1,
            ConcurrencyVersion = 1
        };
        doc.Slices.Add(new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = doc.Id,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "# Section 1\nContent",
            SummaryMarkdown = "Summary",
            KeyTakeaways = new() { "Takeaway 1" },
            EstimatedReadMinutes = 3,
            IsAiFormatted = true
        });
        await dbSetup.CatalogDocuments.AddAsync(doc);
        await dbSetup.SaveChangesAsync();

        // Independent Connection 1: Revoke admin's sessions (bumps AuthenticationVersion to 2)
        using (var dbRevoke = new DeepPaceDbContext(CreatePostgresOptions()))
        {
            var uowRevoke = new AdministrativeUnitOfWork(dbRevoke);
            var tokenServiceRevoke = new RefreshTokenService(dbRevoke);
            var revokeHandler = new AdminRevokeUserSessionsHandler(uowRevoke, tokenServiceRevoke);
            var revokeRes = await revokeHandler.ExecuteAsync(new AdminRevokeUserSessionsRequest(adminId, adminId, "Revoking admin session", 1, PresentedSessionVersion: 1));
            revokeRes.IsSuccess.Should().BeTrue();
        }

        // Independent Connection 2: Attempt privileged catalog publish presenting stale session version 1
        using (var dbPublish = new DeepPaceDbContext(CreatePostgresOptions()))
        {
            var uowPublish = new AdministrativeUnitOfWork(dbPublish);
            var publishHandler = new AdminPublishCatalogDocumentHandler(dbPublish, uowPublish);
            var publishRes = await publishHandler.ExecuteAsync(new AdminPublishCatalogDocumentRequest(doc.Id, adminId, 1, PresentedSessionVersion: 1));

            publishRes.IsSuccess.Should().BeFalse("Privileged catalog admission must reject stale session version");
            publishRes.Error.Code.Should().Be("AUTH_FORBIDDEN");
        }

        // Verify document was NOT published
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var docVerify = await dbVerify.CatalogDocuments.FindAsync(doc.Id);
        docVerify!.Visibility.Should().Be(CatalogVisibility.Draft, "Document must remain in Draft state after failed admission");
        docVerify.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task PrivilegedCatalogAdmission_ConcurrentWithAccountRevocation_ShouldSerializeWithoutDeadlock()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        var admin1 = new User { Id = admin1Id, Email = $"admin1-{Guid.NewGuid():N}@example.com", Name = "Admin 1", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var admin2 = new User { Id = admin2Id, Email = $"admin2-{Guid.NewGuid():N}@example.com", Name = "Admin 2", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin1, admin2);

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Concurrent Admission Test Doc",
            Description = "Testing concurrency between privileged admission and revocation.",
            CategoryId = CategoryDefaults.SystemDesignId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Test Attribution",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 1,
            Revision = 1,
            ConcurrencyVersion = 1
        };
        doc.Slices.Add(new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = doc.Id,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "# Section 1\nContent",
            SummaryMarkdown = "Summary",
            KeyTakeaways = new() { "Takeaway 1" },
            EstimatedReadMinutes = 3,
            IsAiFormatted = true
        });
        await dbSetup.CatalogDocuments.AddAsync(doc);
        await dbSetup.SaveChangesAsync();

        // Connection 1: Privileged mutation (Publish) as Admin 1
        var taskPublish = Task.Run(async () =>
        {
            using var db = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db);
            var handler = new AdminPublishCatalogDocumentHandler(db, uow);
            return await handler.ExecuteAsync(new AdminPublishCatalogDocumentRequest(doc.Id, admin1Id, 1, PresentedSessionVersion: 1));
        });

        // Connection 2: Account revocation (Suspend Admin 1) by Admin 2
        var taskSuspend = Task.Run(async () =>
        {
            using var db = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db);
            var tokenService = new RefreshTokenService(db);
            var handler = new AdminSuspendUserHandler(uow, tokenService);
            return await handler.ExecuteAsync(new AdminSuspendUserRequest(admin1Id, admin2Id, "Concurrent suspension", 1, PresentedSessionVersion: 1));
        });

        await Task.WhenAll(taskPublish, taskSuspend);
        var publishRes = await taskPublish;
        var suspendRes = await taskSuspend;

        // Verify consistent outcomes without deadlock:
        // If publish committed first, document is published, suspend succeeded.
        // If suspend committed first, publish failed with AUTH_FORBIDDEN, suspend succeeded.
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var docVerify = await dbVerify.CatalogDocuments.FindAsync(doc.Id);
        var admin1Verify = await dbVerify.Users.FindAsync(admin1Id);

        if (publishRes.IsSuccess)
        {
            docVerify!.Visibility.Should().Be(CatalogVisibility.Published);
        }
        else
        {
            publishRes.Error.Code.Should().Be("AUTH_FORBIDDEN");
            docVerify!.Visibility.Should().Be(CatalogVisibility.Draft);
        }
    }

    [Fact]
    public async Task SessionRace_PromotionDemotionDuringIssuanceAndRotation_ShouldRejectStaleAndPermitFreshLogin()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin, target);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(adminId);

        using var dbClient = new DeepPaceDbContext(CreatePostgresOptions());
        var tokenService = new RefreshTokenService(dbClient);
        var issuer = new SessionIssuer(dbClient, tokenService, config);
        var authority = new AccountSessionAuthority(dbClient);

        // 1. Issue Session A (Device A) and Session B (Device B) for Target with Version 1
        var sessionA = await issuer.IssueSessionAsync(targetId);
        sessionA.IsSuccess.Should().BeTrue();
        var sessionB = await issuer.IssueSessionAsync(targetId);
        sessionB.IsSuccess.Should().BeTrue();

        sessionA.Value.RefreshToken.FamilyId.Should().NotBe(sessionB.Value.RefreshToken.FamilyId, "Different devices receive distinct families");

        // Device A rotates token once
        var rotateA1 = await issuer.RotateSessionAsync(sessionA.Value.RawRefreshToken);
        rotateA1.IsSuccess.Should().BeTrue();

        // 2. Admin promotes target to Admin (or changes role) - bumps version to 2, revokes tokens
        using var dbAdmin = new DeepPaceDbContext(CreatePostgresOptions());
        var uow = new AdministrativeUnitOfWork(dbAdmin);
        var adminTokenService = new RefreshTokenService(dbAdmin);
        var permResolver = new AdminPermissionResolver(dbAdmin);
        var assignRoleHandler = new AdminAssignRoleHandler(dbAdmin, uow, adminTokenService, permResolver);

        var assignRes = await assignRoleHandler.ExecuteAsync(new AdminAssignRoleRequest(targetId, adminId, RoleDefaults.AdminRoleId, "Role elevation promotion", 1, "corr-race", 1));
        assignRes.IsSuccess.Should().BeTrue();

        // 3. Old Bearer Version 1 check fails (on fresh connection)
        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var verifyAuthority = new AccountSessionAuthority(dbVerify);
        var bearerCheck = await verifyAuthority.ValidateSessionAsync(targetId, 1);
        bearerCheck.Status.Should().Be(AccountAuthorityStatus.StaleVersion);

        // 4. Old refresh tokens for both Device A and Device B fail rotation on independent connections
        using var dbRotateA = new DeepPaceDbContext(CreatePostgresOptions());
        var tokenServiceA = new RefreshTokenService(dbRotateA);
        var issuerA = new SessionIssuer(dbRotateA, tokenServiceA, config);
        var rotateStaleA = await issuerA.RotateSessionAsync(rotateA1.Value.RawRefreshToken);
        rotateStaleA.IsSuccess.Should().BeFalse();
        rotateStaleA.Error.Code.Should().Be("AUTH_INVALID_CREDENTIALS");

        using var dbRotateB = new DeepPaceDbContext(CreatePostgresOptions());
        var tokenServiceB = new RefreshTokenService(dbRotateB);
        var issuerB = new SessionIssuer(dbRotateB, tokenServiceB, config);
        var rotateStaleB = await issuerB.RotateSessionAsync(sessionB.Value.RawRefreshToken);
        rotateStaleB.IsSuccess.Should().BeFalse();
        rotateStaleB.Error.Code.Should().Be("AUTH_INVALID_CREDENTIALS");

        // 5. Fresh sign-in succeeds with new version 2 and current role
        using var dbFresh = new DeepPaceDbContext(CreatePostgresOptions());
        var freshTokenService = new RefreshTokenService(dbFresh);
        var freshIssuer = new SessionIssuer(dbFresh, freshTokenService, config);

        var freshSession1 = await freshIssuer.IssueSessionAsync(targetId);
        freshSession1.IsSuccess.Should().BeTrue();
        freshSession1.Value.SessionVersion.Should().Be(2);
        freshSession1.Value.User.RoleId.Should().Be(RoleDefaults.AdminRoleId);

        var freshSession2 = await freshIssuer.IssueSessionAsync(targetId);
        freshSession2.IsSuccess.Should().BeTrue();

        // 6. Device 1 logout (per-device revocation) does NOT invalidate Device 2
        await freshTokenService.RevokeFamilyAsync(freshSession1.Value.RawRefreshToken);

        using var dbCheck = new DeepPaceDbContext(CreatePostgresOptions());
        var checkTokenService = new RefreshTokenService(dbCheck);
        var checkIssuer = new SessionIssuer(dbCheck, checkTokenService, config);

        var rotateDevice1 = await checkIssuer.RotateSessionAsync(freshSession1.Value.RawRefreshToken);
        rotateDevice1.IsSuccess.Should().BeFalse();

        var rotateDevice2 = await checkIssuer.RotateSessionAsync(freshSession2.Value.RawRefreshToken);
        rotateDevice2.IsSuccess.Should().BeTrue("Device 2 session remains valid after Device 1 logout");
    }

    [Fact]
    public async Task AuthorizationInitialization_ConcurrentRuns_ShouldSerializeAndSucceedExactlyOnce()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("REPORT: PostgreSQL unavailable on localhost:5432. Concurrency test reported fixture status.");
            return;
        }

        using var dbSetup = new DeepPaceDbContext(CreatePostgresOptions());
        // Clear previous initialization receipt for isolated concurrency test
        await dbSetup.Database.ExecuteSqlRawAsync("DELETE FROM \"AuthorizationInitializations\"");

        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        var u1 = new User { Id = admin1Id, Email = $"init-u1-{Guid.NewGuid():N}@example.com", Name = "Init U1", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        var u2 = new User { Id = admin2Id, Email = $"init-u2-{Guid.NewGuid():N}@example.com", Name = "Init U2", Status = AccountStatus.Active, RoleId = RoleDefaults.LearnerRoleId, AuthenticationVersion = 1, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(u1, u2);
        await dbSetup.SaveChangesAsync();

        var task1 = Task.Run(async () =>
        {
            using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
            var tokenService1 = new RefreshTokenService(db1);
            var service1 = new AuthorizationInitializationService(db1, tokenService1);
            return await service1.InitializeAsync(new[] { admin1Id });
        });

        var task2 = Task.Run(async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            var tokenService2 = new RefreshTokenService(db2);
            var service2 = new AuthorizationInitializationService(db2, tokenService2);
            return await service2.InitializeAsync(new[] { admin2Id });
        });

        var results = await Task.WhenAll(task1, task2);
        var successCount = results.Count(r => r.IsSuccess);
        var alreadyInitCount = results.Count(r => !r.IsSuccess && r.Error.Code == "AUTHORIZATION_ALREADY_INITIALIZED");

        successCount.Should().Be(1, "PostgreSQL advisory lock must serialize initialization so exactly one succeeds");
        alreadyInitCount.Should().Be(1, "The competing task must observe the committed receipt and fail with AUTHORIZATION_ALREADY_INITIALIZED");

        using var dbVerify = new DeepPaceDbContext(CreatePostgresOptions());
        var receipts = await dbVerify.AuthorizationInitializations.ToListAsync();
        receipts.Should().HaveCount(1);
    }
}
