namespace DeepPace.Application.Features.Admin.Categories.DTOs;

public record CategoryDescriptorDto(
    Guid Id,
    string Slug,
    string NameEn,
    string NameVi,
    int SortOrder,
    bool IsActive,
    int Version);

public record AdminCategoryDetailDto(
    CategoryDescriptorDto Category,
    int BooksCount,
    int CatalogDocumentsCount,
    int QuizQuestionsCount,
    int TechInsightsCount,
    int TotalUsageCount,
    bool CanDelete)
{
    public Guid Id => Category.Id;
    public string Slug => Category.Slug;
    public string NameEn => Category.NameEn;
    public string NameVi => Category.NameVi;
    public int SortOrder => Category.SortOrder;
    public bool IsActive => Category.IsActive;
    public int Version => Category.Version;
    public int UsageCount => TotalUsageCount;
}

public record CreateCategoryRequest(
    string Slug,
    string NameEn,
    string NameVi,
    int SortOrder = 0,
    bool IsActive = true,
    Guid ActorUserId = default,
    int PresentedSessionVersion = 1,
    string? CorrelationId = null);

public record UpdateCategoryRequest(
    Guid Id,
    string NameEn,
    string NameVi,
    int SortOrder,
    bool IsActive,
    int ExpectedVersion,
    Guid ActorUserId = default,
    int PresentedSessionVersion = 1,
    string? CorrelationId = null);

public record DeleteCategoryRequest(
    Guid Id,
    int ExpectedVersion,
    Guid ActorUserId = default,
    int PresentedSessionVersion = 1,
    string? CorrelationId = null);

public record GetAdminCategoriesRequest(
    string? Search = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);

public record PagedCategoriesDto(
    IReadOnlyList<CategoryDescriptorDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record GetCategoriesRequest();
