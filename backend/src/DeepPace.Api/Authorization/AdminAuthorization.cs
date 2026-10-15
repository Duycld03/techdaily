using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Authorization;

public class AccountAdminRequirement : IAuthorizationRequirement { }
public class AuditReadRequirement : IAuthorizationRequirement { }
public class DocumentOperationsAdminRequirement : IAuthorizationRequirement { }

public class AccountAdminAuthorizationHandler : AuthorizationHandler<AccountAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public AccountAdminAuthorizationHandler(IAdminPermissionResolver resolver)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId) && await _resolver.HasPermissionAsync(userId, AdminPermission.ManageAccounts))
        {
            context.Succeed(requirement);
        }
    }
}

public class AuditReadAuthorizationHandler : AuthorizationHandler<AuditReadRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public AuditReadAuthorizationHandler(IAdminPermissionResolver resolver)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuditReadRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId) && await _resolver.HasPermissionAsync(userId, AdminPermission.ReadAudit))
        {
            context.Succeed(requirement);
        }
    }
}

public class DocumentOperationsAdminAuthorizationHandler : AuthorizationHandler<DocumentOperationsAdminRequirement>
{
    private readonly IAdminPermissionResolver _resolver;

    public DocumentOperationsAdminAuthorizationHandler(IAdminPermissionResolver resolver)
    {
        _resolver = resolver;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DocumentOperationsAdminRequirement requirement)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId) && await _resolver.HasPermissionAsync(userId, AdminPermission.ManageDocumentOperations))
        {
            context.Succeed(requirement);
        }
    }
}
