using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.UpdateMetadata;

public record AdminUpdateCatalogMetadataRequest(
    Guid Id, string Title, string Description, Guid CategoryId, string Language,
    string? AuthorOrSourceUrl, string? AttributionText, int ExpectedConcurrencyVersion,
    Guid? AdminUserId = null, int PresentedSessionVersion = 1);

public class AdminUpdateCatalogMetadataValidator : AbstractValidator<AdminUpdateCatalogMetadataRequest>
{
    public AdminUpdateCatalogMetadataValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
    }
}

public class AdminUpdateCatalogMetadataHandler(
    IDeepPaceDbContext dbContext,
    IValidator<AdminUpdateCatalogMetadataRequest> validator,
    ICategoryAssignmentGuard categoryGuard,
    IAdministrativeUnitOfWork unitOfWork,
    IDocumentMutationGuard mutationGuard) : IUseCase<AdminUpdateCatalogMetadataRequest, AdminCatalogDocumentDto>
{
    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(AdminUpdateCatalogMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Error.Custom(request.CategoryId == Guid.Empty ? "Category.ValidationFailed" : "Validation.Failed", validation.Errors.First().ErrorMessage);

        var result = await mutationGuard.ExecuteWithMutationGuardAsync(request.Id, IngestionTargetScope.Catalog,
            () => unitOfWork.ExecutePrivilegedMutationAsync(request.AdminUserId ?? Guid.Empty, request.PresentedSessionVersion,
                new[] { AdminPermission.ManageCatalog }, () => AdmitAsync(request, cancellationToken), cancellationToken), cancellationToken);
        if (!result.IsSuccess && request.AdminUserId is { } actor && actor != Guid.Empty && result.Error.Code.StartsWith("Catalog.", StringComparison.Ordinal))
            await unitOfWork.AppendCleanAuditEventAsync(AdminAuditEvent.Create(actor, AdminAuditActions.CatalogUpdateMetadata,
                "CatalogDocument", request.Id.ToString(), AdminAuditOutcome.Rejected, errorCode: result.Error.Code), cancellationToken);
        return result;
    }

    private async Task<Result<AdminCatalogDocumentDto>> AdmitAsync(AdminUpdateCatalogMetadataRequest request, CancellationToken ct)
    {
        var candidate = await dbContext.CatalogDocuments.AsNoTracking().IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == request.Id, ct);
        if (candidate == null || candidate.IsDeleted) return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        if (candidate.CategoryId == request.CategoryId)
            return await categoryGuard.ExecuteForCatalogAsync(request.Id, candidate.CategoryId,
                (doc, category) => UpdateAsync(doc, category, request, ct), ct);

        return await categoryGuard.ExecuteAsync<AdminCatalogDocumentDto>(request.CategoryId, async category =>
        {
            if (dbContext is not DbContext context) return Error.Custom("Category.Unavailable", "Document persistence is unavailable.");
            var doc = context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                ? await dbContext.CatalogDocuments.FromSqlInterpolated($"SELECT * FROM \"CatalogDocuments\" WHERE \"Id\" = {request.Id} FOR UPDATE").IgnoreAutoIncludes().FirstOrDefaultAsync(ct)
                : await dbContext.CatalogDocuments.IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == request.Id, ct);
            if (doc == null) return Error.Custom("Catalog.NotFound", "Catalog document not found.");
            await context.Entry(doc).ReloadAsync(ct);
            if (doc.IsDeleted) return Error.Custom("Catalog.NotFound", "Catalog document not found.");
            if (doc.CategoryId != candidate.CategoryId) return Error.Custom("Category.ConcurrencyConflict", "The catalog category changed. Reload before editing.");
            return await UpdateAsync(doc, category, request, ct);
        }, ct);
    }

    private async Task<Result<AdminCatalogDocumentDto>> UpdateAsync(CatalogDocument doc, DocumentCategory category, AdminUpdateCatalogMetadataRequest request, CancellationToken ct)
    {
        if (await dbContext.IngestionAttempts.AnyAsync(a => a.TargetScope == IngestionTargetScope.Catalog && a.TargetDocumentId == doc.Id &&
            (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running), ct))
            return Error.Custom("Catalog.ActiveAttemptInProgress", "Cannot modify metadata while document ingestion is actively in progress.");
        if (doc.Visibility == CatalogVisibility.Published)
            return Error.Custom("Catalog.PublishedImmutable", "Published documents must be hidden before editing metadata.");
        if (doc.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
            return Error.Custom("Catalog.ConcurrencyConflict", "Document metadata changed. Reload before editing.");
        if (dbContext is DbContext context) await context.Entry(doc).Collection(d => d.Slices).LoadAsync(ct);
        doc.UpdateMetadata(request.Title, request.Description, category.Id, request.Language, request.AuthorOrSourceUrl,
            string.IsNullOrWhiteSpace(request.AttributionText) ? request.AuthorOrSourceUrl ?? request.Title : request.AttributionText);
        doc.Category = category;
        await dbContext.AdminAuditEvents.AddAsync(AdminAuditEvent.Create(request.AdminUserId!.Value,
            AdminAuditActions.CatalogUpdateMetadata, "CatalogDocument", doc.Id.ToString(), AdminAuditOutcome.Succeeded,
            correlationId: doc.Id.ToString(), changedFields: new[] { "Title", "Description", "CategoryId", "Language", "AuthorOrSourceUrl", "AttributionText", "ConcurrencyVersion" },
            statePayload: JsonSerializer.Serialize(new { doc.ConcurrencyVersion })), ct);
        return doc.ToAdminDto();
    }
}
