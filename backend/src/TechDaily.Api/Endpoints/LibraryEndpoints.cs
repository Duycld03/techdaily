using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Application.Features.Library.ImportDocument;
using TechDaily.Domain.Enums;

namespace TechDaily.Api.Endpoints;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/library")
            .WithTags("Library");

        // Public Book Browsing
        group.MapGet("/books", async (
            [FromQuery] Category? category,
            [FromQuery] string? search,
            [FromServices] IUseCase<GetBooksRequest, GetBooksResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetBooksRequest(category, search), ct);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("GetBooks");

        // Public Book Details
        group.MapGet("/books/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<GetBookByIdRequest, GetBookByIdResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetBookByIdRequest(id), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound ? Results.NotFound() : Results.BadRequest(new { error = error.Message })
            );
        })
        .WithName("GetBookById");

        // Protected Document Import (Requires Authentication)
        group.MapPost("/import", async (
            [FromBody] ImportDocumentRequest request,
            [FromServices] IUseCase<ImportDocumentRequest, ImportDocumentResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Created($"/api/v1/library/books/{success.Book.Id}", success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("ImportDocument");

        return app;
    }
}
