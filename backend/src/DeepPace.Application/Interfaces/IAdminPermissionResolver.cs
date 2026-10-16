using DeepPace.Application.Common;

namespace DeepPace.Application.Interfaces;

public enum AdminPermission
{
    ViewAdminDashboard,
    ManageCatalog,
    ManageAccounts,
    ReadAudit,
    ManageDocumentOperations,
    ManageRoles,
    ManageCategories
}

public record AdminCapabilitiesDto(
    bool CanManageCatalog = false,
    bool CanManageAccounts = false,
    bool CanReadAudit = false,
    bool CanManageDocumentOperations = false,
    bool CanViewAdminDashboard = false,
    bool CanManageRoles = false,
    bool CanManageCategories = false);

public enum AuthorityOutcome
{
    Authorized,
    Denied,
    Unavailable
}

public sealed record AuthorityResult(AuthorityOutcome Outcome, string? Reason = null)
{
    public bool IsAuthorized => Outcome == AuthorityOutcome.Authorized;
    public bool IsDenied => Outcome == AuthorityOutcome.Denied;
    public bool IsUnavailable => Outcome == AuthorityOutcome.Unavailable;

    public static AuthorityResult Authorized() => new(AuthorityOutcome.Authorized);
    public static AuthorityResult Denied(string? reason = null) => new(AuthorityOutcome.Denied, reason);
    public static AuthorityResult Unavailable(string? reason = null) => new(AuthorityOutcome.Unavailable, reason);
}

public interface IAdminPermissionResolver
{
    Task<Result<AdminCapabilitiesDto>> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default);
    Task<AuthorityResult> HasPermissionAsync(Guid userId, AdminPermission permission, CancellationToken ct = default);
}
