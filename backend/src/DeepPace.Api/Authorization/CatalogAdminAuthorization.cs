using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Authorization;

public class CatalogAdminRequirement : IAuthorizationRequirement
{
}

public class CatalogAdminAuthorizationHandler : AuthorizationHandler<CatalogAdminRequirement>
{
    private readonly ICatalogAdminService _adminService;

    public CatalogAdminAuthorizationHandler(ICatalogAdminService adminService)
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

        if (await _adminService.IsAdminAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
