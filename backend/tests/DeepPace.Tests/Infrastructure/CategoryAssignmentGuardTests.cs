using DeepPace.Application.Common;
using DeepPace.Domain.Entities;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public sealed class CategoryAssignmentGuardTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<DeepPaceDbContext> _options;
    private readonly DeepPaceDbContext _db;
    private readonly CategoryAssignmentGuard _guard;
    private readonly Guid _ownerId = Guid.NewGuid();

    public CategoryAssignmentGuardTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        _db = new DeepPaceDbContext(_options);
        _db.Database.EnsureCreated();
        _db.Users.Add(new User { Id = _ownerId, Email = "category-owner@example.com", Name = "Category Owner" });
        _db.SaveChanges();
        _guard = new CategoryAssignmentGuard(_db);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallbackRejects_RollsBackSavedAssignment()
    {
        var book = CreateBook(CategoryDefaults.FrontendWebId);

        var result = await _guard.ExecuteAsync<Guid>(book.CategoryId, async _ =>
        {
            _db.DocumentBooks.Add(book);
            await _db.SaveChangesAsync();
            return Error.Custom("Import.Rejected", "The import could not finish.");
        });

        result.Error.Code.Should().Be("Import.Rejected");
        await using var fresh = new DeepPaceDbContext(_options);
        (await fresh.DocumentBooks.AnyAsync(b => b.Id == book.Id)).Should().BeFalse();
        await _db.SaveChangesAsync();
        (await fresh.DocumentBooks.AnyAsync(b => b.Id == book.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTrackedCategoryWasDeactivated_RejectsFreshState()
    {
        var category = await _db.DocumentCategories.SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        await using var concurrent = new DeepPaceDbContext(_options);
        await concurrent.DocumentCategories.Where(c => c.Id == category.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.IsActive, false));
        var book = CreateBook(category.Id);

        var result = await _guard.ExecuteAsync<Guid>(category.Id, _ =>
        {
            _db.DocumentBooks.Add(book);
            return Task.FromResult(Result<Guid>.Success(book.Id));
        });

        result.Error.Code.Should().Be("Category.Inactive");
        (await concurrent.DocumentBooks.AnyAsync(b => b.Id == book.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteForBookAsync_WhenSourceWasReclassified_RejectsCachedAssignment()
    {
        var book = CreateBook(CategoryDefaults.FrontendWebId);
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();
        await using var concurrent = new DeepPaceDbContext(_options);
        await concurrent.DocumentBooks.Where(b => b.Id == book.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(b => b.CategoryId, CategoryDefaults.BackendRuntimeId));

        var result = await _guard.ExecuteForBookAsync<Guid>(book.Id, _ownerId, book.CategoryId, (source, _) =>
        {
            source.Title = "An outdated source must not be edited";
            return Task.FromResult(Result<Guid>.Success(source.Id));
        });

        result.Error.Code.Should().Be("Category.ConcurrencyConflict");
        var persisted = await concurrent.DocumentBooks.AsNoTracking().SingleAsync(b => b.Id == book.Id);
        persisted.Title.Should().Be("Category guard source");
        persisted.CategoryId.Should().Be(CategoryDefaults.BackendRuntimeId);
    }

    [Fact]
    public async Task ExecuteForBookAsync_WhenCallerDoesNotOwnSource_RejectsMutation()
    {
        var book = CreateBook(CategoryDefaults.FrontendWebId);
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        var result = await _guard.ExecuteForBookAsync<Guid>(book.Id, Guid.NewGuid(), book.CategoryId, (source, _) =>
        {
            source.Title = "Another account must not edit this source";
            return Task.FromResult(Result<Guid>.Success(source.Id));
        });

        result.Error.Code.Should().Be("LIBRARY_FORBIDDEN");
        await using var fresh = new DeepPaceDbContext(_options);
        (await fresh.DocumentBooks.SingleAsync(b => b.Id == book.Id)).Title.Should().Be("Category guard source");
    }

    [Fact]
    public async Task ExecuteForBookAsync_WhenRetainingInactiveAssignment_AllowsOwnedMetadataEdit()
    {
        var category = DocumentCategory.Create("retained-subject", "Retained Subject", "Chủ đề được giữ", isActive: false);
        _db.DocumentCategories.Add(category);
        var book = CreateBook(category.Id);
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _guard.ExecuteForBookAsync<Guid>(book.Id, _ownerId, category.Id, (source, _) =>
        {
            source.Title = "Updated retained source";
            return Task.FromResult(Result<Guid>.Success(source.Id));
        });

        result.IsSuccess.Should().BeTrue();
        await using var fresh = new DeepPaceDbContext(_options);
        var persisted = await fresh.DocumentBooks.SingleAsync(b => b.Id == book.Id);
        persisted.Title.Should().Be("Updated retained source");
        persisted.CategoryId.Should().Be(category.Id);
        (await fresh.DocumentCategories.SingleAsync(c => c.Id == category.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteForCatalogAsync_WhenSourceIsUntracked_RetainsItsCategory()
    {
        var document = new CatalogDocument
        {
            Title = "Catalog guard source", CategoryId = CategoryDefaults.BackendRuntimeId
        };
        _db.CatalogDocuments.Add(document);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _guard.ExecuteForCatalogAsync<Guid>(document.Id, document.CategoryId, (source, category) =>
        {
            source.Title = "Updated catalog source";
            source.CategoryId.Should().Be(category.Id);
            return Task.FromResult(Result<Guid>.Success(source.Id));
        });

        result.IsSuccess.Should().BeTrue();
        await using var fresh = new DeepPaceDbContext(_options);
        (await fresh.CatalogDocuments.SingleAsync(d => d.Id == document.Id)).Title.Should().Be("Updated catalog source");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_WhenCallbackThrows_DoesNotLeaveAnAssignmentAvailableForResaving(bool canceled)
    {
        var book = CreateBook(CategoryDefaults.FrontendWebId);
        var execute = () => _guard.ExecuteAsync<Guid>(book.CategoryId, _ =>
        {
            _db.DocumentBooks.Add(book);
            return canceled
                ? Task.FromCanceled<Result<Guid>>(new CancellationToken(canceled: true))
                : Task.FromException<Result<Guid>>(new InvalidOperationException("Mutation failed."));
        });

        if (canceled) await execute.Should().ThrowAsync<OperationCanceledException>();
        else await execute.Should().ThrowAsync<InvalidOperationException>();
        await _db.SaveChangesAsync();

        await using var fresh = new DeepPaceDbContext(_options);
        (await fresh.DocumentBooks.AnyAsync(b => b.Id == book.Id)).Should().BeFalse();
        _db.Database.CurrentTransaction.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnOuterTransactionExists_DoesNotCommitIt()
    {
        var book = CreateBook(CategoryDefaults.FrontendWebId);
        await using (var outer = await _db.Database.BeginTransactionAsync())
        {
            var result = await _guard.ExecuteAsync<Guid>(book.CategoryId, _ =>
            {
                _db.DocumentBooks.Add(book);
                return Task.FromResult(Result<Guid>.Success(book.Id));
            });
            result.IsSuccess.Should().BeTrue();
            _db.Database.CurrentTransaction.Should().BeSameAs(outer);
            await outer.RollbackAsync();
        }

        await using var fresh = new DeepPaceDbContext(_options);
        (await fresh.DocumentBooks.AnyAsync(b => b.Id == book.Id)).Should().BeFalse();
    }

    private DocumentBook CreateBook(Guid categoryId) => new()
    {
        Title = "Category guard source",
        Slug = "category-guard-source",
        CategoryId = categoryId,
        CreatedByUserId = _ownerId
    };

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
