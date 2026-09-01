using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Curriculum.GetCurriculumRoadmap;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class CurriculumRoadmapTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public CurriculumRoadmapTests()
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
    public async Task GetCurriculumRoadmap_ShouldReturnAll30DaysGroupedInto4Modules()
    {
        // Arrange: Seed 30 topics
        for (int i = 1; i <= 30; i++)
        {
            var category = i switch
            {
                <= 7 => Category.FrontendWeb,
                <= 15 => Category.BackendDotNet,
                <= 22 => Category.DatabaseStorage,
                _ => Category.SystemDesign
            };

            await _db.Topics.AddAsync(new Topic
            {
                Id = Guid.NewGuid(),
                Slug = $"topic-day-{i}",
                Title = $"Topic Day {i}",
                Summary = $"Summary for day {i}",
                Category = category,
                Difficulty = Difficulty.Senior,
                DayOrder = i
            });
        }
        await _db.SaveChangesAsync();

        var handler = new GetCurriculumRoadmapHandler(_db);
        var testUserId = Guid.NewGuid();

        // Act
        var result = await handler.ExecuteAsync(new GetCurriculumRoadmapRequest(testUserId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.TotalDays.Should().Be(30);
        response.Modules.Should().HaveCount(4);

        response.Modules[0].Category.Should().Be(Category.FrontendWeb);
        response.Modules[0].Days.Should().HaveCount(7);

        response.Modules[1].Category.Should().Be(Category.BackendDotNet);
        response.Modules[1].Days.Should().HaveCount(8);

        response.Modules[2].Category.Should().Be(Category.DatabaseStorage);
        response.Modules[2].Days.Should().HaveCount(7);

        response.Modules[3].Category.Should().Be(Category.SystemDesign);
        response.Modules[3].Days.Should().HaveCount(8);
    }

    [Fact]
    public async Task GetCurriculumRoadmap_ShouldComputeUserProgressCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "senior@techdaily.local", Name = "Senior" };
        await _db.Users.AddAsync(user);

        var streak = StreakRecord.Create(userId);
        streak.RecordCompletion(DateOnly.FromDateTime(DateTime.UtcNow), 10);
        await _db.StreakRecords.AddAsync(streak);

        var topic1 = new Topic
        {
            Id = Guid.NewGuid(),
            Slug = "topic-day-1",
            Title = "Topic Day 1",
            Summary = "Day 1",
            Category = Category.FrontendWeb,
            Difficulty = Difficulty.Senior,
            DayOrder = 1
        };
        var question1 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            TopicId = topic1.Id,
            QuestionText = "Question 1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Explanation"
        };
        await _db.Topics.AddAsync(topic1);
        await _db.InterviewQuestions.AddAsync(question1);

        var drill1 = new DailyDrill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = question1.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = DrillStatus.Reviewed,
            SelectedOptionIndex = 0,
            IsCorrect = true,
            Score = 10,
            Question = question1
        };
        await _db.DailyDrills.AddAsync(drill1);
        await _db.SaveChangesAsync();

        var handler = new GetCurriculumRoadmapHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetCurriculumRoadmapRequest(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.CompletedDaysCount.Should().Be(1);
        response.CurrentActiveDay.Should().Be(2); // (1 % 30) + 1 = 2

        var frontendModule = response.Modules.First(m => m.Category == Category.FrontendWeb);
        frontendModule.CompletedCount.Should().Be(1);
        var day1 = frontendModule.Days.First(d => d.DayOrder == 1);
        day1.IsCompleted.Should().BeTrue();
        day1.DrillScore.Should().Be(10);
        day1.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurriculumRoadmap_ShouldReturnUnauthorized_WhenUserIdIsEmpty()
    {
        var handler = new GetCurriculumRoadmapHandler(_db);
        var result = await handler.ExecuteAsync(new GetCurriculumRoadmapRequest(Guid.Empty));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TechDaily.Application.Common.Error.Unauthorized);
    }
}
