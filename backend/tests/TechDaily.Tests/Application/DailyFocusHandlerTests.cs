using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.DTOs;
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
    public async Task SubmitDailyDrill_ValidText_ShouldEvaluateAndIncrementStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topicId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var topic = new Topic { Id = topicId, Title = "GC", Slug = "gc", Category = Category.BackendDotNet, Difficulty = Difficulty.Senior, DayOrder = 1 };
        var question = new InterviewQuestion { Id = questionId, TopicId = topicId, QuestionText = "Explain LOH", ModelAnswerMarkdown = "Model answer", Difficulty = Difficulty.Senior };
        var drill = new DailyDrill { Id = Guid.NewGuid(), UserId = userId, QuestionId = questionId, ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        await _db.Topics.AddAsync(topic);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();

        var mockAiService = new MockAiService(new AiReviewDto
        {
            Score = 9,
            SummaryFeedback = "Excellent answer",
            Strengths = new() { "Clear memory explanation" },
            MissingPoints = new(),
            ImprovedAnswerMarkdown = "Principal answer",
            AiModelUsed = "gemini-2.5-flash"
        });

        var mockStorage = new MockAudioStorage();
        var validator = new SubmitDailyDrillValidator();

        var handler = new SubmitDailyDrillHandler(_db, mockAiService, mockStorage, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, "This is a detailed explanation of LOH and Gen 2 garbage collection.");

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Review.Score.Should().Be(9);
        result.Value.CurrentStreak.Should().Be(1);

        var updatedDrill = await _db.DailyDrills.Include(d => d.AiReview).FirstAsync(d => d.Id == drill.Id);
        updatedDrill.Status.Should().Be(DrillStatus.Reviewed);
        updatedDrill.AiReview.Should().NotBeNull();
        updatedDrill.AiReview!.Score.Should().Be(9);
    }

    private class MockAiService : IAiReviewService
    {
        private readonly AiReviewDto _response;
        public MockAiService(AiReviewDto response) => _response = response;

        public Task<Result<AiReviewDto>> EvaluateSubmissionAsync(
            string questionText, List<string> expectedKeyPoints, string modelAnswer,
            string? userAnswerText, byte[]? audioBytes, string? audioMimeType,
            string locale = "en", CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AiReviewDto>.Success(_response));
        }
    }

    private class MockAudioStorage : IAudioStorageService
    {
        public Task<string> SaveAudioAsync(Guid drillId, Stream audioStream, string fileExtension, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"/uploads/audios/{drillId}{fileExtension}");
        }

        public string GetAudioUrl(string relativePath) => relativePath;
    }
}
