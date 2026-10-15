using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class AdminPermissionResolver : IAdminPermissionResolver
{
    private readonly IConfiguration _configuration;
    private readonly IDeepPaceDbContext _dbContext;

    public AdminPermissionResolver(IConfiguration configuration, IDeepPaceDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    private HashSet<Guid> GetConfiguredAdminIds()
    {
        var configuredIds = _configuration.GetSection("CatalogAdministration:UserIds").Get<string[]>()
            ?? _configuration.GetSection("CatalogAdministration__UserIds").Get<string[]>()
            ?? Array.Empty<string>();

        var set = new HashSet<Guid>();
        foreach (var idStr in configuredIds)
        {
            if (Guid.TryParse(idStr, out var id) && id != Guid.Empty)
            {
                set.Add(id);
            }
        }
        return set;
    }

    public async Task<AdminCapabilitiesDto> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return new AdminCapabilitiesDto(false, false, false, false);
        }

        var adminIds = GetConfiguredAdminIds();
        if (!adminIds.Contains(userId))
        {
            return new AdminCapabilitiesDto(false, false, false, false);
        }

        // Active-user and non-deleted check against authoritative database state
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null || user.IsDeleted || user.Status != AccountStatus.Active)
        {
            return new AdminCapabilitiesDto(false, false, false, false);
        }

        return new AdminCapabilitiesDto(
            CanManageCatalog: true,
            CanManageAccounts: true,
            CanReadAudit: true,
            CanManageDocumentOperations: true);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, AdminPermission permission, CancellationToken ct = default)
    {
        var capabilities = await GetCapabilitiesAsync(userId, ct);
        return permission switch
        {
            AdminPermission.ManageCatalog => capabilities.CanManageCatalog,
            AdminPermission.ManageAccounts => capabilities.CanManageAccounts,
            AdminPermission.ReadAudit => capabilities.CanReadAudit,
            AdminPermission.ManageDocumentOperations => capabilities.CanManageDocumentOperations,
            _ => false
        };
    }
}
