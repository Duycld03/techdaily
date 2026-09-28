using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _options;
    private readonly Guid _userId = Guid.NewGuid();

    public RefreshTokenServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TechDailyDbContext>().UseSqlite(_connection).Options;
        using var db = new TechDailyDbContext(_options);
        db.Database.EnsureCreated();

        // Seed user
        var user = new User
        {
            Id = _userId,
            Email = "refresh_test@techdaily.local",
            Name = "Refresh Tester",
            PasswordHash = "mock-hash",
            PreferredLocale = "en"
        };
        db.Users.Add(user);
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private RefreshTokenService NewService(TechDailyDbContext? db = null)
    {
        return new RefreshTokenService(db ?? new TechDailyDbContext(_options));
    }

    [Fact]
    public async Task RotateTokenAsync_Within60SecondsGraceWindow_ReturnsSuccessor()
    {
        using var db = new TechDailyDbContext(_options);
        var svc = NewService(db);

        // 1. Issue initial token
        var (rawToken1, token1) = await svc.IssueTokenAsync(_userId);

        // 2. Rotate token first time (creates successor token2)
        var rotate1 = await svc.RotateTokenAsync(rawToken1);
        rotate1.IsSuccess.Should().BeTrue();
        var (_, token2, _) = rotate1.Value;

        // 3. Simulate 30 seconds elapsed (within 60-second grace window)
        var updatedToken1 = await db.RefreshTokens.FindAsync(token1.Id);
        updatedToken1!.UsedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
        await db.SaveChangesAsync();

        // 4. Concurrent tab rotates with the same old rawToken1
        var rotate2 = await svc.RotateTokenAsync(rawToken1);
        rotate2.IsSuccess.Should().BeTrue();
        rotate2.Value.NewToken.Id.Should().Be(token2.Id);

        // Verify family was NOT revoked
        var familyTokens = await db.RefreshTokens.Where(t => t.FamilyId == token1.FamilyId).ToListAsync();
        familyTokens.Should().OnlyContain(t => t.RevokedAt == null);
    }

    [Fact]
    public async Task RotateTokenAsync_Outside60SecondsGraceWindow_RevokesEntireFamily()
    {
        using var db = new TechDailyDbContext(_options);
        var svc = NewService(db);

        // 1. Issue initial token
        var (rawToken1, token1) = await svc.IssueTokenAsync(_userId);

        // 2. Rotate token first time
        var rotate1 = await svc.RotateTokenAsync(rawToken1);
        rotate1.IsSuccess.Should().BeTrue();

        // 3. Simulate 65 seconds elapsed (outside 60-second grace window)
        var updatedToken1 = await db.RefreshTokens.FindAsync(token1.Id);
        updatedToken1!.UsedAt = DateTimeOffset.UtcNow.AddSeconds(-65);
        await db.SaveChangesAsync();

        // 4. Token reuse attempt with old rawToken1
        var rotate2 = await svc.RotateTokenAsync(rawToken1);
        rotate2.IsFailure.Should().BeTrue();
        rotate2.Error.Code.Should().Be("AUTH_TOKEN_REUSE_DETECTED");

        // Verify entire family is now revoked
        var familyTokens = await db.RefreshTokens.Where(t => t.FamilyId == token1.FamilyId).ToListAsync();
        familyTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }
}
