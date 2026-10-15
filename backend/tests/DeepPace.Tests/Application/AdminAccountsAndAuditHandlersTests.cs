using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _config;
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

        var dict = new Dictionary<string, string?>
        {
            ["CatalogAdministration:UserIds:0"] = _adminUserId.ToString()
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        _uow = new AdministrativeUnitOfWork(_db, _config);
        _tokenService = new RefreshTokenService(_db);

        // Seed the administrator
        var admin = new User
        {
            Id = _adminUserId,
            Email = "admin@example.com",
            Name = "Admin User",
            Status = AccountStatus.Active
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
        var activeUser = new User { Id = Guid.NewGuid(), Email = "active@example.com", Name = "Active", Status = AccountStatus.Active };
        var suspendedUser = new User { Id = Guid.NewGuid(), Email = "suspended@example.com", Name = "Suspended", Status = AccountStatus.Suspended };
        var deletedUser = new User { Id = Guid.NewGuid(), Email = "deleted@example.com", Name = "Deleted", Status = AccountStatus.Active, IsDeleted = true };
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
}
