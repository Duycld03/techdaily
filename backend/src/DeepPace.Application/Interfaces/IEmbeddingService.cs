using Pgvector;
using DeepPace.Application.Common;

namespace DeepPace.Application.Interfaces;

public interface IEmbeddingService
{
    Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default);
}
