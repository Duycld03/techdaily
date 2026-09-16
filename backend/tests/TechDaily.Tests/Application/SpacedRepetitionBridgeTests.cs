using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.CreateCardFromHighlight;
using TechDaily.Application.Features.Review.CreateCardFromQuizMistake;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class SpacedRepetitionBridgeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly FakeGeminiAiService _fakeGemini;

    public SpacedRepetitionBridgeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _fakeGemini = new FakeGeminiAiService();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private class FakeGeminiAiService : IGeminiAiService
    {
        public int CallCount = 0;
        public string MockFront = "What is the primary trade-off of LSM-Trees?";
        public string MockBack = "LSM-Trees optimize for sequential write throughput at the expense of read amplification.";

        public Task<(string Front, string Back)> SynthesizeActiveRecallCardAsync(
            string quote,
            string? note,
            string chapterTitle,
            string locale = "en",
            CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult((MockFront, MockBack));
        }
    }

    [Fact]
    public async Task CreateCardFromHighlight_ShouldCreateCardWithActiveRecall_WhenHighlightExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@test.com", Name = "Test User" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook { Id = Guid.NewGuid(), Title = "Designing Data-Intensive Applications", Slug = "ddia" };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Storage and Retrieval",
            OriginalTextMarkdown = "LSM-Trees append writes sequentially to a WAL."
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentChunkId = chunk.Id,
            SelectedText = "LSM-Trees append writes sequentially to a WAL.",
            Note = "Converts random disk I/O into sequential append writes."
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        var handler = new CreateCardFromHighlightHandler(_db, _fakeGemini);

        // Act
        var result = await handler.ExecuteAsync(new CreateCardFromHighlightRequest(highlight.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Front.Should().Be(_fakeGemini.MockFront);
        result.Value.Back.Should().Be(_fakeGemini.MockBack);
        _fakeGemini.CallCount.Should().Be(1);

        var cardInDb = await _db.SpacedRepetitionCards.FindAsync(result.Value.CardId);
        cardInDb.Should().NotBeNull();
        cardInDb!.SourceType.Should().Be(CardSourceType.Highlight);
        cardInDb.SourceHighlightId.Should().Be(highlight.Id);
        cardInDb.TopicId.Should().BeNull();
    }

    [Fact]
    public async Task CreateCardFromHighlight_ShouldBeIdempotent_WhenCardAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user2@test.com", Name = "Test User 2" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook { Id = Guid.NewGuid(), Title = "Book", Slug = "book" };
        var chunk = new DocumentChunk { Id = Guid.NewGuid(), DocumentBookId = book.Id, ChunkOrder = 1, ChapterTitle = "Ch 1", OriginalTextMarkdown = "Text" };
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentChunkId = chunk.Id,
            SelectedText = "Sample quote",
            Note = "Sample note"
        };
        await _db.UserHighlights.AddAsync(highlight);

        var existingCard = SpacedRepetitionCard.CreateFromHighlight(userId, highlight.Id, "Existing Front", "Existing Back");
        await _db.SpacedRepetitionCards.AddAsync(existingCard);
        await _db.SaveChangesAsync();

        var handler = new CreateCardFromHighlightHandler(_db, _fakeGemini);

        // Act
        var result = await handler.ExecuteAsync(new CreateCardFromHighlightRequest(highlight.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CardId.Should().Be(existingCard.Id);
        result.Value.Front.Should().Be("Existing Front");
        result.Value.Back.Should().Be("Existing Back");
        _fakeGemini.CallCount.Should().Be(0); // Did not invoke Gemini again
    }

    [Fact]
    public async Task CreateCardFromQuizMistake_ShouldCreateCardWithFormattedQuestionAndAnswer_WhenQuestionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user3@test.com", Name = "Test User 3" };
        await _db.Users.AddAsync(user);

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "PostgreSQL MVCC",
            Category = Category.DatabaseStorage,
            Level = QuizLevel.Senior,
            QuestionText = "How does PostgreSQL handle dead tuples created by updates?",
            Options = new List<string>
            {
                "It immediately deletes them from disk",
                "AUTOVACUUM marks them as reusable for new inserts",
                "It truncates the table file",
                "Dead tuples are kept indefinitely without reclaim"
            },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "AUTOVACUUM reclaims dead tuple slots and updates the Free Space Map (FSM)."
        };
        await _db.QuizQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new CreateCardFromQuizMistakeHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new CreateCardFromQuizMistakeRequest(question.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();

        var cardInDb = await _db.SpacedRepetitionCards.FindAsync(result.Value.CardId);
        cardInDb.Should().NotBeNull();
        cardInDb!.SourceType.Should().Be(CardSourceType.QuizMistake);
        cardInDb.SourceQuizQuestionId.Should().Be(question.Id);
        cardInDb.TopicId.Should().BeNull();
        cardInDb.FrontMarkdown.Should().Contain("How does PostgreSQL handle dead tuples created by updates?");
        cardInDb.FrontMarkdown.Should().Contain("- **A.** It immediately deletes them from disk");
        cardInDb.FrontMarkdown.Should().Contain("- **B.** AUTOVACUUM marks them as reusable for new inserts");
        cardInDb.BackMarkdown.Should().Contain("**Correct Option: B. AUTOVACUUM marks them as reusable for new inserts**");
        cardInDb.BackMarkdown.Should().Contain("AUTOVACUUM reclaims dead tuple slots");
    }

    [Fact]
    public async Task CreateCardFromQuizMistake_ShouldBeIdempotent_WhenCardAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user4@test.com", Name = "Test User 4" };
        await _db.Users.AddAsync(user);

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Topic",
            QuestionText = "Question?",
            Options = new List<string> { "A", "B" },
            CorrectOptionIndex = 0
        };
        await _db.QuizQuestions.AddAsync(question);

        var existingCard = SpacedRepetitionCard.CreateFromQuizMistake(userId, question.Id, "Front", "Back");
        await _db.SpacedRepetitionCards.AddAsync(existingCard);
        await _db.SaveChangesAsync();

        var handler = new CreateCardFromQuizMistakeHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new CreateCardFromQuizMistakeRequest(question.Id, userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CardId.Should().Be(existingCard.Id);
    }

    [Fact]
    public async Task GradeReviewCard_WithQuizMistakeSource_ShouldSyncMastery_WhenGradeIs3OrHigher()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user5@test.com", Name = "Test User 5" };
        await _db.Users.AddAsync(user);

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Kestrel",
            QuestionText = "Question?",
            Options = new List<string> { "Opt A" },
            CorrectOptionIndex = 0
        };
        await _db.QuizQuestions.AddAsync(question);

        var quizProgress = new UserQuizProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = question.Id,
            IsMastered = false,
            CorrectCount = 0,
            IncorrectCount = 2
        };
        await _db.UserQuizProgresses.AddAsync(quizProgress);

        var card = SpacedRepetitionCard.CreateFromQuizMistake(userId, question.Id, "Front", "Back");
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var validator = new GradeReviewCardValidator();
        var handler = new GradeReviewCardHandler(_db, validator);

        // Act - Grade with 4 (Passing score >= 3)
        var result = await handler.ExecuteAsync(new GradeReviewCardRequest(card.Id, userId, 4));

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updatedProgress = await _db.UserQuizProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.QuestionId == question.Id);
        updatedProgress.Should().NotBeNull();
        updatedProgress!.IsMastered.Should().BeTrue();
        updatedProgress.CorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewDeck_ShouldReturnFrontAndBack_ForNonTopicCards()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user6@test.com", Name = "Test User 6" };
        await _db.Users.AddAsync(user);
        var book = new DocumentBook { Id = Guid.NewGuid(), Title = "Book", Slug = "book" };
        var chunk = new DocumentChunk { Id = Guid.NewGuid(), DocumentBookId = book.Id, ChunkOrder = 1, ChapterTitle = "Ch 1", OriginalTextMarkdown = "Text" };
        var highlight = new UserHighlight { Id = Guid.NewGuid(), UserId = userId, DocumentChunkId = chunk.Id, SelectedText = "Text" };
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.UserHighlights.AddAsync(highlight);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var card = SpacedRepetitionCard.CreateFromHighlight(userId, highlight.Id, "Custom Front Question", "Custom Back Explanation", today);
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetReviewDeckRequest(userId, today));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCardsDue.Should().Be(1);
        var dueCard = result.Value.DueCards.First();
        dueCard.SourceType.Should().Be(CardSourceType.Highlight);
        dueCard.TopicId.Should().BeNull();
        dueCard.FrontMarkdown.Should().Be("Custom Front Question");
        dueCard.BackMarkdown.Should().Be("Custom Back Explanation");
        dueCard.TopicTitle.Should().Be("Custom Front Question");
        dueCard.TopicSummary.Should().Be("Custom Back Explanation");
    }
}
