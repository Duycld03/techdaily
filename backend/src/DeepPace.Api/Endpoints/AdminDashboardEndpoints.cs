using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using DeepPace.Api.Http;
using DeepPace.Application.Common;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Features.AdminDashboard.GetOverview;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Endpoints;

public static class AdminDashboardEndpoints
{
    public const string AiProbePolicy = "AdminAiProbePolicy";

    public static IServiceCollection AddAdminDashboardRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected ??= async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await Results.Problem(statusCode: 429, title: "Too Many Requests",
                    detail: "Please wait before checking AI services again.",
                    extensions: new Dictionary<string, object?> { ["code"] = "RATE_LIMITED" })
                    .ExecuteAsync(context.HttpContext);
            };
            options.AddPolicy(AiProbePolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        return services;
    }

    public static WebApplication UseAdminDashboardNoStore(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1/admin/dashboard"))
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.Pragma = "no-cache";
            }
            await next(context);
        });
        return app;
    }

    public static RouteGroupBuilder MapAdminDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/dashboard")
            .WithTags("Administrator Dashboard")
            .RequireAuthorization("ViewAdminDashboard");

        group.MapGet("/overview", async (
            IUseCase<GetAdminDashboardOverviewRequest, AdminDashboardOverviewDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetAdminDashboardOverviewRequest(), ct);
            return result.Match<IResult>(Results.Ok, error => error.ToProblem(503));
        })
        .WithName("GetAdminDashboardOverview")
        .WithSummary("Get administrator inventory overview")
        .WithDescription("Non-deleted accounts, owned personal documents (including adopted copies), and separate catalog publication/processing totals from one consistent read snapshot. Processing counts are persisted document states, not queue depth.")
        .Produces<AdminDashboardOverviewDto>()
        .ProducesProblem(401).ProducesProblem(403).ProducesProblem(503);

        group.MapGet("/status", async (IAdminSystemStatusService service, CancellationToken ct)
            => Results.Ok(await service.ObserveAsync(ct)))
        .WithName("GetAdminDashboardStatus")
        .WithSummary("Observe database connectivity")
        .WithDescription("Bounded database connectivity check with an observation timestamp. Does not contact external AI services.")
        .Produces<AdminDashboardStatusDto>().ProducesProblem(401).ProducesProblem(403);

        group.MapPost("/ai-probe", async (IAiHealthProbe probe, CancellationToken ct) =>
        {
            var result = await probe.ProbeAsync(ct);
            return Results.Json(result, statusCode: result.IsHealthy ? 200 : 503);
        })
        .RequireRateLimiting(AiProbePolicy)
        .WithName("ProbeAdminAiServices")
        .WithSummary("Explicitly check AI services")
        .WithDescription("User-triggered text-generation and embedding probes with a 15-second overall timeout. Limited to one request per administrator per minute. Returns sanitized observations, never provider bodies or credentials.")
        .Produces<AdminAiProbeDto>().Produces<AdminAiProbeDto>(503)
        .ProducesProblem(401).ProducesProblem(403).ProducesProblem(429);
        return group;
    }
}
