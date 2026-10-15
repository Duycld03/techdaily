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

    private static IConfiguration CreateConfiguration(params string[] userIds)
    {
        var dict = new Dictionary<string, string?>();
        for (int i = 0; i < userIds.Length; i++)
        {
            dict[$"CatalogAdministration:UserIds:{i}"] = userIds[i];
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task GetCapabilities_EmptyConfiguration_ShouldReturnAllFalse()
    {
        var config = CreateConfiguration();
        var resolver = new AdminPermissionResolver(config, _db);

        var result = await resolver.GetCapabilitiesAsync(Guid.NewGuid());

        result.CanManageCatalog.Should().BeFalse();
        result.CanManageAccounts.Should().BeFalse();
        result.CanReadAudit.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_MalformedAndDuplicateConfig_ShouldParseValidOnly()
    {
        var validAdminId = Guid.NewGuid();
        var user = new User
        {
            Id = validAdminId,
            Email = "admin@example.com",
            Name = "Admin",
            Status = AccountStatus.Active
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var config = CreateConfiguration("not-a-guid", validAdminId.ToString(), validAdminId.ToString());
        var resolver = new AdminPermissionResolver(config, _db);

        var result = await resolver.GetCapabilitiesAsync(validAdminId);

        result.CanManageCatalog.Should().BeTrue();
        result.CanManageAccounts.Should().BeTrue();
        result.CanReadAudit.Should().BeTrue();
    }

    [Fact]
    public async Task GetCapabilities_LearnerUser_ShouldReturnAllFalse()
    {
        var adminId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var learner = new User
        {
            Id = learnerId,
            Email = "learner@example.com",
            Name = "Learner",
            Status = AccountStatus.Active
        };
        await _db.Users.AddAsync(learner);
        await _db.SaveChangesAsync();

        var config = CreateConfiguration(adminId.ToString());
        var resolver = new AdminPermissionResolver(config, _db);

        var result = await resolver.GetCapabilitiesAsync(learnerId);

        result.CanManageCatalog.Should().BeFalse();
        result.CanManageAccounts.Should().BeFalse();
        result.CanReadAudit.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_SuspendedAdmin_ShouldReturnAllFalse()
    {
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Suspended Admin",
            Status = AccountStatus.Suspended
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var config = CreateConfiguration(adminId.ToString());
        var resolver = new AdminPermissionResolver(config, _db);

        var result = await resolver.GetCapabilitiesAsync(adminId);

        result.CanManageCatalog.Should().BeFalse();
        result.CanManageAccounts.Should().BeFalse();
        result.CanReadAudit.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilities_SoftDeletedAdmin_ShouldReturnAllFalse()
    {
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Deleted Admin",
            Status = AccountStatus.Active,
            IsDeleted = true
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var config = CreateConfiguration(adminId.ToString());
        var resolver = new AdminPermissionResolver(config, _db);

        var result = await resolver.GetCapabilitiesAsync(adminId);

        result.CanManageCatalog.Should().BeFalse();
        result.CanManageAccounts.Should().BeFalse();
        result.CanReadAudit.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermission_ConfiguredActiveAdmin_ShouldReturnTrueForSupportedPermissions()
    {
        var adminId = Guid.NewGuid();
        var user = new User
        {
            Id = adminId,
            Email = "admin@example.com",
            Name = "Active Admin",
            Status = AccountStatus.Active
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var config = CreateConfiguration(adminId.ToString());
        var resolver = new AdminPermissionResolver(config, _db);

        var canManageCatalog = await resolver.HasPermissionAsync(adminId, AdminPermission.ManageCatalog);
        var canManageAccounts = await resolver.HasPermissionAsync(adminId, AdminPermission.ManageAccounts);
        var canReadAudit = await resolver.HasPermissionAsync(adminId, AdminPermission.ReadAudit);

        canManageCatalog.Should().BeTrue();
        canManageAccounts.Should().BeTrue();
        canReadAudit.Should().BeTrue();
    }
}
