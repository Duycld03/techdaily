using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.CrawlUrl;
using TechDaily.Application.Features.Library.DeleteBook;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Features.Library.GetBookById;
using TechDaily.Application.Features.Library.GetBookStatus;
using TechDaily.Application.Features.Library.GetBooks;
using TechDaily.Application.Features.Library.ImportDocument;
using TechDaily.Application.Features.Library.UploadPdf;
using TechDaily.Application.Features.Library.ExportBookMarkdown;
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
                error => Results.BadRequest(new { code = error.Code, error = error.Message })
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
                error => error == Error.NotFound 
                    ? Results.NotFound(new { code = error.Code, error = error.Message }) 
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .WithName("GetBookById");

        // Public Book Ingestion Status Polling
        group.MapGet("/books/{id:guid}/status", async (
            Guid id,
            [FromServices] IUseCase<GetBookStatusRequest, BookIngestionStatusDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new GetBookStatusRequest(id), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? Results.NotFound(new { code = error.Code, error = error.Message })
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .WithName("GetBookStatus");


        // Public On-Demand Single Slice Retrieval
        group.MapGet("/books/{id:guid}/slices/{order:int}", async (
            Guid id,
            int order,
            [FromServices] IUseCase<TechDaily.Application.Features.Library.GetBookSlice.GetBookSliceRequest, TechDaily.Application.Features.Library.GetBookSlice.GetBookSliceResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new TechDaily.Application.Features.Library.GetBookSlice.GetBookSliceRequest(id, order), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? Results.NotFound(new { code = error.Code, error = error.Message })
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .WithName("GetBookSlice");

        // Public On-Demand JIT Slice Curation
        group.MapPost("/books/{id:guid}/slices/{order:int}/curate", async (
            Guid id,
            int order,
            [FromServices] IUseCase<TechDaily.Application.Features.Library.CurateSlice.CurateSliceRequest, TechDaily.Application.Features.Library.CurateSlice.CurateSliceResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new TechDaily.Application.Features.Library.CurateSlice.CurateSliceRequest(id, order), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error == Error.NotFound
                    ? Results.NotFound(new { code = error.Code, error = error.Message })
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .WithName("CurateSlice");

        // Protected Document Import (Requires Authentication)
        group.MapPost("/import", async (
            [FromBody] ImportDocumentRequest request,
            [FromServices] IUseCase<ImportDocumentRequest, ImportDocumentResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Created($"/api/v1/library/books/{success.Book.Id}", success),
                error => Results.BadRequest(new { code = error.Code, error = error.Message })
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
                error => error == Error.NotFound 
                    ? Results.NotFound(new { code = error.Code, error = error.Message }) 
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("DeleteBook");

        // Protected PDF Upload (Requires Authentication, supports up to 300MB, Zero-LOH streaming)
        group.MapPost("/upload-pdf", async (
            HttpRequest httpRequest,
            [FromServices] IUseCase<UploadPdfRequest, UploadPdfResponse> handler,
            CancellationToken ct) =>
        {
            if (!httpRequest.HasFormContentType)
            {
                return Results.BadRequest(new { code = Error.MultipartRequired.Code, error = Error.MultipartRequired.Message });
            }

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { code = Error.PdfRequired.Code, error = Error.PdfRequired.Message });
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
                success => Results.Accepted($"/api/v1/library/books/{success.Book.Id}/status", success),
                error => Results.BadRequest(new { code = error.Code, error = error.Message })
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
                error => Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("CrawlWebDocument");

        // Protected Book Markdown Export (Requires Authentication)
        group.MapGet("/books/{id:guid}/export-markdown", async (
            Guid id,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<ExportBookMarkdownRequest, ExportBookMarkdownResponse> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new ExportBookMarkdownRequest(id, userId.Value), ct);
            return result.Match(
                success => Results.File(Encoding.UTF8.GetBytes(success.MarkdownContent), "text/markdown", success.FileName),
                error => error == Error.NotFound
                    ? Results.NotFound(new { code = error.Code, error = error.Message })
                    : Results.BadRequest(new { code = error.Code, error = error.Message })
            );
        })
        .RequireAuthorization()
        .WithName("ExportBookMarkdown");

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

