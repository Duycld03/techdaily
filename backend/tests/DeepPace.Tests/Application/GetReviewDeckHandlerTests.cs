using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.Review.GetReviewDeck;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class GetReviewDeckHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public GetReviewDeckHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
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
            Email = $"dev_{Guid.NewGuid()}@deeppace.test",
            Name = "Deck Review Dev"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Theory]
    [InlineData("private-question")]
    [InlineData("foreign-chunk")]
    [InlineData("foreign-highlight")]
    [InlineData("foreign-highlight-book")]
    [InlineData("deleted-question")]
    [InlineData("deleted-chunk")]
    [InlineData("deleted-highlight")]
    [InlineData("deleted-book")]
    public async Task ReviewResponses_UnauthorizedOrDeletedSources_DoNotRevealProvenance(string source)
    {
        var user = await SeedUserAsync();
        var other = await SeedUserAsync();
        var category = DocumentCategory.Create("private-review-subject", "Private subject", "Riêng tư");
        _db.DocumentCategories.Add(category);
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(), Title = "Private book", Slug = $"private-{source}",
            CategoryId = category.Id,
            CreatedByUserId = source is "foreign-chunk" or "foreign-highlight-book" ? other.Id : user.Id
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(), DocumentBookId = book.Id, DocumentBook = book,
            ChapterTitle = "Private chapter"
        };
        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(), UserId = source == "foreign-highlight" ? other.Id : user.Id,
            DocumentChunkId = chunk.Id, DocumentChunk = chunk, SelectedText = "Private quote"
        };
        var question = new QuizQuestion
        {
            Id = Guid.NewGuid(), Topic = "Private quiz topic", QuestionText = "Private question",
            CategoryId = category.Id, CreatedByUserId = source == "private-question" ? other.Id : user.Id,
            Level = QuizLevel.Mastery
        };
        _db.UserHighlights.Add(highlight);
        _db.QuizQuestions.Add(question);
        var card = source.Contains("question")
            ? SpacedRepetitionCard.CreateFromQuizMistake(user.Id, question.Id, "Own front", "Own back")
            : source.Contains("highlight")
                ? SpacedRepetitionCard.CreateFromHighlight(user.Id, highlight.Id, "Own front", "Own back")
                : SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "Own front", "Own back");
        card.ApplyReview(5);
        _db.SpacedRepetitionCards.Add(card);
        if (source == "deleted-question") question.SoftDelete();
        if (source == "deleted-chunk") chunk.SoftDelete();
        if (source == "deleted-highlight") highlight.SoftDelete();
        if (source == "deleted-book") book.SoftDelete();
        await _db.SaveChangesAsync();
        var trackedProjection = DeepPace.Application.Features.Review.DTOs.ReviewCardDto.FromEntity(card);
        _db.ChangeTracker.Clear();

        var deck = await new GetReviewDeckHandler(_db).ExecuteAsync(new(user.Id, DateOnly.MaxValue));
        var list = await new DeepPace.Application.Features.Review.GetReviewCards.GetReviewCardsHandler(_db)
            .ExecuteAsync(new(user.Id));
        var updated = await new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardHandler(
            _db, new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardValidator())
            .ExecuteAsync(new(card.Id, user.Id, "Edited own front", "Own back"));
        var reset = await new DeepPace.Application.Features.Review.ResetReviewCardProgress.ResetReviewCardProgressHandler(_db)
            .ExecuteAsync(new(card.Id, user.Id));

        foreach (var dto in new[] { trackedProjection, deck.Value.DueCards.Single(), list.Value.Cards.Single(), updated.Value.Card, reset.Value.Card })
        {
            dto.CategoryId.Should().BeNull();
            dto.Category.Should().BeNull();
            dto.Difficulty.Should().BeNull();
            dto.TopicTitle.Should().Be(dto.FrontMarkdown);
            dto.Id.Should().Be(card.Id);
        }
        updated.Value.Card.RepetitionCount.Should().Be(1);
        (await _db.SpacedRepetitionCards.FindAsync(card.Id))!.UserId.Should().Be(user.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReviewResponses_FollowReclassifiedBookAndCurrentInactiveMetadata_WithoutChangingMastery(bool fromHighlight)
    {
        var user = await SeedUserAsync();
        var original = DocumentCategory.Create("original-topic", "Original", "Ban đầu");
        var current = DocumentCategory.Create("novel-topic", "Novel subject", "Chủ đề mới", 37, false);
        _db.DocumentCategories.AddRange(original, current);
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(), Title = "Book", Slug = "reclassified-book",
            CategoryId = original.Id, CreatedByUserId = user.Id
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(), DocumentBook = book, DocumentBookId = book.Id,
            ChapterTitle = "Authoritative chapter"
        };
        _db.DocumentChunks.Add(chunk);
        var highlight = new UserHighlight
        {
            Id = Guid.NewGuid(), UserId = user.Id, DocumentChunkId = chunk.Id,
            SelectedText = "Source quote"
        };
        _db.UserHighlights.Add(highlight);
        var card = fromHighlight
            ? SpacedRepetitionCard.CreateFromHighlight(user.Id, highlight.Id, "Front", "Back")
            : SpacedRepetitionCard.CreateFromDrillMistake(user.Id, chunk.Id, "Front", "Back");
        card.ApplyReview(5);
        card.ApplyReview(5);
        _db.SpacedRepetitionCards.Add(card);
        await _db.SaveChangesAsync();
        book.CategoryId = current.Id;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var deck = await new GetReviewDeckHandler(_db).ExecuteAsync(new(user.Id, DateOnly.MaxValue));
        var list = await new DeepPace.Application.Features.Review.GetReviewCards.GetReviewCardsHandler(_db)
            .ExecuteAsync(new(user.Id));
        var updated = await new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardHandler(
            _db, new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardValidator())
            .ExecuteAsync(new(card.Id, user.Id, "Edited front", "Edited back"));
        var reset = await new DeepPace.Application.Features.Review.ResetReviewCardProgress.ResetReviewCardProgressHandler(_db)
            .ExecuteAsync(new(card.Id, user.Id));

        foreach (var dto in new[] { deck.Value.DueCards.Single(), list.Value.Cards.Single(), updated.Value.Card, reset.Value.Card })
        {
            dto.CategoryId.Should().Be(current.Id);
            dto.Category!.Id.Should().Be(current.Id);
            dto.Category.NameVi.Should().Be("Chủ đề mới");
            dto.Category.IsActive.Should().BeFalse();
            dto.TopicTitle.Should().Be("Authoritative chapter");
            dto.Difficulty.Should().BeNull();
            dto.SourceDocumentChunkId.Should().Be(fromHighlight ? null : chunk.Id);
            dto.SourceHighlightId.Should().Be(fromHighlight ? highlight.Id : null);
        }
        updated.Value.Card.RepetitionCount.Should().Be(2);
        updated.Value.Card.IntervalDays.Should().Be(6);
        reset.Value.Card.RepetitionCount.Should().Be(0);
        (await _db.SpacedRepetitionCards.FindAsync(card.Id))!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task ReviewResponses_GenuineOrphan_RemainsUnclassifiedWithoutInventedDifficulty()
    {
        var user = await SeedUserAsync();
        var card = SpacedRepetitionCard.CreateFromDrillMistake(user.Id, null, "Orphan question", "Answer");
        _db.SpacedRepetitionCards.Add(card);
        await _db.SaveChangesAsync();
        var deck = await new GetReviewDeckHandler(_db).ExecuteAsync(new(user.Id, DateOnly.MaxValue));
        var list = await new DeepPace.Application.Features.Review.GetReviewCards.GetReviewCardsHandler(_db)
            .ExecuteAsync(new(user.Id));
        var updated = await new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardHandler(
            _db, new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardValidator())
            .ExecuteAsync(new(card.Id, user.Id, "Edited orphan", "Answer"));
        var reset = await new DeepPace.Application.Features.Review.ResetReviewCardProgress.ResetReviewCardProgressHandler(_db)
            .ExecuteAsync(new(card.Id, user.Id));
        foreach (var dto in new[] { deck.Value.DueCards.Single(), list.Value.Cards.Single(), updated.Value.Card, reset.Value.Card })
        {
            dto.CategoryId.Should().BeNull();
            dto.Category.Should().BeNull();
            dto.Difficulty.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetReviewDeck_WhenCardFromQuizMistake_ShouldProjectQuizTopicCategoryAndDifficulty()
    {
        // Arrange
        var user = await SeedUserAsync();
        var category = DocumentCategory.Create("novel-quiz-subject", "Novel quiz subject", "Chủ đề trắc nghiệm", 23, false);
        _db.DocumentCategories.Add(category);
        var quizQuestion = new QuizQuestion
        {
            Id = Guid.NewGuid(),
            Topic = "Kestrel Socket Pipeline",
            CategoryId = category.Id,
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
        var updated = await new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardHandler(
            _db, new DeepPace.Application.Features.Review.UpdateReviewCard.UpdateReviewCardValidator())
            .ExecuteAsync(new(card.Id, user.Id, "Edited quiz question", "Edited answer"));
        var reset = await new DeepPace.Application.Features.Review.ResetReviewCardProgress.ResetReviewCardProgressHandler(_db)
            .ExecuteAsync(new(card.Id, user.Id));
        foreach (var mutationCard in new[] { updated.Value.Card, reset.Value.Card })
        {
            mutationCard.CategoryId.Should().Be(category.Id);
            mutationCard.Category!.NameVi.Should().Be("Chủ đề trắc nghiệm");
            mutationCard.Difficulty.Should().Be(Difficulty.Lead);
            mutationCard.SourceQuizQuestionId.Should().Be(quizQuestion.Id);
            mutationCard.TopicTitle.Should().Be("Kestrel Socket Pipeline");
        }
        result.Value.DueCards.Should().HaveCount(1);
        var dto = result.Value.DueCards.First();
        dto.TopicTitle.Should().Be("Kestrel Socket Pipeline");
        dto.CategoryId.Should().Be(category.Id);
        dto.Category!.Id.Should().Be(category.Id);
        dto.Category.NameVi.Should().Be("Chủ đề trắc nghiệm");
        dto.Category.IsActive.Should().BeFalse();
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
            CategoryId = CategoryDefaults.DatabaseStorageId,
            CreatedByUserId = user.Id
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
        dto.CategoryId.Should().Be(CategoryDefaults.DatabaseStorageId);
        dto.Category!.Id.Should().Be(CategoryDefaults.DatabaseStorageId);
        dto.Difficulty.Should().BeNull();
    }
}
