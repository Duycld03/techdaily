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
    public async Task ExecuteAsync_WhenAuthenticatedUserRequestsGraph_ShouldReturnScopedNodesAndEdges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);


        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage,
            AuthorOrSourceUrl = "Martin Kleppmann",
            IsPublished = true,
            IsDeleted = false,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "PostgreSQL MVCC Mechanics",
            ChunkOrder = 1,
            OriginalTextMarkdown = "LSM-Tree and B-Tree storage."
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "MVCC Question", "MVCC Answer");
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
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Pillar && n.Id == "pillar-DatabaseStorage");
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Chunk && n.Id == chunk.Id.ToString());
        response.Nodes.Should().NotContain(n => n.Type == "topic");
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Book && n.Id == book.Id.ToString());
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Card && n.Id == card.Id.ToString());
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Highlight && n.Id == highlight.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToPillar);
        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.ChunkToBook
            && e.Source == chunk.Id.ToString()
            && e.Target == book.Id.ToString());
        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.CardToChunk
            && e.Source == card.Id.ToString()
            && e.Target == chunk.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToBook
            && e.Source == highlight.Id.ToString()
            && e.Target == book.Id.ToString());

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToChunk
            && e.Source == highlight.Id.ToString()
            && e.Target == chunk.Id.ToString());

        // Every artifact is DatabaseStorage, so exactly one pillar hub is emitted.
        response.Stats.TotalNodes.Should().Be(5);
        response.Stats.NodeTypeCounts[GraphNodeType.Pillar].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Chunk].Should().Be(1);
        response.Stats.NodeTypeCounts[GraphNodeType.Topic].Should().Be(0);
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


        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "CLR via C#",
            Slug = "clr-via-csharp",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = userA.Id
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
        var cardA = SpacedRepetitionCard.CreateFromDrillMistake(userA.Id, chunk.Id, "Front A", "Back A");
        var highlightA = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = userA.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "User A quote on GC",
            Tags = new List<string> { "dotnet" }
        };

        // User B items
        var cardB = SpacedRepetitionCard.CreateFromDrillMistake(userB.Id, chunk.Id, "Front B", "Back B");
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
    public async Task ExecuteAsync_BookNodes_ShouldExcludeBooksOwnedByOtherUsers()
    {
        // Arrange — task 1.1: books are scoped to the caller (parity with the Library page).
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@techdaily.local", Name = "Owner" };
        var stranger = new User { Id = Guid.NewGuid(), Email = "stranger@techdaily.local", Name = "Stranger" };
        await _db.Users.AddRangeAsync(owner, stranger);

        var ownBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "My Imported Book",
            Slug = "my-book",
            Category = Category.SystemDesign,
            IsPublished = true,
            CreatedByUserId = owner.Id
        };
        var strangerBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Someone Else's Book",
            Slug = "stranger-book",
            Category = Category.SystemDesign,
            IsPublished = true,
            CreatedByUserId = stranger.Id
        };
        await _db.DocumentBooks.AddRangeAsync(ownBook, strangerBook);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(owner.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var bookNodes = result.Value.Nodes.Where(n => n.Type == GraphNodeType.Book).ToList();
        bookNodes.Should().Contain(n => n.Id == ownBook.Id.ToString());
        bookNodes.Should().NotContain(n => n.Id == strangerBook.Id.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_OwnerBooks_ShouldExcludeUnpublishedAndSoftDeleted()
    {
        // Arrange — book nodes require owned + published + not-deleted.
        var user = new User { Id = Guid.NewGuid(), Email = "filter@techdaily.local", Name = "Filter Tester" };
        await _db.Users.AddAsync(user);

        var publishedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Published Book",
            Slug = "pub-book",
            Category = Category.BackendRuntime,
            IsPublished = true,
            IsDeleted = false,
            CreatedByUserId = user.Id
        };

        var unpublishedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Draft Book",
            Slug = "draft-book",
            Category = Category.BackendRuntime,
            IsPublished = false,
            IsDeleted = false,
            CreatedByUserId = user.Id
        };

        var deletedBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Book",
            Slug = "deleted-book",
            Category = Category.BackendRuntime,
            IsPublished = true,
            IsDeleted = true,
            CreatedByUserId = user.Id
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

    [Fact]
    public async Task ExecuteAsync_ChunkNodes_ShouldEmitOnlyChunksTheUserHasTouched()
    {
        // Arrange — a chunk is emitted only when a card or highlight references it.
        var user = new User { Id = Guid.NewGuid(), Email = "touched@techdaily.local", Name = "Touched" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Backend Book",
            Slug = "backend-book",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);

        var touchedChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Chunk A",
            ChunkOrder = 1
        };
        var untouchedChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Chunk B",
            ChunkOrder = 2
        };
        await _db.DocumentChunks.AddRangeAsync(touchedChunk, untouchedChunk);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, touchedChunk.Id, "Question", "Answer");
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var chunkNodes = result.Value.Nodes.Where(n => n.Type == GraphNodeType.Chunk).ToList();

        chunkNodes.Should().Contain(n => n.Id == touchedChunk.Id.ToString());
        chunkNodes.Should().NotContain(n => n.Id == untouchedChunk.Id.ToString());

        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.ChunkToBook
            && e.Source == touchedChunk.Id.ToString()
            && e.Target == book.Id.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_PillarHubs_ShouldEmitOnlyForActiveCategories()
    {
        // Arrange — task 1.3: only Backend & Runtime activity, so exactly one pillar hub is returned.
        var user = new User { Id = Guid.NewGuid(), Email = "pillar@techdaily.local", Name = "Pillar User" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Backend Book",
            Slug = "backend-book",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Async I/O and Threads",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "Question", "Answer");
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pillars = result.Value.Nodes.Where(n => n.Type == GraphNodeType.Pillar).ToList();

        pillars.Should().ContainSingle();
        pillars.Single().Id.Should().Be("pillar-BackendRuntime");
        result.Value.Stats.NodeTypeCounts[GraphNodeType.Pillar].Should().Be(1);

        pillars.Should().NotContain(p => p.Id == "pillar-FrontendWeb");
        pillars.Should().NotContain(p => p.Id == "pillar-DatabaseStorage");
        pillars.Should().NotContain(p => p.Id == "pillar-SystemDesign");
        pillars.Should().NotContain(p => p.Id == "pillar-EngineeringCraft");
        pillars.Should().NotContain(p => p.Id == "pillar-MentalModels");
        pillars.Should().NotContain(p => p.Id == "pillar-HabitsProductivity");
    }

    [Fact]
    public async Task ExecuteAsync_CurriculumTitledBook_ShouldConnectOnlyToItsOwnPillar_NoFanOut()
    {
        // Arrange — task 1.4: the removed master-curriculum fan-out no longer links a book to all pillars.
        var user = new User { Id = Guid.NewGuid(), Email = "curriculum@techdaily.local", Name = "Curriculum User" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "30-Day Senior Engineer Curriculum",
            Slug = "30-day-senior-curriculum",
            Category = Category.EngineeringCraft,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var bookToPillar = result.Value.Edges
            .Where(e => e.RelationType == GraphRelationType.BookToPillar && e.Source == book.Id.ToString())
            .ToList();

        bookToPillar.Should().ContainSingle();
        bookToPillar.Single().Target.Should().Be("pillar-EngineeringCraft");
    }

    [Fact]
    public async Task ExecuteAsync_BookCategory_ShouldUseStoredCategoryWithoutCrossPillarRemap()
    {
        // Arrange — task 1.4: GetEffectiveBookCategory's FrontendWeb->BackendRuntime remap is removed.
        var user = new User { Id = Guid.NewGuid(), Email = "category@techdaily.local", Name = "Category User" };
        await _db.Users.AddAsync(user);

        // Historically remapped to BackendRuntime by keyword; now kept as stored.
        var aspnetBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "ASP.NET Core Web APIs",
            Slug = "aspnet-core-10.0",
            Category = Category.FrontendWeb,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(aspnetBook);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var node = result.Value.Nodes.Single(n => n.Id == aspnetBook.Id.ToString());
        node.Category.Should().Be(Category.FrontendWeb.ToString());

        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToPillar
            && e.Source == aspnetBook.Id.ToString()
            && e.Target == "pillar-FrontendWeb");
    }

    [Fact]
    public async Task ExecuteAsync_ChunkToBook_ShouldConnectChunksWithoutCartesianBlowout()
    {
        // Arrange — direct ChunkToBook edges are generated only for that book's own emitted chunks.
        var user = new User { Id = Guid.NewGuid(), Email = "cartesian@techdaily.local", Name = "Cartesian User" };
        await _db.Users.AddAsync(user);

        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Pro .NET Garbage Collection",
            Slug = "pro-dotnet-gc",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Kubernetes in Action",
            Slug = "k8s-action",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddRangeAsync(book1, book2);

        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book1.Id,
            ChapterTitle = "Garbage Collection",
            ChunkOrder = 1
        };
        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book2.Id,
            ChapterTitle = "Kubernetes Networking",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddRangeAsync(chunk1, chunk2);

        var card1 = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk1.Id, "Q1", "A1");
        var card2 = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk2.Id, "Q2", "A2");
        await _db.SpacedRepetitionCards.AddRangeAsync(card1, card2);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.ChunkToBook
            && e.Source == chunk1.Id.ToString()
            && e.Target == book1.Id.ToString());

        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.ChunkToBook
            && e.Source == chunk2.Id.ToString()
            && e.Target == book2.Id.ToString());

        // No cross-linking between chunk1 and book2
        result.Value.Edges.Should().NotContain(e => e.RelationType == GraphRelationType.ChunkToBook
            && e.Source == chunk1.Id.ToString()
            && e.Target == book2.Id.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_SM2MasteryCategorization_ShouldCategorizeCardsCorrectly()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "sm2@techdaily.local", Name = "SM2 Tester" };
        await _db.Users.AddAsync(user);


        // Card 1: Default new card -> Interval=1, EF=2.50 -> "Learning"
        var cardLearning = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, null, "F1", "B1");

        // Card 2: 2 successful reviews -> Interval=6, EF=2.50 -> "Reviewing"
        var cardReviewing = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, null, "F2", "B2");
        cardReviewing.ApplyReview(5);
        cardReviewing.ApplyReview(5);

        // Card 3: 4 successful reviews -> Interval >= 21, EF >= 2.2 -> "Mastered"
        var cardMastered = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, null, "F3", "B3");
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
            IsPublished = true,
            CreatedByUserId = user.Id
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
    public async Task ExecuteAsync_HighlightToChunk_ShouldDeriveEdgeWhenHighlightTargetsChunk()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "tagmatch@techdaily.local", Name = "Matcher" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Pro .NET Memory Management",
            Slug = "pro-dotnet-memory",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
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

        // Assert — the chunk is emitted (referenced by highlight), and HighlightToChunk + HighlightToBook edges are derived.
        result.IsSuccess.Should().BeTrue();
        result.Value.Nodes.Should().Contain(n => n.Type == GraphNodeType.Chunk && n.Id == chunk.Id.ToString());
        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToChunk
            && e.Source == highlight.Id.ToString()
            && e.Target == chunk.Id.ToString());
        result.Value.Edges.Should().Contain(e => e.RelationType == GraphRelationType.HighlightToBook
            && e.Source == highlight.Id.ToString()
            && e.Target == book.Id.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_UserWithNoArtifacts_ShouldReturnEmptyGraph()
    {
        // Arrange — task 1.6: a brand-new user with no owned artifacts gets an empty personal graph,
        // even when seeded topics and other users' books exist.
        var newUser = new User { Id = Guid.NewGuid(), Email = "newbie@techdaily.local", Name = "New User" };
        var otherUser = new User { Id = Guid.NewGuid(), Email = "other@techdaily.local", Name = "Other" };
        await _db.Users.AddRangeAsync(newUser, otherUser);


        var othersBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "High Performance Browser Networking",
            Slug = "hpbn",
            Category = Category.FrontendWeb,
            IsPublished = true,
            CreatedByUserId = otherUser.Id
        };
        await _db.DocumentBooks.AddAsync(othersBook);

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(newUser.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.Nodes.Should().BeEmpty();
        response.Edges.Should().BeEmpty();

        response.Stats.TotalNodes.Should().Be(0);
        response.Stats.TotalEdges.Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Pillar].Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Topic].Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Book].Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Card].Should().Be(0);
        response.Stats.NodeTypeCounts[GraphNodeType.Highlight].Should().Be(0);
        response.Stats.MasteredCardsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_SoftDeletedHighlight_ShouldBeExcludedAndUnanchoredNodesDropped()
    {
        // Arrange — soft-deleting the only artifact referencing a chunk drops the highlight, its edges, and unreferenced chunk.
        var user = new User { Id = Guid.NewGuid(), Email = "softdelete@techdaily.local", Name = "Soft Delete" };
        var otherUser = new User { Id = Guid.NewGuid(), Email = "seedowner@techdaily.local", Name = "Seed Owner" };
        await _db.Users.AddRangeAsync(user, otherUser);

        // Book is NOT owned by the caller, so it never becomes a node and cannot keep the pillar alive.
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "CLR Internals",
            Slug = "clr-internals",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = otherUser.Id
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "GC Roots",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "Roots keep objects alive",
            Tags = new List<string> { "garbage-collection" }
        };
        await _db.UserHighlights.AddAsync(highlight);
        await _db.SaveChangesAsync();

        // Sanity check before deletion: highlight is present.
        var before = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));
        before.IsSuccess.Should().BeTrue();
        before.Value.Nodes.Should().Contain(n => n.Id == highlight.Id.ToString());

        // Act — soft-delete the highlight.
        highlight.SoftDelete();
        await _db.SaveChangesAsync();

        var after = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        after.IsSuccess.Should().BeTrue();
        var response = after.Value;

        response.Nodes.Should().NotContain(n => n.Id == highlight.Id.ToString());
        response.Edges.Should().NotContain(e => e.Source == highlight.Id.ToString() || e.Target == highlight.Id.ToString());
        response.Nodes.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_AllEdges_ShouldReferenceOnlyPresentNodes_AndOrphanCardForcesPillar()
    {
        // Arrange — task 1.5: exercise every edge type and assert no edge dangles to a missing node.
        var user = new User { Id = Guid.NewGuid(), Email = "edges@techdaily.local", Name = "Edge User" };
        await _db.Users.AddAsync(user);


        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Pro .NET Memory",
            Slug = "pro-dotnet-memory",
            Category = Category.BackendRuntime,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChapterTitle = "Garbage Collection",
            ChunkOrder = 1
        };
        await _db.DocumentChunks.AddAsync(chunk);

        var highlight1 = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "Roots keep objects alive",
            Tags = new List<string> { "garbage-collection", "memory" }
        };
        var highlight2 = new UserHighlight
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentChunkId = chunk.Id,
            SelectedText = "The LOH is collected in gen 2",
            Tags = new List<string> { "memory" }
        };
        await _db.UserHighlights.AddRangeAsync(highlight1, highlight2);

        var chunkCard = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "What is GC?", "Memory management");
        var highlightCard = SpacedRepetitionCard.CreateFromHighlight(user.Id, highlight1.Id, "What are GC roots?", "References that keep objects alive");

        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "System Design",
            Category = Category.SystemDesign,
            Level = QuizLevel.Senior,
            QuestionText = "What is MVCC?",
            ExplanationMarkdown = "Multi-version concurrency control",
            Options = new() { "A", "B", "C", "D" },
            CorrectOptionIndex = 0
        };
        await _db.QuizQuestions.AddAsync(question);
        var orphanCard = SpacedRepetitionCard.CreateFromQuizMistake(user.Id, question.Id, "What is MVCC?", "Multi-version concurrency control");

        await _db.SpacedRepetitionCards.AddRangeAsync(chunkCard, highlightCard, orphanCard);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        var nodeIds = response.Nodes.Select(n => n.Id).ToHashSet();
        foreach (var edge in response.Edges)
        {
            nodeIds.Should().Contain(edge.Source, $"edge {edge.Id} source must be a present node");
            nodeIds.Should().Contain(edge.Target, $"edge {edge.Id} target must be a present node");
        }

        // The orphan quiz card is anchored via CardToPillar, forcing its pillar hub to exist.
        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.CardToPillar
            && e.Source == orphanCard.Id.ToString()
            && e.Target == "pillar-EngineeringCraft");
        response.Nodes.Should().Contain(n => n.Id == "pillar-EngineeringCraft");

        // No card is a degree-0 node.
        foreach (var cardId in new[] { chunkCard.Id.ToString(), highlightCard.Id.ToString(), orphanCard.Id.ToString() })
        {
            response.Edges.Any(e => e.Source == cardId || e.Target == cardId).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteAsync_DeepPaceMentalModelsAndHabitsBooks_ShouldEmitPillarHubsWithoutDanglingEdges()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "deeppace@techdaily.local", Name = "DeepPace User" };
        await _db.Users.AddAsync(user);

        var mentalBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "The Great Mental Models",
            Slug = "great-mental-models",
            Category = Category.MentalModels,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        var habitsBook = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Atomic Habits",
            Slug = "atomic-habits",
            Category = Category.HabitsProductivity,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddRangeAsync(mentalBook, habitsBook);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        var pillars = response.Nodes.Where(n => n.Type == GraphNodeType.Pillar).ToList();

        pillars.Should().HaveCount(2);
        pillars.Should().Contain(p => p.Id == "pillar-MentalModels");
        pillars.Should().Contain(p => p.Id == "pillar-HabitsProductivity");

        // Verify zero dangling edges
        var nodeIds = response.Nodes.Select(n => n.Id).ToHashSet();
        foreach (var edge in response.Edges)
        {
            nodeIds.Should().Contain(edge.Source, $"edge {edge.Id} source must be present");
            nodeIds.Should().Contain(edge.Target, $"edge {edge.Id} target must be present");
        }

        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToPillar
            && e.Source == mentalBook.Id.ToString()
            && e.Target == "pillar-MentalModels");
        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToPillar
            && e.Source == habitsBook.Id.ToString()
            && e.Target == "pillar-HabitsProductivity");
    }

    [Fact]
    public async Task ExecuteAsync_CardLinkedToHabitsChunk_ShouldAnchorToHabitsPillarHub()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "habitscard@techdaily.local", Name = "Habits Card User" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Atomic Habits",
            Slug = "atomic-habits",
            Category = Category.HabitsProductivity,
            IsPublished = true,
            CreatedByUserId = user.Id
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "The 1% Rule",
            SummaryMarkdown = "Small improvements compound",
            OriginalTextMarkdown = "Every day get 1% better"
        };
        book.Chunks.Add(chunk);
        await _db.DocumentBooks.AddAsync(book);

        var card = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "What is the 1% rule?", "Compounding habits.");
        await _db.SpacedRepetitionCards.AddAsync(card);
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetKnowledgeGraphQuery(user.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Pillar && n.Id == "pillar-HabitsProductivity");
        response.Nodes.Should().Contain(n => n.Type == GraphNodeType.Card && n.Id == card.Id.ToString() && n.Category == "HabitsProductivity");
        response.Edges.Should().Contain(e => e.RelationType == GraphRelationType.CardToChunk
            && e.Source == card.Id.ToString()
            && e.Target == chunk.Id.ToString());

        var nodeIds = response.Nodes.Select(n => n.Id).ToHashSet();
        foreach (var edge in response.Edges)
        {
            nodeIds.Should().Contain(edge.Source);
            nodeIds.Should().Contain(edge.Target);
        }
    }
}
