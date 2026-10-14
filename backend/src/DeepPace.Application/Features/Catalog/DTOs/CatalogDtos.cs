using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.DTOs;

public record AdminCatalogDocumentDto(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    string Language,
    SourceType SourceType,
    string? AuthorOrSourceUrl,
    string AttributionText,

    CatalogVisibility Visibility,
    ProcessingStatus ProcessingStatus,
    int TotalSlices,
    int EstimatedReadMinutes,
    int Revision,
    int ConcurrencyVersion,
    long ProcessingGeneration,
    string? ProcessingStatusMessage,
    string? ProcessingErrorMessage,
    Guid? CreatedByUserId,
    Guid? ReviewedByUserId,
    int? ReviewedRevision,
    DateTimeOffset? ReviewedAt,
    Guid? PublishedByUserId,
    DateTimeOffset? PublishedAt,
    Guid? HiddenByUserId,
    DateTimeOffset? HiddenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsEligibleForPublication,
    IReadOnlyList<string> UnmetPublicationRequirements);

public record AdminCatalogSliceDto(
    Guid Id,
    Guid CatalogDocumentId,
    int SliceOrder,
    string ChapterTitle,
    string OriginalTextMarkdown,
    string SummaryMarkdown,
    IReadOnlyList<string> KeyTakeaways,
    string Language,
    int EstimatedReadMinutes,
    bool IsAiFormatted);

public record LearnerCatalogDocumentDto(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    string Language,
    SourceType SourceType,
    string? AuthorOrSourceUrl,
    string AttributionText,
    int TotalSlices,
    int EstimatedReadMinutes,
    DateTimeOffset? PublishedAt,
    Guid? ExistingBookId);

public record CatalogTocItemDto(
    int SliceOrder,
    string ChapterTitle,
    int EstimatedReadMinutes);

public record CatalogFirstSlicePreviewDto(
    int SliceOrder,
    string ChapterTitle,
    string OriginalTextMarkdown,
    string SummaryMarkdown,
    IReadOnlyList<string> KeyTakeaways,
    int EstimatedReadMinutes);

public record LearnerCatalogDetailDto(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    string Language,
    SourceType SourceType,
    string? AuthorOrSourceUrl,
    string AttributionText,
    int TotalSlices,
    int EstimatedReadMinutes,
    DateTimeOffset? PublishedAt,
    Guid? ExistingBookId,
    IReadOnlyList<CatalogTocItemDto> TableOfContents,
    CatalogFirstSlicePreviewDto? FirstSlicePreview);

public record CatalogCapabilitiesDto(bool CanManageCatalog);
