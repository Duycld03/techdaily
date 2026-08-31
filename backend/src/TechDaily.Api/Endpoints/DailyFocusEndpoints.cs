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
        // Default dev user ID until full auth header is hooked up
        var defaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        group.MapGet("/today", async (
            [FromQuery] string? date,
            [FromQuery] string? locale,
            IUseCase<GetTodayFocusRequest, GetTodayFocusResponse> handler,
            CancellationToken ct) =>
        {
            DateOnly? parsedDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
            {
                parsedDate = d;
            }

            var request = new GetTodayFocusRequest(defaultUserId, parsedDate, locale ?? "en");
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetTodayFocus")
        .WithSummary("Retrieves today's reading slice, micro-quiz, and interview scenario challenge.");

        group.MapPost("/drills/{id:guid}/submit", async (
            Guid id,
            [FromBody] SubmitDrillJsonRequest body,
            IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse> handler,
            CancellationToken ct) =>
        {
            byte[]? audioBytes = null;
            if (!string.IsNullOrWhiteSpace(body.AudioBase64))
            {
                audioBytes = Convert.FromBase64String(body.AudioBase64);
            }

            var request = new SubmitDailyDrillRequest(
                DrillId: id,
                UserId: defaultUserId,
                AnswerText: body.AnswerText,
                AudioBytes: audioBytes,
                AudioMimeType: body.AudioMimeType,
                Locale: body.Locale ?? "en");

            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message, code = result.Error.Code });
        })
        .WithName("SubmitDailyDrill")
        .WithSummary("Submits written or audio response and returns instant AI evaluation.");

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
        .WithSummary("Provides 2-sentence technical explanation of a highlighted term with DB caching.");

        return group;
    }
}

public class SubmitDrillJsonRequest
{
    public string? AnswerText { get; set; }
    public string? AudioBase64 { get; set; }
    public string? AudioMimeType { get; set; }
    public string? Locale { get; set; } = "en";
}
