using System.Data.Common;
using DeepPace.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public sealed class CategoryAssignmentGuard : ICategoryAssignmentGuard
{
    private readonly DeepPaceDbContext _dbContext;
    private readonly ILogger<CategoryAssignmentGuard> _logger;

    public CategoryAssignmentGuard(DeepPaceDbContext dbContext, ILogger<CategoryAssignmentGuard>? logger = null)
    {
        _dbContext = dbContext;
        _logger = logger ?? NullLogger<CategoryAssignmentGuard>.Instance;
    }

    public async Task<Result<DocumentCategory>> ValidateAsync(Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            var category = await _dbContext.DocumentCategories.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == categoryId, ct);
            return ValidateCategory(category, requireActive: true);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Category validation failed for {CategoryId}", categoryId);
            return AssignmentUnavailable;
        }
    }

    public Task<Result<TResult>> ExecuteAsync<TResult>(
        Guid categoryId,
        Func<DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default) =>
        ExecuteCoreAsync(categoryId, requireActive: true, mutation, ct);

    public Task<Result<TResult>> ExecuteForBookAsync<TResult>(
        Guid bookId,
        Guid userId,
        Guid expectedCategoryId,
        Func<DocumentBook, DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default) =>
        ExecuteCoreAsync(expectedCategoryId, requireActive: false, async category =>
        {
            var source = IsPostgres
                ? await _dbContext.DocumentBooks.FromSqlInterpolated(
                    $"SELECT * FROM \"DocumentBooks\" WHERE \"Id\" = {bookId} AND \"IsDeleted\" = false FOR UPDATE")
                    .IgnoreAutoIncludes().AsNoTracking().SingleOrDefaultAsync(ct)
                : await _dbContext.DocumentBooks.IgnoreAutoIncludes().AsNoTracking()
                    .SingleOrDefaultAsync(b => b.Id == bookId, ct);
            if (source == null) return Error.NotFound;
            if (source.CreatedByUserId != userId)
                return Error.Custom("LIBRARY_FORBIDDEN", "You do not own this book.");
            if (source.CategoryId != expectedCategoryId) return SourceCategoryConflict;

            var tracked = TrackFresh(source);
            tracked.Category = category;
            return await mutation(tracked, category);
        }, ct);

    public Task<Result<TResult>> ExecuteForCatalogAsync<TResult>(
        Guid catalogDocumentId,
        Guid expectedCategoryId,
        Func<CatalogDocument, DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default) =>
        ExecuteCoreAsync(expectedCategoryId, requireActive: false, async category =>
        {
            var source = IsPostgres
                ? await _dbContext.CatalogDocuments.FromSqlInterpolated(
                    $"SELECT * FROM \"CatalogDocuments\" WHERE \"Id\" = {catalogDocumentId} AND \"IsDeleted\" = false FOR UPDATE")
                    .IgnoreAutoIncludes().AsNoTracking().SingleOrDefaultAsync(ct)
                : await _dbContext.CatalogDocuments.IgnoreAutoIncludes().AsNoTracking()
                    .SingleOrDefaultAsync(d => d.Id == catalogDocumentId, ct);
            if (source == null) return Error.NotFound;
            if (source.CategoryId != expectedCategoryId) return SourceCategoryConflict;

            var tracked = TrackFresh(source);
            tracked.Category = category;
            return await mutation(tracked, category);
        }, ct);

    private async Task<Result<TResult>> ExecuteCoreAsync<TResult>(
        Guid categoryId,
        bool requireActive,
        Func<DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct)
    {
        IDbContextTransaction? transaction = null;
        var succeeded = false;
        try
        {
            if (_dbContext.Database.IsRelational() && _dbContext.Database.CurrentTransaction == null)
                transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            var category = IsPostgres
                ? await _dbContext.DocumentCategories.FromSqlInterpolated(
                    $"SELECT * FROM \"DocumentCategories\" WHERE \"Id\" = {categoryId} AND \"IsDeleted\" = false FOR SHARE")
                    .AsNoTracking().SingleOrDefaultAsync(ct)
                : await _dbContext.DocumentCategories.AsNoTracking()
                    .SingleOrDefaultAsync(c => c.Id == categoryId, ct);
            var validation = ValidateCategory(category, requireActive);
            if (validation.IsFailure)
                return await RejectAsync<TResult>(validation.Error, transaction, ct);

            var result = await mutation(TrackFresh(validation.Value));
            if (result.IsFailure)
                return await RejectAsync<TResult>(result.Error, transaction, ct);

            await _dbContext.SaveChangesAsync(ct);
            if (transaction != null) await transaction.CommitAsync(ct);
            succeeded = true;
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Category assignment conflicted for {CategoryId}", categoryId);
            return await RejectAsync<TResult>(SourceCategoryConflict, transaction, ct);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            _logger.LogError(ex, "Category assignment persistence failed for {CategoryId}", categoryId);
            return await RejectAsync<TResult>(AssignmentUnavailable, transaction, ct);
        }
        finally
        {
            if (!succeeded) _dbContext.ChangeTracker.Clear();
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    private async Task<Result<TResult>> RejectAsync<TResult>(
        Error error, IDbContextTransaction? transaction, CancellationToken ct)
    {
        try
        {
            if (transaction != null) await transaction.RollbackAsync(ct);
            return error;
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Category assignment rollback failed");
            return AssignmentUnavailable;
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    private TEntity TrackFresh<TEntity>(TEntity fresh) where TEntity : BaseEntity
    {
        var tracked = _dbContext.Set<TEntity>().Local.SingleOrDefault(entity => entity.Id == fresh.Id);
        if (tracked == null)
        {
            _dbContext.Attach(fresh);
            return fresh;
        }

        _dbContext.Entry(tracked).CurrentValues.SetValues(fresh);
        _dbContext.Entry(tracked).OriginalValues.SetValues(fresh);
        return tracked;
    }

    private static Result<DocumentCategory> ValidateCategory(DocumentCategory? category, bool requireActive)
    {
        if (category == null || category.IsDeleted)
            return Error.Custom("Category.ValidationFailed", "Select an existing category.");
        if (requireActive && !category.IsActive)
            return Error.Custom("Category.Inactive", "The selected category is inactive and cannot receive new assignments.");
        return category;
    }

    private bool IsPostgres =>
        _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    private static readonly Error SourceCategoryConflict =
        Error.Custom("Category.ConcurrencyConflict", "The source category changed. Reload before trying again.");

    private static readonly Error AssignmentUnavailable =
        Error.Custom("Category.Unavailable", "Category assignment is temporarily unavailable.");
}
