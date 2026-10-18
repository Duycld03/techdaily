using DeepPace.Application.Features.Admin.Categories.DTOs;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public record AdminGetDocumentDetailRequest(string Scope, Guid Id);

public class AdminGetDocumentDetailHandler : IUseCase<AdminGetDocumentDetailRequest, AdminDocumentDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IDocumentMutationGuard _mutationGuard;

    public AdminGetDocumentDetailHandler(
        IDeepPaceDbContext dbContext,
        IDocumentMutationGuard mutationGuard)
    {
        _dbContext = dbContext;
        _mutationGuard = mutationGuard;
    }

    public async Task<Result<AdminDocumentDetailDto>> ExecuteAsync(
        AdminGetDocumentDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var scope = request.Scope.Trim().ToLowerInvariant();

        if (scope == "personal")
        {
            var book = await _dbContext.DocumentBooks
                .AsNoTracking()
                .Include(b => b.CreatedByUser)
                .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken);

            if (book == null)
            {
                return new Error("Document.NotFound", "Personal document was not found or is deleted.");
            }

            var hasActiveAttempt = await _dbContext.IngestionAttempts
                .AsNoTracking()
                .AnyAsync(a => a.TargetScope == IngestionTargetScope.Personal &&
                               a.TargetDocumentId == book.Id &&
                               (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                    cancellationToken);

            var hasLearningActivity = await _mutationGuard.HasLearningReferencesAsync(book.Id, cancellationToken);

            IngestionSource? source = null;
            if (book.CurrentAttemptId.HasValue)
            {
                var attempt = await _dbContext.IngestionAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == book.CurrentAttemptId.Value, cancellationToken);

                if (attempt?.SourceId.HasValue == true)
                {
                    source = await _dbContext.IngestionSources
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == attempt.SourceId.Value, cancellationToken);
                }
            }

            var eligibility = DocumentRetryEligibility.EvaluatePersonalRetry(
                book, source, hasActiveAttempt, hasLearningActivity, now);

            var availability = source?.Availability.ToString() ?? "Missing";

            var dto = new AdminDocumentDetailDto(
                book.Id,
                "Personal",
                book.Title,
                book.CreatedByUserId,
                book.CreatedByUser?.Name,
                book.CreatedByUser?.Email,
                book.SourceType.ToString(),
                book.CategoryId,
                book.Category!.ToDescriptor(),
                book.DeclaredLanguage ?? "en",
                book.Status.ToString(),
                availability,
                book.CreatedAt,
                now,
                book.IngestionControlVersion,
                book.CurrentAttemptGeneration,
                book.CurrentAttemptId,
                eligibility.IsEligible,
                eligibility.IneligibleReasonCode,
                eligibility.LocalizedMessage,
                source?.RetentionExpiresAtUtc,
                SanitizeUrl(book.AuthorOrSourceUrl),
                null);

            return dto;
        }
        else if (scope == "catalog")
        {
            var doc = await _dbContext.CatalogDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, cancellationToken);

            if (doc == null)
            {
                return new Error("Document.NotFound", "Catalog document was not found or is deleted.");
            }

            var hasActiveAttempt = await _dbContext.IngestionAttempts
                .AsNoTracking()
                .AnyAsync(a => a.TargetScope == IngestionTargetScope.Catalog &&
                               a.TargetDocumentId == doc.Id &&
                               (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                    cancellationToken);

            IngestionSource? source = null;
            if (doc.CurrentAttemptId.HasValue)
            {
                var attempt = await _dbContext.IngestionAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == doc.CurrentAttemptId.Value, cancellationToken);

                if (attempt?.SourceId.HasValue == true)
                {
                    source = await _dbContext.IngestionSources
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == attempt.SourceId.Value, cancellationToken);
                }
            }

            var eligibility = DocumentRetryEligibility.EvaluateCatalogRetry(
                doc, source, hasActiveAttempt, now);

            var availability = source?.Availability.ToString() ?? "Missing";

            var dto = new AdminDocumentDetailDto(
                doc.Id,
                "Catalog",
                doc.Title,
                null,
                "System Curated",
                null,
                doc.SourceType.ToString(),
                doc.CategoryId,
                doc.Category!.ToDescriptor(),
                doc.Language,
                doc.ProcessingStatus.ToString(),
                availability,
                doc.CreatedAt,
                now,
                doc.ConcurrencyVersion,
                doc.ProcessingGeneration,
                doc.CurrentAttemptId,
                eligibility.IsEligible,
                eligibility.IneligibleReasonCode,
                eligibility.LocalizedMessage,
                source?.RetentionExpiresAtUtc,
                SanitizeUrl(doc.AuthorOrSourceUrl),
                doc.Visibility);

            return dto;
        }

        return new Error("Document.InvalidScope", $"Unsupported scope '{request.Scope}'. Must be 'personal' or 'catalog'.");
    }

    private static string? SanitizeUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return null;

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            // Remove user info, query parameters, and fragments to prevent leaking secrets/tokens
            var builder = new UriBuilder(uri)
            {
                UserName = null,
                Password = null,
                Query = null,
                Fragment = null
            };
            return builder.Uri.ToString();
        }

        return rawUrl.Split('?')[0].Split('#')[0];
    }
}
