using System.Security.Claims;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Curriculum.DTOs;
using TechDaily.Application.Features.Curriculum.GetCurriculumRoadmap;

namespace TechDaily.Api.Endpoints;

public static class CurriculumEndpoints
{
    public static RouteGroupBuilder MapCurriculumEndpoints(this RouteGroupBuilder group)
    {
        // Public / Authenticated Curriculum Roadmap
        group.MapGet("/roadmap", async (
            ClaimsPrincipal userClaims,
            IUseCase<GetCurriculumRoadmapRequest, CurriculumRoadmapResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            var request = new GetCurriculumRoadmapRequest(userId);
            var result = await handler.ExecuteAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetCurriculumRoadmap")
        .WithSummary("Retrieves the full 30-day curriculum roadmap grouped into 4 core technical modules with user progress.");

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
