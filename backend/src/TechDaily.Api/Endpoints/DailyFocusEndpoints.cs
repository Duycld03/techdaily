using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.ExplainTerm;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;

namespace TechDaily.Api.Endpoints;

public static class DailyFocusEndpoints
{
    public static RouteGroupBuilder MapDailyFocusEndpoints(this RouteGroupBuilder group)
    {
        // Public / Authenticated Today Curriculum
        group.MapGet("/today", async (
            [FromQuery] string? date,
            [FromQuery] string? locale,
            ClaimsPrincipal userClaims,
            IUseCase<GetTodayFocusRequest, GetTodayFocusResponse> handler,
            CancellationToken ct) =>
        {
            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            {
                parsedDate = d;
            }

            var userId = GetUserIdFromClaims(userClaims);
            var request = new GetTodayFocusRequest(userId, parsedDate, locale ?? "en");
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
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

            byte[]? audioBytes = null;
            if (!string.IsNullOrWhiteSpace(body.AudioBase64))
            {
                audioBytes = Convert.FromBase64String(body.AudioBase64);
            }

            var request = new SubmitDailyDrillRequest(
                DrillId: id,
                UserId: userId.Value,
                AnswerText: body.AnswerText,
                AudioBytes: audioBytes,
                AudioMimeType: body.AudioMimeType,
                Locale: body.Locale ?? "en");

            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("SubmitDailyDrill")
        .WithSummary("Evaluates senior interview answer with Gemini 3.5 Flash and updates user streak.");

        // Public Term Explanation (Backed by Semantic Cache)
        group.MapPost("/explain-term", async (
            [FromBody] ExplainTermRequest request,
            IUseCase<ExplainTermRequest, ExplainTermResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ExplainTerm")
        .WithSummary("Provides instant AI terminology explanation tooltip.");

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
    public string? AnswerText { get; set; }
    public string? AudioBase64 { get; set; }
    public string? AudioMimeType { get; set; }
    public string? Locale { get; set; }
}
