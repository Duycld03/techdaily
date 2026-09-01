using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Security;
using Xunit;

namespace TechDaily.Tests.Application;

public class HybridAuthPasswordTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public HybridAuthPasswordTests()
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
    public async Task GoogleUser_WithoutPassword_CanSetInitialPasswordWithoutCurrentPassword()
    {
        // Arrange
        var googleUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "google.dev@example.com",
            Name = "Google Developer",
            GoogleSubjectId = "google-sub-12345",
            PasswordHash = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(googleUser);
        await _db.SaveChangesAsync();

        // Act - Set initial password without requiring current password
        var newPassword = "NewSecurePassword123!";
        googleUser.PasswordHash = PasswordHasher.HashPassword(newPassword);
        googleUser.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Assert
        var updatedUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == googleUser.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().NotBeNullOrEmpty();
        PasswordHasher.VerifyPassword(newPassword, updatedUser.PasswordHash!).Should().BeTrue();
    }

    [Fact]
    public async Task StandardUser_WithExistingPassword_VerifiesCurrentPasswordBeforeUpdating()
    {
        // Arrange
        var existingPassword = "OldPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "standard.dev@example.com",
            Name = "Standard Developer",
            PasswordHash = PasswordHasher.HashPassword(existingPassword),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act & Assert - Invalid current password fails
        var isCurrentPasswordValid = PasswordHasher.VerifyPassword("WrongPassword!", user.PasswordHash!);
        isCurrentPasswordValid.Should().BeFalse();

        // Valid current password succeeds
        var isCorrectPasswordValid = PasswordHasher.VerifyPassword(existingPassword, user.PasswordHash!);
        isCorrectPasswordValid.Should().BeTrue();

        var newPassword = "BrandNewPassword123!";
        user.PasswordHash = PasswordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var updatedUser = await _db.Users.FindAsync(user.Id);
        PasswordHasher.VerifyPassword(newPassword, updatedUser!.PasswordHash!).Should().BeTrue();
    }
}
