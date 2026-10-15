using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class DocumentMutationGuard : IDocumentMutationGuard
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly IDeepPaceDbContext _dbContext;

    public DocumentMutationGuard(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteWithMutationGuardAsync<TResult>(
        Guid documentId,
        IngestionTargetScope scope,
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var key = $"{scope}:{documentId}";
        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<bool> HasLearningReferencesAsync(
        Guid personalBookId,
        CancellationToken cancellationToken = default)
    {
        // 1. Check pacers
        var hasPacer = await _dbContext.UserBookPacers
            .AsNoTracking()
            .AnyAsync(p => p.DocumentBookId == personalBookId && !p.IsDeleted, cancellationToken);

        if (hasPacer)
        {
            return true;
        }

        // 2. Check chunk IDs
        var chunkIds = await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentBookId == personalBookId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (chunkIds.Count == 0)
        {
            return false;
        }

        // 3. Check highlights
        var hasHighlights = await _dbContext.UserHighlights
            .AsNoTracking()
            .AnyAsync(h => !h.IsDeleted && chunkIds.Contains(h.DocumentChunkId), cancellationToken);

        if (hasHighlights)
        {
            return true;
        }

        // 4. Check spaced repetition cards
        var hasCards = await _dbContext.SpacedRepetitionCards
            .AsNoTracking()
            .AnyAsync(c => !c.IsDeleted && c.SourceDocumentChunkId.HasValue && chunkIds.Contains(c.SourceDocumentChunkId.Value), cancellationToken);

        return hasCards;
    }
}
