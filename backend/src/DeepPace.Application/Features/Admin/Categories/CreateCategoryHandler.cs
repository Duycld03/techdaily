using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Categories;

public class CreateCategoryHandler : IUseCase<CreateCategoryRequest, CategoryDescriptorDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCategoryRequest> _validator;

    public CreateCategoryHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork,
        IValidator<CreateCategoryRequest> validator)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public Task<Result<CategoryDescriptorDto>> ExecuteAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ExecuteCategoryCommandAsync<CategoryDescriptorDto>(
            actorUserId: request.ActorUserId,
            presentedSessionVersion: request.PresentedSessionVersion,
            requiredPermissions: new[] { AdminPermission.ManageCategories },
            targetCategoryId: null,
            expectedVersion: null,
            commandName: AdminAuditActions.CategoryCreate,
            correlationId: request.CorrelationId,
            command: async (actor, _) =>
            {
                var validation = await _validator.ValidateAsync(request, cancellationToken);
                if (!validation.IsValid)
                    return Error.Custom("Category.ValidationFailed", validation.Errors.First().ErrorMessage);

                var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
                if (await _dbContext.DocumentCategories.AnyAsync(
                    category => category.Slug == normalizedSlug && !category.IsDeleted, cancellationToken))
                    return Error.Custom("Category.SlugConflict", "A category with this slug already exists.");

                var category = DocumentCategory.Create(
                    slug: normalizedSlug,
                    nameEn: request.NameEn,
                    nameVi: request.NameVi,
                    sortOrder: request.SortOrder,
                    isActive: request.IsActive);

                await _dbContext.DocumentCategories.AddAsync(category, cancellationToken);

                var descriptor = category.ToDescriptor();

                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: AdminAuditActions.CategoryCreate,
                    targetType: "Category",
                    targetId: category.Id.ToString(),
                    outcome: AdminAuditOutcome.Accepted,
                    correlationId: request.CorrelationId,
                    changedFields: new[] { "Slug", "NameEn", "NameVi", "SortOrder", "IsActive" },
                    statePayload: JsonSerializer.Serialize(new { category.Id, category.IsActive, category.SortOrder, Version = category.ConcurrencyVersion }));

                return Result<(CategoryDescriptorDto, AdminAuditEvent?)>.Success((descriptor, auditEvent));
            },
            ct: cancellationToken);
    }
}
