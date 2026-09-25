using System.Security.Claims;
using TechDaily.Api.Http;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Curriculum.DTOs;
using TechDaily.Application.Features.Curriculum.GetCurriculumRoadmap;

namespace TechDaily.Api.Endpoints;

public static class CurriculumEndpoints
{
    public static RouteGroupBuilder MapCurriculumEndpoints(this RouteGroupBuilder group)
    {
        // Protected Curriculum Roadmap (Requires Logged-In User)
        group.MapGet("/roadmap", async (
            ClaimsPrincipal userClaims,
            IUseCase<GetCurriculumRoadmapRequest, CurriculumRoadmapResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new GetCurriculumRoadmapRequest(userId.Value);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("GetCurriculumRoadmap")
        .WithSummary("Get Curriculum Roadmap")
        .WithDescription("Retrieves the Senior Engineering Craft Handbook roadmap grouped into 4 core technical pillars with authenticated user progress.")
        .Produces<CurriculumRoadmapResponse>(StatusCodes.Status200OK)
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
