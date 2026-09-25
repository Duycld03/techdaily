using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Api.Http;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Domain.Enums;

namespace TechDaily.Api.Endpoints;

public static class InsightsEndpoints
{
    public static RouteGroupBuilder MapInsightsEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // Dynamic Insights Metadata & Topic Inspirations
        group.MapGet("/meta", async (
            IUseCase<GetInsightsMetaRequest, GetInsightsMetaResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetInsightsMetaRequest(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .WithName("GetInsightsMeta")
        .WithSummary("Get Insights Metadata")
        .WithDescription("Retrieves dynamic category metadata and curated AI topic suggestions.")
        .Produces<GetInsightsMetaResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Authenticated Infinite Feed
        group.MapGet("/feed", async (
            [FromQuery] int? category,
            [FromQuery] string? tag,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] bool? randomize,
            [FromQuery] bool? onlyBookmarked,
            ClaimsPrincipal userClaims,
            IUseCase<GetInsightsFeedRequest, GetInsightsFeedResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            Category? catEnum = category.HasValue ? (Category)category.Value : null;

            var request = new GetInsightsFeedRequest(
                catEnum,
                tag,
                page ?? 1,
                pageSize ?? 10,
                randomize ?? false,
                userId.Value,
                onlyBookmarked ?? false
            );

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .WithName("GetInsightsFeed")
        .WithSummary("Get Insights Feed")
        .WithDescription("Retrieves bite-sized senior technical insights with category and tag filtering.")
        .Produces<GetInsightsFeedResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Generate Insight with Gemini Flash
        group.MapPost("/generate", async (
            [FromBody] GenerateInsightRequest request,
            ClaimsPrincipal userClaims,
            IUseCase<GenerateInsightRequest, TechInsightDto> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/insights/{result.Value.Id}", result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .WithName("GenerateInsight")
        .WithSummary("Generate Insight")
        .WithDescription("Generates an on-demand senior technical insight using Google Gemini Flash Lite.")
        .Produces<TechInsightDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Bookmark Insight (Toggle Save)
        group.MapPost("/{id:guid}/bookmark", async (
            Guid id,
            ClaimsPrincipal userClaims,
            IUseCase<BookmarkInsightRequest, BookmarkInsightResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new BookmarkInsightRequest(id, userId.Value);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .WithName("BookmarkInsight")
        .WithSummary("Bookmark Insight")
        .WithDescription("Toggles bookmark status and updates bookmark count for authenticated user.")
        .Produces<BookmarkInsightResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal claims)
    {
        var idClaim = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var guid))
        {
            return guid;
        }
        return null;
    }
}
