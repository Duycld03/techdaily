using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class DailyFocusHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public DailyFocusHandlerTests()
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
    public async Task GetTodayFocus_ShouldCreateIdempotentDrill_WhenNoneExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Slug = "dotnet-gc",
            Title = "Garbage Collection",
            Category = Category.BackendDotNet,
            Difficulty = Difficulty.Senior,
            DayOrder = 1,
            Summary = "GC internals"
        };
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            TopicId = topic.Id,
            QuestionText = "Explain LOH",
            ModelAnswerMarkdown = "Model answer",
            Difficulty = Difficulty.Senior
        };
        topic.InterviewQuestions.Add(question);

        await _db.Topics.AddAsync(topic);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);
        var request = new GetTodayFocusRequest(userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Topic.Title.Should().Be("Garbage Collection");
        result.Value.Question.QuestionText.Should().Be("Explain LOH");
        result.Value.Drill.Status.Should().Be(DrillStatus.Pending);

        // Call again on the same day -> should return the exact same drill record
        var secondResult = await handler.ExecuteAsync(request);
        secondResult.Value.Drill.Id.Should().Be(result.Value.Drill.Id);
    }

    [Fact]
    public async Task SubmitDailyDrill_CorrectOption_ShouldAwardTenPointsAndIncrementStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topicId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var topic = new Topic { Id = topicId, Title = "GC", Slug = "gc", Category = Category.BackendDotNet, Difficulty = Difficulty.Senior, DayOrder = 1 };
        var question = new InterviewQuestion
        {
            Id = questionId,
            TopicId = topicId,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool rents reusable contiguous byte arrays.",
            Difficulty = Difficulty.Senior
        };
        var drill = new DailyDrill { Id = Guid.NewGuid(), UserId = userId, QuestionId = questionId, ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        await _db.Topics.AddAsync(topic);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();

        var validator = new SubmitDailyDrillValidator();
        var handler = new SubmitDailyDrillHandler(_db, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, SelectedOptionIndex: 1);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeTrue();
        result.Value.Score.Should().Be(10);
        result.Value.CorrectOptionIndex.Should().Be(1);
        result.Value.ExplanationMarkdown.Should().Contain("ArrayPool");
        result.Value.CurrentStreak.Should().Be(1);

        var updatedDrill = await _db.DailyDrills.FirstAsync(d => d.Id == drill.Id);
        updatedDrill.Status.Should().Be(DrillStatus.Reviewed);
        updatedDrill.IsCorrect.Should().BeTrue();
        updatedDrill.Score.Should().Be(10);
        updatedDrill.SelectedOptionIndex.Should().Be(1);
    }

    [Fact]
    public async Task SubmitDailyDrill_IncorrectOption_ShouldAwardZeroAndScheduleSm2Card()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topicId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var topic = new Topic { Id = topicId, Title = "GC", Slug = "gc", Category = Category.BackendDotNet, Difficulty = Difficulty.Senior, DayOrder = 1 };
        var question = new InterviewQuestion
        {
            Id = questionId,
            TopicId = topicId,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool rents reusable contiguous byte arrays.",
            Difficulty = Difficulty.Senior
        };
        var drill = new DailyDrill { Id = Guid.NewGuid(), UserId = userId, QuestionId = questionId, ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        await _db.Topics.AddAsync(topic);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();

        var validator = new SubmitDailyDrillValidator();
        var handler = new SubmitDailyDrillHandler(_db, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, SelectedOptionIndex: 0); // Chose wrong option 0

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeFalse();
        result.Value.Score.Should().Be(0);
        result.Value.CorrectOptionIndex.Should().Be(1);

        var updatedDrill = await _db.DailyDrills.FirstAsync(d => d.Id == drill.Id);
        updatedDrill.Status.Should().Be(DrillStatus.Reviewed);
        updatedDrill.IsCorrect.Should().BeFalse();
        updatedDrill.Score.Should().Be(0);

        // Verify SM-2 card was created for unmastered concept
        var sm2Card = await _db.SpacedRepetitionCards.FirstOrDefaultAsync(c => c.UserId == userId && c.TopicId == topicId);
        sm2Card.Should().NotBeNull();
        sm2Card!.NextReviewDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
    }

    [Fact]
    public async Task GetTodayFocus_ShouldMaskCorrectOptionAndExplanation_WhenPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topicId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var topic = new Topic { Id = topicId, Title = "GC", Slug = "gc", Category = Category.BackendDotNet, Difficulty = Difficulty.Senior, DayOrder = 1 };
        var question = new InterviewQuestion
        {
            Id = questionId,
            TopicId = topicId,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool explanation secret",
            Difficulty = Difficulty.Senior
        };

        topic.InterviewQuestions.Add(question);
        await _db.Topics.AddAsync(topic);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetTodayFocusRequest(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Question.Options.Should().HaveCount(4);
        result.Value.Question.CorrectOptionIndex.Should().BeNull();
        result.Value.Question.ExplanationMarkdown.Should().BeNull();
    }
}
