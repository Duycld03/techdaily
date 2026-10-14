using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class PostgresCatalogAdoptionTests
{
    private const string PostgresConnStr = "Host=localhost;Port=5432;Database=deeppace_db;Username=deeppace_user;Password=techdaily_password_secret";

    private static DbContextOptions<DeepPaceDbContext> CreatePostgresOptions()
    {
        return new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseNpgsql(PostgresConnStr, o => o.UseVector())
            .Options;
    }

    private static bool IsPostgresAvailable()
    {
        try
        {
            using var db = new DeepPaceDbContext(CreatePostgresOptions());
            return db.Database.CanConnect();
        }
        catch
        {
            return false;
        }
    }

    private async Task<(CatalogDocument Document, User User)> SeedPublishedCatalogDocumentAndUserAsync(DeepPaceDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-learner-{Guid.NewGuid():N}@example.com",
            Name = "Postgres Learner",
            IsDeleted = false
        };
        await db.Users.AddAsync(user);

        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = $"Postgres Test Book {Guid.NewGuid():N}",
            Description = "A book for PostgreSQL concurrency and transaction testing.",
            Category = Category.DatabaseStorage,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Attribution test",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 2,
            Revision = 1
        };

        for (int i = 1; i <= 2; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}",
                OriginalTextMarkdown = $"Source text for chapter {i}",
                SummaryMarkdown = $"Summary {i}",
                KeyTakeaways = new() { $"Takeaway {i}" },
                EstimatedReadMinutes = 4,
                IsAiFormatted = true
            });
        }

        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Publish(adminId);

        await db.CatalogDocuments.AddAsync(doc);
        await db.SaveChangesAsync();

        return (doc, user);
    }

    [Fact]
    public async Task ConcurrentAdoptions_ShouldYieldSamePersonalBookWithoutDuplicateSlices()
    {
        if (!IsPostgresAvailable()) return;

        var options = CreatePostgresOptions();
        using var setupDb = new DeepPaceDbContext(options);
        var (doc, user) = await SeedPublishedCatalogDocumentAndUserAsync(setupDb);

        // Run two concurrent adoption requests
        using var db1 = new DeepPaceDbContext(options);
        using var db2 = new DeepPaceDbContext(options);

        var service1 = new CatalogAdoptionService(db1, NullLogger<CatalogAdoptionService>.Instance);
        var service2 = new CatalogAdoptionService(db2, NullLogger<CatalogAdoptionService>.Instance);

        var task1 = service1.AdoptAsync(doc.Id, user.Id);
        var task2 = service2.AdoptAsync(doc.Id, user.Id);

        var results = await Task.WhenAll(task1, task2);

        results[0].IsSuccess.Should().BeTrue();
        results[1].IsSuccess.Should().BeTrue();

        // Both must return the EXACT same book ID
        results[0].Value.BookId.Should().Be(results[1].Value.BookId);

        // One had AlreadyAdded false, the other true
        var alreadyAddedFlags = results.Select(r => r.Value.AlreadyAdded).ToList();
        alreadyAddedFlags.Should().Contain(false);
        alreadyAddedFlags.Should().Contain(true);

        // Verify only 1 DocumentBook exists in DB
        using var verifyDb = new DeepPaceDbContext(options);
        var books = await verifyDb.DocumentBooks
            .Where(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        books.Should().HaveCount(1);
    }

    [Fact]
    public async Task HideAdoptOrdering_WhenHiddenBeforeAdopt_RejectsAdoption()
    {
        if (!IsPostgresAvailable()) return;

        var options = CreatePostgresOptions();
        using var setupDb = new DeepPaceDbContext(options);
        var (doc, user) = await SeedPublishedCatalogDocumentAndUserAsync(setupDb);

        // Hide the document
        doc.Hide(Guid.NewGuid());
        await setupDb.SaveChangesAsync();

        using var adoptDb = new DeepPaceDbContext(options);
        var service = new CatalogAdoptionService(adoptDb, NullLogger<CatalogAdoptionService>.Instance);

        var result = await service.AdoptAsync(doc.Id, user.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.NotAvailable");

        // Verify no book was created
        using var verifyDb = new DeepPaceDbContext(options);
        var booksCount = await verifyDb.DocumentBooks
            .CountAsync(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id);
        booksCount.Should().Be(0);
    }

    [Fact]
    public async Task ReAddAfterSoftDeletion_CreatesNewBookWithFreshSlices()
    {
        if (!IsPostgresAvailable()) return;

        var options = CreatePostgresOptions();
        using var db = new DeepPaceDbContext(options);
        var (doc, user) = await SeedPublishedCatalogDocumentAndUserAsync(db);

        var service = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance);

        // 1. First adoption
        var adoptResult1 = await service.AdoptAsync(doc.Id, user.Id);
        adoptResult1.IsSuccess.Should().BeTrue();
        adoptResult1.Value.AlreadyAdded.Should().BeFalse();
        var firstBookId = adoptResult1.Value.BookId;

        // 2. Soft-delete the first book
        var firstBook = await db.DocumentBooks.FindAsync(firstBookId);
        firstBook!.SoftDelete();
        await db.SaveChangesAsync();

        // 3. Re-adopt the still-published document
        var adoptResult2 = await service.AdoptAsync(doc.Id, user.Id);
        adoptResult2.IsSuccess.Should().BeTrue();
        adoptResult2.Value.AlreadyAdded.Should().BeFalse();
        var secondBookId = adoptResult2.Value.BookId;

        secondBookId.Should().NotBe(firstBookId);

        // Verify active book count is 1
        var activeBooks = await db.DocumentBooks
            .Where(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id)
            .ToListAsync();

        activeBooks.Should().HaveCount(1);
        activeBooks.First().Id.Should().Be(secondBookId);
    }

    [Fact]
    public async Task RollbackOnCancellation_LeavesNoPartialBookOrSlices()
    {
        if (!IsPostgresAvailable()) return;

        var options = CreatePostgresOptions();
        using var db = new DeepPaceDbContext(options);
        var (doc, user) = await SeedPublishedCatalogDocumentAndUserAsync(db);

        var service = new CatalogAdoptionService(db, NullLogger<CatalogAdoptionService>.Instance);

        // Create an already-cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await service.AdoptAsync(doc.Id, user.Id, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify no book or chunks were committed
        var booksCount = await db.DocumentBooks
            .CountAsync(b => b.CreatedByUserId == user.Id && b.SourceCatalogDocumentId == doc.Id);
        booksCount.Should().Be(0);
    }

    [Fact]
    public async Task TwoDistinctLearners_AdoptingSameCatalogDocument_GetCompletelyIsolatedBooksAndChunks_InPostgres()
    {
        if (!IsPostgresAvailable()) return;

        var options = CreatePostgresOptions();
        using var setupDb = new DeepPaceDbContext(options);
        var (doc, user1) = await SeedPublishedCatalogDocumentAndUserAsync(setupDb);

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-learner2-{Guid.NewGuid():N}@example.com",
            Name = "Postgres Learner Two",
            IsDeleted = false
        };
        await setupDb.Users.AddAsync(user2);
        await setupDb.SaveChangesAsync();

        using var db1 = new DeepPaceDbContext(options);
        using var db2 = new DeepPaceDbContext(options);

        var service1 = new CatalogAdoptionService(db1, NullLogger<CatalogAdoptionService>.Instance);
        var service2 = new CatalogAdoptionService(db2, NullLogger<CatalogAdoptionService>.Instance);

        var adopt1 = await service1.AdoptAsync(doc.Id, user1.Id);
        var adopt2 = await service2.AdoptAsync(doc.Id, user2.Id);

        adopt1.IsSuccess.Should().BeTrue();
        adopt2.IsSuccess.Should().BeTrue();

        var bookId1 = adopt1.Value.BookId;
        var bookId2 = adopt2.Value.BookId;

        bookId1.Should().NotBe(bookId2);

        // Verify chunk isolation in PostgreSQL
        using var verifyDb = new DeepPaceDbContext(options);
        var chunks1 = await verifyDb.DocumentChunks.Where(c => c.DocumentBookId == bookId1).ToListAsync();
        var chunks2 = await verifyDb.DocumentChunks.Where(c => c.DocumentBookId == bookId2).ToListAsync();

        chunks1.Should().HaveCount(2);
        chunks2.Should().HaveCount(2);

        var chunkIds1 = chunks1.Select(c => c.Id).ToHashSet();
        var chunkIds2 = chunks2.Select(c => c.Id).ToHashSet();
        var sliceIds = doc.Slices.Select(s => s.Id).ToHashSet();

        chunkIds1.Intersect(chunkIds2).Should().BeEmpty();
        chunkIds1.Intersect(sliceIds).Should().BeEmpty();
        chunkIds2.Intersect(sliceIds).Should().BeEmpty();

        // Hide the catalog document in PostgreSQL
        var docToHide = await verifyDb.CatalogDocuments.FindAsync(doc.Id);
        docToHide!.Hide(Guid.NewGuid());
        await verifyDb.SaveChangesAsync();

        // Both learners' books remain fully present and accessible in PostgreSQL
        var activeBook1 = await verifyDb.DocumentBooks.Include(b => b.Chunks).FirstOrDefaultAsync(b => b.Id == bookId1 && b.CreatedByUserId == user1.Id);
        var activeBook2 = await verifyDb.DocumentBooks.Include(b => b.Chunks).FirstOrDefaultAsync(b => b.Id == bookId2 && b.CreatedByUserId == user2.Id);

        activeBook1.Should().NotBeNull();
        activeBook1!.Chunks.Should().HaveCount(2);

        activeBook2.Should().NotBeNull();
        activeBook2!.Chunks.Should().HaveCount(2);
    }
}
