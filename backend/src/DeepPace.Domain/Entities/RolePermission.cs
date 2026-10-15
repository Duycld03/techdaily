namespace DeepPace.Domain.Entities;

public class RolePermission
{
    public static readonly IReadOnlySet<string> SupportedPermissionKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        PermissionKeys.ViewAdminDashboard,
        PermissionKeys.ManageCatalog,
        PermissionKeys.ManageAccounts,
        PermissionKeys.ReadAudit,
        PermissionKeys.ManageDocumentOperations,
        PermissionKeys.ManageRoles
    };

    public Guid RoleId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;

    public Role? Role { get; set; }

    public RolePermission() { }

    public static RolePermission Create(Guid roleId, string permissionKey)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role ID cannot be empty.", nameof(roleId));
        }

        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            throw new ArgumentException("Permission key cannot be empty.", nameof(permissionKey));
        }

        var trimmed = permissionKey.Trim();
        if (!SupportedPermissionKeys.Contains(trimmed))
        {
            throw new ArgumentException($"Unsupported permission key: '{trimmed}'.", nameof(permissionKey));
        }

        return new RolePermission
        {
            RoleId = roleId,
            PermissionKey = trimmed
        };
    }
}
