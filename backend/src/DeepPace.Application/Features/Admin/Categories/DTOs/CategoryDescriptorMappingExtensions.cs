using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Admin.Categories.DTOs;

public static class CategoryDescriptorMappingExtensions
{
    public static CategoryDescriptorDto ToDescriptor(this DocumentCategory category) => new(
        category.Id,
        category.Slug,
        category.NameEn,
        category.NameVi,
        category.SortOrder,
        category.IsActive,
        category.ConcurrencyVersion);
}
