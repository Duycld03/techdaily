using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;
public class CatalogAdminService : ICatalogAdminService
{
    private readonly IConfiguration _configuration;
    private readonly IDeepPaceDbContext _dbContext;

    public CatalogAdminService(IConfiguration configuration, IDeepPaceDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var configuredIds = _configuration.GetSection("CatalogAdministration:UserIds").Get<string[]>()
            ?? _configuration.GetSection("CatalogAdministration__UserIds").Get<string[]>()
            ?? Array.Empty<string>();

        if (configuredIds.Length == 0)
        {
            return false;
        }

        var isConfigured = configuredIds.Any(idStr => Guid.TryParse(idStr, out var id) && id == userId);
        if (!isConfigured)
        {
            return false;
        }

        // Verify that the user exists and is active (active-user check, default-deny)
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user != null && !user.IsDeleted && user.Status == AccountStatus.Active;
    }
}
