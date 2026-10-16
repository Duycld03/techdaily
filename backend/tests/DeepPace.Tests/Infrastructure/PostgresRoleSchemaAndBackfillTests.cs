using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace DeepPace.Tests.Infrastructure;

public class PostgresRoleSchemaAndBackfillTests
{
    private static string? PostgresConnStr => Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
    private readonly ITestOutputHelper _output;

    public PostgresRoleSchemaAndBackfillTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static bool CheckPostgresConnection()
    {
        if (string.IsNullOrWhiteSpace(PostgresConnStr))
        {
            return false;
        }

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
        catch
        {
            return false;
        }
    }

    private static DbContextOptions<DeepPaceDbContext> CreatePostgresOptions()
    {
        return new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseNpgsql(PostgresConnStr ?? "Host=localhost;Database=deeppace_empty;Username=none", o => o.UseVector())
            .Options;
    }

    [Fact]
    public async Task DefaultSeed_ShouldContainFixedLearnerAndAdminRolesWithSevenGrants()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("PostgreSQL unavailable on localhost:5432. Skipped.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());

        var learner = await db.Roles.FindAsync(RoleDefaults.LearnerRoleId);
        learner.Should().NotBeNull();
        learner!.Key.Should().Be("learner");
        learner.IsProtected.Should().BeTrue();

        var admin = await db.Roles.FindAsync(RoleDefaults.AdminRoleId);
        admin.Should().NotBeNull();
        admin!.Key.Should().Be("admin");
        admin.IsProtected.Should().BeTrue();

        var adminGrants = await db.RolePermissions
            .Where(rp => rp.RoleId == RoleDefaults.AdminRoleId)
            .Select(rp => rp.PermissionKey)
            .ToListAsync();

        adminGrants.Should().HaveCount(7);
        adminGrants.Should().Contain(new[]
        {
            PermissionKeys.ManageAccounts,
            PermissionKeys.ManageCatalog,
            PermissionKeys.ManageDocumentOperations,
            PermissionKeys.ManageRoles,
            PermissionKeys.ReadAudit,
            PermissionKeys.ViewAdminDashboard,
            PermissionKeys.ManageCategories
        });
        var learnerGrants = await db.RolePermissions
            .Where(rp => rp.RoleId == RoleDefaults.LearnerRoleId)
            .ToListAsync();
        learnerGrants.Should().BeEmpty();
    }

    [Fact]
    public async Task Schema_ShouldEnforceUniqueLiveRoleKey()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("PostgreSQL unavailable on localhost:5432. Skipped.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var uniqueKey = $"role_{Guid.NewGuid():N}";

        var role1 = Role.Create(uniqueKey, "Role 1", "Vai trò 1");
        await db.Roles.AddAsync(role1);
        await db.SaveChangesAsync();

        // Attempting to add duplicate live role key should fail
        var role2 = Role.Create(uniqueKey, "Role 2", "Vai trò 2");
        await db.Roles.AddAsync(role2);

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();

        // Clean up
        db.Entry(role2).State = EntityState.Detached;
        role1.IsDeleted = true;
        await db.SaveChangesAsync();

        // Adding role with same key as soft-deleted role should succeed
        var role3 = Role.Create(uniqueKey, "Role 3", "Vai trò 3");
        await db.Roles.AddAsync(role3);
        await db.SaveChangesAsync();

        // Clean up
        role3.IsDeleted = true;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Schema_ShouldEnforceCompositePrimaryKeyOnRolePermissions()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("PostgreSQL unavailable on localhost:5432. Skipped.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var testRoleId = Guid.NewGuid();
        var testRole = Role.Create($"test_perm_{Guid.NewGuid():N}", "Test Perm", "Kiểm tra");
        testRole.Id = testRoleId;
        await db.Roles.AddAsync(testRole);

        var perm1 = RolePermission.Create(testRoleId, PermissionKeys.ReadAudit);
        await db.RolePermissions.AddAsync(perm1);
        await db.SaveChangesAsync();

        // Attempting to add duplicate composite key directly into PostgreSQL
        var act = async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            await db2.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"RolePermissions\" (\"RoleId\", \"PermissionKey\") VALUES ({0}, {1})",
                testRoleId, PermissionKeys.ReadAudit);
        };
        await act.Should().ThrowAsync<PostgresException>();

        // Clean up
        db.RolePermissions.Remove(perm1);
        db.Roles.Remove(testRole);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Schema_ShouldEnforceForeignKeyConstraint_OnUsersRoleId()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("PostgreSQL unavailable on localhost:5432. Skipped.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var nonExistentRoleId = Guid.NewGuid();

        var invalidUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"invalidfk_{Guid.NewGuid():N}@example.com",
            Name = "Invalid FK",
            RoleId = nonExistentRoleId
        };

        await db.Users.AddAsync(invalidUser);
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Schema_ShouldRestrictDeletionOfReferencedRole()
    {
        if (!CheckPostgresConnection())
        {
            _output.WriteLine("PostgreSQL unavailable on localhost:5432. Skipped.");
            return;
        }

        using var db = new DeepPaceDbContext(CreatePostgresOptions());
        var roleId = Guid.NewGuid();
        var role = Role.Create($"ref_role_{Guid.NewGuid():N}", "Ref Role", "Vai trò tham chiếu");
        role.Id = roleId;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"refuser_{Guid.NewGuid():N}@example.com",
            Name = "Ref User",
            RoleId = roleId
        };

        await db.Roles.AddAsync(role);
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        // Hard deleting referenced role should be restricted by FK in PostgreSQL
        var act = async () =>
        {
            using var db2 = new DeepPaceDbContext(CreatePostgresOptions());
            await db2.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Roles\" WHERE \"Id\" = {0}", roleId);
        };
        await act.Should().ThrowAsync<PostgresException>();
        // Clean up: detach role and remove user first
        db.Entry(role).State = EntityState.Unchanged;
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        db.Roles.Remove(role);
        await db.SaveChangesAsync();
    }
}
