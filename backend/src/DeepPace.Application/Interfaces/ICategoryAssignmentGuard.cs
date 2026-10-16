using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface ICategoryAssignmentGuard
{
    Task<Result<DocumentCategory>> ValidateAsync(Guid categoryId, CancellationToken ct = default);

    Task<Result<TResult>> ExecuteAsync<TResult>(
        Guid categoryId,
        Func<DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default);

    Task<Result<TResult>> ExecuteForBookAsync<TResult>(
        Guid bookId,
        Guid userId,
        Guid expectedCategoryId,
        Func<DocumentBook, DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default);

    Task<Result<TResult>> ExecuteForCatalogAsync<TResult>(
        Guid catalogDocumentId,
        Guid expectedCategoryId,
        Func<CatalogDocument, DocumentCategory, Task<Result<TResult>>> mutation,
        CancellationToken ct = default);
}
