using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Learner.GetDocumentById;

public record GetPublishedCatalogDocumentByIdRequest(Guid Id, Guid? UserId = null);

public class GetPublishedCatalogDocumentByIdHandler : IUseCase<GetPublishedCatalogDocumentByIdRequest, LearnerCatalogDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public GetPublishedCatalogDocumentByIdHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<LearnerCatalogDetailDto>> ExecuteAsync(
        GetPublishedCatalogDocumentByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.Visibility == CatalogVisibility.Published && !d.IsDeleted, cancellationToken);

        if (doc == null)
        {
            return Error.Custom("Catalog.NotFound", "The requested document was not found or is not currently published.");
        }

        Guid? existingBookId = null;
        if (request.UserId.HasValue)
        {
            var existingBook = await _dbContext.DocumentBooks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CreatedByUserId == request.UserId.Value
                    && b.SourceCatalogDocumentId == doc.Id
                    && !b.IsDeleted, cancellationToken);

            existingBookId = existingBook?.Id;
        }

        return doc.ToLearnerDetailDto(existingBookId);
    }
}
