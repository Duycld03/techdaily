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
    private readonly IAdminPermissionResolver _permissionResolver;

    public AdminAssignRoleHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokenService,
        IAdminPermissionResolver permissionResolver)
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
        // 1. Conjunction check: Actor must have ManageAccounts AND ManageRoles
        var capabilitiesResult = await _permissionResolver.GetCapabilitiesAsync(request.ActorUserId, cancellationToken);
        if (capabilitiesResult.IsFailure)
        {
            return capabilitiesResult.Error;
        }

        var capabilities = capabilitiesResult.Value;
        if (!capabilities.CanManageRoles || !capabilities.CanManageAccounts)
        {
            return Error.Custom("FORBIDDEN", "Actor lacks required role administration authority.");
        }

        // 2. Input validation
        if (request.ExpectedVersion <= 0)
        {
            return Error.Custom("Validation.ExpectedVersionInvalid", "Expected version must be positive.");
        }

        if (request.RoleId == Guid.Empty)
        {
            return Error.Custom("Role.InvalidId", "Role ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Custom("Account.ReasonRequired", "A reason is required for role assignment.");
        }

        var trimmedReason = request.Reason.Trim();
        if (trimmedReason.Length > 500)
        {
            return Error.Custom("Account.ReasonTooLong", "Role assignment reason cannot exceed 500 characters.");
        }

        // 3. Self-role change protection
        if (request.ActorUserId == request.TargetUserId)
        {
            return Error.Custom("ACCOUNT_SELF_ROLE_CHANGE_BLOCKED", "An administrator cannot change their own role.");
        }

        // 4. Target account existence
        var targetUser = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken);

        if (targetUser == null)
        {
            return Error.Custom("Account.NotFound", "Target account was not found.");
        }

        // 5. Concurrency check
        if (targetUser.AdminControlVersion != request.ExpectedVersion)
        {
            return Error.Custom("ACCOUNT_CONCURRENCY_CONFLICT", "Account was modified by another administrator. Please reload.");
        }

        // 6. Role existence
        var newRole = await _dbContext.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && !r.IsDeleted, cancellationToken);

        if (newRole == null)
        {
            return Error.Custom("Role.NotFound", "The specified role was not found.");
        }

        // 7. Unchanged role check
        if (targetUser.RoleId == request.RoleId)
        {
            return Error.Custom("ACCOUNT_ROLE_UNCHANGED", "Target user already has the specified role.");
        }

        // 8. Last active administrator protection
        if (targetUser.RoleId == RoleDefaults.AdminRoleId && request.RoleId != RoleDefaults.AdminRoleId)
        {
            var activeAdminCount = await _dbContext.Users
                .CountAsync(u => u.RoleId == RoleDefaults.AdminRoleId && !u.IsDeleted && u.Status == AccountStatus.Active, cancellationToken);

            if (activeAdminCount <= 1)
            {
                return Error.Custom("ACCOUNT_LAST_ADMIN_PROTECTED", "Cannot reassign role: at least one active administrator must remain.");
            }
        }

        // 9. Execute transaction via unit of work
        return await _unitOfWork.ExecuteAccountControlCommandAsync<AdminAccountDetailDto>(
            request.ActorUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageAccounts, AdminPermission.ManageRoles },
            request.TargetUserId,
            request.ExpectedVersion,
            AdminAuditActions.UserAssignRole,
            request.CorrelationId,
            async (actor, target) =>
            {
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
