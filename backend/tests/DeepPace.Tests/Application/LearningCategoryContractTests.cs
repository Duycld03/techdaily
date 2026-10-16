using DeepPace.Application.Common;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Features.Insights.GenerateInsight;
using DeepPace.Application.Features.Insights.GetInsightsFeed;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Features.InterviewQuiz.GenerateQuiz;
using DeepPace.Application.Interfaces;
using DeepPace.Application.Features.InterviewQuiz.GetQuizStats;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeepPace.Tests.Application;

public sealed class LearningCategoryContractTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DeepPaceDbContext _db;
    private readonly User _user = new() { Email = "learning-category@example.test", Name = "Learner" };
    private readonly DocumentCategory _category = DocumentCategory.Create("novel-learning", "Novel learning", "Học tập mới");

    public LearningCategoryContractTests()
    {
        _connection.Open();
        _db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Users.Add(_user);
        _db.DocumentCategories.Add(_category);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Feed_PreservesNovelUuidAndAuthoritativeDescriptor()
    {
        var insight = NewInsight(_category.Id);
        _db.TechInsights.Add(insight);
        await _db.SaveChangesAsync();

        var result = await new GetInsightsFeedHandler(_db).ExecuteAsync(new GetInsightsFeedRequest(CategoryId: _category.Id));

        var dto = result.Value.Insights.Should().ContainSingle().Subject;
        dto.CategoryId.Should().Be(_category.Id);
        dto.Category.Id.Should().Be(_category.Id);
        dto.Category.NameVi.Should().Be("Học tập mới");
    }

    [Fact]
    public async Task StandaloneQuiz_DoesNotReuseIdenticalTopicFromAnotherCategory_WhenProviderFails()
    {
        var other = DocumentCategory.Create("other-learning", "Other", "Khác");
        _db.DocumentCategories.Add(other);
        _db.QuizQuestions.Add(NewQuestion(other.Id));
        await _db.SaveChangesAsync();
        var provider = new QuizProvider((_, _) => Task.FromResult<Result<List<QuizQuestion>>>(Error.Custom("AI.Unavailable", "Provider unavailable")));

        var result = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", _category.Id, QuizLevel.Senior, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AI.Unavailable");
        (await _db.QuizQuestions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task StandaloneQuiz_PreservesNovelUuidAndQuestionText()
    {
        var provider = new QuizProvider((categoryId, _) => Task.FromResult<Result<List<QuizQuestion>>>(new List<QuizQuestion> { NewQuestion(categoryId) }));

        var result = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", _category.Id, QuizLevel.Senior, 1));

        var question = result.Value.Questions.Should().ContainSingle().Subject;
        question.CategoryId.Should().Be(_category.Id);
        question.Category.Id.Should().Be(_category.Id);
        question.QuestionText.Should().Be("What does the source establish?");
        (await _db.QuizQuestions.SingleAsync()).CreatedByUserId.Should().Be(_user.Id);
    }

    [Fact]
    public async Task StandaloneQuiz_DeactivationDuringGeneration_PersistsNothing()
    {
        var provider = new QuizProvider(async (categoryId, ct) =>
        {
            _db.Database.CurrentTransaction.Should().BeNull("provider work must not hold assignment locks");
            _category.UpdateMetadata(_category.NameEn, _category.NameVi, _category.SortOrder, false, _category.ConcurrencyVersion);
            await _db.SaveChangesAsync(ct);
            return new List<QuizQuestion> { NewQuestion(categoryId) };
        });

        var result = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", _category.Id, QuizLevel.Senior, 1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.Inactive");
        (await _db.QuizQuestions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BookQuiz_RetainsInactiveCategory_ButRejectsContradictoryExplicitId()
    {
        _category.UpdateMetadata(_category.NameEn, _category.NameVi, _category.SortOrder, false, _category.ConcurrencyVersion);
        var book = NewBook();
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();
        var provider = new QuizProvider((categoryId, _) => Task.FromResult<Result<List<QuizQuestion>>>(new List<QuizQuestion> { NewQuestion(categoryId) }));

        var accepted = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", null, QuizLevel.Senior, 1, BookId: book.Id));
        var rejected = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", Guid.NewGuid(), QuizLevel.Senior, 1, BookId: book.Id));

        accepted.IsSuccess.Should().BeTrue();
        accepted.Value.Questions.Single().CategoryId.Should().Be(_category.Id);
        rejected.IsSuccess.Should().BeFalse();
        rejected.Error.Code.Should().Be("Category.ValidationFailed");
    }

    [Fact]
    public async Task BookQuiz_ReclassificationDuringGeneration_ReturnsConflictAndPersistsNothing()
    {
        var replacement = DocumentCategory.Create("replacement-learning", "Replacement", "Thay thế");
        _db.DocumentCategories.Add(replacement);
        var book = NewBook();
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();
        var provider = new QuizProvider(async (categoryId, ct) =>
        {
            book.CategoryId = replacement.Id;
            await _db.SaveChangesAsync(ct);
            return new List<QuizQuestion> { NewQuestion(categoryId) };
        });

        var result = await QuizHandler(provider).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", null, QuizLevel.Senior, 1, BookId: book.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.ConcurrencyConflict");
        (await _db.QuizQuestions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Insight_DeactivationDuringGeneration_PersistsNothing()
    {
        var provider = new InsightProvider(async (categoryId, ct) =>
        {
            _db.Database.CurrentTransaction.Should().BeNull("provider work must not hold assignment locks");
            _category.UpdateMetadata(_category.NameEn, _category.NameVi, _category.SortOrder, false, _category.ConcurrencyVersion);
            await _db.SaveChangesAsync(ct);
            return NewInsight(categoryId);
        });

        var result = await new GenerateInsightHandler(_db, provider, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.Inactive");
        (await _db.TechInsights.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task QuizAnalytics_RetainsNovelInactiveCategoryIdentityAndCurrentMetadata()
    {
        var question = NewQuestion(_category.Id);
        _db.QuizQuestions.Add(question);
        _db.UserQuizProgresses.Add(new UserQuizProgress { UserId = _user.Id, QuestionId = question.Id, CorrectCount = 1, IncorrectCount = 3 });
        _category.UpdateMetadata("Renamed subject", "Đổi tên", 4, false, _category.ConcurrencyVersion);
        await _db.SaveChangesAsync();

        var result = await new GetQuizStatsHandler(_db, new GetQuizStatsValidator()).ExecuteAsync(new GetQuizStatsRequest(_user.Id));

        var category = result.Value.CategoryBreakdown.Should().ContainSingle().Subject;
        category.CategoryId.Should().Be(_category.Id);
        category.Category.NameEn.Should().Be("Renamed subject");
        category.Category.IsActive.Should().BeFalse();
        category.AccuracyRate.Should().Be(25m);
    }

    [Fact]
    public async Task StandaloneGeneration_RejectsInactiveCategoryBeforeCallingEitherProvider()
    {
        _category.UpdateMetadata(_category.NameEn, _category.NameVi, _category.SortOrder, false, _category.ConcurrencyVersion);
        await _db.SaveChangesAsync();
        var quiz = new QuizProvider((_, _) => throw new InvalidOperationException("Provider must not be called"));
        var insight = new InsightProvider((_, _) => throw new InvalidOperationException("Provider must not be called"));

        var quizResult = await QuizHandler(quiz).ExecuteAsync(new GenerateQuizRequest(_user.Id, "same topic", _category.Id, QuizLevel.Senior, 1));
        var insightResult = await new GenerateInsightHandler(_db, insight, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        quizResult.Error.Code.Should().Be("Category.Inactive");
        insightResult.Error.Code.Should().Be("Category.Inactive");
        (await _db.QuizQuestions.CountAsync()).Should().Be(0);
        (await _db.TechInsights.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task InsightGeneration_PreservesSelectedUuidAndDescriptorEvenWhenProviderSuppliesDifferentCategory()
    {
        var provider = new InsightProvider((_, _) =>
        {
            var generated = NewInsight(CategoryDefaults.BackendRuntimeId);
            generated.Category = _db.DocumentCategories.Single(c => c.Id == CategoryDefaults.BackendRuntimeId);
            return Task.FromResult<Result<TechInsight>>(generated);
        });

        var result = await new GenerateInsightHandler(_db, provider, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        result.Value.CategoryId.Should().Be(_category.Id);
        result.Value.Category.NameVi.Should().Be(_category.NameVi);
        (await _db.TechInsights.SingleAsync()).CategoryId.Should().Be(_category.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BookQuiz_ReusesSameSourceCategoryQuestionsWithMixedCaseBookTitle(bool ownedQuestion)
    {
        var book = NewBook();
        var question = NewQuestion(_category.Id);
        question.Topic = "owned source: same topic";
        question.CreatedByUserId = ownedQuestion ? _user.Id : null;
        _db.DocumentBooks.Add(book);
        _db.QuizQuestions.Add(question);
        await _db.SaveChangesAsync();
        var provider = new QuizProvider((_, _) => throw new InvalidOperationException("Matching source-category question should be reused"));

        var result = await QuizHandler(provider).ExecuteAsync(
            new GenerateQuizRequest(_user.Id, "same topic", null, QuizLevel.Senior, 1, BookId: book.Id, IsGrounded: true));

        result.Value.Questions.Single().Id.Should().Be(question.Id);
        result.Value.Questions.Single().CategoryId.Should().Be(book.CategoryId);
        (await _db.QuizQuestions.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BookQuiz_DoesNotReuseMatchingTopicFromAnotherCategoryOrOwner(bool foreignOwner)
    {
        var book = NewBook();
        var otherUser = new User { Email = "other-learner@example.test", Name = "Other learner" };
        var otherCategory = DocumentCategory.Create("other-book-learning", "Other source", "Nguồn khác");
        var question = NewQuestion(foreignOwner ? _category.Id : otherCategory.Id);
        question.Topic = "owned source: same topic";
        question.CreatedByUserId = foreignOwner ? otherUser.Id : null;
        _db.Users.Add(otherUser);
        _db.DocumentCategories.Add(otherCategory);
        _db.DocumentBooks.Add(book);
        _db.QuizQuestions.Add(question);
        await _db.SaveChangesAsync();
        var provider = new QuizProvider((_, _) =>
            Task.FromResult<Result<List<QuizQuestion>>>(Error.Custom("AI.Unavailable", "Provider unavailable")));

        var result = await QuizHandler(provider).ExecuteAsync(
            new GenerateQuizRequest(_user.Id, "same topic", null, QuizLevel.Senior, 1, BookId: book.Id, IsGrounded: true));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AI.Unavailable");
        (await _db.QuizQuestions.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsightGeneration_DoesNotDiscloseOrReuseUnpublishedDuplicate(bool maximumLengthSlug)
    {
        var hidden = NewInsight(_category.Id);
        hidden.IsPublished = false;
        hidden.SummaryMarkdown = "Private unpublished source";
        hidden.ProblemSnippet = "Private problem";
        hidden.SolutionSnippet = "Private solution";
        hidden.UnderTheHoodMarkdown = "Private implementation";
        if (maximumLengthSlug)
            hidden.Slug = new string('s', 255);
        _db.TechInsights.Add(hidden);
        await _db.SaveChangesAsync();
        var generated = NewInsight(_category.Id);
        generated.Slug = hidden.Slug;
        generated.SummaryMarkdown = "Independent public explanation";
        var provider = new InsightProvider((_, _) => Task.FromResult<Result<TechInsight>>(generated));

        var result = await new GenerateInsightHandler(_db, provider, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(generated.Id).And.NotBe(hidden.Id);
        result.Value.CategoryId.Should().Be(_category.Id);
        result.Value.Slug.Should().NotBe(hidden.Slug);
        result.Value.Slug.Length.Should().BeLessThanOrEqualTo(255);
        result.Value.SummaryMarkdown.Should().Be(generated.SummaryMarkdown);
        result.Value.ProblemSnippet.Should().Be(generated.ProblemSnippet);
        result.Value.SolutionSnippet.Should().Be(generated.SolutionSnippet);
        result.Value.UnderTheHoodMarkdown.Should().Be(generated.UnderTheHoodMarkdown);
        provider.ExistingTitles.Should().NotContain(hidden.Title);
        _db.ChangeTracker.Clear();
        var persistedHidden = await _db.TechInsights.SingleAsync(i => i.Id == hidden.Id);
        persistedHidden.IsPublished.Should().BeFalse();
        persistedHidden.SummaryMarkdown.Should().Be("Private unpublished source");
        persistedHidden.ProblemSnippet.Should().Be("Private problem");
        persistedHidden.SolutionSnippet.Should().Be("Private solution");
        persistedHidden.UnderTheHoodMarkdown.Should().Be("Private implementation");
        var published = await _db.TechInsights.SingleAsync(i => i.Id == generated.Id);
        published.IsPublished.Should().BeTrue();
        published.CategoryId.Should().Be(_category.Id);
        (await _db.TechInsights.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task InsightGeneration_ReusesPublishedDuplicateFromSelectedCategory()
    {
        var published = NewInsight(_category.Id);
        published.SummaryMarkdown = "Published explanation";
        _db.TechInsights.Add(published);
        await _db.SaveChangesAsync();
        var provider = new InsightProvider((categoryId, _) =>
            Task.FromResult<Result<TechInsight>>(NewInsight(categoryId)));

        var result = await new GenerateInsightHandler(_db, provider, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(published.Id);
        result.Value.CategoryId.Should().Be(_category.Id);
        result.Value.SummaryMarkdown.Should().Be("Published explanation");
        (await _db.TechInsights.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InsightGeneration_DoesNotReturnUnpublishedProviderContent()
    {
        var draft = NewInsight(_category.Id);
        draft.IsPublished = false;
        var provider = new InsightProvider((_, _) => Task.FromResult<Result<TechInsight>>(draft));

        var result = await new GenerateInsightHandler(_db, provider, new CategoryAssignmentGuard(_db))
            .ExecuteAsync(new GenerateInsightRequest(_category.Id, "Source topic"));

        result.IsSuccess.Should().BeFalse();
        (await _db.TechInsights.CountAsync()).Should().Be(0);
    }

    private GenerateQuizHandler QuizHandler(IQuizGeneratorService provider) => new(_db, provider, new GenerateQuizValidator(), new CategoryAssignmentGuard(_db));
    private DocumentBook NewBook() => new() { Title = "Owned source", Slug = "owned-source", CategoryId = _category.Id, CreatedByUserId = _user.Id, Status = ProcessingStatus.Ready };
    private static TechInsight NewInsight(Guid categoryId) => new() { Slug = $"insight-{Guid.NewGuid():N}", Title = "Source topic", CategoryId = categoryId, IsPublished = true };
    private static QuizQuestion NewQuestion(Guid categoryId) => new() { Topic = "same topic", CategoryId = categoryId, Level = QuizLevel.Senior, QuestionText = "What does the source establish?", Options = ["A", "B", "C", "D"], ExplanationMarkdown = "Source explanation" };

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    private sealed class QuizProvider(Func<Guid, CancellationToken, Task<Result<List<QuizQuestion>>>> generate) : IQuizGeneratorService
    {
        public Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(string topic, Guid categoryId, QuizLevel level, int count, List<string> existingTitlesToAvoid, string locale = "en", CancellationToken cancellationToken = default) => generate(categoryId, cancellationToken);
    }
    private sealed class InsightProvider(Func<Guid, CancellationToken, Task<Result<TechInsight>>> generate) : ITechInsightGenerator
    {
        public IReadOnlyList<string> ExistingTitles { get; private set; } = [];

        public Task<Result<TechInsight>> GenerateInsightAsync(Guid categoryId, string? preferredTopic, List<string>? existingTitlesToAvoid = null, string locale = "en", CancellationToken cancellationToken = default)
        {
            ExistingTitles = existingTitlesToAvoid?.ToArray() ?? [];
            return generate(categoryId, cancellationToken);
        }
    }
}
