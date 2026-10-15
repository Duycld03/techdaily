using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminGetRolesRequest(
    Guid ActorUserId,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public class AdminGetRolesHandler : IUseCase<AdminGetRolesRequest, AdminPagedResult<AdminRoleOptionDto>>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdminPermissionResolver _permissionResolver;

    public AdminGetRolesHandler(
        IDeepPaceDbContext dbContext,
        IAdminPermissionResolver permissionResolver)
    {
        _dbContext = dbContext;
        _permissionResolver = permissionResolver;
    }

    public async Task<Result<AdminPagedResult<AdminRoleOptionDto>>> ExecuteAsync(
        AdminGetRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var capabilitiesResult = await _permissionResolver.GetCapabilitiesAsync(request.ActorUserId, cancellationToken);
        if (capabilitiesResult.IsFailure)
        {
            return capabilitiesResult.Error;
        }

        var capabilities = capabilitiesResult.Value;
        if (!capabilities.CanManageRoles || !capabilities.CanManageAccounts)
        {
            return Error.Custom("FORBIDDEN", "Actor lacks required role administration authority.");
        }

        if (!string.IsNullOrWhiteSpace(request.Search) && request.Search.Trim().Length > 200)
        {
            return Error.Custom("Command.SearchTooLong", "Search query cannot exceed 200 characters.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _dbContext.Roles.AsNoTracking().Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(r => r.Key.ToLower().Contains(search)
                || r.NameEn.ToLower().Contains(search)
                || r.NameVi.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .OrderBy(r => r.Key)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Key,
                r.NameEn,
                r.NameVi,
                Permissions = r.Permissions.Select(p => p.PermissionKey).ToList()
            })
            .ToListAsync(cancellationToken);

        var items = roles.Select(r => new AdminRoleOptionDto(
            r.Id,
            r.Key,
            r.NameEn,
            r.NameVi,
            r.Permissions
                .Where(p => RolePermission.SupportedPermissionKeys.Contains(p))
                .ToList()))
            .ToList();

        return new AdminPagedResult<AdminRoleOptionDto>(items, totalCount, page, pageSize);
    }
}
