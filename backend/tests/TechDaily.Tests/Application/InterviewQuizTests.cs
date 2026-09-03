using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.InterviewQuiz.DTOs;
using TechDaily.Application.Features.InterviewQuiz.GenerateQuiz;
using TechDaily.Application.Features.InterviewQuiz.GetQuizReviewQueue;
using TechDaily.Application.Features.InterviewQuiz.GetQuizStats;
using TechDaily.Application.Features.InterviewQuiz.SubmitQuizAnswer;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class InterviewQuizTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly FakeQuizGeneratorService _fakeGenerator;

    public InterviewQuizTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _fakeGenerator = new FakeQuizGeneratorService();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Name = "Test User",
            PreferredLocale = "en"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GenerateQuiz_ShouldGenerateAndPersistQuestions_WhenNoneInDb()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var handler = new GenerateQuizHandler(_db, _fakeGenerator, new GenerateQuizValidator());
        var request = new GenerateQuizRequest(user.Id, ".NET Memory", Category.BackendDotNet, QuizLevel.Senior, 5);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Questions.Should().HaveCount(5);
        result.Value.Topic.Should().Be(".net memory");
        result.Value.Level.Should().Be(QuizLevel.Senior);

        var dbCount = await _db.QuizQuestions.CountAsync();
        dbCount.Should().Be(5);
    }

    [Fact]
    public async Task GenerateQuiz_ShouldExcludeAttemptedQuestions_EvenIfNotMastered()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var qAttemptedFailed = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "postgresql mvcc",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Middle,
            QuestionText = "Attempted Failed Question 1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp 1"
        };
        var qUnattempted = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "postgresql mvcc",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Middle,
            QuestionText = "Unattempted Question 2",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "Exp 2"
        };

        await _db.QuizQuestions.AddRangeAsync(qAttemptedFailed, qUnattempted);
        await _db.UserQuizProgresses.AddAsync(new UserQuizProgress
        {
            UserId = user.Id,
            QuestionId = qAttemptedFailed.Id,
            IsMastered = false,
            IncorrectCount = 1
        });
        await _db.SaveChangesAsync();

        var handler = new GenerateQuizHandler(_db, _fakeGenerator, new GenerateQuizValidator());
        var request = new GenerateQuizRequest(user.Id, "PostgreSQL MVCC", Category.DatabaseStorage, QuizLevel.Middle, 1);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert: It must pick qUnattempted, NOT qAttemptedFailed
        result.IsSuccess.Should().BeTrue();
        result.Value.Questions.Should().HaveCount(1);
        result.Value.Questions[0].Id.Should().Be(qUnattempted.Id);
    }

    [Fact]
    public async Task GenerateQuiz_ShouldGenerateFreshQuestions_WhenAllExistingAttempted()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var q1 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "c#",
            Category = Category.BackendDotNet,
            Level = QuizLevel.Junior,
            QuestionText = "Old Question 1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp 1"
        };

        await _db.QuizQuestions.AddAsync(q1);
        await _db.UserQuizProgresses.AddAsync(new UserQuizProgress
        {
            UserId = user.Id,
            QuestionId = q1.Id,
            IsMastered = true,
            CorrectCount = 1
        });
        await _db.SaveChangesAsync();

        var handler = new GenerateQuizHandler(_db, _fakeGenerator, new GenerateQuizValidator());
        var request = new GenerateQuizRequest(user.Id, "về c#", Category.BackendDotNet, QuizLevel.Junior, 1);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert: Must generate fresh question, not return old q1
        result.IsSuccess.Should().BeTrue();
        result.Value.Questions.Should().HaveCount(1);
        result.Value.Questions[0].Id.Should().NotBe(q1.Id);
        result.Value.Topic.Should().Be("c#");
    }

    [Fact]
    public void NormalizeTopic_ShouldStripConversationalPrefixes()
    {
        GenerateQuizHandler.NormalizeTopic("về C#").Should().Be("c#");
        GenerateQuizHandler.NormalizeTopic("Về ASP.NET").Should().Be("asp.net");
        GenerateQuizHandler.NormalizeTopic("about PostgreSQL").Should().Be("postgresql");
        GenerateQuizHandler.NormalizeTopic("chủ đề Docker").Should().Be("docker");
        GenerateQuizHandler.NormalizeTopic("React 19").Should().Be("react 19");
    }

    [Fact]
    public async Task GenerateQuiz_ShouldExcludeMasteredQuestions()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var q1 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "postgresql mvcc",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Middle,
            QuestionText = "Mastered Question 1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp 1"
        };
        var q2 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "postgresql mvcc",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Middle,
            QuestionText = "Unmastered Question 2",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "Exp 2"
        };

        await _db.QuizQuestions.AddRangeAsync(q1, q2);
        await _db.UserQuizProgresses.AddAsync(new UserQuizProgress
        {
            UserId = user.Id,
            QuestionId = q1.Id,
            IsMastered = true,
            CorrectCount = 1
        });
        await _db.SaveChangesAsync();

        var handler = new GenerateQuizHandler(_db, _fakeGenerator, new GenerateQuizValidator());
        var request = new GenerateQuizRequest(user.Id, "PostgreSQL MVCC", Category.DatabaseStorage, QuizLevel.Middle, 1);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Questions.Should().HaveCount(1);
        result.Value.Questions[0].Id.Should().Be(q2.Id);
    }

    [Fact]
    public async Task SubmitQuizAnswer_ShouldMarkMastered_WhenAnswerIsCorrect()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "React Concurrency",
            Category = Category.FrontendWeb,
            Level = QuizLevel.Senior,
            QuestionText = "How does useTransition prioritize updates?",
            Options = new() { "Low priority", "Immediate blocking", "Sync only", "Never" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "useTransition yields to urgent updates."
        };
        await _db.QuizQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new SubmitQuizAnswerHandler(_db, new SubmitQuizAnswerValidator());
        var request = new SubmitQuizAnswerRequest(user.Id, question.Id, 0);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeTrue();
        result.Value.IsMastered.Should().BeTrue();
        result.Value.CorrectCount.Should().Be(1);

        var progress = await _db.UserQuizProgresses.FirstAsync(p => p.UserId == user.Id && p.QuestionId == question.Id);
        progress.IsMastered.Should().BeTrue();
        progress.CorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitQuizAnswer_ShouldMarkUnmasteredAndAddToReviewQueue_WhenAnswerIsIncorrect()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Distributed Systems",
            Category = Category.SystemDesign,
            Level = QuizLevel.Senior,
            QuestionText = "What guarantees linearizability in Raft?",
            Options = new() { "Follower reads", "Leader log append quorum", "Eventual gossip", "Random election" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "Quorum commitment on leader log ensures linearizability."
        };
        await _db.QuizQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new SubmitQuizAnswerHandler(_db, new SubmitQuizAnswerValidator());
        var request = new SubmitQuizAnswerRequest(user.Id, question.Id, 0); // Incorrect answer (selected 0, correct is 1)

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeFalse();
        result.Value.IsMastered.Should().BeFalse();
        result.Value.IncorrectCount.Should().Be(1);

        var progress = await _db.UserQuizProgresses.FirstAsync(p => p.UserId == user.Id && p.QuestionId == question.Id);
        progress.IsMastered.Should().BeFalse();
        progress.IncorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetQuizReviewQueue_ShouldReturnOnlyUnmasteredQuestions()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var qMastered = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "C# GC",
            Category = Category.BackendDotNet,
            Level = QuizLevel.Senior,
            QuestionText = "GC question 1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp"
        };
        var qFailed = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "C# GC",
            Category = Category.BackendDotNet,
            Level = QuizLevel.Senior,
            QuestionText = "GC question 2",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "Exp"
        };

        await _db.QuizQuestions.AddRangeAsync(qMastered, qFailed);
        await _db.UserQuizProgresses.AddRangeAsync(
            new UserQuizProgress { UserId = user.Id, QuestionId = qMastered.Id, IsMastered = true, CorrectCount = 1 },
            new UserQuizProgress { UserId = user.Id, QuestionId = qFailed.Id, IsMastered = false, IncorrectCount = 2, LastAttemptedAt = DateTimeOffset.UtcNow }
        );
        await _db.SaveChangesAsync();

        var handler = new GetQuizReviewQueueHandler(_db, new GetQuizReviewQueueValidator());
        var request = new GetQuizReviewQueueRequest(user.Id);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Questions.Should().HaveCount(1);
        result.Value.Questions[0].Id.Should().Be(qFailed.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetQuizStats_ShouldCalculateAccuracyAndBreakdownCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var q1 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "PostgreSQL",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Senior,
            QuestionText = "Q1",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp"
        };
        var q2 = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "PostgreSQL",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Senior,
            QuestionText = "Q2",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0,
            ExplanationMarkdown = "Exp"
        };

        await _db.QuizQuestions.AddRangeAsync(q1, q2);
        await _db.UserQuizProgresses.AddRangeAsync(
            new UserQuizProgress { UserId = user.Id, QuestionId = q1.Id, IsMastered = true, CorrectCount = 1, IncorrectCount = 0 },
            new UserQuizProgress { UserId = user.Id, QuestionId = q2.Id, IsMastered = false, CorrectCount = 0, IncorrectCount = 1 }
        );
        await _db.SaveChangesAsync();

        var handler = new GetQuizStatsHandler(_db, new GetQuizStatsValidator());
        var request = new GetQuizStatsRequest(user.Id);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnswered.Should().Be(2);
        result.Value.MasteredCount.Should().Be(1);
        result.Value.ReviewQueueCount.Should().Be(1);
        result.Value.AccuracyRate.Should().Be(50.0m); // 1 correct out of 2 attempts
    }

    [Fact]
    public async Task GeminiAiService_ShouldParseQuestions_EvenWhenResponseHasTrailingDataOrCodeFences()
    {
        // Arrange: simulate Gemini returning JSON array with trailing backticks and whitespace
        var rawJsonContent = @"[
  {
    ""questionText"": ""How does DI handle Scoped services in ASP.NET Core?"",
    ""options"": [""Per HTTP Request"", ""Singleton"", ""Transient per injection"", ""Thread static""],
    ""correctOptionIndex"": 0,
    ""explanationMarkdown"": ""Scoped services are created once per client request pipeline."",
    ""tags"": [""aspnet"", ""di""]
  }
]
```
// Trailing garbage and thoughts here
";

        var fakeGeminiResponse = System.Text.Json.JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = rawJsonContent }
                        }
                    }
                }
            }
        });

        var handler = new MockHttpMessageHandler(fakeGeminiResponse);
        var httpClient = new HttpClient(handler);
        var fakeConfig = new FakeConfiguration(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "fake-api-key",
            ["Gemini:Model"] = "gemini-3.1-flash-lite"
        });

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TechDaily.Infrastructure.Services.GeminiAiService>.Instance;
        var service = new TechDaily.Infrastructure.Services.GeminiAiService(httpClient, fakeConfig, logger);

        // Act
        var result = await service.GenerateQuestionsAsync("asp.net", Category.BackendDotNet, QuizLevel.Junior, 1, new());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].QuestionText.Should().Be("How does DI handle Scoped services in ASP.NET Core?");
        result.Value[0].Options[0].Should().Be("Per HTTP Request");
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        public MockHttpMessageHandler(string response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private class FakeConfiguration : Microsoft.Extensions.Configuration.IConfiguration
    {
        private readonly Dictionary<string, string?> _data;
        public FakeConfiguration(Dictionary<string, string?> data) => _data = data;
        public string? this[string key] { get => _data.TryGetValue(key, out var v) ? v : null; set => _data[key] = value; }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => Enumerable.Empty<Microsoft.Extensions.Configuration.IConfigurationSection>();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotImplementedException();
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) => throw new NotImplementedException();
    }

    private class FakeQuizGeneratorService : IQuizGeneratorService
    {
        public Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(
            string topic,
            Category category,
            QuizLevel level,
            int count,
            List<string> existingTitlesToAvoid,
            string locale = "en",
            CancellationToken cancellationToken = default)
        {
            var list = new List<QuizQuestion>();
            for (var i = 1; i <= count; i++)
            {
                list.Add(new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    Topic = topic,
                    Category = category,
                    Level = level,
                    QuestionText = $"Generated Question {i} for {topic}",
                    Options = new() { "Option A", "Option B", "Option C", "Option D" },
                    CorrectOptionIndex = 0,
                    ExplanationMarkdown = "Generated deep explanation.",
                    Tags = new() { "test" }
                });
            }
            return Task.FromResult(Result<List<QuizQuestion>>.Success(list));
        }
    }
}
