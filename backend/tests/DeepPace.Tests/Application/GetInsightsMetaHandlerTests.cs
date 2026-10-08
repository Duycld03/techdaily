using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Features.Insights.GetInsightsMeta;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class GetInsightsMetaHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly GetInsightsMetaHandler _handler;

    public GetInsightsMetaHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
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

        response.Categories.Should().HaveCount(7);

        var cat0 = response.Categories.First(c => c.Id == 0);
        cat0.Key.Should().Be("frontend");
        cat0.LabelEn.Should().Be("Frontend & Web Architecture");
        cat0.LabelVi.Should().Be("Frontend & Trình Duyệt");
        cat0.Count.Should().Be(0);

        var cat1 = response.Categories.First(c => c.Id == 1);
        cat1.Key.Should().Be("backend");
        cat1.LabelEn.Should().Be("Backend & Runtime Systems");
        cat1.LabelVi.Should().Be("Hệ Thống Backend & Runtime");
        cat1.Count.Should().Be(0);

        var cat2 = response.Categories.First(c => c.Id == 2);
        cat2.Key.Should().Be("database");
        cat2.LabelEn.Should().Be("Database & Storage");
        cat2.LabelVi.Should().Be("Cơ Sở Dữ Liệu & Lưu Trữ");
        cat2.Count.Should().Be(0);

        var cat3 = response.Categories.First(c => c.Id == 3);
        cat3.Key.Should().Be("system_design");
        cat3.LabelEn.Should().Be("Distributed Systems & Architecture");
        cat3.LabelVi.Should().Be("Thiết Kế Hệ Thống");
        cat3.Count.Should().Be(0);

        var cat4 = response.Categories.First(c => c.Id == 4);
        cat4.Key.Should().Be("craft");
        cat4.LabelEn.Should().Be("Clean Code & Software Design");
        cat4.LabelVi.Should().Be("Mã Sạch & Thiết Kế Mã");
        cat4.Count.Should().Be(0);
        var cat5 = response.Categories.First(c => c.Id == 5);
        cat5.Key.Should().Be("mental_models");
        cat5.LabelEn.Should().Be("Mental Models & Decisions");
        cat5.Count.Should().Be(0);

        var cat6 = response.Categories.First(c => c.Id == 6);
        cat6.Key.Should().Be("habits");
        cat6.LabelEn.Should().Be("Habits & Deep Work");
        cat6.Count.Should().Be(0);

        // Suggested topics should contain all 7 categories with defaults
        response.SuggestedTopics.Should().ContainKey(0);
        response.SuggestedTopics.Should().ContainKey(1);
        response.SuggestedTopics.Should().ContainKey(2);
        response.SuggestedTopics.Should().ContainKey(3);
        response.SuggestedTopics.Should().ContainKey(4);
        response.SuggestedTopics.Should().ContainKey(5);
        response.SuggestedTopics.Should().ContainKey(6);

        response.SuggestedTopics[0].Should().Contain("Vue 3 shallowRef vs reactive");
        response.SuggestedTopics[1].Should().Contain("Kestrel Socket Pipeline");
        response.SuggestedTopics[2].Should().Contain("PostgreSQL Index-Only Scan & INCLUDE");
        response.SuggestedTopics[3].Should().Contain("Transactional Outbox & CDC");
        response.SuggestedTopics[4].Should().Contain("Clean Code Refactoring");
        response.SuggestedTopics[5].Should().Contain("First Principles Thinking");
        response.SuggestedTopics[6].Should().Contain("Atomic Habits & Cue Design");
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
                Category = Category.BackendRuntime,
                IsPublished = true
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-2",
                Title = "Insight 2",
                Category = Category.BackendRuntime,
                IsPublished = true
            },
            new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = "insight-3",
                Title = "Unpublished",
                Category = Category.BackendRuntime,
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

        // Add ready DocumentBook and chunks for BackendRuntime (>= 2, so should not fall back to defaults)
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Backend Architecture Guide",
            Slug = "backend-arch",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready
        };
        await _db.DocumentBooks.AddAsync(book);

        await _db.DocumentChunks.AddRangeAsync(
            new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = book.Id,
                ChunkOrder = 1,
                ChapterTitle = "Kestrel Connection Pooling"
            },
            new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = book.Id,
                ChunkOrder = 2,
                ChapterTitle = "Garbage Collection Latency Tuning"
            },
            new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = book.Id,
                ChunkOrder = 3,
                ChapterTitle = "Deleted Chapter",
                IsDeleted = true
            }
        );

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        var backendCat = response.Categories.First(c => c.Id == (int)Category.BackendRuntime);
        backendCat.Count.Should().Be(2); // Only the 2 published insights

        var frontendCat = response.Categories.First(c => c.Id == (int)Category.FrontendWeb);
        frontendCat.Count.Should().Be(1);

        // BackendRuntime had 2 chunks, so its suggested topics should contain the chapter titles and not need defaults
        response.SuggestedTopics[(int)Category.BackendRuntime].Should().Contain(new[] { "Kestrel Connection Pooling", "Garbage Collection Latency Tuning" });
        response.SuggestedTopics[(int)Category.BackendRuntime].Should().NotContain("Deleted Chapter");
        // DatabaseStorage had 0 topics, so should have default topics
        response.SuggestedTopics[(int)Category.DatabaseStorage].Should().Contain("PostgreSQL Index-Only Scan & INCLUDE");
    }
    [Theory]
    [InlineData("CHƯƠNG 4: QUY LUẬT SỐ 1 - KHIẾN VIỆC ĐÓ TRỞ NÊN HIỂN NHIÊN (Section 1)", "QUY LUẬT SỐ 1 - KHIẾN VIỆC ĐÓ TRỞ NÊN HIỂN NHIÊN")]
    [InlineData("Chapter 19: Query Optimization & Cost-Based Execution Planning", "Query Optimization & Cost-Based Execution Planning")]
    [InlineData("Tóm tắt chương (Section 3)", "")]
    [InlineData("Raft Consensus Protocol", "Raft Consensus Protocol")]
    [InlineData("QUI LUẬT SỐ 12: NỖ LỰC TỐI THIỂU (Section 1)", "NỖ LỰC TỐI THIỂU")]
    public void CleanTopicTitle_StripsNoiseAndChapterPrefixes(string raw, string expected)
    {
        var cleaned = GetInsightsMetaHandler.CleanTopicTitle(raw);
        cleaned.Should().Be(expected);
    }

    [Fact]
    public async Task GetInsightsMeta_CapsSuggestedTopicsAtEightItems()
    {
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Large Architecture Volume",
            Slug = "large-arch-vol",
            Category = Category.SystemDesign,
            Status = ProcessingStatus.Ready
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunks = Enumerable.Range(1, 15).Select(i => new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = i,
            ChapterTitle = $"Chapter {i}: Architectural Subsystem Design Number {i}"
        }).ToList();
        await _db.DocumentChunks.AddRangeAsync(chunks);
        await _db.SaveChangesAsync();

        var result = await _handler.ExecuteAsync(new GetInsightsMetaRequest());
        result.IsSuccess.Should().BeTrue();

        var systemTopics = result.Value.SuggestedTopics[(int)Category.SystemDesign];
        systemTopics.Should().HaveCount(8);
        systemTopics[0].Should().Be("Architectural Subsystem Design Number 1");
        systemTopics[7].Should().Be("Architectural Subsystem Design Number 8");
    }
}
