using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Authorization;

public class AccountAdminRequirement : IAuthorizationRequirement { }
public class AuditReadRequirement : IAuthorizationRequirement { }
public class DocumentOperationsAdminRequirement : IAuthorizationRequirement { }
public class ViewAdminDashboardRequirement : IAuthorizationRequirement { }
public class RoleAdminRequirement : IAuthorizationRequirement { }

public abstract class BaseAdminAuthorizationHandler<TRequirement> : AuthorizationHandler<TRequirement>
    where TRequirement : IAuthorizationRequirement
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    protected BaseAdminAuthorizationHandler(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected void SetUnavailable(AuthorizationHandlerContext context, string? reason)
    {
        var httpContext = _httpContextAccessor?.HttpContext ?? (context.Resource as HttpContext);
        if (httpContext != null)
        {
            httpContext.Items["Authorization_Unavailable"] = reason ?? "Privileged authorization is currently unavailable.";
        }
    }
}

public class AccountAdminAuthorizationHandler : BaseAdminAuthorizationHandler<AccountAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public AccountAdminAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId))
        {
            var auth = await _resolver.HasPermissionAsync(userId, AdminPermission.ManageAccounts);
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
}

public class AuditReadAuthorizationHandler : BaseAdminAuthorizationHandler<AuditReadRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public AuditReadAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuditReadRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId))
        {
            var auth = await _resolver.HasPermissionAsync(userId, AdminPermission.ReadAudit);
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
}

public class DocumentOperationsAdminAuthorizationHandler : BaseAdminAuthorizationHandler<DocumentOperationsAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public DocumentOperationsAdminAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DocumentOperationsAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId))
        {
            var auth = await _resolver.HasPermissionAsync(userId, AdminPermission.ManageDocumentOperations);
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
}

public class ViewAdminDashboardAuthorizationHandler : BaseAdminAuthorizationHandler<ViewAdminDashboardRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public ViewAdminDashboardAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ViewAdminDashboardRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId))
        {
            var auth = await _resolver.HasPermissionAsync(userId, AdminPermission.ViewAdminDashboard);
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
}

public class RoleAdminAuthorizationHandler : BaseAdminAuthorizationHandler<RoleAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public RoleAdminAuthorizationHandler(IAdminPermissionResolver resolver, IHttpContextAccessor? httpContextAccessor = null)
        : base(httpContextAccessor)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId))
        {
            var authAccounts = await _resolver.HasPermissionAsync(userId, AdminPermission.ManageAccounts);
            var authRoles = await _resolver.HasPermissionAsync(userId, AdminPermission.ManageRoles);

            if (authAccounts.IsUnavailable || authRoles.IsUnavailable)
            {
                SetUnavailable(context, authAccounts.Reason ?? authRoles.Reason);
                return;
            }

            // Conjunction: requires BOTH ManageAccounts AND ManageRoles
            if (authAccounts.IsAuthorized && authRoles.IsAuthorized)
            {
                context.Succeed(requirement);
            }
        }
    }
}
