using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Features.Insights.GetInsightsMeta;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetInsightsMetaHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly GetInsightsMetaHandler _handler;

    public GetInsightsMetaHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _handler = new GetInsightsMetaHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetInsightsMeta_WhenDatabaseEmpty_ReturnsAllFourCategoriesWithZeroCountAndDefaultTopics()
    {
        // Act
        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.Categories.Should().HaveCount(4);

        var cat0 = response.Categories.First(c => c.Id == 0);
        cat0.Key.Should().Be("frontend");
        cat0.LabelEn.Should().Be("Frontend & Web Architecture");
        cat0.LabelVi.Should().Be("Frontend & Trình Duyệt");
        cat0.Count.Should().Be(0);

        var cat1 = response.Categories.First(c => c.Id == 1);
        cat1.Key.Should().Be("dotnet");
        cat1.LabelEn.Should().Be(".NET 10 & C# 13");
        cat1.LabelVi.Should().Be("Nền Tảng .NET & C#");
        cat1.Count.Should().Be(0);

        var cat2 = response.Categories.First(c => c.Id == 2);
        cat2.Key.Should().Be("database");
        cat2.LabelEn.Should().Be("PostgreSQL & Database");
        cat2.LabelVi.Should().Be("Postgres & Cơ Sở Dữ Liệu");
        cat2.Count.Should().Be(0);

        var cat3 = response.Categories.First(c => c.Id == 3);
        cat3.Key.Should().Be("system_design");
        cat3.LabelEn.Should().Be("Distributed Systems & Architecture");
        cat3.LabelVi.Should().Be("Thiết Kế Hệ Thống");
        cat3.Count.Should().Be(0);

        // Suggested topics should contain all 4 categories with defaults
        response.SuggestedTopics.Should().ContainKey(0);
        response.SuggestedTopics.Should().ContainKey(1);
        response.SuggestedTopics.Should().ContainKey(2);
        response.SuggestedTopics.Should().ContainKey(3);

        response.SuggestedTopics[0].Should().Contain("Vue 3 shallowRef vs reactive");
        response.SuggestedTopics[1].Should().Contain("Kestrel Socket Pipeline");
        response.SuggestedTopics[2].Should().Contain("PostgreSQL Index-Only Scan & INCLUDE");
        response.SuggestedTopics[3].Should().Contain("Transactional Outbox & CDC");
    }

    [Fact]
    public async Task GetInsightsMeta_AggregatesPublishedCountAndCuratesTopics()
    {
        // Arrange
        // Add published and unpublished insights
        await _db.TechInsights.AddRangeAsync(
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-1",
                Title = "Insight 1",
                Category = Category.BackendDotNet,
                IsPublished = true
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-2",
                Title = "Insight 2",
                Category = Category.BackendDotNet,
                IsPublished = true
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-3",
                Title = "Unpublished",
                Category = Category.BackendDotNet,
                IsPublished = false
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-4",
                Title = "Insight 4",
                Category = Category.FrontendWeb,
                IsPublished = true
            }
        );

        // Add 2 curriculum topics for BackendDotNet (>= 2, so should not fall back to defaults)
        await _db.Topics.AddRangeAsync(
            new Topic
            {
                Id = Guid.NewGuid(),
                Slug = "topic-1",
                Title = "Curriculum Topic Alpha",
                Category = Category.BackendDotNet,
                DayOrder = 1
            },
            new Topic
            {
                Id = Guid.NewGuid(),
                Slug = "topic-2",
                Title = "Curriculum Topic Beta",
                Category = Category.BackendDotNet,
                DayOrder = 2
            },
            new Topic
            {
                Id = Guid.NewGuid(),
                Slug = "topic-deleted",
                Title = "Deleted Topic",
                Category = Category.BackendDotNet,
                IsDeleted = true,
                DayOrder = 3
            }
        );

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        var dotnetCat = response.Categories.First(c => c.Id == (int)Category.BackendDotNet);
        dotnetCat.Count.Should().Be(2); // Only the 2 published insights

        var frontendCat = response.Categories.First(c => c.Id == (int)Category.FrontendWeb);
        frontendCat.Count.Should().Be(1);

        // BackendDotNet had 2 topics, so its suggested topics should contain the DB topics and not need defaults
        response.SuggestedTopics[(int)Category.BackendDotNet].Should().Contain(new[] { "Curriculum Topic Alpha", "Curriculum Topic Beta" });
        response.SuggestedTopics[(int)Category.BackendDotNet].Should().NotContain("Deleted Topic");

        // DatabaseStorage had 0 topics, so should have default topics
        response.SuggestedTopics[(int)Category.DatabaseStorage].Should().Contain("PostgreSQL Index-Only Scan & INCLUDE");
    }
}
