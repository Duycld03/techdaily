using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.Library.GetBooks;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class GetBooksHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly GetBooksHandler _handler;

    public GetBooksHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        _handler = new GetBooksHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetBooks_DefaultPagination_ReturnsPage1AndCalculatesTotalPages()
    {
        // Arrange: Seed 15 published books
        for (int i = 1; i <= 15; i++)
        {
            _db.DocumentBooks.Add(new DocumentBook
            {
                Title = $"Book {i:D2}",
                Slug = $"book-{i:D2}",
                CategoryId = CategoryDefaults.BackendRuntimeId,
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(15);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(12);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Books.Should().HaveCount(12);
        // Ordered by CreatedAt descending
        result.Value.Books.First().Title.Should().Be("Book 15");
    }

    [Fact]
    public async Task GetBooks_SecondPage_ReturnsRemainingItems()
    {
        // Arrange: Seed 15 published books
        for (int i = 1; i <= 15; i++)
        {
            _db.DocumentBooks.Add(new DocumentBook
            {
                Title = $"Book {i:D2}",
                Slug = $"book-{i:D2}",
                CategoryId = CategoryDefaults.BackendRuntimeId,
                IsPublished = true,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest(Page: 2, PageSize: 12));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(15);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(12);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Books.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetBooks_FilterByCategory_ReturnsOnlyMatchingBooks()
    {
        // Arrange
        _db.DocumentBooks.AddRange(
            new DocumentBook { Title = "DotNet Book", Slug = "dotnet-book", CategoryId = CategoryDefaults.BackendRuntimeId, IsPublished = true },
            new DocumentBook { Title = "Frontend Book", Slug = "frontend-book", CategoryId = CategoryDefaults.FrontendWebId, IsPublished = true },
            new DocumentBook { Title = "System Design Book", Slug = "system-design-book", CategoryId = CategoryDefaults.SystemDesignId, IsPublished = true }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest(CategoryId : CategoryDefaults.FrontendWebId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
        result.Value.Books.Should().ContainSingle().Which.Title.Should().Be("Frontend Book");
    }

    [Fact]
    public async Task GetBooks_FilterBySearch_MatchesTitleOrSlug()
    {
        // Arrange
        _db.DocumentBooks.AddRange(
            new DocumentBook { Title = "Designing Data-Intensive Applications", Slug = "ddia", CategoryId = CategoryDefaults.DatabaseStorageId, IsPublished = true },
            new DocumentBook { Title = "Clean Architecture", Slug = "clean-architecture", CategoryId = CategoryDefaults.EngineeringCraftId, IsPublished = true }
        );
        await _db.SaveChangesAsync();

        // Act: search by slug substring
        var resultSlug = await _handler.ExecuteAsync(new GetBooksRequest(Search: "ddia"));
        // Act: search by title substring
        var resultTitle = await _handler.ExecuteAsync(new GetBooksRequest(Search: "intensive"));

        // Assert
        resultSlug.IsSuccess.Should().BeTrue();
        resultSlug.Value.Books.Should().ContainSingle().Which.Title.Should().Be("Designing Data-Intensive Applications");

        resultTitle.IsSuccess.Should().BeTrue();
        resultTitle.Value.Books.Should().ContainSingle().Which.Title.Should().Be("Designing Data-Intensive Applications");
    }

    [Fact]
    public async Task GetBooks_ExcludesUnpublishedAndDeletedBooks()
    {
        // Arrange
        _db.DocumentBooks.AddRange(
            new DocumentBook { Title = "Published Book", Slug = "pub", CategoryId = CategoryDefaults.BackendRuntimeId, IsPublished = true, IsDeleted = false },
            new DocumentBook { Title = "Unpublished Book", Slug = "unpub", CategoryId = CategoryDefaults.BackendRuntimeId, IsPublished = false, IsDeleted = false },
            new DocumentBook { Title = "Deleted Book", Slug = "del", CategoryId = CategoryDefaults.BackendRuntimeId, IsPublished = true, IsDeleted = true }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Books.Should().ContainSingle().Which.Title.Should().Be("Published Book");
    }

    [Fact]
    public async Task GetBooks_WhenEmpty_ReturnsZeroCountAndTotalPages()
    {
        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.Books.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBooks_WithUserId_ReturnsOnlyBooksOwnedByUser()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        _db.Users.Add(new User { Id = user1Id, Email = "u1@test.com", Name = "User 1" });
        _db.Users.Add(new User { Id = user2Id, Email = "u2@test.com", Name = "User 2" });
        _db.DocumentBooks.Add(new DocumentBook
        {
            Title = "User 1 Book",
            Slug = "user-1-book",
            CategoryId = CategoryDefaults.FrontendWebId,
            IsPublished = true,
            CreatedByUserId = user1Id
        });
        _db.DocumentBooks.Add(new DocumentBook
        {
            Title = "User 2 Book",
            Slug = "user-2-book",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            IsPublished = true,
            CreatedByUserId = user2Id
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest(UserId: user1Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Books.Should().HaveCount(1);
        result.Value.Books[0].Title.Should().Be("User 1 Book");
    }

    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("en", "en")]
    [InlineData(null, "en")]
    public async Task GetBooks_WithoutActiveSlices_UsesDeclarationThenLegacyDefault(string? declaration, string expectedLanguage)
    {
        var book = new DocumentBook
        {
            Title = "Incomplete import", Slug = "incomplete-import",
            CategoryId = CategoryDefaults.BackendRuntimeId, DeclaredLanguage = declaration,
            Chunks = [new DocumentChunk { ChunkOrder = 1, Language = "en", IsDeleted = true }]
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        var result = await _handler.ExecuteAsync(new GetBooksRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value.Books.Should().ContainSingle().Which.Language.Should().Be(expectedLanguage);
    }

    [Fact]
    public async Task GetBooks_RetainedAdoptedSlices_UseFirstActiveSliceInOrderNotDeclaration()
    {
        var owner = new User { Email = "adopter@example.test", Name = "Adopter" };
        var source = new CatalogDocument
        {
            Title = "Retained source", CategoryId = CategoryDefaults.BackendRuntimeId,
            Visibility = CatalogVisibility.Published, ProcessingStatus = ProcessingStatus.Ready, Language = "en"
        };
        var copy = DocumentBook.CreateFromCatalogSnapshot(source, owner.Id, "retained-copy");
        copy.DeclaredLanguage = "en";
        copy.Chunks =
        [
            new DocumentChunk { ChunkOrder = 3, Language = "en" },
            new DocumentChunk { ChunkOrder = 1, Language = "en", IsDeleted = true },
            new DocumentChunk { ChunkOrder = 2, Language = "vi" }
        ];
        source.Hide(owner.Id);
        _db.Users.Add(owner);
        _db.CatalogDocuments.Add(source);
        _db.DocumentBooks.Add(copy);
        await _db.SaveChangesAsync();

        var result = await _handler.ExecuteAsync(new GetBooksRequest(UserId: owner.Id));

        result.IsSuccess.Should().BeTrue();
        var returned = result.Value.Books.Should().ContainSingle().Which;
        returned.Id.Should().Be(copy.Id);
        returned.Language.Should().Be("vi");
    }
}
