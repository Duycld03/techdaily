using DeepPace.Application.Common;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Features.InterviewQuiz.GenerateQuiz;
using DeepPace.Application.Features.InterviewQuiz.GetQuizReviewQueue;
using DeepPace.Application.Features.InterviewQuiz.SubmitQuizAnswer;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeepPace.Tests.Application;

public sealed class QuizQuestionOwnershipBoundaryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DeepPaceDbContext _db;
    private readonly User _caller = new() { Email = "quiz-caller@example.test", Name = "Caller" };
    private readonly User _other = new() { Email = "quiz-owner@example.test", Name = "Owner" };

    public QuizQuestionOwnershipBoundaryTests()
    {
        _connection.Open();
        _db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Users.AddRange(_caller, _other);
        _db.SaveChanges();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Submit_ForeignPrivateQuestion_ReturnsNotFoundWithoutCreatingOrChangingProgress(bool hasOldProgress)
    {
        var question = Question(_other.Id);
        _db.QuizQuestions.Add(question);
        if (hasOldProgress)
            _db.UserQuizProgresses.Add(Progress(_caller.Id, question.Id));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await SubmitHandler().ExecuteAsync(new SubmitQuizAnswerRequest(_caller.Id, question.Id, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
        var stored = await _db.UserQuizProgresses.AsNoTracking().ToListAsync();
        if (hasOldProgress)
        {
            var progress = stored.Should().ContainSingle().Subject;
            progress.UserId.Should().Be(_caller.Id);
            progress.QuestionId.Should().Be(question.Id);
            progress.CorrectCount.Should().Be(0);
            progress.IncorrectCount.Should().Be(1);
            progress.LastSelectedOptionIndex.Should().Be(0);
            progress.IsMastered.Should().BeFalse();
        }
        else
        {
            stored.Should().BeEmpty();
        }
        var persistedQuestion = await _db.QuizQuestions.AsNoTracking().SingleAsync();
        persistedQuestion.CreatedByUserId.Should().Be(_other.Id);
        persistedQuestion.ExplanationMarkdown.Should().Be(question.ExplanationMarkdown);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Submit_SharedOrOwnedQuestion_ReturnsExplanationAndPersistsOnlyCallerProgress(bool owned)
    {
        var question = Question(owned ? _caller.Id : null);
        _db.QuizQuestions.Add(question);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await SubmitHandler().ExecuteAsync(new SubmitQuizAnswerRequest(_caller.Id, question.Id, 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeTrue();
        result.Value.ExplanationMarkdown.Should().Be(question.ExplanationMarkdown);
        result.Value.CorrectOptionIndex.Should().Be(1);
        var progress = (await _db.UserQuizProgresses.AsNoTracking().ToListAsync()).Should().ContainSingle().Subject;
        progress.UserId.Should().Be(_caller.Id);
        progress.QuestionId.Should().Be(question.Id);
        progress.LastSelectedOptionIndex.Should().Be(1);
        progress.CorrectCount.Should().Be(1);
        progress.IncorrectCount.Should().Be(0);
        progress.IsMastered.Should().BeTrue();
        (await _db.QuizQuestions.AsNoTracking().SingleAsync()).CreatedByUserId.Should().Be(question.CreatedByUserId);
    }

    [Fact]
    public async Task ReviewQueue_OldForeignPrivateProgressIsExcludedFromItemsAndTotal_ButSharedAndOwnedRemain()
    {
        var foreign = Question(_other.Id);
        var shared = Question(null);
        var owned = Question(_caller.Id);
        _db.QuizQuestions.AddRange(foreign, shared, owned);
        _db.UserQuizProgresses.AddRange(
            Progress(_caller.Id, foreign.Id), Progress(_caller.Id, shared.Id),
            Progress(_caller.Id, owned.Id), Progress(_other.Id, shared.Id));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await new GetQuizReviewQueueHandler(_db, new GetQuizReviewQueueValidator())
            .ExecuteAsync(new GetQuizReviewQueueRequest(_caller.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Questions.Select(q => q.Id).Should().BeEquivalentTo([shared.Id, owned.Id]);
        result.Value.Questions.Select(q => q.ExplanationMarkdown).Should()
            .BeEquivalentTo([shared.ExplanationMarkdown, owned.ExplanationMarkdown]);
        result.Value.Questions.Should().OnlyContain(q => q.IncorrectCount == 1 && !q.IsMastered);
        (await _db.UserQuizProgresses.CountAsync()).Should().Be(4, "filtering must not rewrite historical progress");
        (await _db.QuizQuestions.SingleAsync(q => q.Id == foreign.Id)).CreatedByUserId.Should().Be(_other.Id);
    }

    [Fact]
    public async Task Generate_ForeignOwnedBook_ReturnsLibraryForbiddenWithoutPersistingQuestions()
    {
        var book = new DocumentBook
        {
            Title = "Private source", Slug = "private-source", CategoryId = CategoryDefaults.BackendRuntimeId,
            CreatedByUserId = _other.Id, Status = ProcessingStatus.Ready
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        var handler = new GenerateQuizHandler(_db, new RejectUnexpectedGeneration(), new GenerateQuizValidator(), new CategoryAssignmentGuard(_db));

        var result = await handler.ExecuteAsync(new GenerateQuizRequest(_caller.Id, "private topic", null, QuizLevel.Senior, 1, BookId: book.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("LIBRARY_FORBIDDEN");
        (await _db.QuizQuestions.AnyAsync()).Should().BeFalse();
        (await _db.UserQuizProgresses.AnyAsync()).Should().BeFalse();
        (await _db.DocumentBooks.AsNoTracking().SingleAsync()).CreatedByUserId.Should().Be(_other.Id);
    }

    private SubmitQuizAnswerHandler SubmitHandler() => new(_db, new SubmitQuizAnswerValidator());

    private static QuizQuestion Question(Guid? ownerId) => new()
    {
        CreatedByUserId = ownerId, CategoryId = CategoryDefaults.BackendRuntimeId,
        Topic = "Ownership boundaries", Level = QuizLevel.Senior,
        QuestionText = "Which source is visible?", Options = ["No", "Yes", "Other", "Neither"],
        CorrectOptionIndex = 1, ExplanationMarkdown = $"Private or shared explanation {Guid.NewGuid():N}"
    };

    private static UserQuizProgress Progress(Guid userId, Guid questionId) => new()
    {
        UserId = userId, QuestionId = questionId, IncorrectCount = 1,
        LastSelectedOptionIndex = 0, IsLastAnswerCorrect = false, LastAttemptedAt = DateTimeOffset.UtcNow
    };

    private sealed class RejectUnexpectedGeneration : IQuizGeneratorService
    {
        public Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(string topic, Guid categoryId, QuizLevel level,
            int count, List<string> existingTitlesToAvoid, string locale = "en", CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("An inaccessible source must be rejected before generation.");
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
