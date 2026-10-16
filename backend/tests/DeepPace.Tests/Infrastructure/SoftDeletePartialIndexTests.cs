using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class SoftDeletePartialIndexTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public SoftDeletePartialIndexTests()
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

    [Fact]
    public void ModelMetadata_ShouldConfigurePartialUniqueIndexesWithSoftDeleteFilter()
    {
        // 1. User.Email
        var userEntity = _db.Model.FindEntityType(typeof(User))!;
        var emailIndex = userEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.Email)))!;
        emailIndex.Should().NotBeNull();
        emailIndex.IsUnique.Should().BeTrue();
        emailIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 2. DocumentBook.Slug
        var bookEntity = _db.Model.FindEntityType(typeof(DocumentBook))!;
        var slugIndex = bookEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(DocumentBook.Slug)))!;
        slugIndex.Should().NotBeNull();
        slugIndex.IsUnique.Should().BeTrue();
        slugIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 3. DocumentChunk.(DocumentBookId, ChunkOrder)
        var chunkEntity = _db.Model.FindEntityType(typeof(DocumentChunk))!;
        var chunkSliceIndex = chunkEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(DocumentChunk.DocumentBookId)
                && i.Properties[1].Name == nameof(DocumentChunk.ChunkOrder))!;
        chunkSliceIndex.Should().NotBeNull();
        chunkSliceIndex.IsUnique.Should().BeTrue();
        chunkSliceIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 4. UserBookPacer.(UserId, DocumentBookId)
        var pacerEntity = _db.Model.FindEntityType(typeof(UserBookPacer))!;
        var pacerIndex = pacerEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(UserBookPacer.UserId)
                && i.Properties[1].Name == nameof(UserBookPacer.DocumentBookId))!;
        pacerIndex.Should().NotBeNull();
        pacerIndex.IsUnique.Should().BeTrue();
        pacerIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 5. DailyDrill.(UserId, ScheduledDate, QuestionId)
        var drillEntity = _db.Model.FindEntityType(typeof(DailyDrill))!;
        var drillIndex = drillEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 3
                && i.Properties[0].Name == nameof(DailyDrill.UserId)
                && i.Properties[1].Name == nameof(DailyDrill.ScheduledDate)
                && i.Properties[2].Name == nameof(DailyDrill.QuestionId))!;
        drillIndex.Should().NotBeNull();
        drillIndex.IsUnique.Should().BeTrue();
        drillIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 6. TechInsight.Slug & UserInsightBookmark.(UserId, InsightId)
        var insightEntity = _db.Model.FindEntityType(typeof(TechInsight))!;
        var insightSlugIndex = insightEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(TechInsight.Slug)))!;
        insightSlugIndex.Should().NotBeNull();
        insightSlugIndex.IsUnique.Should().BeTrue();
        insightSlugIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        var bookmarkEntity = _db.Model.FindEntityType(typeof(UserInsightBookmark))!;
        var bookmarkIndex = bookmarkEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(UserInsightBookmark.UserId)
                && i.Properties[1].Name == nameof(UserInsightBookmark.InsightId))!;
        bookmarkIndex.Should().NotBeNull();
        bookmarkIndex.IsUnique.Should().BeTrue();
        bookmarkIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 7. UserQuizProgress.(UserId, QuestionId) & UserPushSubscription.(UserId, Endpoint)
        var quizProgressEntity = _db.Model.FindEntityType(typeof(UserQuizProgress))!;
        var quizProgressIndex = quizProgressEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(UserQuizProgress.UserId)
                && i.Properties[1].Name == nameof(UserQuizProgress.QuestionId))!;
        quizProgressIndex.Should().NotBeNull();
        quizProgressIndex.IsUnique.Should().BeTrue();
        quizProgressIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        var pushEntity = _db.Model.FindEntityType(typeof(UserPushSubscription))!;
        var pushIndex = pushEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(UserPushSubscription.UserId)
                && i.Properties[1].Name == nameof(UserPushSubscription.Endpoint))!;
        pushIndex.Should().NotBeNull();
        pushIndex.IsUnique.Should().BeTrue();
        pushIndex.GetFilter().Should().Be("\"IsDeleted\" = false");
    }

    [Fact]
    public async Task TwoActiveUsers_WithSameEmail_ThrowsDbUpdateException()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "engineer@example.com",
            Name = "Engineer One",
            IsDeleted = false
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "engineer@example.com",
            Name = "Engineer Two",
            IsDeleted = false
        };

        await _db.Users.AddAsync(user1);
        await _db.SaveChangesAsync();

        await _db.Users.AddAsync(user2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SoftDeletedUser_AllowsReRegistrationWithSameEmail()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "engineer@example.com",
            Name = "Original Engineer",
            IsDeleted = true
        };
        await _db.Users.AddAsync(user1);
        await _db.SaveChangesAsync();

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "engineer@example.com",
            Name = "New Engineer",
            IsDeleted = false
        };
        await _db.Users.AddAsync(user2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().NotThrowAsync();

        // Query with ignore query filter should see both users
        var allUsers = await _db.Users.IgnoreQueryFilters().Where(u => u.Email == "engineer@example.com").ToListAsync();
        allUsers.Should().HaveCount(2);

        // Standard query should return only the active user
        var activeUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "engineer@example.com");
        activeUser.Should().NotBeNull();
        activeUser!.Name.Should().Be("New Engineer");
    }

    [Fact]
    public async Task TwoActiveBooks_WithSameSlug_ThrowsDbUpdateException()
    {
        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Design Patterns Part 1",
            Slug = "design-patterns",
            CategoryId = CategoryDefaults.SystemDesignId,
            SourceType = SourceType.MarkdownSeries,
            IsDeleted = false
        };
        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Design Patterns Part 2",
            Slug = "design-patterns",
            CategoryId = CategoryDefaults.SystemDesignId,
            SourceType = SourceType.MarkdownSeries,
            IsDeleted = false
        };

        await _db.DocumentBooks.AddAsync(book1);
        await _db.SaveChangesAsync();

        await _db.DocumentBooks.AddAsync(book2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SoftDeletedBook_AllowsReImportWithSameSlug()
    {
        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Design Patterns (Old)",
            Slug = "design-patterns",
            CategoryId = CategoryDefaults.SystemDesignId,
            SourceType = SourceType.MarkdownSeries,
            IsDeleted = true
        };
        await _db.DocumentBooks.AddAsync(book1);
        await _db.SaveChangesAsync();

        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Design Patterns (Re-imported)",
            Slug = "design-patterns",
            CategoryId = CategoryDefaults.SystemDesignId,
            SourceType = SourceType.MarkdownSeries,
            IsDeleted = false
        };
        await _db.DocumentBooks.AddAsync(book2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().NotThrowAsync();

        var activeBook = await _db.DocumentBooks.FirstOrDefaultAsync(b => b.Slug == "design-patterns");
        activeBook.Should().NotBeNull();
        activeBook!.Title.Should().Be("Design Patterns (Re-imported)");
    }

    [Fact]
    public async Task DocumentChunk_CompositeSliceIndex_EnforcesUniquenessAndAllowsSoftDeletedReuse()
    {
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Clean Code",
            Slug = "clean-code",
            CategoryId = CategoryDefaults.SystemDesignId,
            SourceType = SourceType.MarkdownSeries,
            IsDeleted = false
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Content 1",
            SummaryMarkdown = "Summary 1",
            IsDeleted = false
        };
        await _db.DocumentChunks.AddAsync(chunk1);
        await _db.SaveChangesAsync();

        // Inserting another active chunk with same book and chunk order fails
        var duplicateActiveChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1 Dupe",
            OriginalTextMarkdown = "Content 1 Dupe",
            SummaryMarkdown = "Summary 1 Dupe",
            IsDeleted = false
        };
        await _db.DocumentChunks.AddAsync(duplicateActiveChunk);
        var act = async () => await _db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();

        // Remove the failed entity from tracker
        _db.Entry(duplicateActiveChunk).State = EntityState.Detached;

        // Soft-delete chunk1
        chunk1.IsDeleted = true;
        await _db.SaveChangesAsync();

        // Now adding a new active chunk with ChunkOrder = 1 succeeds
        var replacementChunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1 Replacement",
            OriginalTextMarkdown = "Content 1 Replacement",
            SummaryMarkdown = "Summary 1 Replacement",
            IsDeleted = false
        };
        await _db.DocumentChunks.AddAsync(replacementChunk);
        var act2 = async () => await _db.SaveChangesAsync();
        await act2.Should().NotThrowAsync();

        var activeChunk = await _db.DocumentChunks.FirstOrDefaultAsync(c => c.DocumentBookId == book.Id && c.ChunkOrder == 1);
        activeChunk.Should().NotBeNull();
        activeChunk!.ChapterTitle.Should().Be("Chapter 1 Replacement");
    }
}
