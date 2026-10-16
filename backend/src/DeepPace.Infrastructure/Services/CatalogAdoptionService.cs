using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class CatalogAdoptionService(
    DeepPaceDbContext dbContext,
    ILogger<CatalogAdoptionService> logger,
    ICategoryAssignmentGuard categoryGuard) : ICatalogAdoptionService
{
    public async Task<Result<CatalogAdoptionResult>> AdoptAsync(Guid catalogDocumentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await FindOwnedAsync(catalogDocumentId, userId, cancellationToken);
        if (existing != null) return new CatalogAdoptionResult(existing.Id, AlreadyAdded: true);

        var candidate = await dbContext.CatalogDocuments.AsNoTracking().IgnoreAutoIncludes()
            .FirstOrDefaultAsync(d => d.Id == catalogDocumentId && !d.IsDeleted, cancellationToken);
        if (candidate == null)
        {
            var committedCopy = await FindOwnedAsync(catalogDocumentId, userId, cancellationToken);
            return committedCopy != null
                ? new CatalogAdoptionResult(committedCopy.Id, AlreadyAdded: true)
                : Error.Custom("Catalog.NotFound", "The requested catalog document was not found.");
        }

        var result = await categoryGuard.ExecuteAsync<CatalogAdoptionResult>(candidate.CategoryId, async category =>
        {
            var source = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                ? await dbContext.CatalogDocuments.FromSqlInterpolated($"SELECT * FROM \"CatalogDocuments\" WHERE \"Id\" = {catalogDocumentId} FOR UPDATE")
                    .IgnoreAutoIncludes().FirstOrDefaultAsync(cancellationToken)
                : await dbContext.CatalogDocuments.IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == catalogDocumentId, cancellationToken);
            if (source == null) return Error.Custom("Catalog.NotFound", "The requested catalog document was not found.");
            await dbContext.Entry(source).ReloadAsync(cancellationToken);
            if (source.IsDeleted) return Error.Custom("Catalog.NotFound", "The requested catalog document was not found.");
            if (source.CategoryId != candidate.CategoryId)
                return Error.Custom("Category.ConcurrencyConflict", "The source category changed. Reload the catalog before adopting.");
            existing = await FindOwnedAsync(catalogDocumentId, userId, cancellationToken);
            if (existing != null) return new CatalogAdoptionResult(existing.Id, AlreadyAdded: true);
            if (source.Visibility != CatalogVisibility.Published || source.ProcessingStatus != ProcessingStatus.Ready)
                return Error.Custom("Catalog.NotAvailable", "The document is not currently published and available for adoption.");

            await dbContext.Entry(source).Collection(d => d.Slices).LoadAsync(cancellationToken);
            source.Category = category;
            var slug = $"{GenerateSlug(source.Title)}-{userId:N}";
            if (await dbContext.DocumentBooks.AnyAsync(b => b.Slug == slug && !b.IsDeleted, cancellationToken))
                slug = $"{slug}-{Guid.NewGuid():N}";
            var book = DocumentBook.CreateFromCatalogSnapshot(source, userId, slug);
            await dbContext.DocumentBooks.AddAsync(book, cancellationToken);
            logger.LogInformation("Learner {UserId} adopted catalog {CatalogId} as book {BookId}", userId, catalogDocumentId, book.Id);
            return new CatalogAdoptionResult(book.Id, AlreadyAdded: false);
        }, cancellationToken);
        if (result.IsSuccess) return result;

        // Admission may lose a race to a committed adoption, even if the source is no longer assignable.
        // The guard has disposed its owned transaction and cleared any failed staged writes.
        var winningCopy = await FindOwnedAsync(catalogDocumentId, userId, cancellationToken);
        return winningCopy != null
            ? new CatalogAdoptionResult(winningCopy.Id, AlreadyAdded: true)
            : result;
    }

    private Task<DocumentBook?> FindOwnedAsync(Guid catalogId, Guid userId, CancellationToken ct) => dbContext.DocumentBooks
        .AsNoTracking().IgnoreAutoIncludes()
        .FirstOrDefaultAsync(b => b.CreatedByUserId == userId && b.SourceCatalogDocumentId == catalogId && !b.IsDeleted, ct);

    private static string GenerateSlug(string title)
    {
        var slug = Regex.Replace(title.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(Regex.Replace(slug, @"\s+", "-"), @"-+", "-").Trim('-');
    }
}
