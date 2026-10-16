using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.GetBooks;
using DeepPace.Application.Features.Library.ImportDocument;
using DeepPace.Application.Features.Library.UpdateBook;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector;
using Xunit;

namespace DeepPace.Tests.Application;

public sealed class DocumentCategoryAssignmentRegressionTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DeepPaceDbContext _db;
    private readonly CategoryAssignmentGuard _guard;

    public DocumentCategoryAssignmentRegressionTests()
    {
        _connection.Open();
        _db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _guard = new CategoryAssignmentGuard(_db);
    }

    [Fact]
    public void Snapshot_WithUnloadedNovelCategory_PreservesForeignKeyWithoutInventingNavigation()
    {
        var categoryId = Guid.NewGuid();
        var source = new CatalogDocument
        {
            CategoryId = categoryId, Title = "Novel source", Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Ready, AuthorOrSourceUrl = "Original author"
        };

        var snapshot = DocumentBook.CreateFromCatalogSnapshot(source, Guid.NewGuid(), "novel-source-copy");

        snapshot.CategoryId.Should().Be(categoryId);
        snapshot.Category.Should().BeNull();
        snapshot.AuthorOrSourceUrl.Should().Be("Original author");
    }

    [Fact]
    public async Task ImportAndFilter_WithNovelCategory_ReturnExactUuidAndDescriptor()
    {
        var category = await AddCategoryAsync();
        var handler = new ImportDocumentHandler(_db, new RecordingEmbeddings(), new ImportDocumentValidator(), _guard);
        var result = await handler.ExecuteAsync(new ImportDocumentRequest("Novel import", new string('a', 80), category.Id, SourceUrl: "https://example.com/source"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Book.CategoryId.Should().Be(category.Id);
        result.Value.Book.Category.Id.Should().Be(category.Id);
        result.Value.Book.Category.Slug.Should().Be(category.Slug);
        result.Value.Book.AuthorOrSourceUrl.Should().Be("https://example.com/source");
        _db.ChangeTracker.Clear();
        var filtered = await new GetBooksHandler(_db).ExecuteAsync(new GetBooksRequest(CategoryId: category.Id));
        filtered.Value.Books.Should().ContainSingle().Which.CategoryId.Should().Be(category.Id);
        filtered.Value.Books.Single().Category.NameVi.Should().Be(category.NameVi);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_WithInactiveOrUnknownCategory_RejectsBeforeEmbeddingOrPersistence(bool unknown)
    {
        var category = await AddCategoryAsync(isActive: false);
        var embeddings = new RecordingEmbeddings();
        var handler = new ImportDocumentHandler(_db, embeddings, new ImportDocumentValidator(), _guard);
        var result = await handler.ExecuteAsync(new ImportDocumentRequest("Rejected import", new string('a', 80), unknown ? Guid.NewGuid() : category.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(unknown ? "Category.ValidationFailed" : "Category.Inactive");
        (await _db.DocumentBooks.CountAsync()).Should().Be(0);
        embeddings.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Edit_RetainedInactiveCategory_AllowsOwnerWithoutChangingAssignment()
    {
        var category = await AddCategoryAsync(isActive: false);
        var user = new User { Email = "retained@example.test", Name = "Owner", PasswordHash = "hash" };
        var book = new DocumentBook { Title = "Before", Slug = "before", CategoryId = category.Id, CreatedByUserId = user.Id };
        _db.AddRange(user, book);
        await _db.SaveChangesAsync();
        var handler = new UpdateBookHandler(_db, new UpdateBookValidator(), _guard);

        var result = await handler.ExecuteAsync(new UpdateBookRequest(book.Id, user.Id, "After", "Author", category.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("After");
        result.Value.Book.CategoryId.Should().Be(category.Id);
        result.Value.Book.Category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Adoption_InactiveCategory_ReturnsOwnedCopyButRejectsNewAssignment()
    {
        var category = await AddCategoryAsync(isActive: false);
        var user = new User { Email = "adopter@example.test", Name = "Owner", PasswordHash = "hash" };
        var source = new CatalogDocument { Title = "Source", Description = "Description", CategoryId = category.Id, Visibility = CatalogVisibility.Published, ProcessingStatus = ProcessingStatus.Ready };
        var copy = DocumentBook.CreateFromCatalogSnapshot(source, user.Id, "existing-copy");
        _db.AddRange(user, source, copy);
        await _db.SaveChangesAsync();
        var service = new CatalogAdoptionService(_db, NullLogger<CatalogAdoptionService>.Instance, _guard);

        var existing = await service.AdoptAsync(source.Id, user.Id);
        var rejected = await service.AdoptAsync(source.Id, Guid.NewGuid());

        existing.IsSuccess.Should().BeTrue();
        existing.Value.BookId.Should().Be(copy.Id);
        existing.Value.AlreadyAdded.Should().BeTrue();
        rejected.IsSuccess.Should().BeFalse();
        rejected.Error.Code.Should().Be("Category.Inactive");
        (await _db.DocumentBooks.CountAsync()).Should().Be(1);
    }

    private async Task<DocumentCategory> AddCategoryAsync(bool isActive = true)
    {
        var category = DocumentCategory.Create("novel-taxonomy", "Novel taxonomy", "Chủ đề mới", isActive: isActive);
        _db.DocumentCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class RecordingEmbeddings : IEmbeddingService
    {
        public int CallCount { get; private set; }
        public Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Vector>.Failure(Error.Custom("Embedding.Unavailable", "Not configured")));

        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result<List<Vector>>.Success(new List<Vector>()));
        }
    }

}
