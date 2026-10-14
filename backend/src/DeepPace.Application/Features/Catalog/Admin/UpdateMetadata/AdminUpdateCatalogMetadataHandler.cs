using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.UpdateMetadata;

public record AdminUpdateCatalogMetadataRequest(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    int ExpectedConcurrencyVersion);

public class AdminUpdateCatalogMetadataValidator : AbstractValidator<AdminUpdateCatalogMetadataRequest>
{
    public AdminUpdateCatalogMetadataValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
        RuleFor(x => x.AttributionText).MaximumLength(2000);
    }
}

public class AdminUpdateCatalogMetadataHandler : IUseCase<AdminUpdateCatalogMetadataRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<AdminUpdateCatalogMetadataRequest> _validator;

    public AdminUpdateCatalogMetadataHandler(
        IDeepPaceDbContext dbContext,
        IValidator<AdminUpdateCatalogMetadataRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminUpdateCatalogMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doc == null || doc.IsDeleted)
        {
            return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        }

        if (doc.Visibility == CatalogVisibility.Published)
        {
            return Error.Custom("Catalog.PublishedImmutable",
                "Published documents are immutable. The document must be hidden before editing metadata.");
        }

        if (doc.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            return Error.Custom("Catalog.ConcurrencyConflict",
                "Document metadata has been modified by another administrator. Please reload.");
        }

        try
        {
            doc.UpdateMetadata(
                title: request.Title,
                description: request.Description,
                category: request.Category,
                language: request.Language,
                authorOrSourceUrl: request.AuthorOrSourceUrl,
                attributionText: string.IsNullOrWhiteSpace(request.AttributionText) ? (request.AuthorOrSourceUrl ?? request.Title) : request.AttributionText);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return doc.ToAdminDto();
        }
        catch (InvalidOperationException ex)
        {
            return Error.Custom("Catalog.InvalidOperation", ex.Message);
        }
    }
}
