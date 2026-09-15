using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Features.DailyFocus.ExplainTerm;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Features.DailyFocus.SwitchBook;
using TechDaily.Application.Interfaces;

namespace TechDaily.Api.Endpoints;

public static class DailyFocusEndpoints
{
    public static RouteGroupBuilder MapDailyFocusEndpoints(this RouteGroupBuilder group)
    {
        // Protected Today Curriculum & Active Book Pacer (Requires Logged-In User)
        group.MapGet("/today", async (
            [FromQuery] Guid? bookId,
            [FromQuery] int? chunkOrder,
            [FromQuery] int? dayOrder,
            [FromQuery] string? date,
            [FromQuery] string? locale,
            ClaimsPrincipal userClaims,
            IUseCase<GetTodayFocusRequest, GetTodayFocusResponse> handler,
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

            var request = new GetTodayFocusRequest(userId, bookId, chunkOrder, dayOrder, parsedDate, locale ?? "en");
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("GetTodayFocus")
        .WithSummary("Retrieves today's reading slice, micro-quiz, and interview scenario challenge.");

        // Protected Drill Submission (Requires Logged-In User)
        group.MapPost("/drills/{id:guid}/submit", async (
            Guid id,
            [FromBody] SubmitDrillJsonRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new SubmitDailyDrillRequest(
                DrillId: id,
                UserId: userId.Value,
                SelectedOptionIndex: body.SelectedOptionIndex,
                Locale: body.Locale ?? "en");

            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { code = result.Error.Code, error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("SubmitDailyDrill")
        .WithSummary("Evaluates multiple-choice senior scenario decision and updates user streak.");

        // Public Term Explanation (Backed by Semantic Cache)
        group.MapPost("/explain-term", async (
            [FromBody] ExplainTermRequest request,
            IUseCase<ExplainTermRequest, ExplainTermResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { code = result.Error.Code, error = result.Error.Message });
        })
        .WithName("ExplainTerm")
        .WithSummary("Provides instant AI terminology explanation tooltip.");

        // Protected Active Book Switcher (Requires Logged-In User)
        group.MapPost("/switch-book", async (
            [FromBody] SwitchBookBodyRequest body,
            ClaimsPrincipal userClaims,
            IUseCase<SwitchBookRequest, PacerDto> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new SwitchBookRequest(userId.Value, body.BookId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { code = result.Error.Code, error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("SwitchActiveBook")
        .WithSummary("Switches the user's currently active reading book pacer.");

        // Priority Promotion / On-Demand Challenge Generation for Chunk
        group.MapGet("/chunk-challenge/{chunkId:guid}", async (
            Guid chunkId,
            ILookAheadBufferService lookAheadService,
            CancellationToken ct) =>
        {
            var question = await lookAheadService.GenerateChallengeForChunkAsync(chunkId, ct);
            if (question == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new InterviewQuestionDto
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                Options = question.Options,
                CorrectOptionIndex = null,
                ExplanationMarkdown = null,
                ExpectedKeyPoints = question.ExpectedKeyPoints,
                ModelAnswerMarkdown = string.Empty,
                Difficulty = question.Difficulty
            });
        })
        .WithName("GetOrGenerateChunkChallenge")
        .WithSummary("Retrieves or triggers high-priority generation for a slice's senior trade-off scenario.");

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

public class SubmitDrillJsonRequest
{
    public int SelectedOptionIndex { get; set; }
    public string? Locale { get; set; }
}

public record SwitchBookBodyRequest(Guid BookId);
