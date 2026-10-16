using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public sealed class CatalogAdoptionIdempotencyBoundaryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<DeepPaceDbContext> _options;
    private readonly DeepPaceDbContext _db;
    private readonly User _caller = new() { Email = "adoption-caller@example.test", Name = "Caller" };
    private readonly User _other = new() { Email = "adoption-other@example.test", Name = "Other" };
    private readonly DocumentCategory _category = DocumentCategory.Create("idempotency-subject", "Subject", "Chủ đề");
    private readonly CatalogDocument _source;

    public CatalogAdoptionIdempotencyBoundaryTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        _db = new DeepPaceDbContext(_options);
        _db.Database.EnsureCreated();
        _source = new CatalogDocument
        {
            Title = "Original catalog snapshot", CategoryId = _category.Id,
            Visibility = CatalogVisibility.Published, ProcessingStatus = ProcessingStatus.Ready,
            AttributionText = "Original attribution", TotalSlices = 1,
            Slices = [new CatalogSlice
            {
                SliceOrder = 1, ChapterTitle = "Original chapter", OriginalTextMarkdown = "Original source content",
                SummaryMarkdown = "Original summary", IsAiFormatted = true
            }]
        };
        _db.Users.AddRange(_caller, _other);
        _db.DocumentCategories.Add(_category);
        _db.CatalogDocuments.Add(_source);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("reclassified")]
    [InlineData("deleted")]
    [InlineData("hidden")]
    public async Task Adopt_ConcurrentWinnerThenAdmissionChanges_ReturnsExactOwnedSnapshotWithoutNewAssignment(string change)
    {
        Guid winningId = Guid.Empty;
        var guard = new BeforeAdmissionGuard(new CategoryAssignmentGuard(_db), async () =>
        {
            await using var concurrent = new DeepPaceDbContext(_options);
            var winner = await Service(concurrent).AdoptAsync(_source.Id, _caller.Id);
            winner.IsSuccess.Should().BeTrue();
            winner.Value.AlreadyAdded.Should().BeFalse();
            winningId = winner.Value.BookId;
            switch (change)
            {
                case "inactive":
                    await concurrent.DocumentCategories.Where(c => c.Id == _category.Id)
                        .ExecuteUpdateAsync(update => update.SetProperty(c => c.IsActive, false));
                    break;
                case "reclassified":
                    await concurrent.CatalogDocuments.Where(d => d.Id == _source.Id)
                        .ExecuteUpdateAsync(update => update.SetProperty(d => d.CategoryId, CategoryDefaults.BackendRuntimeId));
                    break;
                case "deleted":
                    await concurrent.CatalogDocuments.Where(d => d.Id == _source.Id)
                        .ExecuteUpdateAsync(update => update.SetProperty(d => d.IsDeleted, true));
                    break;
                case "hidden":
                    await concurrent.CatalogDocuments.Where(d => d.Id == _source.Id)
                        .ExecuteUpdateAsync(update => update.SetProperty(d => d.Visibility, CatalogVisibility.Hidden));
                    break;
            }
        });

        var result = await Service(_db, guard).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyAdded.Should().BeTrue();
        result.Value.BookId.Should().Be(winningId);
        await using var persisted = new DeepPaceDbContext(_options);
        var book = (await persisted.DocumentBooks.Include(b => b.Chunks).ToListAsync()).Should().ContainSingle().Subject;
        book.Id.Should().Be(winningId);
        book.CreatedByUserId.Should().Be(_caller.Id);
        book.SourceCatalogDocumentId.Should().Be(_source.Id);
        book.CategoryId.Should().Be(_category.Id, "the existing snapshot must not be reclassified with the source");
        book.Title.Should().Be("Original catalog snapshot");
        book.SourceAttribution.Should().Be("Original attribution");
        book.Chunks.Should().ContainSingle().Which.OriginalTextMarkdown.Should().Be("Original source content");
        (await persisted.DocumentChunks.CountAsync()).Should().Be(1);
        if (change == "inactive")
            (await persisted.DocumentCategories.SingleAsync(c => c.Id == _category.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Adopt_ExistingCopyAndInactiveCategory_ReturnsOriginalOwnedCopy()
    {
        var first = await Service(_db).AdoptAsync(_source.Id, _caller.Id);
        first.IsSuccess.Should().BeTrue();
        await _db.DocumentCategories.Where(c => c.Id == _category.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.IsActive, false));
        _db.ChangeTracker.Clear();

        var result = await Service(_db).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.BookId.Should().Be(first.Value.BookId);
        result.Value.AlreadyAdded.Should().BeTrue();
        var stored = (await _db.DocumentBooks.AsNoTracking().ToListAsync()).Should().ContainSingle().Subject;
        stored.CreatedByUserId.Should().Be(_caller.Id);
        stored.CategoryId.Should().Be(_category.Id);
        (await _db.DocumentChunks.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("foreign")]
    [InlineData("deleted")]
    public async Task Adopt_InactiveWithoutLiveOwnedCopy_DoesNotCreateOrReturnAnotherCopy(string existingCopy)
    {
        Guid? previousId = null;
        if (existingCopy != "none")
        {
            var first = await Service(_db).AdoptAsync(_source.Id, existingCopy == "foreign" ? _other.Id : _caller.Id);
            first.IsSuccess.Should().BeTrue();
            previousId = first.Value.BookId;
            if (existingCopy == "deleted")
                await _db.DocumentBooks.Where(b => b.Id == previousId)
                    .ExecuteUpdateAsync(update => update.SetProperty(b => b.IsDeleted, true));
        }
        await _db.DocumentCategories.Where(c => c.Id == _category.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.IsActive, false));
        _db.ChangeTracker.Clear();

        var result = await Service(_db).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.Inactive");
        await using var persisted = new DeepPaceDbContext(_options);
        (await persisted.DocumentBooks.AnyAsync(b => b.CreatedByUserId == _caller.Id)).Should().BeFalse();
        var allBooks = await persisted.DocumentBooks.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        allBooks.Should().HaveCount(previousId.HasValue ? 1 : 0);
        if (previousId.HasValue)
        {
            var previous = allBooks.Single();
            previous.Id.Should().Be(previousId.Value);
            previous.CreatedByUserId.Should().Be(existingCopy == "foreign" ? _other.Id : _caller.Id);
            previous.IsDeleted.Should().Be(existingCopy == "deleted");
        }
        (await persisted.DocumentChunks.CountAsync()).Should().Be(previousId.HasValue ? 1 : 0);
    }

    [Fact]
    public async Task Adopt_ReclassifiedSourceWithoutWinner_ReturnsConflictAndPersistsNoSnapshot()
    {
        var guard = new BeforeAdmissionGuard(new CategoryAssignmentGuard(_db), async () =>
        {
            await using var concurrent = new DeepPaceDbContext(_options);
            await concurrent.CatalogDocuments.Where(d => d.Id == _source.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(d => d.CategoryId, CategoryDefaults.BackendRuntimeId));
        });

        var result = await Service(_db, guard).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.ConcurrencyConflict");
        await using var persisted = new DeepPaceDbContext(_options);
        (await persisted.DocumentBooks.AnyAsync()).Should().BeFalse();
        (await persisted.DocumentChunks.AnyAsync()).Should().BeFalse();
        (await persisted.CatalogDocuments.SingleAsync()).CategoryId.Should().Be(CategoryDefaults.BackendRuntimeId);
    }

    [Fact]
    public async Task Adopt_PersistenceFailureWithoutWinner_PreservesFailureInsteadOfReturningAFakeCopy()
    {
        await _db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER reject_adoption BEFORE INSERT ON "DocumentBooks"
            BEGIN
                SELECT RAISE(ABORT, 'Adoption persistence rejected');
            END;
            """);

        var result = await Service(_db).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.Unavailable");
        await using var persisted = new DeepPaceDbContext(_options);
        (await persisted.DocumentBooks.AnyAsync()).Should().BeFalse();
        (await persisted.DocumentChunks.AnyAsync()).Should().BeFalse();
        (await persisted.CatalogDocuments.SingleAsync()).CategoryId.Should().Be(_category.Id);
        (await persisted.DocumentCategories.SingleAsync(c => c.Id == _category.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Adopt_WinnerCommitsBeforeCandidateReadAndSourceIsDeleted_ReturnsExactExistingSnapshot()
    {
        Guid winningId = Guid.Empty;
        var interceptor = new BeforeCatalogReadInterceptor(async () =>
        {
            await using var concurrent = new DeepPaceDbContext(_options);
            var winner = await Service(concurrent).AdoptAsync(_source.Id, _caller.Id);
            winner.IsSuccess.Should().BeTrue();
            winningId = winner.Value.BookId;
            await concurrent.CatalogDocuments.Where(d => d.Id == _source.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(d => d.IsDeleted, true));
        });
        var racingOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(interceptor).Options;
        await using var racing = new DeepPaceDbContext(racingOptions);

        var result = await Service(racing).AdoptAsync(_source.Id, _caller.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.BookId.Should().Be(winningId);
        result.Value.AlreadyAdded.Should().BeTrue();
        await using var persisted = new DeepPaceDbContext(_options);
        var book = (await persisted.DocumentBooks.Include(b => b.Chunks).ToListAsync()).Should().ContainSingle().Subject;
        book.Id.Should().Be(winningId);
        book.CreatedByUserId.Should().Be(_caller.Id);
        book.SourceCatalogDocumentId.Should().Be(_source.Id);
        book.CategoryId.Should().Be(_category.Id);
        book.Title.Should().Be("Original catalog snapshot");
        book.Chunks.Should().ContainSingle().Which.OriginalTextMarkdown.Should().Be("Original source content");
        (await persisted.CatalogDocuments.AnyAsync()).Should().BeFalse();
    }

    private sealed class BeforeCatalogReadInterceptor(Func<Task> beforeRead) : DbCommandInterceptor
    {
        private bool _ran;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (!_ran && command.CommandText.Contains("\"CatalogDocuments\"", StringComparison.Ordinal))
            {
                _ran = true;
                await beforeRead();
            }
            return result;
        }
    }

    private static CatalogAdoptionService Service(DeepPaceDbContext db, ICategoryAssignmentGuard? guard = null) =>
        new(db, NullLogger<CatalogAdoptionService>.Instance, guard ?? new CategoryAssignmentGuard(db));

    private sealed class BeforeAdmissionGuard(ICategoryAssignmentGuard inner, Func<Task> beforeAdmission) : ICategoryAssignmentGuard
    {
        public async Task<Result<TResult>> ExecuteAsync<TResult>(Guid categoryId,
            Func<DocumentCategory, Task<Result<TResult>>> mutation, CancellationToken ct = default)
        {
            await beforeAdmission();
            return await inner.ExecuteAsync(categoryId, mutation, ct);
        }

        public Task<Result<DocumentCategory>> ValidateAsync(Guid categoryId, CancellationToken ct = default) =>
            inner.ValidateAsync(categoryId, ct);

        public Task<Result<TResult>> ExecuteForBookAsync<TResult>(Guid bookId, Guid userId, Guid expectedCategoryId,
            Func<DocumentBook, DocumentCategory, Task<Result<TResult>>> mutation, CancellationToken ct = default) =>
            inner.ExecuteForBookAsync(bookId, userId, expectedCategoryId, mutation, ct);

        public Task<Result<TResult>> ExecuteForCatalogAsync<TResult>(Guid catalogDocumentId, Guid expectedCategoryId,
            Func<CatalogDocument, DocumentCategory, Task<Result<TResult>>> mutation, CancellationToken ct = default) =>
            inner.ExecuteForCatalogAsync(catalogDocumentId, expectedCategoryId, mutation, ct);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
