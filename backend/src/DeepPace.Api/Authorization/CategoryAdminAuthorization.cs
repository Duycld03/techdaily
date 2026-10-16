using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Api.Authorization;

public class CategoryAdminRequirement : IAuthorizationRequirement { }

public class CategoryAdminAuthorizationHandler : BaseAdminAuthorizationHandler<CategoryAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public CategoryAdminAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CategoryAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var userId))
        {
            return;
        }

        var auth = await _resolver.HasPermissionAsync(userId, AdminPermission.ManageCategories);
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
