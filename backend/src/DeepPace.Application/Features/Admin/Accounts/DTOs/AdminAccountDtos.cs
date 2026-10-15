using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts.DTOs;

public record RoleDescriptorDto(
    Guid Id,
    string Key,
    string NameEn,
    string NameVi);

public record AdminRoleOptionDto(
    Guid Id,
    string Key,
    string NameEn,
    string NameVi,
    IReadOnlyList<string> PermissionKeys);

public record AdminAccountSummaryDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    AccountStatus Status,
    DateTimeOffset CreatedAt,
    RoleDescriptorDto Role);

public record AdminAccountDetailDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string PreferredLocale,
    DateTimeOffset CreatedAt,
    AccountStatus Status,
    int AdminControlVersion,
    bool HasPassword,
    bool HasGoogleAuth,
    Guid? SuspendedByUserId,
    DateTimeOffset? SuspendedAt,
    string? SuspensionReason,
    RoleDescriptorDto Role);

public record AdminPagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record AdminAccountActionRequest(
    string Reason,
    int ExpectedVersion);

public record AdminAssignRoleActionRequest(
    Guid RoleId,
    string Reason,
    int ExpectedVersion);

public static class RoleMappingExtensions
{
    public static readonly RoleDescriptorDto DefaultLearnerDescriptor =
        new(RoleDefaults.LearnerRoleId, RoleDefaults.LearnerKey, "Learner", "Người học");

    public static RoleDescriptorDto ToDescriptor(this Role? role)
    {
        if (role == null) return DefaultLearnerDescriptor;
        return new RoleDescriptorDto(role.Id, role.Key, role.NameEn, role.NameVi);
    }
}
