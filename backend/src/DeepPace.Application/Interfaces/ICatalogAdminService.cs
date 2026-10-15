namespace DeepPace.Application.Interfaces;

public interface ICatalogAdminService
{
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthorityResult> CheckCatalogAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
