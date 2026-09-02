using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Insights.BookmarkInsight;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Features.Insights.GetInsightsFeed;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class TechInsightsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public TechInsightsTests()
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
    public async Task GetInsightsFeed_ShouldReturnInsightsFilteredByCategoryAndTags()
    {
        // Arrange
        await _db.TechInsights.AddRangeAsync(
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "dotnet-span-split",
                Title = "Span Split Optimization",
                Category = Category.BackendDotNet,
                Tags = new() { "csharp", "dotnet", "memory" },
                SummaryMarkdown = "Summary 1",
                ProblemSnippet = "Problem 1",
                SolutionSnippet = "Solution 1",
                UnderTheHoodMarkdown = "UnderTheHood 1",
                BenchmarkStats = "⚡ 10x",
                IsPublished = true
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "postgres-hot-updates",
                Title = "Postgres HOT Updates",
                Category = Category.DatabaseStorage,
                Tags = new() { "postgres", "mvcc", "fillfactor" },
                SummaryMarkdown = "Summary 2",
                ProblemSnippet = "Problem 2",
                SolutionSnippet = "Solution 2",
                UnderTheHoodMarkdown = "UnderTheHood 2",
                BenchmarkStats = "⚡ 5x",
                IsPublished = true
            }
        );
        await _db.SaveChangesAsync();

        var handler = new GetInsightsFeedHandler(_db);

        // Act 1: Get all
        var allResult = await handler.ExecuteAsync(new GetInsightsFeedRequest());
        allResult.IsSuccess.Should().BeTrue();
        allResult.Value.TotalCount.Should().Be(2);

        // Act 2: Filter by Category
        var dotnetResult = await handler.ExecuteAsync(new GetInsightsFeedRequest(Category: Category.BackendDotNet));
        dotnetResult.IsSuccess.Should().BeTrue();
        dotnetResult.Value.Insights.Should().HaveCount(1);
        dotnetResult.Value.Insights[0].Slug.Should().Be("dotnet-span-split");

        // Act 3: Filter by Tag
        var tagResult = await handler.ExecuteAsync(new GetInsightsFeedRequest(Tag: "fillfactor"));
        tagResult.IsSuccess.Should().BeTrue();
        tagResult.Value.Insights.Should().HaveCount(1);
        tagResult.Value.Insights[0].Slug.Should().Be("postgres-hot-updates");
    }

    [Fact]
    public async Task BookmarkInsight_ShouldToggleBookmarkOnAndOff()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@techdaily.io",
            Name = "Test Engineer",
            CreatedAt = DateTime.UtcNow
        };
        var insight = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "dotnet-channels",
            Title = "Dotnet Channels",
            Category = Category.BackendDotNet,
            Tags = new() { "channels", "concurrency" },
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Problem",
            SolutionSnippet = "Solution",
            UnderTheHoodMarkdown = "UnderTheHood",
            BenchmarkStats = "⚡ 4M ops",
            BookmarksCount = 0,
            IsPublished = true
        };
        await _db.Users.AddAsync(user);
        await _db.TechInsights.AddAsync(insight);
        await _db.SaveChangesAsync();

        var handler = new BookmarkInsightHandler(_db);

        // Act 1: Toggle ON (Bookmark)
        var result1 = await handler.ExecuteAsync(new BookmarkInsightRequest(insight.Id, user.Id));

        // Assert 1
        result1.IsSuccess.Should().BeTrue();
        result1.Value.IsBookmarked.Should().BeTrue();
        result1.Value.TotalBookmarks.Should().Be(1);

        var countInDb = await _db.UserInsightBookmarks.CountAsync(b => b.UserId == user.Id && b.InsightId == insight.Id);
        countInDb.Should().Be(1);

        // Act 2: Toggle OFF (Unbookmark)
        var result2 = await handler.ExecuteAsync(new BookmarkInsightRequest(insight.Id, user.Id));

        // Assert 2
        result2.IsSuccess.Should().BeTrue();
        result2.Value.IsBookmarked.Should().BeFalse();
        result2.Value.TotalBookmarks.Should().Be(0);

        var countAfterUnbookmark = await _db.UserInsightBookmarks.CountAsync(b => b.UserId == user.Id && b.InsightId == insight.Id);
        countAfterUnbookmark.Should().Be(0);
    }

    [Fact]
    public async Task GetInsightsFeed_ShouldResolveIsBookmarkedByUserAndFilterByOnlyBookmarked()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "dev@techdaily.io",
            Name = "Dev",
            CreatedAt = DateTime.UtcNow
        };
        var ins1 = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "card-1",
            Title = "Card 1",
            Category = Category.BackendDotNet,
            Tags = new() { "csharp" },
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Problem",
            SolutionSnippet = "Solution",
            UnderTheHoodMarkdown = "Internals",
            IsPublished = true
        };
        var ins2 = new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = "card-2",
            Title = "Card 2",
            Category = Category.DatabaseStorage,
            Tags = new() { "sql" },
            SummaryMarkdown = "Summary",
            ProblemSnippet = "Problem",
            SolutionSnippet = "Solution",
            UnderTheHoodMarkdown = "Internals",
            IsPublished = true
        };
        await _db.Users.AddAsync(user);
        await _db.TechInsights.AddRangeAsync(ins1, ins2);
        await _db.UserInsightBookmarks.AddAsync(new UserInsightBookmark
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            InsightId = ins1.Id,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var feedHandler = new GetInsightsFeedHandler(_db);

        // Act 1: Get feed for user
        var feedResponse = await feedHandler.ExecuteAsync(new GetInsightsFeedRequest(UserId: user.Id));
        feedResponse.IsSuccess.Should().BeTrue();
        feedResponse.Value.Insights.Should().HaveCount(2);

        var dto1 = feedResponse.Value.Insights.First(i => i.Id == ins1.Id);
        var dto2 = feedResponse.Value.Insights.First(i => i.Id == ins2.Id);
        dto1.IsBookmarkedByUser.Should().BeTrue();
        dto2.IsBookmarkedByUser.Should().BeFalse();

        // Act 2: Filter only bookmarked
        var onlyBookmarkedResponse = await feedHandler.ExecuteAsync(new GetInsightsFeedRequest(UserId: user.Id, OnlyBookmarked: true));
        onlyBookmarkedResponse.IsSuccess.Should().BeTrue();
        onlyBookmarkedResponse.Value.Insights.Should().HaveCount(1);
        onlyBookmarkedResponse.Value.Insights[0].Id.Should().Be(ins1.Id);
    }
}
