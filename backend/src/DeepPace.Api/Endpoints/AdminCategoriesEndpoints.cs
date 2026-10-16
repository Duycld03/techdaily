using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using DeepPace.Api.Http;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Api.Endpoints;

public record CreateCategoryInput(
    string Slug,
    string NameEn,
    string NameVi,
    int SortOrder = 0,
    bool IsActive = true);

public record UpdateCategoryInput(
    string NameEn,
    string NameVi,
    int SortOrder,
    bool IsActive,
    int ExpectedVersion);

public static class AdminCategoriesEndpoints
{
    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }

    private static int GetSessionVersion(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("session_version")?.Value;
        return int.TryParse(claim, out var v) && v > 0 ? v : 1;
    }

    public static WebApplication MapAdminCategoriesEndpoints(this WebApplication app)
    {
        // Public/Learner Authenticated Category Listing
        app.MapGet("/api/v1/categories", async (
            [FromServices] IUseCase<GetCategoriesRequest, IReadOnlyList<CategoryDescriptorDto>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetCategoriesRequest(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetCategories")
        .WithSummary("List all categories")
        .WithDescription("Returns all active and inactive non-deleted categories ordered by sort order then slug for authenticated clients.")
        .Produces<List<CategoryDescriptorDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();

        // Admin Categories Management
        var adminGroup = app.MapGroup("/api/v1/admin/categories")
            .WithTags("Administrator Categories")
            .RequireAuthorization("CategoryAdmin");

        adminGroup.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            return await next(context);
        });

        // 1. GET /api/v1/admin/categories
        adminGroup.MapGet("/", async (
            [FromServices] IUseCase<GetAdminCategoriesRequest, AdminPagedResult<AdminCategoryDetailDto>> handler,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var request = new GetAdminCategoriesRequest(search, isActive, page, pageSize);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminCategories")
        .WithSummary("List administrative categories")
        .WithDescription("Returns paginated categories with usage statistics and delete eligibility.")
        .Produces<AdminPagedResult<AdminCategoryDetailDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting("CategoryAdminReads");

        // 2. POST /api/v1/admin/categories
        adminGroup.MapPost("/", async (
            [FromBody] CreateCategoryInput input,
            ClaimsPrincipal principal,
            HttpContext httpContext,
            [FromServices] IUseCase<CreateCategoryRequest, CategoryDescriptorDto> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var sessionVersion = GetSessionVersion(principal);

            var request = new CreateCategoryRequest(
                Slug: input.Slug,
                NameEn: input.NameEn,
                NameVi: input.NameVi,
                SortOrder: input.SortOrder,
                IsActive: input.IsActive,
                ActorUserId: userId ?? Guid.Empty,
                PresentedSessionVersion: sessionVersion,
                CorrelationId: httpContext.TraceIdentifier);

            var result = await handler.ExecuteAsync(request, ct);
            if (result.IsFailure)
            {
                return result.Error.ToProblem();
            }

            return Results.Created($"/api/v1/admin/categories/{result.Value.Id}", result.Value);
        })
        .WithName("CreateAdminCategory")
        .WithSummary("Create a new document category")
        .WithDescription("Creates a new taxonomy category with slug, English name, Vietnamese name, sort order, and active flag.")
        .Produces<CategoryDescriptorDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting("CategoryAdminWrites");

        // 3. GET /api/v1/admin/categories/{id:guid}
        adminGroup.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<Guid, AdminCategoryDetailDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminCategoryById")
        .WithSummary("Get administrative category details")
        .WithDescription("Returns detailed category descriptor with aggregate usage breakdown across books, catalog, quiz, and insights.")
        .Produces<AdminCategoryDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting("CategoryAdminReads");

        // 4. PATCH /api/v1/admin/categories/{id:guid}
        adminGroup.MapPatch("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCategoryInput input,
            ClaimsPrincipal principal,
            HttpContext httpContext,
            [FromServices] IUseCase<UpdateCategoryRequest, CategoryDescriptorDto> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var sessionVersion = GetSessionVersion(principal);

            var request = new UpdateCategoryRequest(
                Id: id,
                NameEn: input.NameEn,
                NameVi: input.NameVi,
                SortOrder: input.SortOrder,
                IsActive: input.IsActive,
                ExpectedVersion: input.ExpectedVersion,
                ActorUserId: userId ?? Guid.Empty,
                PresentedSessionVersion: sessionVersion,
                CorrelationId: httpContext.TraceIdentifier);

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("UpdateAdminCategory")
        .WithSummary("Update category metadata")
        .WithDescription("Updates category names, sort order, and active status under optimistic concurrency control.")
        .Produces<CategoryDescriptorDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting("CategoryAdminWrites");

        // 5. DELETE /api/v1/admin/categories/{id:guid}
        adminGroup.MapDelete("/{id:guid}", async (
            Guid id,
            [FromQuery] int expectedVersion,
            ClaimsPrincipal principal,
            HttpContext httpContext,
            [FromServices] IUseCase<DeleteCategoryRequest, bool> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var sessionVersion = GetSessionVersion(principal);

            var request = new DeleteCategoryRequest(
                Id: id,
                ExpectedVersion: expectedVersion,
                ActorUserId: userId ?? Guid.Empty,
                PresentedSessionVersion: sessionVersion,
                CorrelationId: httpContext.TraceIdentifier);

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();
        })
        .WithName("DeleteAdminCategory")
        .WithSummary("Soft-delete unused category")
        .WithDescription("Soft-deletes an unused category if it has zero references across document books, catalog, quiz questions, and tech insights.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .RequireRateLimiting("CategoryAdminWrites");

        return app;
    }
}
