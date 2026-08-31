using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.CreateHighlight;
using TechDaily.Application.Features.Notes.DeleteHighlight;
using TechDaily.Application.Features.Notes.GetHighlights;

namespace TechDaily.Api.Endpoints;

public static class NotesEndpoints
{
    private static readonly Guid DevDefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notes")
            .WithTags("Notes");

        group.MapGet("/highlights", async (
            [FromQuery] string? tag,
            [FromServices] IUseCase<GetHighlightsRequest, GetHighlightsResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetHighlightsRequest(DevDefaultUserId, tag), ct);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("GetHighlights");

        group.MapPost("/highlights", async (
            [FromBody] CreateHighlightApiRequest body,
            [FromServices] IUseCase<CreateHighlightRequest, CreateHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var request = new CreateHighlightRequest(
                DevDefaultUserId,
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
            [FromServices] IUseCase<DeleteHighlightRequest, DeleteHighlightResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new DeleteHighlightRequest(id, DevDefaultUserId), ct);
            return result.Match(
                success => Results.NoContent(),
                error => error == Error.NotFound ? Results.NotFound() : Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("DeleteHighlight");

        return app;
    }
}

public record CreateHighlightApiRequest(
    Guid DocumentChunkId,
    string SelectedText,
    string? Note = null,
    List<string>? Tags = null);
