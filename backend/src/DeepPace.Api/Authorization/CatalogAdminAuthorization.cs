using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Authorization;

public class CatalogAdminRequirement : IAuthorizationRequirement
{
}

public class CatalogAdminAuthorizationHandler : BaseAdminAuthorizationHandler<CatalogAdminRequirement>
{
    private readonly ICatalogAdminService _adminService;

    public CatalogAdminAuthorizationHandler(ICatalogAdminService adminService, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _adminService = adminService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CatalogAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId))
        {
            return;
        }

        var auth = await _adminService.CheckCatalogAdminAsync(userId);
        if (auth.IsUnavailable)
        {
            SetUnavailable(context, auth.Reason);
            return;
        }

        if (auth.IsAuthorized)
        {
            context.Succeed(requirement);
        }
    }
}
