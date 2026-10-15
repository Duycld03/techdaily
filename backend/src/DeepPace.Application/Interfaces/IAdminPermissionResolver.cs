namespace DeepPace.Application.Interfaces;

public enum AdminPermission
{
    ManageCatalog,
    ManageAccounts,
    ReadAudit,
    ManageDocumentOperations
}

public record AdminCapabilitiesDto(
    bool CanManageCatalog,
    bool CanManageAccounts,
    bool CanReadAudit,
    bool CanManageDocumentOperations = true);

public interface IAdminPermissionResolver
{
    Task<AdminCapabilitiesDto> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid userId, AdminPermission permission, CancellationToken ct = default);
}
