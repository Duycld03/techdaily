using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class CuratedCatalogPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public CuratedCatalogPersistenceTests()
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

    private async Task<User> CreateTestUserAsync(string email = "learner@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = "Learner",
            IsDeleted = false
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public void ModelMetadata_ShouldConfigureCatalogIndexesAndFilters()
    {
        // 1. CatalogSlice.(CatalogDocumentId, SliceOrder)
        var sliceEntity = _db.Model.FindEntityType(typeof(CatalogSlice))!;
        var sliceIndex = sliceEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(CatalogSlice.CatalogDocumentId)
                && i.Properties[1].Name == nameof(CatalogSlice.SliceOrder))!;
        sliceIndex.Should().NotBeNull();
        sliceIndex.IsUnique.Should().BeTrue();
        sliceIndex.GetFilter().Should().Be("\"IsDeleted\" = false");

        // 2. DocumentBook.(CreatedByUserId, SourceCatalogDocumentId)
        var bookEntity = _db.Model.FindEntityType(typeof(DocumentBook))!;
        var adoptionIndex = bookEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2
                && i.Properties[0].Name == nameof(DocumentBook.CreatedByUserId)
                && i.Properties[1].Name == nameof(DocumentBook.SourceCatalogDocumentId))!;
        adoptionIndex.Should().NotBeNull();
        adoptionIndex.IsUnique.Should().BeTrue();
        adoptionIndex.GetFilter().Should().Be("\"CreatedByUserId\" IS NOT NULL AND \"SourceCatalogDocumentId\" IS NOT NULL AND \"IsDeleted\" = false");
    }

    [Fact]
    public async Task TwoActiveSlices_WithSameSliceOrder_ThrowsDbUpdateException()
    {
        var docId = Guid.NewGuid();
        var doc = new CatalogDocument
        {
            Id = docId,
            Title = "Test Catalog Doc",
            Description = "Desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attr",
            PermissionBasis = "Basis",
            PermissionEvidence = "Evidence",
            IsDeleted = false
        };
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var slice1 = new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = docId,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Markdown 1",
            SummaryMarkdown = "Summary 1",
            IsDeleted = false
        };
        var slice2 = new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = docId,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1 Duplicate",
            OriginalTextMarkdown = "Markdown 1 Duplicate",
            SummaryMarkdown = "Summary 1 Duplicate",
            IsDeleted = false
        };

        await _db.CatalogSlices.AddAsync(slice1);
        await _db.SaveChangesAsync();

        await _db.CatalogSlices.AddAsync(slice2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SoftDeletedCatalogSlice_AllowsRecreationOfSameSliceOrder()
    {
        var docId = Guid.NewGuid();
        var doc = new CatalogDocument
        {
            Id = docId,
            Title = "Test Catalog Doc",
            Description = "Desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attr",
            PermissionBasis = "Basis",
            PermissionEvidence = "Evidence",
            IsDeleted = false
        };
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var slice1 = new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = docId,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Markdown 1",
            SummaryMarkdown = "Summary 1",
            IsDeleted = false
        };
        await _db.CatalogSlices.AddAsync(slice1);
        await _db.SaveChangesAsync();

        slice1.SoftDelete();
        await _db.SaveChangesAsync();

        var slice2 = new CatalogSlice
        {
            Id = Guid.NewGuid(),
            CatalogDocumentId = docId,
            SliceOrder = 1,
            ChapterTitle = "Chapter 1 Replacement",
            OriginalTextMarkdown = "Markdown 1 Replacement",
            SummaryMarkdown = "Summary 1 Replacement",
            IsDeleted = false
        };
        await _db.CatalogSlices.AddAsync(slice2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TwoActiveBooks_WithSameUserAndSourceCatalogId_ThrowsDbUpdateException()
    {
        var user = await CreateTestUserAsync("user1@example.com");
        var sourceCatalogId = Guid.NewGuid();

        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Book 1",
            Slug = "book-1",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = sourceCatalogId,
            IsDeleted = false
        };
        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Book 2",
            Slug = "book-2",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = sourceCatalogId,
            IsDeleted = false
        };

        await _db.DocumentBooks.AddAsync(book1);
        await _db.SaveChangesAsync();

        await _db.DocumentBooks.AddAsync(book2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SoftDeletedBook_AllowsSameUserToAdoptSameCatalogDocAgain()
    {
        var user = await CreateTestUserAsync("user2@example.com");
        var sourceCatalogId = Guid.NewGuid();

        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Book 1",
            Slug = "book-1",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = sourceCatalogId,
            IsDeleted = false
        };
        await _db.DocumentBooks.AddAsync(book1);
        await _db.SaveChangesAsync();

        book1.SoftDelete();
        await _db.SaveChangesAsync();

        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Book 1 Re-adopted",
            Slug = "book-1-readopted",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = sourceCatalogId,
            IsDeleted = false
        };

        await _db.DocumentBooks.AddAsync(book2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PersonalBooksWithoutCatalogId_DoNotConflictOnNullSourceCatalogDocumentId()
    {
        var user = await CreateTestUserAsync("user3@example.com");

        var book1 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Personal Book 1",
            Slug = "personal-book-1",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = null,
            IsDeleted = false
        };
        var book2 = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Personal Book 2",
            Slug = "personal-book-2",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = null,
            IsDeleted = false
        };

        await _db.DocumentBooks.AddAsync(book1);
        await _db.DocumentBooks.AddAsync(book2);
        var act = async () => await _db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CatalogDocument_SoftDeleteQueryFilter_ExcludesDeletedEntities()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Doc to delete",
            Description = "Desc",
            Category = Category.BackendRuntime,
            AttributionText = "Attr",
            PermissionBasis = "Basis",
            PermissionEvidence = "Evidence",
            IsDeleted = false
        };
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var queryBefore = await _db.CatalogDocuments.ToListAsync();
        queryBefore.Should().ContainSingle(d => d.Id == doc.Id);

        doc.SoftDelete();
        await _db.SaveChangesAsync();

        var queryAfter = await _db.CatalogDocuments.ToListAsync();
        queryAfter.Should().NotContain(d => d.Id == doc.Id);

        var queryIgnored = await _db.CatalogDocuments.IgnoreQueryFilters().ToListAsync();
        queryIgnored.Should().ContainSingle(d => d.Id == doc.Id);
    }
}
