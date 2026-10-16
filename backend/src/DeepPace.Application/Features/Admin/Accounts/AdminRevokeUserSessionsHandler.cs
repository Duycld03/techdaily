using System.Text.Json;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminRevokeUserSessionsRequest(
    Guid TargetUserId,
    Guid ActorUserId,
    string Reason,
    int ExpectedVersion,
    string? CorrelationId = null,
    int PresentedSessionVersion = 1);

public class AdminRevokeUserSessionsHandler : IUseCase<AdminRevokeUserSessionsRequest, AdminAccountDetailDto>
{
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IRefreshTokenService _refreshTokenService;

    public AdminRevokeUserSessionsHandler(
        IAdministrativeUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokenService)
    {
        _unitOfWork = unitOfWork;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result<AdminAccountDetailDto>> ExecuteAsync(
        AdminRevokeUserSessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Custom("Account.ReasonRequired", "A reason is required to revoke sessions.");
        }

        var trimmedReason = request.Reason.Trim();
        if (trimmedReason.Length > 500)
        {
            return Error.Custom("Account.ReasonTooLong", "Reason cannot exceed 500 characters.");
        }

        return await _unitOfWork.ExecuteAccountControlCommandAsync<AdminAccountDetailDto>(
            request.ActorUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageAccounts },
            request.TargetUserId,
            request.ExpectedVersion,
            AdminAuditActions.UserRevokeSessions,
            request.CorrelationId,
            preTargetValidation: null,
            command: async (actor, target, targetRole) =>
            {
                try
                {
                    target.RevokeAllSessions(actor.Id, trimmedReason);
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
                    Status = target.Status.ToString(),
                    AuthenticationVersion = target.AuthenticationVersion,
                    AdminControlVersion = target.AdminControlVersion
                });

                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: AdminAuditActions.UserRevokeSessions,
                    targetType: "User",
                    targetId: target.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    reason: trimmedReason,
                    correlationId: request.CorrelationId,
                    changedFields: new[] { "AuthenticationVersion", "AdminControlVersion" },
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
                    targetRole.ToDescriptor());

                return (dto, auditEvent);
            },
            cancellationToken);
    }
}
