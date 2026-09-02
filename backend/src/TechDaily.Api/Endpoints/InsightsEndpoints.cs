using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Domain.Enums;

namespace TechDaily.Api.Endpoints;

public static class InsightsEndpoints
{
    public static RouteGroupBuilder MapInsightsEndpoints(this RouteGroupBuilder group)
    {
        // Public/Authenticated Infinite Feed
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
            Category? catEnum = category.HasValue ? (Category)category.Value : null;

            var request = new GetInsightsFeedRequest(
                catEnum,
                tag,
                page ?? 1,
                pageSize ?? 10,
                randomize ?? false,
                userId,
                onlyBookmarked ?? false
            );

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetInsightsFeed")
        .WithSummary("Retrieves bite-sized senior technical insights with category and tag filtering.");

        // Generate Insight with Gemini Flash
        group.MapPost("/generate", async (
            [FromBody] GenerateInsightRequest request,
            IUseCase<GenerateInsightRequest, TechInsightDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GenerateInsight")
        .WithSummary("Generates an on-demand senior technical insight using Gemini 3.6 Flash.");

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
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("BookmarkInsight")
        .WithSummary("Toggles bookmark status and updates bookmark count for authenticated user.");

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
