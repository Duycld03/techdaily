using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;
using TechDaily.Infrastructure.Security;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class E2EAccountSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public E2EAccountSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SeedAsync_WhenUserDoesNotExist_CreatesUserWithPbkdf2Hash()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "E2E_PROD_EMAIL", "test_seeder@techdaily.local" },
            { "E2E_PROD_PASSWORD", "SecretPassword123!" }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        await E2EAccountSeeder.SeedAsync(_db, config, NullLogger.Instance);

        // Assert
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "test_seeder@techdaily.local");
        user.Should().NotBeNull();
        user!.Email.Should().Be("test_seeder@techdaily.local");
        PasswordHasher.VerifyPassword("SecretPassword123!", user.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenUserExistsWithDifferentPassword_UpdatesPasswordHash()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "E2E_PROD_EMAIL", "test_seeder@techdaily.local" },
            { "E2E_PROD_PASSWORD", "NewRotatedPassword2026!" }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Seed old password first
        var oldConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "E2E_PROD_EMAIL", "test_seeder@techdaily.local" },
                { "E2E_PROD_PASSWORD", "OldPassword1!" }
            })
            .Build();
        await E2EAccountSeeder.SeedAsync(_db, oldConfig, NullLogger.Instance);

        // Act: re-seed with new password
        await E2EAccountSeeder.SeedAsync(_db, config, NullLogger.Instance);

        // Assert
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "test_seeder@techdaily.local");
        user.Should().NotBeNull();
        PasswordHasher.VerifyPassword("NewRotatedPassword2026!", user!.PasswordHash!).Should().BeTrue();
        PasswordHasher.VerifyPassword("OldPassword1!", user.PasswordHash!).Should().BeFalse();
    }
}
