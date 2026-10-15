using DeepPace.Application.Interfaces;

namespace DeepPace.Infrastructure.Services;

public class CatalogAdminService : ICatalogAdminService
{
    private readonly IAdminPermissionResolver _permissionResolver;

    public CatalogAdminService(IAdminPermissionResolver permissionResolver)
    {
        _permissionResolver = permissionResolver;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var auth = await CheckCatalogAdminAsync(userId, cancellationToken);
        return auth.IsAuthorized;
    }

    public async Task<AuthorityResult> CheckCatalogAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return AuthorityResult.Denied();
        }

        return await _permissionResolver.HasPermissionAsync(userId, AdminPermission.ManageCatalog, cancellationToken);
    }
}
