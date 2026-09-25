using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetBooksHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly GetBooksHandler _handler;

    public GetBooksHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
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
                Category = Category.BackendRuntime,
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
                Category = Category.BackendRuntime,
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
            new DocumentBook { Title = "DotNet Book", Slug = "dotnet-book", Category = Category.BackendRuntime, IsPublished = true },
            new DocumentBook { Title = "Frontend Book", Slug = "frontend-book", Category = Category.FrontendWeb, IsPublished = true },
            new DocumentBook { Title = "System Design Book", Slug = "system-design-book", Category = Category.SystemDesign, IsPublished = true }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.ExecuteAsync(new GetBooksRequest(Category: Category.FrontendWeb));

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
            new DocumentBook { Title = "Designing Data-Intensive Applications", Slug = "ddia", Category = Category.DatabaseStorage, IsPublished = true },
            new DocumentBook { Title = "Clean Architecture", Slug = "clean-architecture", Category = Category.EngineeringCraft, IsPublished = true }
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
            new DocumentBook { Title = "Published Book", Slug = "pub", Category = Category.BackendRuntime, IsPublished = true, IsDeleted = false },
            new DocumentBook { Title = "Unpublished Book", Slug = "unpub", Category = Category.BackendRuntime, IsPublished = false, IsDeleted = false },
            new DocumentBook { Title = "Deleted Book", Slug = "del", Category = Category.BackendRuntime, IsPublished = true, IsDeleted = true }
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
            Category = Category.FrontendWeb,
            IsPublished = true,
            CreatedByUserId = user1Id
        });
        _db.DocumentBooks.Add(new DocumentBook
        {
            Title = "User 2 Book",
            Slug = "user-2-book",
            Category = Category.BackendRuntime,
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
}
