using Pgvector;
using TechDaily.Application.Common;

namespace TechDaily.Application.Interfaces;

public interface IEmbeddingService
{
    Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default);
}
