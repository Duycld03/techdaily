using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Api.Http;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Notes.GetHighlights;
using TechDaily.Application.Features.Notes.UpdateHighlight;

namespace TechDaily.Api.Endpoints;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notes")
            .WithTags("Reading Highlights & Notes")
            .RequireAuthorization();

        group.MapGet("/highlights", async (
            [FromQuery] string? tag,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 15,
            ClaimsPrincipal userClaims = null!,
            [FromServices] IUseCase<GetHighlightsRequest, GetHighlightsResponse> handler = null!,
            CancellationToken ct = default) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new GetHighlightsRequest(userId.Value, tag, search, page, pageSize), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .WithName("GetHighlights")
        .WithSummary("Get Highlights")
        .WithDescription("Retrieves paginated reading highlights, architectural takeaways, and user tags with search filtering.")
        .Produces<GetHighlightsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                error => error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .WithName("CreateHighlight")
        .WithSummary("Create Highlight")
        .WithDescription("Saves a key technical excerpt, reflection note, and taxonomy tags from a reading slice.")
        .Produces<CreateHighlightResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/highlights/{id:guid}", async (
            Guid id,
            [FromBody] UpdateHighlightApiRequest body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<UpdateHighlightRequest, UpdateHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var request = new UpdateHighlightRequest(id, userId.Value, body.Note, body.Tags);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? error.ToProblem(StatusCodes.Status404NotFound)
                    : error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .WithName("UpdateHighlight")
        .WithSummary("Update Highlight")
        .WithDescription("Updates personal reflection notes and tags for an existing reading highlight.")
        .Produces<UpdateHighlightResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                error => error == Error.NotFound
                    ? error.ToProblem(StatusCodes.Status404NotFound)
                    : error.ToProblem(StatusCodes.Status400BadRequest)
            );
        })
        .WithName("DeleteHighlight")
        .WithSummary("Delete Highlight")
        .WithDescription("Removes a reading highlight and its associated flashcard references.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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

public record UpdateHighlightApiRequest(
    string? Note = null,
    List<string>? Tags = null);
