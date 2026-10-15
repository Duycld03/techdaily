using DeepPace.Domain.Enums;

namespace DeepPace.Application.Interfaces;

/// <summary>
/// Coordinates exclusive execution between active document ingestion completion and learner writes.
/// Prevents race conditions where a concurrent learner write (highlights, flashcards, drills, pacer)
/// might occur while an ingestion attempt or recovery retry is replacing chunks or output.
/// </summary>
public interface IDocumentMutationGuard
{
    /// <summary>
    /// Executes the specified action under an exclusive concurrency lease scoped to the target document.
    /// Concurrent calls targeting the same document ID and scope will await completion in order.
    /// </summary>
    Task<TResult> ExecuteWithMutationGuardAsync<TResult>(
        Guid documentId,
        IngestionTargetScope scope,
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any active learner references (highlights, notes, spaced repetition cards,
    /// daily drills, or reading pacers) exist for the given personal book. If true, destructive source
    /// replacement or retry is rejected to preserve learner recall history.
    /// </summary>
    Task<bool> HasLearningReferencesAsync(
        Guid personalBookId,
        CancellationToken cancellationToken = default);
}
