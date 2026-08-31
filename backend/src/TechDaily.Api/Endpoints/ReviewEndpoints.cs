using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;

namespace TechDaily.Api.Endpoints;

public static class ReviewEndpoints
{
    public static RouteGroupBuilder MapReviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/deck", async (
            [FromQuery] string? date,
            ClaimsPrincipal userClaims,
            IUseCase<GetReviewDeckRequest, GetReviewDeckResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            {
                parsedDate = d;
            }

            var request = new GetReviewDeckRequest(userId.Value, parsedDate);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("GetReviewDeck")
        .WithSummary("Retrieves pending SM-2 spaced repetition cards due for current user.");

        group.MapPost("/cards/{id:guid}/grade", async (
            Guid id,
            [FromBody] GradeCardJsonRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<GradeReviewCardRequest, GradeReviewCardResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new GradeReviewCardRequest(id, userId.Value, body.QualityGrade);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("GradeReviewCard")
        .WithSummary("Grades a review card (0-5) and recalculates next interval using SM-2.");

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

public class GradeCardJsonRequest
{
    public int QualityGrade { get; set; }
}
