using System.Security.Claims;
using TechDaily.Api.Http;
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
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization()
        .WithName("GetKnowledgeGraph")
        .WithSummary("Get Knowledge Graph")
        .WithDescription("Retrieves the authenticated user's personal knowledge graph, derived strictly from their own learned artifacts: books they imported, curriculum topics they have touched (via their flashcards or highlight tags), their personal highlights and spaced-repetition cards, and only the pillar hubs those nodes connect to. It does not project the global seeded curriculum or content owned by other users.")
        .Produces<KnowledgeGraphResponse>(StatusCodes.Status200OK)
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
