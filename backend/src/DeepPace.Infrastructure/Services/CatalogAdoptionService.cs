using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class CatalogAdoptionService : ICatalogAdoptionService
{
    private readonly DeepPaceDbContext _dbContext;
    private readonly ILogger<CatalogAdoptionService> _logger;

    public CatalogAdoptionService(
        DeepPaceDbContext dbContext,
        ILogger<CatalogAdoptionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CatalogAdoptionResult>> AdoptAsync(
        Guid catalogDocumentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // 1. Quick check for existing active owned copy
        var existingCopy = await _dbContext.DocumentBooks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CreatedByUserId == userId && b.SourceCatalogDocumentId == catalogDocumentId && !b.IsDeleted, cancellationToken);

        if (existingCopy != null)
        {
            return new CatalogAdoptionResult(existingCopy.Id, AlreadyAdded: true);
        }

        // 2. Begin transaction for atomic row locking, eligibility check, and snapshot copy
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            CatalogDocument? doc;
            if (isNpgsql)
            {
                // PostgreSQL row lock to ensure publication and visibility state cannot change during adoption
                doc = await _dbContext.CatalogDocuments
                    .FromSqlInterpolated($"SELECT * FROM \"CatalogDocuments\" WHERE \"Id\" = {catalogDocumentId} FOR UPDATE")
                    .Include(d => d.Slices)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                doc = await _dbContext.CatalogDocuments
                    .Include(d => d.Slices)
                    .FirstOrDefaultAsync(d => d.Id == catalogDocumentId, cancellationToken);
            }

            if (doc == null || doc.IsDeleted)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Error.Custom("Catalog.NotFound", "The requested catalog document was not found.");
            }

            if (doc.Visibility != CatalogVisibility.Published || doc.ProcessingStatus != ProcessingStatus.Ready)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return Error.Custom("Catalog.NotAvailable", "The document is not currently published and available for adoption.");
            }

            // Check again inside transaction with row locked
            existingCopy = await _dbContext.DocumentBooks
                .FirstOrDefaultAsync(b => b.CreatedByUserId == userId && b.SourceCatalogDocumentId == catalogDocumentId && !b.IsDeleted, cancellationToken);

            if (existingCopy != null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new CatalogAdoptionResult(existingCopy.Id, AlreadyAdded: true);
            }

            // Generate unique slug for the user's book copy
            var baseSlug = GenerateSlug(doc.Title);
            var uniqueSlug = $"{baseSlug}-{userId.ToString("N").Substring(0, 8)}";

            // If slug exists, append random suffix
            var slugExists = await _dbContext.DocumentBooks
                .AnyAsync(b => b.Slug == uniqueSlug && !b.IsDeleted, cancellationToken);
            if (slugExists)
            {
                uniqueSlug = $"{uniqueSlug}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            }

            // Create personal snapshot from catalog document (domain logic)
            var book = DocumentBook.CreateFromCatalogSnapshot(doc, userId, uniqueSlug);

            await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Learner {UserId} successfully adopted catalog document {CatalogId} as personal book {BookId}",
                userId, catalogDocumentId, book.Id);

            return new CatalogAdoptionResult(book.Id, AlreadyAdded: false);
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            // Handle potential concurrent race caught by partial unique index IX_DocumentBooks_CreatedByUserId_SourceCatalogDocumentId
            var winningCopy = await _dbContext.DocumentBooks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CreatedByUserId == userId && b.SourceCatalogDocumentId == catalogDocumentId && !b.IsDeleted, CancellationToken.None);

            if (winningCopy != null)
            {
                _logger.LogInformation("Concurrent adoption for user {UserId} and catalog {CatalogId} resolved to existing book {BookId}",
                    userId, catalogDocumentId, winningCopy.Id);
                return new CatalogAdoptionResult(winningCopy.Id, AlreadyAdded: true);
            }

            _logger.LogError(dbEx, "Database error during adoption of catalog document {CatalogId} by user {UserId}",
                catalogDocumentId, userId);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(ex, "Unexpected error adopting catalog document {CatalogId} by user {UserId}",
                catalogDocumentId, userId);
            throw;
        }
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}
