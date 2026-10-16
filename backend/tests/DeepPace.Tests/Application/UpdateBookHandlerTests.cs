using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.UpdateBook;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class UpdateBookHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly UpdateBookValidator _validator;
    private readonly UpdateBookHandler _handler;

    public UpdateBookHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        _validator = new UpdateBookValidator();
        _handler = new UpdateBookHandler(_db, _validator, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> CreateUserAsync(string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = "Test User",
            PasswordHash = "hash"
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UpdateBook_ShouldUpdateMetadataAndCategory_WhenCallerIsOwner()
    {
        // Arrange
        var user = await CreateUserAsync("owner1@example.com");
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Original Technical Title",
            Slug = "original-technical-title",
            AuthorOrSourceUrl = "Old Author",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            SourceType = SourceType.MarkdownSeries,
            CreatedByUserId = user.Id
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: user.Id,
            Title: "Atomic Habits & Focus",
            AuthorOrSourceUrl: "James Clear",
            CategoryId : CategoryDefaults.HabitsProductivityId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("Atomic Habits & Focus");
        result.Value.Book.AuthorOrSourceUrl.Should().Be("James Clear");
        result.Value.Book.CategoryId.Should().Be(CategoryDefaults.HabitsProductivityId);
        result.Value.Book.Category.Id.Should().Be(CategoryDefaults.HabitsProductivityId);

        var updatedBook = await _db.DocumentBooks.FindAsync(book.Id);
        updatedBook.Should().NotBeNull();
        updatedBook!.Title.Should().Be("Atomic Habits & Focus");
        updatedBook.AuthorOrSourceUrl.Should().Be("James Clear");
        updatedBook.CategoryId.Should().Be(CategoryDefaults.HabitsProductivityId);
        updatedBook.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Valid Title",
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.MentalModelsId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnNotFound_WhenBookIsDeleted()
    {
        // Arrange
        var user = await CreateUserAsync("owner2@example.com");
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Deleted Book",
            Slug = "deleted-book",
            CategoryId = CategoryDefaults.SystemDesignId,
            CreatedByUserId = user.Id,
            IsDeleted = true
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: user.Id,
            Title: "New Title",
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.SystemDesignId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnForbidden_WhenCallerIsNotOwner()
    {
        // Arrange
        var owner = await CreateUserAsync("owner3@example.com");
        var caller = await CreateUserAsync("caller3@example.com");
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Owner Book",
            Slug = "owner-book",
            CategoryId = CategoryDefaults.EngineeringCraftId,
            CreatedByUserId = owner.Id
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: caller.Id,
            Title: "Tampered Title",
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.EngineeringCraftId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("LIBRARY_FORBIDDEN");
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnForbidden_WhenBookHasNoOwner()
    {
        // Arrange
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "System Curated Book",
            Slug = "system-curated-book",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            CreatedByUserId = null
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: Guid.NewGuid(),
            Title: "Modified Title",
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.BackendRuntimeId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("LIBRARY_FORBIDDEN");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateBook_ShouldReturnValidationError_WhenTitleIsEmpty(string invalidTitle)
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: invalidTitle,
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.FrontendWebId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnValidationError_WhenTitleExceeds255Chars()
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: new string('A', 256),
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.FrontendWebId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnValidationError_WhenCategoryIsInvalid()
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Valid Title",
            AuthorOrSourceUrl: null,
            CategoryId: Guid.Empty);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.ValidationFailed");
    }

    [Fact]
    public async Task UpdateBook_ShouldReturnValidationError_WhenAuthorExceeds500Chars()
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Valid Title",
            AuthorOrSourceUrl: new string('B', 501),
            CategoryId : CategoryDefaults.DatabaseStorageId);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("invalid")]
    public async Task UpdateBook_ShouldReturnValidationError_WhenLanguageIsUnsupported(string invalidLanguage)
    {
        // Arrange
        var request = new UpdateBookRequest(
            BookId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Valid Title",
            AuthorOrSourceUrl: null,
            CategoryId : CategoryDefaults.FrontendWebId,
            Language: invalidLanguage);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task UpdateBook_ShouldCascadeLanguageToChunks_WhenLanguageProvided()
    {
        // Arrange
        var user = await CreateUserAsync("owner_lang@example.com");
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Book in English",
            Slug = "book-in-english",
            CategoryId = CategoryDefaults.EngineeringCraftId,
            SourceType = SourceType.MarkdownSeries,
            CreatedByUserId = user.Id
        };
        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1",
            OriginalTextMarkdown = "Content in English",
            Language = "en"
        };
        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 2,
            ChapterTitle = "Chapter 2",
            OriginalTextMarkdown = "More content",
            Language = "en"
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddRangeAsync(chunk1, chunk2);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: user.Id,
            Title: "Sách tiếng Việt",
            AuthorOrSourceUrl: "Tác giả VN",
            CategoryId : CategoryDefaults.HabitsProductivityId,
            Language: "vi");

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Language.Should().Be("vi");

        var reloadedChunks = await _db.DocumentChunks
            .Where(c => c.DocumentBookId == book.Id)
            .ToListAsync();

        reloadedChunks.Should().HaveCount(2);
        reloadedChunks.All(c => c.Language == "vi").Should().BeTrue();
        reloadedChunks.All(c => c.UpdatedAt != null).Should().BeTrue();
    }
}
