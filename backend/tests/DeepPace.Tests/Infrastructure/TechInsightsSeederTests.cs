using DeepPace.Domain.Entities;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Persistence.Seeders;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class TechInsightsSeederTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(999)]
    public void LegacySeedCategory_RejectsUnknownOrdinals(int ordinal)
    {
        var converter = typeof(TechInsightsSeeder).GetMethod("ResolveCategoryUuid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        Action convert = () => converter.Invoke(null, new object[] { ordinal });

        convert.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<InvalidDataException>();
    }

    public TechInsightsSeederTests()
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

    [Fact]
    public async Task SeedAsync_PopulatesAllSevenCategoriesAndIsIdempotent()
    {
        // Act - First seeding run
        await TechInsightsSeeder.SeedAsync(_db);

        // Assert
        var insights = await _db.TechInsights.AsNoTracking().ToListAsync();
        insights.Should().NotBeEmpty();

        // Verify all 7 categories (0 through 6) have at least one insight
        var categories = insights.Select(i => i.CategoryId).Distinct().ToList();
        categories.Should().Contain(new[] {
            CategoryDefaults.FrontendWebId, CategoryDefaults.BackendRuntimeId,
            CategoryDefaults.DatabaseStorageId, CategoryDefaults.SystemDesignId,
            CategoryDefaults.EngineeringCraftId, CategoryDefaults.MentalModelsId,
            CategoryDefaults.HabitsProductivityId });

        var initialCount = insights.Count;

        // Act - Second seeding run (Idempotence)
        await TechInsightsSeeder.SeedAsync(_db);

        // Assert
        var secondRunCount = await _db.TechInsights.CountAsync();
        secondRunCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task SeedAsync_PreservesExistingSeedIdentityAndEditedContent()
    {
        await TechInsightsSeeder.SeedAsync(_db);
        var insight = await _db.TechInsights.FirstAsync();
        var id = insight.Id;
        insight.Title = "Administrator edited title";
        insight.CategoryId = CategoryDefaults.EngineeringCraftId;
        insight.IsDeleted = true;
        await _db.SaveChangesAsync();

        await TechInsightsSeeder.SeedAsync(_db);

        var persisted = await _db.TechInsights.IgnoreQueryFilters().SingleAsync(i => i.Id == id);
        persisted.Title.Should().Be("Administrator edited title");
        persisted.CategoryId.Should().Be(CategoryDefaults.EngineeringCraftId);
        persisted.IsDeleted.Should().BeTrue();
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
            Email = "bookmark_tester@deeppace.test",
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
