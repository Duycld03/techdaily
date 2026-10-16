using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Categories;

public class DeleteCategoryHandler : IUseCase<DeleteCategoryRequest, bool>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IValidator<DeleteCategoryRequest> _validator;

    public DeleteCategoryHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork,
        IValidator<DeleteCategoryRequest> validator)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public Task<Result<bool>> ExecuteAsync(
        DeleteCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _unitOfWork.ExecuteCategoryCommandAsync<bool>(
            actorUserId: request.ActorUserId,
            presentedSessionVersion: request.PresentedSessionVersion,
            requiredPermissions: new[] { AdminPermission.ManageCategories },
            targetCategoryId: request.Id,
            expectedVersion: request.ExpectedVersion,
            commandName: AdminAuditActions.CategoryDelete,
            correlationId: request.CorrelationId,
            command: async (actor, target) =>
            {
                var validation = await _validator.ValidateAsync(request, cancellationToken);
                if (!validation.IsValid)
                    return Error.Custom("Category.ValidationFailed", validation.Errors.First().ErrorMessage);

                if (target == null)
                {
                    return Error.Custom("Category.NotFound", "The specified category was not found.");
                }

                // Re-verify zero references inside locked transaction
                var finalBooks = await _dbContext.DocumentBooks.IgnoreQueryFilters()
                    .CountAsync(b => b.CategoryId == request.Id, cancellationToken);
                var finalCatalog = await _dbContext.CatalogDocuments.IgnoreQueryFilters()
                    .CountAsync(c => c.CategoryId == request.Id, cancellationToken);
                var finalQuiz = await _dbContext.QuizQuestions.IgnoreQueryFilters()
                    .CountAsync(q => q.CategoryId == request.Id, cancellationToken);
                var finalInsights = await _dbContext.TechInsights.IgnoreQueryFilters()
                    .CountAsync(t => t.CategoryId == request.Id, cancellationToken);

                if (finalBooks > 0 || finalCatalog > 0 || finalQuiz > 0 || finalInsights > 0)
                {
                    return Error.Custom("Category.InUse", "Cannot delete a category referenced by existing content. Deactivate it instead.");
                }

                target.Delete(request.ExpectedVersion);

                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: AdminAuditActions.CategoryDelete,
                    targetType: "Category",
                    targetId: target.Id.ToString(),
                    outcome: AdminAuditOutcome.Accepted,
                    correlationId: request.CorrelationId,
                    changedFields: new[] { "IsDeleted", "ConcurrencyVersion" },
                    statePayload: JsonSerializer.Serialize(new { target.Id, IsDeleted = true, Version = target.ConcurrencyVersion }));

                return Result<(bool, AdminAuditEvent?)>.Success((true, auditEvent));
            },
            ct: cancellationToken);
    }
}
