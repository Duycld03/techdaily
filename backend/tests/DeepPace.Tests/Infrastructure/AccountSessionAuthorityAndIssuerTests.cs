using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class AccountSessionAuthorityAndIssuerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly IConfiguration _config;
    private readonly RefreshTokenService _tokenService;
    private readonly SessionIssuer _issuer;
    private readonly AccountSessionAuthority _authority;

    public AccountSessionAuthorityAndIssuerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "super-secret-key-that-has-at-least-32-characters-entropy!",
            ["Jwt:Issuer"] = "DeepPaceTest",
            ["Jwt:Audience"] = "DeepPaceUsersTest",
            ["Jwt:ExpiryMinutes"] = "60"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        _tokenService = new RefreshTokenService(_db);
        _issuer = new SessionIssuer(_db, _tokenService, _config);
        _authority = new AccountSessionAuthority(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ValidateSession_ActiveAndMatchingVersion_ShouldReturnValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "active@example.com",
            Name = "Active User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 2
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var result = await _authority.ValidateSessionAsync(user.Id, 2);

        result.Status.Should().Be(AccountAuthorityStatus.Valid);
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateSession_SuspendedAccount_ShouldReturnSuspended()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspended@example.com",
            Name = "Suspended User",
            Status = AccountStatus.Suspended,
            AuthenticationVersion = 2
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var result = await _authority.ValidateSessionAsync(user.Id, 2);

        result.Status.Should().Be(AccountAuthorityStatus.Suspended);
    }

    [Fact]
    public async Task ValidateSession_StaleAuthenticationVersion_ShouldReturnStaleVersion()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "revoked@example.com",
            Name = "Revoked User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 3
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // Token has older version 2
        var result = await _authority.ValidateSessionAsync(user.Id, 2);

        result.Status.Should().Be(AccountAuthorityStatus.StaleVersion);
    }

    [Fact]
    public async Task IssueSession_ShouldCreateRefreshTokenWithUserAuthVersion()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Name = "User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 4
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var sessionResult = await _issuer.IssueSessionAsync(user.Id);

        sessionResult.IsSuccess.Should().BeTrue();
        var session = sessionResult.Value;
        session.SessionVersion.Should().Be(4);
        session.RefreshToken.AuthenticationVersion.Should().Be(4);
        session.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RotateSession_SuspendedAccount_ShouldReturnForbiddenError()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspend-rot@example.com",
            Name = "User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var issueRes = await _issuer.IssueSessionAsync(user.Id);
        var rawToken = issueRes.Value.RawRefreshToken;

        // Suspend user
        user.Status = AccountStatus.Suspended;
        user.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        var rotateRes = await _issuer.RotateSessionAsync(rawToken);

        rotateRes.IsSuccess.Should().BeFalse();
        rotateRes.Error.Code.Should().Be("AUTH_ACCOUNT_SUSPENDED");
    }

    [Fact]
    public async Task RotateSession_RevokedSessionVersion_ShouldReturnInvalidCredentials()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "revoked-rot@example.com",
            Name = "User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var issueRes = await _issuer.IssueSessionAsync(user.Id);
        var rawToken = issueRes.Value.RawRefreshToken;

        // Forced sign-out increments AuthenticationVersion without changing status
        user.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        var rotateRes = await _issuer.RotateSessionAsync(rawToken);

        rotateRes.IsSuccess.Should().BeFalse();
        rotateRes.Error.Code.Should().Be("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task AuditEvent_PersistenceGuard_UpdateShouldThrowInvalidOperationException()
    {
        var evt = AdminAuditEvent.Create(
            actorUserId: Guid.NewGuid(),
            action: AdminAuditActions.UserSuspend,
            targetType: "User",
            targetId: Guid.NewGuid().ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Initial reason");

        await _db.AdminAuditEvents.AddAsync(evt);
        await _db.SaveChangesAsync();

        // Attempt tracked update by modifying entry state
        _db.Entry(evt).State = EntityState.Modified;

        var act = () => _db.SaveChanges();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task AuditEvent_PersistenceGuard_DeleteShouldThrowInvalidOperationException()
    {
        var evt = AdminAuditEvent.Create(
            actorUserId: Guid.NewGuid(),
            action: AdminAuditActions.UserSuspend,
            targetType: "User",
            targetId: Guid.NewGuid().ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Initial reason");

        await _db.AdminAuditEvents.AddAsync(evt);
        await _db.SaveChangesAsync();

        _db.AdminAuditEvents.Remove(evt);

        var act = () => _db.SaveChanges();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task AuditEvent_ShouldRetainHistoricalIdsAfterUserDeletion()
    {
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var actor = new User { Id = actorId, Email = "actor@example.com", Name = "Actor" };
        var target = new User { Id = targetId, Email = "target@example.com", Name = "Target" };
        await _db.Users.AddRangeAsync(actor, target);

        var evt = AdminAuditEvent.Create(
            actorUserId: actorId,
            action: AdminAuditActions.UserSuspend,
            targetType: "User",
            targetId: targetId.ToString(),
            outcome: AdminAuditOutcome.Succeeded,
            reason: "Account audit preservation test");

        await _db.AdminAuditEvents.AddAsync(evt);
        await _db.SaveChangesAsync();

        // Soft delete actor and target
        actor.SoftDelete();
        target.SoftDelete();
        await _db.SaveChangesAsync();

        // Audit events are not filtered by soft delete
        var persistedEvt = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.Id == evt.Id);
        persistedEvt.Should().NotBeNull();
        persistedEvt!.ActorUserId.Should().Be(actorId);
        persistedEvt.TargetId.Should().Be(targetId.ToString());
    }
}
