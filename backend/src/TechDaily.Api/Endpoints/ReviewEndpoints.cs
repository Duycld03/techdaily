using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;

namespace TechDaily.Api.Endpoints;

public static class ReviewEndpoints
{
    public static RouteGroupBuilder MapReviewEndpoints(this RouteGroupBuilder group)
    {
        var defaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        group.MapGet("/deck", async (
            [FromQuery] string? date,
            IUseCase<GetReviewDeckRequest, GetReviewDeckResponse> handler,
            CancellationToken ct) =>
        {
            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            {
                parsedDate = d;
            }

            var request = new GetReviewDeckRequest(defaultUserId, parsedDate);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetReviewDeck")
        .WithSummary("Retrieves pending SM-2 spaced repetition cards due for today.");

        group.MapPost("/cards/{id:guid}/grade", async (
            Guid id,
            [FromBody] GradeCardJsonRequest body,
            IUseCase<GradeReviewCardRequest, GradeReviewCardResponse> handler,
            CancellationToken ct) =>
        {
            var request = new GradeReviewCardRequest(id, defaultUserId, body.QualityGrade);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GradeReviewCard")
        .WithSummary("Grades a review card (0-5) and recalculates next interval using SM-2.");

        return group;
    }
}

public class GradeCardJsonRequest
{
    public int QualityGrade { get; set; }
}
