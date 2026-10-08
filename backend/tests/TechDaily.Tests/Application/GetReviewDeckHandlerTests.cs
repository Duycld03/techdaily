using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetReviewDeckHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public GetReviewDeckHandlerTests()
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

    private async Task<User> SeedUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Deck Review Dev"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetReviewDeck_WhenCardFromQuizMistake_ShouldProjectQuizTopicCategoryAndDifficulty()
    {
        // Arrange
        var user = await SeedUserAsync();
        var quizQuestion = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Kestrel Socket Pipeline",
            Category = Category.BackendRuntime,
            Level = QuizLevel.Mastery,
            QuestionText = "How does Kestrel manage socket buffers?",
            ExplanationMarkdown = "Via PipeReader and MemoryPool zero-allocation.",
            CorrectOptionIndex = 0,
            Options = new List<string> { "PipeReader", "ThreadPool" }
        };
        await _db.QuizQuestions.AddAsync(quizQuestion);

        var card = SpacedRepetitionCard.CreateFromQuizMistake(
            user.Id,
            quizQuestion.Id,
            quizQuestion.QuestionText,
            quizQuestion.ExplanationMarkdown,
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetReviewDeckRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DueCards.Should().HaveCount(1);
        var dto = result.Value.DueCards.First();
        dto.TopicTitle.Should().Be("Kestrel Socket Pipeline");
        dto.Category.Should().Be(Category.BackendRuntime);
        dto.Difficulty.Should().Be(Difficulty.Lead);
    }

    [Fact]
    public async Task GetReviewDeck_WhenCardFromDocumentChunk_ShouldProjectChapterTitleAndBookCategory()
    {
        // Arrange
        var user = await SeedUserAsync();
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            DocumentBook = book,
            ChapterTitle = "Chapter 3: Storage and Retrieval",
            OriginalTextMarkdown = "LSM-Trees and B-Trees..."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(
            user.Id,
            chunk.Id,
            "What is an SSTable?",
            "Sorted String Table",
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewDeckHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetReviewDeckRequest(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DueCards.Should().HaveCount(1);
        var dto = result.Value.DueCards.First();
        dto.TopicTitle.Should().Be("Chapter 3: Storage and Retrieval");
        dto.Category.Should().Be(Category.DatabaseStorage);
        dto.Difficulty.Should().Be(Difficulty.Senior);
    }
}
