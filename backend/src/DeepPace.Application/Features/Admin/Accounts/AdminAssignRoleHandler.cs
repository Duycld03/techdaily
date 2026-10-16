using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminAssignRoleRequest(
    Guid TargetUserId,
    Guid ActorUserId,
    Guid RoleId,
    string Reason,
    int ExpectedVersion,
    string? CorrelationId = null,
    int PresentedSessionVersion = 1);

public class AdminAssignRoleHandler : IUseCase<AdminAssignRoleRequest, AdminAccountDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAdminPermissionResolver? _permissionResolver;

    public AdminAssignRoleHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokenService,
        IAdminPermissionResolver? permissionResolver = null)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _refreshTokenService = refreshTokenService;
        _permissionResolver = permissionResolver;
    }

    public async Task<Result<AdminAccountDetailDto>> ExecuteAsync(
        AdminAssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedReason = request.Reason?.Trim() ?? string.Empty;

        return await _unitOfWork.ExecuteAccountControlCommandAsync<AdminAccountDetailDto>(
            request.ActorUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageAccounts, AdminPermission.ManageRoles },
            request.TargetUserId,
            request.ExpectedVersion,
            AdminAuditActions.UserAssignRole,
            request.CorrelationId,
            preTargetValidation: actor =>
            {
                if (request.ExpectedVersion <= 0)
                {
                    return Task.FromResult(Result.Failure(
                        Error.Custom("Validation.ExpectedVersionInvalid", "Expected version must be positive.")));
                }

                if (request.RoleId == Guid.Empty)
                {
                    return Task.FromResult(Result.Failure(
                        Error.Custom("Role.InvalidId", "Role ID cannot be empty.")));
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return Task.FromResult(Result.Failure(
                        Error.Custom("Account.ReasonRequired", "A reason is required for role assignment.")));
                }

                if (trimmedReason.Length > 500)
                {
                    return Task.FromResult(Result.Failure(
                        Error.Custom("Account.ReasonTooLong", "Role assignment reason cannot exceed 500 characters.")));
                }

                if (AdminAuditEvent.ContainsSensitivePatterns(trimmedReason))
                {
                    return Task.FromResult(Result.Failure(
                        Error.Custom("Account.InvalidReason", "Reason contains sensitive credential or token patterns.")));
                }

                return Task.FromResult(Result.Success());
            },
            command: async (actor, target, targetRole) =>
            {
                // Destination role existence
                var newRole = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

                if (newRole == null)
                {
                    return Error.Custom("Role.NotFound", "The specified role was not found.");
                }

                // Unchanged role check evaluated before minimum admin check
                if (target.RoleId == request.RoleId)
                {
                    return Error.Custom("ACCOUNT_ROLE_UNCHANGED", "Target user already has the specified role.");
                }

                // Last active administrator protection: applies ONLY to actual removal of an Active protected Admin
                if (target.RoleId == RoleDefaults.AdminRoleId && target.Status == AccountStatus.Active && request.RoleId != RoleDefaults.AdminRoleId)
                {
                    var activeAdminCount = await _dbContext.Users
                        .CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && !u.IsDeleted && u.Status == AccountStatus.Active, cancellationToken);

                    if (activeAdminCount <= 1)
                    {
                        return Error.Custom("ACCOUNT_LAST_ADMIN_PROTECTED", "Cannot reassign role: at least one active administrator must remain.");
                    }
                }

                var prevRoleId = target.RoleId;
                try
                {
                    target.AssignRole(actor.Id, request.RoleId, trimmedReason);
                }
                catch (InvalidOperationException ex)
                {
                    return Error.Custom("Account.InvalidTransition", ex.Message);
                }
                catch (ArgumentException ex)
                {
                    return Error.Custom("Account.InvalidReason", ex.Message);
                }

                await _refreshTokenService.RevokeAllForUserAsync(target.Id, cancellationToken);

                var statePayload = JsonSerializer.Serialize(new
                {
                    PreviousRoleId = prevRoleId,
                    NewRoleId = request.RoleId,
                    ResultingAuthenticationVersion = target.AuthenticationVersion,
                    ResultingAdminControlVersion = target.AdminControlVersion
                });

                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: AdminAuditActions.UserAssignRole,
                    targetType: "User",
                    targetId: target.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    reason: trimmedReason,
                    correlationId: request.CorrelationId,
                    changedFields: new[] { "RoleId", "AuthenticationVersion", "AdminControlVersion" },
                    statePayload: statePayload);

                var dto = new AdminAccountDetailDto(
                    target.Id,
                    target.Email,
                    target.Name,
                    target.AvatarUrl,
                    target.PreferredLocale,
                    target.CreatedAt,
                    target.Status,
                    target.AdminControlVersion,
                    !string.IsNullOrWhiteSpace(target.PasswordHash),
                    !string.IsNullOrWhiteSpace(target.GoogleSubjectId),
                    target.SuspendedByUserId,
                    target.SuspendedAt,
                    target.SuspensionReason,
                    newRole.ToDescriptor());

                return (dto, auditEvent);
            },
            cancellationToken);
    }
}
