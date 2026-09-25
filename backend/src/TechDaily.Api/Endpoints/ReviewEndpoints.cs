using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Api.Http;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.GetReviewDeck;
using TechDaily.Application.Features.Review.GradeReviewCard;
using TechDaily.Application.Features.Review.CreateCardFromHighlight;
using TechDaily.Application.Features.Review.CreateCardFromQuizMistake;
using TechDaily.Application.Features.Review.GetReviewCards;
using TechDaily.Application.Features.Review.UpdateReviewCard;
using TechDaily.Application.Features.Review.DeleteReviewCard;
using TechDaily.Application.Features.Review.ResetReviewCardProgress;
using TechDaily.Domain.Enums;

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
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("GetReviewDeck")
        .WithSummary("Get Review Deck")
        .WithDescription("Retrieves pending SM-2 spaced repetition cards due for current user.")
        .Produces<GetReviewDeckResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("GradeReviewCard")
        .WithSummary("Grade Review Card")
        .WithDescription("Grades a review card (0-5) and recalculates next interval using SM-2.")
        .Produces<GradeReviewCardResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/cards/from-highlight", async (
            [FromBody] CreateCardFromHighlightJsonRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<CreateCardFromHighlightRequest, CreateCardFromHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new CreateCardFromHighlightRequest(body.HighlightId, userId.Value, body.Locale ?? "en");
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/review/cards/{result.Value.CardId}", result.Value)
                : result.Error == Error.NotFound
                    ? result.Error.ToProblem(StatusCodes.Status404NotFound)
                    : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("CreateCardFromHighlight")
        .WithSummary("Create Card From Highlight")
        .WithDescription("Creates or retrieves an active recall spaced repetition card from a user highlight.")
        .Produces<CreateCardFromHighlightResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/cards/from-quiz-mistake", async (
            [FromBody] CreateCardFromQuizMistakeJsonRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<CreateCardFromQuizMistakeRequest, CreateCardFromQuizMistakeResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new CreateCardFromQuizMistakeRequest(body.QuestionId, userId.Value);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/review/cards/{result.Value.CardId}", result.Value)
                : result.Error == Error.NotFound
                    ? result.Error.ToProblem(StatusCodes.Status404NotFound)
                    : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("CreateCardFromQuizMistake")
        .WithSummary("Create Card From Quiz Mistake")
        .WithDescription("Creates or retrieves a spaced repetition card from a failed quiz question.")
        .Produces<CreateCardFromQuizMistakeResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/cards", async (
            [FromQuery] string? search,
            [FromQuery] CardStatus? status,
            [FromQuery] CardSourceType? sourceType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            ClaimsPrincipal userClaims = null!,
            IUseCase<GetReviewCardsRequest, GetReviewCardsResponse> handler = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new GetReviewCardsRequest(userId.Value, search, status, sourceType, page, pageSize);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .RequireAuthorization()
        .WithName("GetReviewCards")
        .WithSummary("Get Review Cards")
        .WithDescription("Retrieves paginated flashcards in user's personal deck with search, filtering, and deck statistics.")
        .Produces<GetReviewCardsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/cards/{id:guid}", async (
            Guid id,
            [FromBody] UpdateReviewCardApiRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<UpdateReviewCardRequest, UpdateReviewCardResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new UpdateReviewCardRequest(id, userId.Value, body.FrontMarkdown, body.BackMarkdown);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? error.ToProblem(StatusCodes.Status404NotFound)
                    : error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .RequireAuthorization()
        .WithName("UpdateReviewCard")
        .WithSummary("Update Review Card")
        .WithDescription("Updates front and back markdown content for a flashcard.")
        .Produces<UpdateReviewCardResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/cards/{id:guid}", async (
            Guid id,
            ClaimsPrincipal userClaims,
            IUseCase<DeleteReviewCardRequest, DeleteReviewCardResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new DeleteReviewCardRequest(id, userId.Value);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.NoContent(),
                error => error == Error.NotFound
                    ? error.ToProblem(StatusCodes.Status404NotFound)
                    : error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .RequireAuthorization()
        .WithName("DeleteReviewCard")
        .WithSummary("Delete Review Card")
        .WithDescription("Soft-deletes a spaced repetition card from user's personal deck.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/cards/{id:guid}/reset", async (
            Guid id,
            ClaimsPrincipal userClaims,
            IUseCase<ResetReviewCardProgressRequest, ResetReviewCardProgressResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new ResetReviewCardProgressRequest(id, userId.Value);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? error.ToProblem(StatusCodes.Status404NotFound)
                    : error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .RequireAuthorization()
        .WithName("ResetReviewCardProgress")
        .WithSummary("Reset Card Progress")
        .WithDescription("Resets SM-2 progression for a review card back to initial learning state.")
        .Produces<ResetReviewCardProgressResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
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

public class GradeCardJsonRequest
{
    public int QualityGrade { get; set; }
}

public class CreateCardFromHighlightJsonRequest
{
    public Guid HighlightId { get; set; }
    public string? Locale { get; set; }
}

public class CreateCardFromQuizMistakeJsonRequest
{
    public Guid QuestionId { get; set; }
}

public record UpdateReviewCardApiRequest(string FrontMarkdown, string BackMarkdown);
