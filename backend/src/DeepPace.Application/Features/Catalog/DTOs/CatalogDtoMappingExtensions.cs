using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Catalog.DTOs;

public static class CatalogDtoMappingExtensions
{
    public static AdminCatalogDocumentDto ToAdminDto(this CatalogDocument doc)
    {
        var (isEligible, unmet) = doc.CheckPublicationEligibility();

        return new AdminCatalogDocumentDto(
            Id: doc.Id,
            Title: doc.Title,
            Description: doc.Description,
            CategoryId: doc.CategoryId,
            Category: doc.Category!.ToDescriptor(),
            Language: doc.Language,
            SourceType: doc.SourceType,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,

            Visibility: doc.Visibility,
            ProcessingStatus: doc.ProcessingStatus,
            TotalSlices: doc.TotalSlices,
            EstimatedReadMinutes: doc.EstimatedReadMinutes,
            Revision: doc.Revision,
            ConcurrencyVersion: doc.ConcurrencyVersion,
            ProcessingGeneration: doc.ProcessingGeneration,
            ProcessingStatusMessage: doc.ProcessingStatusMessage,
            ProcessingErrorMessage: doc.ProcessingErrorMessage,
            CreatedByUserId: doc.CreatedByUserId,
            ReviewedByUserId: doc.ReviewedByUserId,
            ReviewedRevision: doc.ReviewedRevision,
            ReviewedAt: doc.ReviewedAt,
            PublishedByUserId: doc.PublishedByUserId,
            PublishedAt: doc.PublishedAt,
            HiddenByUserId: doc.HiddenByUserId,
            HiddenAt: doc.HiddenAt,
            CreatedAt: doc.CreatedAt,
            UpdatedAt: doc.UpdatedAt,
            IsEligibleForPublication: isEligible,
            UnmetPublicationRequirements: unmet);
    }

    public static AdminCatalogSliceDto ToAdminSliceDto(this CatalogSlice slice)
    {
        return new AdminCatalogSliceDto(
            Id: slice.Id,
            CatalogDocumentId: slice.CatalogDocumentId,
            SliceOrder: slice.SliceOrder,
            ChapterTitle: slice.ChapterTitle,
            OriginalTextMarkdown: slice.OriginalTextMarkdown,
            SummaryMarkdown: slice.SummaryMarkdown,
            KeyTakeaways: slice.KeyTakeaways,
            Language: slice.Language,
            EstimatedReadMinutes: slice.EstimatedReadMinutes,
            IsAiFormatted: slice.IsAiFormatted);
    }

    public static LearnerCatalogDocumentDto ToLearnerDto(this CatalogDocument doc, Guid? existingBookId)
    {
        return new LearnerCatalogDocumentDto(
            Id: doc.Id,
            Title: doc.Title,
            Description: doc.Description,
            CategoryId: doc.CategoryId,
            Category: doc.Category!.ToDescriptor(),
            Language: doc.Language,
            SourceType: doc.SourceType,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            TotalSlices: doc.TotalSlices,
            EstimatedReadMinutes: doc.EstimatedReadMinutes,
            PublishedAt: doc.PublishedAt,
            ExistingBookId: (existingBookId == null || existingBookId == Guid.Empty) ? null : existingBookId);
    }

    public static LearnerCatalogDetailDto ToLearnerDetailDto(this CatalogDocument doc, Guid? existingBookId)
    {
        var orderedSlices = doc.Slices.OrderBy(s => s.SliceOrder).ToList();

        var toc = orderedSlices.Select(s => new CatalogTocItemDto(
            SliceOrder: s.SliceOrder,
            ChapterTitle: s.ChapterTitle,
            EstimatedReadMinutes: s.EstimatedReadMinutes)).ToList();

        var firstSlice = orderedSlices.FirstOrDefault(s => s.SliceOrder == 1);
        CatalogFirstSlicePreviewDto? preview = null;
        if (firstSlice != null)
        {
            preview = new CatalogFirstSlicePreviewDto(
                SliceOrder: firstSlice.SliceOrder,
                ChapterTitle: firstSlice.ChapterTitle,
                OriginalTextMarkdown: firstSlice.OriginalTextMarkdown,
                SummaryMarkdown: firstSlice.SummaryMarkdown,
                KeyTakeaways: firstSlice.KeyTakeaways,
                EstimatedReadMinutes: firstSlice.EstimatedReadMinutes);
        }

        return new LearnerCatalogDetailDto(
            Id: doc.Id,
            Title: doc.Title,
            Description: doc.Description,
            CategoryId: doc.CategoryId,
            Category: doc.Category!.ToDescriptor(),
            Language: doc.Language,
            SourceType: doc.SourceType,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            TotalSlices: doc.TotalSlices,
            EstimatedReadMinutes: doc.EstimatedReadMinutes,
            PublishedAt: doc.PublishedAt,
            ExistingBookId: existingBookId,
            TableOfContents: toc,
            FirstSlicePreview: preview);
    }
}
