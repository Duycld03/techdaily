using System.Text.Json;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Categories;

public class UpdateCategoryHandler : IUseCase<UpdateCategoryRequest, CategoryDescriptorDto>
{
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateCategoryRequest> _validator;

    public UpdateCategoryHandler(
        IAdministrativeUnitOfWork unitOfWork,
        IValidator<UpdateCategoryRequest> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public Task<Result<CategoryDescriptorDto>> ExecuteAsync(
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ExecuteCategoryCommandAsync<CategoryDescriptorDto>(
            actorUserId: request.ActorUserId,
            presentedSessionVersion: request.PresentedSessionVersion,
            requiredPermissions: new[] { AdminPermission.ManageCategories },
            targetCategoryId: request.Id,
            expectedVersion: request.ExpectedVersion,
            commandName: AdminAuditActions.CategoryUpdateMetadata,
            correlationId: request.CorrelationId,
            command: async (actor, target) =>
            {
                var validation = await _validator.ValidateAsync(request, cancellationToken);
                if (!validation.IsValid)
                    return Error.Custom("Category.ValidationFailed", validation.Errors.First().ErrorMessage);
                if (target == null)
                    return Error.Custom("Category.NotFound", "The specified category was not found.");

                var nameEn = request.NameEn.Trim();
                var nameVi = request.NameVi.Trim();
                var changedFields = new List<string>();
                if (target.NameEn != nameEn) changedFields.Add("NameEn");
                if (target.NameVi != nameVi) changedFields.Add("NameVi");
                if (target.SortOrder != request.SortOrder) changedFields.Add("SortOrder");
                if (target.IsActive != request.IsActive) changedFields.Add("IsActive");

                if (changedFields.Count == 0)
                    return Result<(CategoryDescriptorDto, AdminAuditEvent?)>.Success((target.ToDescriptor(), null));

                var action = changedFields.Count == 1 && changedFields[0] == "SortOrder"
                    ? AdminAuditActions.CategoryReorder
                    : changedFields.Count == 1 && changedFields[0] == "IsActive"
                        ? (request.IsActive ? AdminAuditActions.CategoryActivate : AdminAuditActions.CategoryDeactivate)
                        : AdminAuditActions.CategoryUpdateMetadata;
                var previousIsActive = target.IsActive;
                target.UpdateMetadata(nameEn, nameVi, request.SortOrder, request.IsActive, request.ExpectedVersion);
                var descriptor = target.ToDescriptor();
                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: action,
                    targetType: "Category",
                    targetId: target.Id.ToString(),
                    outcome: AdminAuditOutcome.Accepted,
                    correlationId: request.CorrelationId,
                    changedFields: changedFields,
                    statePayload: JsonSerializer.Serialize(new
                    {
                        target.Id, PreviousIsActive = previousIsActive, target.IsActive,
                        target.SortOrder, Version = target.ConcurrencyVersion
                    }));
                return Result<(CategoryDescriptorDto, AdminAuditEvent?)>.Success((descriptor, auditEvent));
            },
            ct: cancellationToken);
    }
}
