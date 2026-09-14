using TechDaily.Domain.Entities;

namespace TechDaily.Application.Interfaces;

public interface ILookAheadBufferService
{
    Task PreGenerateInitialBufferAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task EnsureBufferDepthAsync(Guid bookId, int currentChunkOrder, CancellationToken cancellationToken = default);
    Task<InterviewQuestion?> PromoteChunkPriorityAsync(Guid bookId, int chunkOrder, CancellationToken cancellationToken = default);
    Task<InterviewQuestion?> GenerateChallengeForChunkAsync(Guid chunkId, CancellationToken cancellationToken = default);
}
