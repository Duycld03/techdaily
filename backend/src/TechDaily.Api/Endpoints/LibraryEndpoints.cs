using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.CrawlUrl;
using TechDaily.Application.Features.Library.DeleteBook;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Application.Features.Library.ImportDocument;
using TechDaily.Application.Features.Library.UploadPdf;
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

        // Protected Document Deletion (Requires Authentication)
        group.MapDelete("/books/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<DeleteBookRequest, DeleteBookResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new DeleteBookRequest(id), ct);
            return result.Match(
                success => Results.NoContent(),
                error => error == Error.NotFound ? Results.NotFound() : Results.BadRequest(new { error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("DeleteBook");

        // Protected PDF Upload (Requires Authentication, supports up to 200MB)
        group.MapPost("/upload-pdf", async (
            HttpRequest httpRequest,
            [FromServices] IUseCase<UploadPdfRequest, UploadPdfResponse> handler,
            CancellationToken ct) =>
        {
            if (!httpRequest.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Multipart form data is required." });
            }

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "A valid PDF file is required." });
            }

            var title = form["title"].ToString();
            var categoryStr = form["category"].ToString();
            var category = Enum.TryParse<Category>(categoryStr, out var cat) ? cat : Category.BackendDotNet;
            var language = form["language"].ToString();
            if (string.IsNullOrWhiteSpace(language)) language = "en";

            using var stream = file.OpenReadStream();
            var request = new UploadPdfRequest(
                FileStream: stream,
                FileName: file.FileName,
                FileLength: file.Length,
                Title: string.IsNullOrWhiteSpace(title) ? null : title,
                Category: category,
                Language: language);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Created($"/api/v1/library/books/{success.Book.Id}", success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .DisableAntiforgery()
        .RequireAuthorization()
        .WithName("UploadPdfDocument");

        // Protected URL Crawler (Requires Authentication)
        group.MapPost("/crawl-url", async (
            [FromBody] CrawlUrlRequest request,
            [FromServices] IUseCase<CrawlUrlRequest, CrawlUrlResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("CrawlWebDocument");

        return app;
    }
}
