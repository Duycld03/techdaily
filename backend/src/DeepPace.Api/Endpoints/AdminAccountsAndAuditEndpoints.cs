using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using DeepPace.Api.Http;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Audit;
using DeepPace.Application.Features.Admin.Audit.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Api.Endpoints;

public static class AdminAccountsAndAuditEndpoints
{
    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }

    public static WebApplication MapAdminAccountsAndAuditEndpoints(this WebApplication app)
    {
        // 1. GET /api/v1/admin/capabilities
        app.MapGet("/api/v1/admin/capabilities", async (
            ClaimsPrincipal principal,
            [FromServices] IAdminPermissionResolver resolver,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var capabilities = await resolver.GetCapabilitiesAsync(userId.Value, ct);
            return Results.Ok(capabilities);
        })
        .WithName("GetAdminCapabilities")
        .WithSummary("Get Administrator Capabilities")
        .WithDescription("Returns explicit flags for catalog management, account management, and audit reading.")
        .Produces<AdminCapabilitiesDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();

        // 2. Accounts Directory and Access-Control Actions
        var usersGroup = app.MapGroup("/api/v1/admin/users")
            .WithTags("Administrator Accounts")
            .RequireAuthorization("AccountAdmin");

        usersGroup.MapGet("/", async (
            [FromServices] IUseCase<AdminGetAccountsRequest, AdminPagedResult<AdminAccountSummaryDto>> handler,
            [FromQuery] string? search,
            [FromQuery] AccountStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var request = new AdminGetAccountsRequest(search, status, page, pageSize);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminAccounts")
        .WithSummary("List and search user accounts")
        .WithDescription("Returns bounded paginated list of accounts with status filter and identity search.")
        .Produces<AdminPagedResult<AdminAccountSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        usersGroup.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<AdminGetAccountByIdRequest, AdminAccountDetailDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new AdminGetAccountByIdRequest(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminAccountById")
        .WithSummary("Get account details")
        .WithDescription("Returns identity, access status, and administrative metadata without exposing credentials or learning content.")
        .Produces<AdminAccountDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        usersGroup.MapPost("/{id:guid}/suspend", async (
            Guid id,
            [FromBody] AdminAccountActionRequest body,
            ClaimsPrincipal principal,
            HttpContext context,
            [FromServices] IUseCase<AdminSuspendUserRequest, AdminAccountDetailDto> handler,
            CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (!actorId.HasValue)
            {
                return Results.Unauthorized();
            }

            var correlationId = context.TraceIdentifier;
            var request = new AdminSuspendUserRequest(id, actorId.Value, body.Reason, body.ExpectedVersion, correlationId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("SuspendAdminAccount")
        .WithSummary("Suspend user account")
        .WithDescription("Transitions account to Suspended, revokes all sessions, and records audit event.")
        .Produces<AdminAccountDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        usersGroup.MapPost("/{id:guid}/reactivate", async (
            Guid id,
            [FromBody] AdminAccountActionRequest body,
            ClaimsPrincipal principal,
            HttpContext context,
            [FromServices] IUseCase<AdminReactivateUserRequest, AdminAccountDetailDto> handler,
            CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (!actorId.HasValue)
            {
                return Results.Unauthorized();
            }

            var correlationId = context.TraceIdentifier;
            var request = new AdminReactivateUserRequest(id, actorId.Value, body.Reason, body.ExpectedVersion, correlationId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("ReactivateAdminAccount")
        .WithSummary("Reactivate suspended account")
        .WithDescription("Restores account to Active state, allowing fresh sign-in.")
        .Produces<AdminAccountDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        usersGroup.MapPost("/{id:guid}/revoke-sessions", async (
            Guid id,
            [FromBody] AdminAccountActionRequest body,
            ClaimsPrincipal principal,
            HttpContext context,
            [FromServices] IUseCase<AdminRevokeUserSessionsRequest, AdminAccountDetailDto> handler,
            CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (!actorId.HasValue)
            {
                return Results.Unauthorized();
            }

            var correlationId = context.TraceIdentifier;
            var request = new AdminRevokeUserSessionsRequest(id, actorId.Value, body.Reason, body.ExpectedVersion, correlationId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("RevokeAdminAccountSessions")
        .WithSummary("Force sign-out across all devices")
        .WithDescription("Revokes all refresh tokens and advances authentication version, invalidating all existing sessions.")
        .Produces<AdminAccountDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // 3. Audit History Read-Only Endpoints
        var auditGroup = app.MapGroup("/api/v1/admin/audit")
            .WithTags("Administrator Audit")
            .RequireAuthorization("AuditRead");

        auditGroup.MapGet("/", async (
            [FromServices] IUseCase<AdminGetAuditEventsRequest, AdminPagedResult<AdminAuditEventDto>> handler,
            [FromQuery] Guid? actorUserId,
            [FromQuery] string? action,
            [FromQuery] string? targetType,
            [FromQuery] string? targetId,
            [FromQuery] AdminAuditOutcome? outcome,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var request = new AdminGetAuditEventsRequest(actorUserId, action, targetType, targetId, outcome, from, to, page, pageSize);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminAuditEvents")
        .WithSummary("List administrative audit events")
        .WithDescription("Returns paginated history of administrative actions with conjunctive filters and ISO UTC time boundaries.")
        .Produces<AdminPagedResult<AdminAuditEventDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        auditGroup.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<AdminGetAuditEventByIdRequest, AdminAuditEventDetailDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new AdminGetAuditEventByIdRequest(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminAuditEventById")
        .WithSummary("Get audit event details")
        .WithDescription("Returns approved event details including changed field names and state payload.")
        .Produces<AdminAuditEventDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
