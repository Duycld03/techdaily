using System.Security.Claims;
using TechDaily.Application.Common;
using TechDaily.Application.Features.KnowledgeGraph.DTOs;
using TechDaily.Application.Features.KnowledgeGraph.GetKnowledgeGraph;

namespace TechDaily.Api.Endpoints;

public static class KnowledgeGraphEndpoints
{
    public static RouteGroupBuilder MapKnowledgeGraphEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            ClaimsPrincipal userClaims,
            IUseCase<GetKnowledgeGraphQuery, KnowledgeGraphResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var query = new GetKnowledgeGraphQuery(userId.Value);
            var result = await handler.ExecuteAsync(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { code = result.Error.Code, error = result.Error.Message });
        })
        .RequireAuthorization()
        .WithName("GetKnowledgeGraph")
        .WithSummary("Retrieves the full architecture knowledge graph with topics, books, cards, and highlights for the authenticated user.");

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
