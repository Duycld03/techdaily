using System.Text.Json;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminReactivateUserRequest(
    Guid TargetUserId,
    Guid ActorUserId,
    string Reason,
    int ExpectedVersion,
    string? CorrelationId = null,
    int PresentedSessionVersion = 1);

public class AdminReactivateUserHandler : IUseCase<AdminReactivateUserRequest, AdminAccountDetailDto>
{
    private readonly IAdministrativeUnitOfWork _unitOfWork;

    public AdminReactivateUserHandler(IAdministrativeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminAccountDetailDto>> ExecuteAsync(
        AdminReactivateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Error.Custom("Account.ReasonRequired", "A reason is required for reactivation.");
        }

        var trimmedReason = request.Reason.Trim();
        if (trimmedReason.Length > 500)
        {
            return Error.Custom("Account.ReasonTooLong", "Reactivation reason cannot exceed 500 characters.");
        }

        return await _unitOfWork.ExecuteAccountControlCommandAsync(
            request.ActorUserId,
            request.PresentedSessionVersion,
            new[] { AdminPermission.ManageAccounts },
            request.TargetUserId,
            request.ExpectedVersion,
            AdminAuditActions.UserReactivate,
            request.CorrelationId,
            (actor, target) =>
            {
                try
                {
                    target.Reactivate(actor.Id, trimmedReason);
                }
                catch (InvalidOperationException ex)
                {
                    return Task.FromResult(Result< (AdminAccountDetailDto Result, AdminAuditEvent AuditEvent) >.Failure(
                        Error.Custom("Account.InvalidTransition", ex.Message)));
                }
                catch (ArgumentException ex)
                {
                    return Task.FromResult(Result< (AdminAccountDetailDto Result, AdminAuditEvent AuditEvent) >.Failure(
                        Error.Custom("Account.InvalidReason", ex.Message)));
                }

                var statePayload = JsonSerializer.Serialize(new
                {
                    Status = target.Status.ToString(),
                    AuthenticationVersion = target.AuthenticationVersion,
                    AdminControlVersion = target.AdminControlVersion
                });

                var auditEvent = AdminAuditEvent.Create(
                    actorUserId: actor.Id,
                    action: AdminAuditActions.UserReactivate,
                    targetType: "User",
                    targetId: target.Id.ToString(),
                    outcome: AdminAuditOutcome.Succeeded,
                    reason: trimmedReason,
                    correlationId: request.CorrelationId,
                    changedFields: new[] { "Status", "AdminControlVersion", "SuspendedByUserId", "SuspendedAt", "SuspensionReason" },
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
                    target.Role.ToDescriptor());

                return Task.FromResult(Result<(AdminAccountDetailDto Result, AdminAuditEvent AuditEvent)>.Success((dto, auditEvent)));
            },
            cancellationToken);
    }
}
