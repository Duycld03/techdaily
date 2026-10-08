using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Review.GetReviewCards;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetReviewCardsHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public GetReviewCardsHandlerTests()
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

    private async Task<Guid> SeedUserAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Deck Manager Dev"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task GetReviewCards_ShouldReturnActiveCardsAndAccurateDeckStatistics()
    {
        // Arrange
        var userId = await SeedUserAsync();

        // Card 1: Learning (RepetitionCount = 0)
        var card1 = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Postgres Indexing B-Trees", "B-tree index structures");

        // Card 2: Reviewing (RepetitionCount = 2)
        var card2 = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Vue 3 Reactivity Engine", "Proxy-based reactivity");
        card2.ApplyReview(4);
        card2.ApplyReview(4);

        // Card 3: Mastered (RepetitionCount = 4)
        var card3 = new SpacedRepetitionCard
        {
            UserId = userId,
            SourceType = CardSourceType.Highlight,
            FrontMarkdown = "What is WAL?",
            BackMarkdown = "Write Ahead Log"
        };
        card3.ApplyReview(5);
        card3.ApplyReview(5);
        card3.ApplyReview(5);
        card3.ApplyReview(5);

        // Card 4: Deleted (should be ignored in statistics and list)
        var cardDeleted = new SpacedRepetitionCard
        {
            UserId = userId,
            SourceType = CardSourceType.Highlight,
            FrontMarkdown = "Deleted Front",
            BackMarkdown = "Deleted Back"
        };
        cardDeleted.SoftDelete();

        // Card 5: Other user's card (should be ignored)
        var otherUserId = Guid.NewGuid();
        var otherUser = new User
        {
            Id = otherUserId,
            Email = $"other_{otherUserId}@techdaily.local",
            Name = "Other Dev"
        };
        await _db.Users.AddAsync(otherUser);
        var cardOtherUser = SpacedRepetitionCard.CreateFromDrillMistake(otherUserId, null, "Other Front", "Other Back");

        await _db.SpacedRepetitionCards.AddRangeAsync(card1, card2, card3, cardDeleted, cardOtherUser);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);
        var request = new GetReviewCardsRequest(userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
        result.Value.Cards.Should().HaveCount(3);

        var stats = result.Value.Statistics;
        stats.TotalCards.Should().Be(3);
        stats.LearningCount.Should().Be(1);
        stats.ReviewingCount.Should().Be(1);
        stats.MasteredCount.Should().Be(1);
        stats.LearningCards.Should().Be(1);
        stats.ReviewingCards.Should().Be(1);
        stats.MasteredCards.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewCards_FilterBySearch_ShouldMatchFrontBackOrTopicTitle()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var card1 = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Postgres Indexing B-Trees", "B-tree index structures");
        var card2 = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Vue 3 Reactivity Engine", "Proxy-based reactivity");

        var card3 = new SpacedRepetitionCard
        {
            UserId = userId,
            SourceType = CardSourceType.Highlight,
            FrontMarkdown = "Async/Await internals",
            BackMarkdown = "Task and SynchronizationContext"
        };

        await _db.SpacedRepetitionCards.AddRangeAsync(card1, card2, card3);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Search 1: by topic title substring ("Postgres")
        var res1 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Search: "postgres"));
        res1.Value.Cards.Should().Contain(c => c.Id == card1.Id);

        // Search 2: by front markdown ("Async/Await")
        var res2 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Search: "async/await"));
        res2.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(card3.Id);

        // Search 3: by back markdown ("SynchronizationContext")
        var res3 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Search: "synchronizationcontext"));
        res3.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(card3.Id);
    }

    [Fact]
    public async Task GetReviewCards_FilterByStatus_ShouldReturnOnlyCardsWithRequestedStatus()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var cardLearning = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Learning Front", "Learning Back");

        var cardReviewing = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Reviewing Front", "Reviewing Back");
        cardReviewing.ApplyReview(4);

        await _db.SpacedRepetitionCards.AddRangeAsync(cardLearning, cardReviewing);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Act
        var resLearning = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Status: CardStatus.Learning));
        var resReviewing = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Status: CardStatus.Reviewing));

        // Assert
        resLearning.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(cardLearning.Id);
        resReviewing.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(cardReviewing.Id);

        // Global statistics should still reflect all active cards
        resLearning.Value.Statistics.TotalCards.Should().Be(2);
    }

    [Fact]
    public async Task GetReviewCards_FilterBySourceType_ShouldReturnOnlyMatchingSourceType()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var chunkCard = SpacedRepetitionCard.CreateFromDrillMistake(userId, null, "Chunk Front", "Chunk Back");
        var highlightCard = new SpacedRepetitionCard
        {
            UserId = userId,
            SourceType = CardSourceType.Highlight,
            FrontMarkdown = "Front",
            BackMarkdown = "Back"
        };

        await _db.SpacedRepetitionCards.AddRangeAsync(chunkCard, highlightCard);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Act
        var resHighlight = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, SourceType: CardSourceType.Highlight));
        var resChunk = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, SourceType: CardSourceType.DocumentChunk));

        // Assert
        resHighlight.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(highlightCard.Id);
        resChunk.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(chunkCard.Id);
    }

    [Fact]
    public async Task GetReviewCards_Pagination_ShouldApplyPageAndPageSize()
    {
        // Arrange
        var userId = await SeedUserAsync();

        var cards = Enumerable.Range(1, 5)
            .Select(i => new SpacedRepetitionCard
            {
                UserId = userId,
                SourceType = CardSourceType.Highlight,
                FrontMarkdown = $"Front {i}",
                BackMarkdown = $"Back {i}"
            })
            .ToList();

        await _db.SpacedRepetitionCards.AddRangeAsync(cards);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Act - Page 1 of size 2
        var resPage1 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Page: 1, PageSize: 2));
        var resPage2 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Page: 2, PageSize: 2));
        var resPage3 = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, Page: 3, PageSize: 2));

        // Assert
        resPage1.Value.TotalCount.Should().Be(5);
        resPage1.Value.Page.Should().Be(1);
        resPage1.Value.PageSize.Should().Be(2);
        resPage1.Value.TotalPages.Should().Be(3);
        resPage1.Value.Cards.Should().HaveCount(2);
        resPage2.Value.Page.Should().Be(2);
        resPage2.Value.Cards.Should().HaveCount(2);

        resPage3.Value.Page.Should().Be(3);
        resPage3.Value.Cards.Should().HaveCount(1);
    }
    [Fact]
    public async Task GetReviewCards_WhenCardFromQuizMistake_ShouldProjectQuizTopicCategoryAndDifficulty()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var quizQuestion = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "MemoryPool Zero-Allocation",
            Category = Category.BackendRuntime,
            Level = QuizLevel.Mastery,
            QuestionText = "How do we rent buffers zero-alloc?",
            ExplanationMarkdown = "Via ArrayPool<T>.Shared or MemoryPool<T>.",
            CorrectOptionIndex = 0,
            Options = new List<string> { "MemoryPool", "new byte[]" }
        };
        await _db.QuizQuestions.AddAsync(quizQuestion);

        var card = SpacedRepetitionCard.CreateFromQuizMistake(
            userId,
            quizQuestion.Id,
            quizQuestion.QuestionText,
            quizQuestion.ExplanationMarkdown);
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetReviewCardsRequest(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Cards.Should().HaveCount(1);
        var dto = result.Value.Cards.First();
        dto.TopicTitle.Should().Be("MemoryPool Zero-Allocation");
        dto.Category.Should().Be(Category.BackendRuntime);
        dto.Difficulty.Should().Be(Difficulty.Lead);
    }
}
