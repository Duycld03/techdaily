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

    private async Task<(Guid UserId, Topic Topic1, Topic Topic2)> SeedUserAndTopicsAsync()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Deck Manager Dev"
        };
        await _db.Users.AddAsync(user);

        var topic1 = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Postgres Indexing B-Trees",
            Slug = "postgres-indexing",
            Category = Category.DatabaseStorage,
            Difficulty = Difficulty.Senior,
            Summary = "Detailed guide on Postgres B-tree index structures."
        };

        var topic2 = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Vue 3 Reactivity Engine",
            Slug = "vue-reactivity",
            Category = Category.FrontendWeb,
            Difficulty = Difficulty.Senior,
            Summary = "Deep dive into Proxy-based reactivity in Vue 3."
        };

        await _db.Topics.AddRangeAsync(topic1, topic2);
        await _db.SaveChangesAsync();

        return (userId, topic1, topic2);
    }

    [Fact]
    public async Task GetReviewCards_ShouldReturnActiveCardsAndAccurateDeckStatistics()
    {
        // Arrange
        var (userId, topic1, topic2) = await SeedUserAndTopicsAsync();

        // Card 1: Learning (RepetitionCount = 0)
        var card1 = SpacedRepetitionCard.Create(userId, topic1.Id);

        // Card 2: Reviewing (RepetitionCount = 2)
        var card2 = SpacedRepetitionCard.Create(userId, topic2.Id);
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
        var cardOtherUser = SpacedRepetitionCard.Create(otherUserId, topic1.Id);

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
        var (userId, topic1, topic2) = await SeedUserAndTopicsAsync();

        var card1 = SpacedRepetitionCard.Create(userId, topic1.Id); // Title: Postgres Indexing B-Trees
        var card2 = SpacedRepetitionCard.Create(userId, topic2.Id); // Title: Vue 3 Reactivity Engine

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
        var (userId, topic1, topic2) = await SeedUserAndTopicsAsync();

        var cardLearning = SpacedRepetitionCard.Create(userId, topic1.Id);

        var cardReviewing = SpacedRepetitionCard.Create(userId, topic2.Id);
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
        var (userId, topic1, _) = await SeedUserAndTopicsAsync();

        var topicCard = SpacedRepetitionCard.Create(userId, topic1.Id);
        var highlightCard = new SpacedRepetitionCard
        {
            UserId = userId,
            SourceType = CardSourceType.Highlight,
            FrontMarkdown = "Front",
            BackMarkdown = "Back"
        };

        await _db.SpacedRepetitionCards.AddRangeAsync(topicCard, highlightCard);
        await _db.SaveChangesAsync();

        var handler = new GetReviewCardsHandler(_db);

        // Act
        var resHighlight = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, SourceType: CardSourceType.Highlight));
        var resTopic = await handler.ExecuteAsync(new GetReviewCardsRequest(userId, SourceType: CardSourceType.Topic));

        // Assert
        resHighlight.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(highlightCard.Id);
        resTopic.Value.Cards.Should().ContainSingle().Which.Id.Should().Be(topicCard.Id);
    }

    [Fact]
    public async Task GetReviewCards_Pagination_ShouldApplyPageAndPageSize()
    {
        // Arrange
        var (userId, topic1, _) = await SeedUserAndTopicsAsync();

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
        resPage1.Value.Cards.Should().HaveCount(2);

        resPage2.Value.Page.Should().Be(2);
        resPage2.Value.Cards.Should().HaveCount(2);

        resPage3.Value.Page.Should().Be(3);
        resPage3.Value.Cards.Should().HaveCount(1);
    }
}
