using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.KnowledgeGraph.DTOs;
using TechDaily.Application.Features.KnowledgeGraph.GetKnowledgeGraph;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetKnowledgeGraphQueryHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly GetKnowledgeGraphQueryHandler _handler;

    public GetKnowledgeGraphQueryHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _handler = new GetKnowledgeGraphQueryHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
    {
        // Arrange
        var query = new GetKnowledgeGraphQuery(Guid.Empty);

        // Act
        var result = await _handler.ExecuteAsync(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.Unauthorized);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAuthenticatedUserRequestsGraph_ShouldReturnAllNodeTypesAndEdges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "PostgreSQL MVCC Mechanics",
            Slug = "postgresql-mvcc",
            Category = Category.DatabaseStorage,
            Difficulty = Difficulty.Senior,
            DayOrder = 7,
            Summary = "Snapshot isolation and multi-version concurrency."
        };
        await _db.Topics.AddAsync(topic);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage,
            AuthorOrSourceUrl = "Martin Kleppmann",
            IsPublished = true,
            IsDeleted = false
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Storage and Retrieval",
            ChunkOrder = 1,
            OriginalTextMarkdown = "LSM-Tree and B-Tree storage."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var card = SpacedRepetitionCard.Create(user.Id, topic.Id);
        await _db.SpacedRepetitionCards.AddAsync(card);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "PostgreSQL uses multi-version concurrency control to ensure read consistency.",
            Note = "Essential for understanding vacuuming",
            Tags = new List<string> { "postgresql-mvcc", "storage" }
        };
        await _db.UserHighlights.AddAsync(highlight);

        await _db.SaveChangesAsync();

        var query = new GetKnowledgeGraphQuery(user.Id);

        // Act
        var result = await _handler.ExecuteAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Topic && n.Id == topic.Id.ToString());
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Book && n.Id == book.Id.ToString());
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Card && n.Id == card.Id.ToString());
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Highlight && n.Id == highlight.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.CardToTopic
            && e.Source == card.Id.ToString()
            && e.Target == topic.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToTopic
            && e.Source == book.Id.ToString()
            && e.Target == topic.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToBook
            && e.Source == highlight.Id.ToString()
            && e.Target == book.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToTopic
            && e.Source == highlight.Id.ToString()
            && e.Target == topic.Id.ToString());

        response.Stats.TotalNodes.Should().Be(4);
        response.Stats.NodeTypeCounts[GraphNodeType.Topic].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Book].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Card].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Highlight].Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_UserDataIsolation_UserAShouldNotSeeUserBCardsOrHighlights()
    {
        // Arrange
        var userA = new User { Id = Guid.NewGuid(), Email = "userA@techdaily.local", Name = "User A" };
        var userB = new User { Id = Guid.NewGuid(), Email = "userB@techdaily.local", Name = "User B" };
        await _db.Users.AddRangeAsync(userA, userB);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Memory Allocations",
            Slug = "memory-allocations",
            Category = Category.BackendDotNet,
            Difficulty = Difficulty.Intermediate,
            DayOrder = 2
        };
        await _db.Topics.AddAsync(topic);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "CLR via C#",
            Slug = "clr-via-csharp",
            Category = Category.BackendDotNet,
            IsPublished = true
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Garbage Collection Basics",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        // User A items
        var cardA = SpacedRepetitionCard.Create(userA.Id, topic.Id);
        var highlightA = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userA.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "User A quote on GC",
            Tags = new List<string> { "dotnet" }
        };

        // User B items
        var cardB = SpacedRepetitionCard.Create(userB.Id, topic.Id);
        var highlightB = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userB.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "User B secret quote",
            Tags = new List<string> { "secret" }
        };

        await _db.SpacedRepetitionCards.AddRangeAsync(cardA, cardB);
        await _db.UserHighlights.AddRangeAsync(highlightA, highlightB);
        await _db.SaveChangesAsync();

        // Act
        var resultA = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(userA.Id));

        // Assert
        resultA.IsSuccess.Should().BeTrue();
        var nodesA = resultA.Value.Nodes;

        nodesA.Should().Contain(n => n.Id == cardA.Id.ToString());
        nodesA.Should().NotContain(n => n.Id == cardB.Id.ToString());

        nodesA.Should().Contain(n => n.Id == highlightA.Id.ToString());
        nodesA.Should().NotContain(n => n.Id == highlightB.Id.ToString());

        resultA.Value.Stats.NodeTypeCounts[GraphNodeType.Card].Should().Be(1);
        resultA.Value.Stats.NodeTypeCounts[GraphNodeType.Highlight].Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_SM2MasteryCategorization_ShouldCategorizeCardsCorrectly()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "sm2@techdaily.local", Name = "SM2 Tester" };
        await _db.Users.AddAsync(user);

        var topic1 = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Topic 1",
            Slug = "topic-1",
            Category = Category.SystemDesign,
            Difficulty = Difficulty.Senior,
            DayOrder = 10
        };
        var topic2 = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Topic 2",
            Slug = "topic-2",
            Category = Category.SystemDesign,
            Difficulty = Difficulty.Senior,
            DayOrder = 11
        };
        var topic3 = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Topic 3",
            Slug = "topic-3",
            Category = Category.SystemDesign,
            Difficulty = Difficulty.Senior,
            DayOrder = 12
        };
        await _db.Topics.AddRangeAsync(topic1, topic2, topic3);

        // Card 1: Default new card -> Interval=1, EF=2.50 -> "Learning"
        var cardLearning = SpacedRepetitionCard.Create(user.Id, topic1.Id);

        // Card 2: 2 successful reviews -> Interval=6, EF=2.50 -> "Reviewing"
        var cardReviewing = SpacedRepetitionCard.Create(user.Id, topic2.Id);
        cardReviewing.ApplyReview(5);
        cardReviewing.ApplyReview(5);

        // Card 3: 4 successful reviews -> Interval >= 21, EF >= 2.2 -> "Mastered"
        var cardMastered = SpacedRepetitionCard.Create(user.Id, topic3.Id);
        cardMastered.ApplyReview(5);
        cardMastered.ApplyReview(5);
        cardMastered.ApplyReview(5);
        cardMastered.ApplyReview(5);

        await _db.SpacedRepetitionCards.AddRangeAsync(cardLearning, cardReviewing, cardMastered);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var nodes = result.Value.Nodes;

        var nodeLearning = nodes.Single(n => n.Id == cardLearning.Id.ToString());
        nodeLearning.Status.Should().Be(MasteryStatus.Learning);

        var nodeReviewing = nodes.Single(n => n.Id == cardReviewing.Id.ToString());
        nodeReviewing.Status.Should().Be(MasteryStatus.Reviewing);

        var nodeMastered = nodes.Single(n => n.Id == cardMastered.Id.ToString());
        nodeMastered.Status.Should().Be(MasteryStatus.Mastered);

        result.Value.Stats.MasteredCardsCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_SharedTagIntersections_ShouldDerivePairwiseEdgesForHighlightsSharingTags()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "tags@techdaily.local", Name = "Tag Tester" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Database Internals",
            Slug = "db-internals",
            Category = Category.DatabaseStorage,
            IsPublished = true
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Transactions",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight1 = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "MVCC eliminates read locks",
            Tags = new List<string> { "mvcc", "concurrency" }
        };

        var highlight2 = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "PostgreSQL snapshot isolation uses MVCC tuples",
            Tags = new List<string> { "MVCC", "postgres" }
        };

        var highlight3 = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "Network partition handling",
            Tags = new List<string> { "consensus", "raft" }
        };

        await _db.UserHighlights.AddRangeAsync(highlight1, highlight2, highlight3);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var edges = result.Value.Edges;

        var sharedTagEdge = edges.FirstOrDefault(e => e.RelationType == GraphRelationType.SharedTag
            && ((e.Source == highlight1.Id.ToString() && e.Target == highlight2.Id.ToString())
                || (e.Source == highlight2.Id.ToString() && e.Target == highlight1.Id.ToString())));

        sharedTagEdge.Should().NotBeNull();
        sharedTagEdge!.Label.Should().Be("mvcc");
        sharedTagEdge.Weight.Should().Be(1);

        // Highlight 3 shares no tags with 1 or 2
        edges.Should().NotContain(e => e.RelationType == GraphRelationType.SharedTag
            && (e.Source == highlight3.Id.ToString() || e.Target == highlight3.Id.ToString()));
    }

    [Fact]
    public async Task ExecuteAsync_NewUserWithZeroCardsOrHighlights_ShouldReturnTopicsAndBooksWithEmptyUserCollections()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "newbie@techdaily.local", Name = "New User" };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Web Vitals & Performance",
            Slug = "web-vitals",
            Category = Category.FrontendWeb,
            Difficulty = Difficulty.Intermediate,
            DayOrder = 1
        };
        await _db.Topics.AddAsync(topic);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "High Performance Browser Networking",
            Slug = "hpbn",
            Category = Category.FrontendWeb,
            IsPublished = true
        };
        await _db.DocumentBooks.AddAsync(book);

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.Nodes.Should().HaveCount(2);
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Topic);
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Book);
        response.Nodes.Should().NotContain(n => n.Type == GraphNodeType.Card);
        response.Nodes.Should().NotContain(n => n.Type == GraphNodeType.Highlight);

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToTopic);

        response.Stats.TotalNodes.Should().Be(2);
        response.Stats.NodeTypeCounts[GraphNodeType.Topic].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Book].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Card].Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Highlight].Should().Be(0);
        response.Stats.MasteredCardsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_HighlightToTopic_ShouldDeriveEdgeWhenTagMatchesTopicTitleOrSlug()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "tagmatch@techdaily.local", Name = "Matcher" };
        await _db.Users.AddAsync(user);

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            Title = "Garbage Collection & LOH",
            Slug = "garbage-collection-loh",
            Category = Category.BackendDotNet,
            Difficulty = Difficulty.Senior,
            DayOrder = 3
        };
        await _db.Topics.AddAsync(topic);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Pro .NET Memory Management",
            Slug = "pro-dotnet-memory",
            Category = Category.BackendDotNet,
            IsPublished = true
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Large Object Heap",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "Objects larger than 85,000 bytes go directly to the Large Object Heap.",
            Tags = new List<string> { "garbage-collection-loh" }
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToTopic
            && e.Source == highlight.Id.ToString()
            && e.Target == topic.Id.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_UnpublishedOrDeletedBooks_ShouldBeExcludedFromGraph()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "filter@techdaily.local", Name = "Filter Tester" };
        await _db.Users.AddAsync(user);

        var publishedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Published Book",
            Slug = "pub-book",
            Category = Category.BackendDotNet,
            IsPublished = true,
            IsDeleted = false
        };

        var unpublishedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Draft Book",
            Slug = "draft-book",
            Category = Category.BackendDotNet,
            IsPublished = false,
            IsDeleted = false
        };

        var deletedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Book",
            Slug = "deleted-book",
            Category = Category.BackendDotNet,
            IsPublished = true,
            IsDeleted = true
        };

        await _db.DocumentBooks.AddRangeAsync(publishedBook, unpublishedBook, deletedBook);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var bookNodes = result.Value.Nodes.Where(n => n.Type == GraphNodeType.Book).ToList();

        bookNodes.Should().Contain(n => n.Id == publishedBook.Id.ToString());
        bookNodes.Should().NotContain(n => n.Id == unpublishedBook.Id.ToString());
        bookNodes.Should().NotContain(n => n.Id == deletedBook.Id.ToString());
    }
}
