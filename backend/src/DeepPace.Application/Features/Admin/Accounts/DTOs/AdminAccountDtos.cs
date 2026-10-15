using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts.DTOs;

public record AdminAccountSummaryDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    AccountStatus Status,
    DateTimeOffset CreatedAt);

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
    string? SuspensionReason);

public record AdminPagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record AdminAccountActionRequest(
    string Reason,
    int ExpectedVersion);
