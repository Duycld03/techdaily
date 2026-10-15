using DeepPace.Domain.Enums;

namespace DeepPace.Application.Interfaces;

public interface IDocumentMutationGuard
{
    Task<TResult> ExecuteWithMutationGuardAsync<TResult>(
        Guid documentId,
        IngestionTargetScope scope,
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);

    Task<bool> HasLearningReferencesAsync(
        Guid personalBookId,
        CancellationToken cancellationToken = default);
}
