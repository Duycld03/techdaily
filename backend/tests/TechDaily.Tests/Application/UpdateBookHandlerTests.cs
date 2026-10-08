using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.UpdateBook;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class UpdateBookHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;
    private readonly UpdateBookValidator _validator;
    private readonly UpdateBookHandler _handler;

    public UpdateBookHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();

        _validator = new UpdateBookValidator();
        _handler = new UpdateBookHandler(_db, _validator);
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
            Category = Category.BackendRuntime,
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
            Category: Category.HabitsProductivity);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Book.Title.Should().Be("Atomic Habits & Focus");
        result.Value.Book.AuthorOrSourceUrl.Should().Be("James Clear");
        result.Value.Book.Category.Should().Be(Category.HabitsProductivity);

        var updatedBook = await _db.DocumentBooks.FindAsync(book.Id);
        updatedBook.Should().NotBeNull();
        updatedBook!.Title.Should().Be("Atomic Habits & Focus");
        updatedBook.AuthorOrSourceUrl.Should().Be("James Clear");
        updatedBook.Category.Should().Be(Category.HabitsProductivity);
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
            Category: Category.MentalModels);

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
            Category = Category.SystemDesign,
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
            Category: Category.SystemDesign);

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
            Category = Category.EngineeringCraft,
            CreatedByUserId = owner.Id
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: caller.Id,
            Title: "Tampered Title",
            AuthorOrSourceUrl: null,
            Category: Category.EngineeringCraft);

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
            Category = Category.BackendRuntime,
            CreatedByUserId = null
        };
        await _db.DocumentBooks.AddAsync(book);
        await _db.SaveChangesAsync();

        var request = new UpdateBookRequest(
            BookId: book.Id,
            UserId: Guid.NewGuid(),
            Title: "Modified Title",
            AuthorOrSourceUrl: null,
            Category: Category.BackendRuntime);

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
            Category: Category.FrontendWeb);

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
            Category: Category.FrontendWeb);

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
            Category: (Category)99);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
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
            Category: Category.DatabaseStorage);

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
            Category: Category.FrontendWeb,
            Language: invalidLanguage);

        // Act
        var result = await _handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
        result.Error.Message.Should().Contain("Language must be either 'en' or 'vi'");
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
            Category = Category.EngineeringCraft,
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
            Category: Category.HabitsProductivity,
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
