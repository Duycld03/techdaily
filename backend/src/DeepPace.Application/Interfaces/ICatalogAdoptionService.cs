using DeepPace.Application.Common;

namespace DeepPace.Application.Interfaces;

public record CatalogAdoptionResult(Guid BookId, bool AlreadyAdded);

public interface ICatalogAdoptionService
{
    Task<Result<CatalogAdoptionResult>> AdoptAsync(
        Guid catalogDocumentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
