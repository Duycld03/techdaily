using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class AuthorizationReadinessService : IAuthorizationReadinessService
{
    private readonly DeepPaceDbContext _dbContext;

    public AuthorizationReadinessService(DeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthorizationReadinessStatus> GetReadinessAsync(CancellationToken ct = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(ct);
            if (!canConnect)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: false,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: "Could not establish database connection.");
            }

            // Verify required role schema and seeds exist
            List<Role> roles;
            try
            {
                roles = await _dbContext.Roles.AsNoTracking()
                    .Where(r => r.Id == RoleDefaults.LearnerRoleId || r.Id == RoleDefaults.AdminRoleId)
                    .ToListAsync(ct);
            }
            catch (Exception ex)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: true,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: $"Required authorization schema is missing: {ex.Message}");
            }

            var learnerRole = roles.FirstOrDefault(r => r.Id == RoleDefaults.LearnerRoleId);
            var adminRole = roles.FirstOrDefault(r => r.Id == RoleDefaults.AdminRoleId);

            if (learnerRole == null || !learnerRole.IsProtected || adminRole == null || !adminRole.IsProtected)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: true,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: "Required protected seed roles (Learner/Admin) are missing or unprotected.");
            }

            // Verify Admin has all 6 required permissions
            int adminPermCount;
            try
            {
                adminPermCount = await _dbContext.RolePermissions.AsNoTracking()
                    .Where(p => p.RoleId == RoleDefaults.AdminRoleId)
                    .CountAsync(ct);
            }
            catch (Exception ex)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: true,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: $"Required role permissions schema is missing: {ex.Message}");
            }

            if (adminPermCount < 6)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: true,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: $"Admin role requires at least 6 protected permissions; found {adminPermCount}.");
            }

            // Validate the schema independently of administrator-managed category rows.
            try
            {
                _ = await _dbContext.DocumentCategories.IgnoreQueryFilters().AsNoTracking()
                    .FirstOrDefaultAsync(ct);
            }
            catch (Exception ex)
            {
                return new AuthorizationReadinessStatus(
                    IsReady: false,
                    IsDatabaseConnected: true,
                    HasRequiredSchemaAndRoles: false,
                    IsInitialized: false,
                    FailureReason: $"Required DocumentCategories table is missing or invalid: {ex.Message}");
            }

            // Check initialization receipt
            bool isInitialized;
            try
            {
                isInitialized = await _dbContext.AuthorizationInitializations.AsNoTracking()
                    .AnyAsync(i => i.Scope == AuthorizationInitialization.DefaultScope, ct);
            }
            catch
            {
                isInitialized = false;
            }

            return new AuthorizationReadinessStatus(
                IsReady: true,
                IsDatabaseConnected: true,
                HasRequiredSchemaAndRoles: true,
                IsInitialized: isInitialized,
                FailureReason: null);
        }
        catch (Exception ex)
        {
            return new AuthorizationReadinessStatus(
                IsReady: false,
                IsDatabaseConnected: false,
                HasRequiredSchemaAndRoles: false,
                IsInitialized: false,
                FailureReason: ex.Message);
        }
    }
}
