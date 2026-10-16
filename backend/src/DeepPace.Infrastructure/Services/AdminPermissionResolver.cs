using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class AdminPermissionResolver : IAdminPermissionResolver
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminPermissionResolver(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminCapabilitiesDto>> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<AdminCapabilitiesDto>.Success(new AdminCapabilitiesDto());
        }

        try
        {
            var user = await _dbContext.Users.AsNoTracking()
                .Where(u => u.Id == userId && !u.IsDeleted && u.Status == AccountStatus.Active)
                .Select(u => new
                {
                    u.RoleId,
                    Role = _dbContext.Roles.AsNoTracking()
                        .Where(r => r.Id == u.RoleId && !r.IsDeleted)
                        .Select(r => new
                        {
                            r.Id,
                            r.Key,
                            Permissions = r.Permissions.Select(p => p.PermissionKey).ToList()
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (user == null || user.Role == null)
            {
                return Result<AdminCapabilitiesDto>.Success(new AdminCapabilitiesDto());
            }

            var recognizedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in user.Role.Permissions)
            {
                if (RolePermission.SupportedPermissionKeys.Contains(key))
                {
                    recognizedKeys.Add(key);
                }
            }

            var canManageCatalog = recognizedKeys.Contains(PermissionKeys.ManageCatalog);
            var canManageAccounts = recognizedKeys.Contains(PermissionKeys.ManageAccounts);
            var canReadAudit = recognizedKeys.Contains(PermissionKeys.ReadAudit);
            var canManageDocOps = recognizedKeys.Contains(PermissionKeys.ManageDocumentOperations);
            var canViewDashboard = recognizedKeys.Contains(PermissionKeys.ViewAdminDashboard);
            var canManageRoles = recognizedKeys.Contains(PermissionKeys.ManageRoles) && canManageAccounts;
            var canManageCategories = recognizedKeys.Contains(PermissionKeys.ManageCategories);

            var dto = new AdminCapabilitiesDto(
                CanManageCatalog: canManageCatalog,
                CanManageAccounts: canManageAccounts,
                CanReadAudit: canReadAudit,
                CanManageDocumentOperations: canManageDocOps,
                CanViewAdminDashboard: canViewDashboard,
                CanManageRoles: canManageRoles,
                CanManageCategories: canManageCategories);

            return Result<AdminCapabilitiesDto>.Success(dto);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result<AdminCapabilitiesDto>.Failure(
                Error.Custom("Authority.Unavailable", "Database authority store is temporarily unavailable."));
        }
    }

    public async Task<AuthorityResult> HasPermissionAsync(Guid userId, AdminPermission permission, CancellationToken ct = default)
    {
        var capResult = await GetCapabilitiesAsync(userId, ct);
        if (capResult.IsFailure)
        {
            return AuthorityResult.Unavailable(capResult.Error.Message);
        }

        var caps = capResult.Value;
        var isAuthorized = permission switch
        {
            AdminPermission.ViewAdminDashboard => caps.CanViewAdminDashboard,
            AdminPermission.ManageCatalog => caps.CanManageCatalog,
            AdminPermission.ManageAccounts => caps.CanManageAccounts,
            AdminPermission.ReadAudit => caps.CanReadAudit,
            AdminPermission.ManageDocumentOperations => caps.CanManageDocumentOperations,
            AdminPermission.ManageRoles => caps.CanManageRoles,
            AdminPermission.ManageCategories => caps.CanManageCategories,
            _ => false
        };

        return isAuthorized
            ? AuthorityResult.Authorized()
            : AuthorityResult.Denied();
    }
}
