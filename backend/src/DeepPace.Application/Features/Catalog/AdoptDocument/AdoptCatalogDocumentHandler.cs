using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Catalog.AdoptDocument;

public record AdoptCatalogDocumentRequest(Guid CatalogDocumentId, Guid UserId);

public class AdoptCatalogDocumentHandler : IUseCase<AdoptCatalogDocumentRequest, CatalogAdoptionResult>
{
    private readonly ICatalogAdoptionService _adoptionService;

    public AdoptCatalogDocumentHandler(ICatalogAdoptionService adoptionService)
    {
        _adoptionService = adoptionService;
    }

    public async Task<Result<CatalogAdoptionResult>> ExecuteAsync(
        AdoptCatalogDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            return Error.Custom("Auth.Unauthorized", "A valid authenticated learner is required.");
        }

        return await _adoptionService.AdoptAsync(request.CatalogDocumentId, request.UserId, cancellationToken);
    }
}
