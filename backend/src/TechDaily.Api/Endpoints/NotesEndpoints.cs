using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Notes.GetHighlights;

namespace TechDaily.Api.Endpoints;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notes")
            .WithTags("Notes")
            .RequireAuthorization();

        group.MapGet("/highlights", async (
            [FromQuery] string? tag,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<GetHighlightsRequest, GetHighlightsResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new GetHighlightsRequest(userId.Value, tag), ct);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("GetHighlights");

        group.MapPost("/highlights", async (
            [FromBody] CreateHighlightApiRequest body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<CreateHighlightRequest, CreateHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new CreateHighlightRequest(
                userId.Value,
                body.DocumentChunkId,
                body.SelectedText,
                body.Note,
                body.Tags);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Created($"/api/v1/notes/highlights/{success.Highlight.Id}", success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("CreateHighlight");

        group.MapDelete("/highlights/{id:guid}", async (
            Guid id,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<DeleteHighlightRequest, DeleteHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new DeleteHighlightRequest(id, userId.Value), ct);
            return result.Match(
                success => Results.NoContent(),
                error => error == Error.NotFound ? Results.NotFound() : Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("DeleteHighlight");

        return app;
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

public record CreateHighlightApiRequest(
    Guid DocumentChunkId,
    string SelectedText,
    string? Note = null,
    List<string>? Tags = null);
