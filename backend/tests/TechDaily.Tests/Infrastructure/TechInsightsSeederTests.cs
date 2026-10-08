using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class TechInsightsSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public TechInsightsSeederTests()
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
    public async Task SeedAsync_PopulatesAllSevenCategoriesAndIsIdempotent()
    {
        // Act - First seeding run
        await TechInsightsSeeder.SeedAsync(_db);

        // Assert
        var insights = await _db.TechInsights.AsNoTracking().ToListAsync();
        insights.Should().NotBeEmpty();

        // Verify all 7 categories (0 through 6) have at least one insight
        var categories = insights.Select(i => (int)i.Category).Distinct().ToList();
        categories.Should().Contain(new[] { 0, 1, 2, 3, 4, 5, 6 });

        var initialCount = insights.Count;

        // Act - Second seeding run (Idempotence)
        await TechInsightsSeeder.SeedAsync(_db);

        // Assert
        var secondRunCount = await _db.TechInsights.CountAsync();
        secondRunCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task SeedAsync_PreservesExistingUserBookmarksOnReseed()
    {
        // Arrange
        await TechInsightsSeeder.SeedAsync(_db);
        var firstInsight = await _db.TechInsights.FirstAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "bookmark_tester@techdaily.local",
            PasswordHash = "hash",
            Name = "Bookmark Tester",
            PreferredLocale = "en"
        };
        await _db.Users.AddAsync(user);

        var bookmark = new UserInsightBookmark
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            InsightId = firstInsight.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _db.UserInsightBookmarks.AddAsync(bookmark);
        await _db.SaveChangesAsync();

        // Act - Re-seed catalog
        await TechInsightsSeeder.SeedAsync(_db);

        // Assert
        var userBookmark = await _db.UserInsightBookmarks.FirstOrDefaultAsync(b => b.UserId == user.Id);
        userBookmark.Should().NotBeNull();
        userBookmark!.InsightId.Should().Be(firstInsight.Id);
    }
}
