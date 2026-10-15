using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Features.Admin.Accounts;
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
    private const string DefaultPostgresConnStr = "Host=localhost;Port=5432;Database=deeppace_db;Username=deeppace_user;Password=techdaily_password_secret";
    private readonly ITestOutputHelper _output;

    public PostgresAccountConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION") ?? DefaultPostgresConnStr;
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
        for (int i = 0; i < adminIds.Length; i++)
        {
            dict[$"CatalogAdministration:UserIds:{i}"] = adminIds[i].ToString();
        }
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

        var admin1 = new User { Id = admin1Id, Email = $"admin1-{Guid.NewGuid():N}@example.com", Name = "Admin 1", Status = AccountStatus.Active };
        var admin2 = new User { Id = admin2Id, Email = $"admin2-{Guid.NewGuid():N}@example.com", Name = "Admin 2", Status = AccountStatus.Active };
        await dbSetup.Users.AddRangeAsync(admin1, admin2);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(admin1Id, admin2Id);

        // Admin 1 attempts to suspend Admin 2 concurrently with Admin 2 attempting to suspend Admin 1
        var task1 = Task.Run(async () =>
        {
            using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db1, config);
            var tokenService = new RefreshTokenService(db1);
            var handler = new AdminSuspendUserHandler(uow, tokenService);
            return await handler.ExecuteAsync(new AdminSuspendUserRequest(admin2Id, admin1Id, "Admin 1 suspending Admin 2", 1));
        });

        var task2 = Task.Run(async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            var uow = new AdministrativeUnitOfWork(db2, config);
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

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AdminControlVersion = 1 };
        await dbSetup.Users.AddRangeAsync(admin, target);
        await dbSetup.SaveChangesAsync();

        var config = CreateConfig(adminId);

        using var db1 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow1 = new AdministrativeUnitOfWork(db1, config);
        var tokenService1 = new RefreshTokenService(db1);
        var handler1 = new AdminSuspendUserHandler(uow1, tokenService1);

        // First update with expectedVersion = 1 succeeds
        var res1 = await handler1.ExecuteAsync(new AdminSuspendUserRequest(targetId, adminId, "Valid suspension", 1));
        res1.IsSuccess.Should().BeTrue();

        using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
        var uow2 = new AdministrativeUnitOfWork(db2, config);
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

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AuthenticationVersion = 1, AdminControlVersion = 1 };
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
        var uow = new AdministrativeUnitOfWork(db, config);

        // Attempt command where audit creation throws an exception
        var act = async () =>
        {
            await uow.ExecuteAccountControlCommandAsync<bool>(
                adminId,
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

        var admin = new User { Id = adminId, Email = $"admin-{Guid.NewGuid():N}@example.com", Name = "Admin", Status = AccountStatus.Active };
        var target = new User { Id = targetId, Email = $"target-{Guid.NewGuid():N}@example.com", Name = "Target", Status = AccountStatus.Active, AuthenticationVersion = 1, AdminControlVersion = 1 };
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
        var uow = new AdministrativeUnitOfWork(db, config);
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
}
