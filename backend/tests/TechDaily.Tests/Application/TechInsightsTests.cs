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
    public async Task BookmarkInsight_ShouldIncrementBookmarkCount()
    {
        // Arrange
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
        await _db.TechInsights.AddAsync(insight);
        await _db.SaveChangesAsync();

        var handler = new BookmarkInsightHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new BookmarkInsightRequest(insight.Id, Guid.NewGuid()));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsBookmarked.Should().BeTrue();
        result.Value.TotalBookmarks.Should().Be(1);

        var updated = await _db.TechInsights.FindAsync(insight.Id);
        updated!.BookmarksCount.Should().Be(1);
    }
}
