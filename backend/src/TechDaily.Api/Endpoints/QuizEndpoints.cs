using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.InterviewQuiz.DTOs;
using TechDaily.Domain.Enums;

namespace TechDaily.Api.Endpoints;

public static class QuizEndpoints
{
    public static RouteGroupBuilder MapQuizEndpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // 1. Generate Quiz (AI + Unmastered DB selection)
        group.MapPost("/generate", async (
            [FromBody] GenerateQuizApiRequest apiRequest,
            ClaimsPrincipal userClaims,
            IUseCase<GenerateQuizRequest, GenerateQuizResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            Category? catEnum = apiRequest.Category.HasValue ? (Category)apiRequest.Category.Value : null;
            var levelEnum = (QuizLevel)Math.Clamp(apiRequest.Level, 0, 3);
            var count = Math.Clamp(apiRequest.Count, 1, 10);

            var request = new GenerateQuizRequest(
                userId.Value,
                apiRequest.Topic,
                catEnum,
                levelEnum,
                count,
                apiRequest.Locale ?? "en"
            );

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GenerateQuiz")
        .WithSummary("Generates an interactive interview quiz batch tailored to seniority level using Gemini 3.6 Flash and unmastered DB questions.");

        // 2. Submit Question Answer
        group.MapPost("/submit", async (
            [FromBody] SubmitQuizAnswerApiRequest apiRequest,
            ClaimsPrincipal userClaims,
            IUseCase<SubmitQuizAnswerRequest, SubmitQuizAnswerResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new SubmitQuizAnswerRequest(
                userId.Value,
                apiRequest.QuestionId,
                apiRequest.SelectedOptionIndex
            );

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("SubmitQuizAnswer")
        .WithSummary("Submits an option choice, returns correctness and deep explanation, and updates user mastery status.");

        // 3. Get Mistake Review Queue
        group.MapGet("/review-queue", async (
            [FromQuery] int? category,
            [FromQuery] int? level,
            [FromQuery] string? topic,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ClaimsPrincipal userClaims,
            IUseCase<GetQuizReviewQueueRequest, GetQuizReviewQueueResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            Category? catEnum = category.HasValue ? (Category)category.Value : null;
            QuizLevel? levelEnum = level.HasValue ? (QuizLevel)level.Value : null;

            var request = new GetQuizReviewQueueRequest(
                userId.Value,
                catEnum,
                levelEnum,
                topic,
                page ?? 1,
                pageSize ?? 20
            );

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetQuizReviewQueue")
        .WithSummary("Retrieves all unmastered/failed quiz questions in the user's review queue for iterative practice.");

        // 4. Get Quiz Mastery Stats
        group.MapGet("/stats", async (
            ClaimsPrincipal userClaims,
            IUseCase<GetQuizStatsRequest, GetQuizStatsResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new GetQuizStatsRequest(userId.Value);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetQuizStats")
        .WithSummary("Calculates overall interview quiz statistics, mastery counts, accuracy rate, and level breakdown.");

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

public record GenerateQuizApiRequest(
    string Topic,
    int? Category,
    int Level,
    int Count = 5,
    string? Locale = "en"
);

public record SubmitQuizAnswerApiRequest(
    Guid QuestionId,
    int SelectedOptionIndex
);
