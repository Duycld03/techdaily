using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using DeepPace.Api.Http;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Documents;
using DeepPace.Application.Features.Admin.Documents.DTOs;

namespace DeepPace.Api.Endpoints;

public record AdminRetryPayload(
    Guid AttemptId,
    int ExpectedVersion,
    string RequestId,
    string Reason);

public static class AdminDocumentOperationsEndpoints
{
    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var userId) ? userId : null;
    }

    public static WebApplication MapAdminDocumentOperationsEndpoints(this WebApplication app)
    {
        var adminGroup = app.MapGroup("/api/v1/admin/documents")
            .WithTags("Administrator Document Operations")
            .RequireAuthorization("DocumentOperationsAdmin");

        // 1. GET /api/v1/admin/documents
        adminGroup.MapGet("/", async (
            [FromServices] IUseCase<AdminDocumentInventoryQuery, AdminPagedResult<AdminDocumentInventoryItemDto>> handler,
            [FromQuery] string? search,
            [FromQuery] string? scope,
            [FromQuery] Guid? ownerId,
            [FromQuery] string? sourceType,
            [FromQuery] string? category,
            [FromQuery] string? language,
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken ct = default) =>
        {
            var query = new AdminDocumentInventoryQuery(
                Search: search,
                Scope: scope,
                OwnerId: ownerId,
                SourceType: sourceType,
                Category: category,
                Language: language,
                Status: status,
                Page: page ?? 1,
                PageSize: pageSize ?? 20);

            var result = await handler.ExecuteAsync(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminDocumentInventory")
        .WithSummary("List and search cross-scope document inventory")
        .Produces<AdminPagedResult<AdminDocumentInventoryItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // 2. GET /api/v1/admin/documents/{scope}/{id}
        adminGroup.MapGet("/{scope}/{id:guid}", async (
            string scope,
            Guid id,
            [FromServices] IUseCase<AdminGetDocumentDetailRequest, AdminDocumentDetailDto> handler,
            CancellationToken ct = default) =>
        {
            var request = new AdminGetDocumentDetailRequest(scope, id);
            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminDocumentDetail")
        .WithSummary("Get allowlisted document detail and retry eligibility")
        .Produces<AdminDocumentDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // 3. GET /api/v1/admin/documents/{scope}/{id}/attempts
        adminGroup.MapGet("/{scope}/{id:guid}/attempts", async (
            string scope,
            Guid id,
            [FromServices] IUseCase<AdminGetDocumentAttemptsRequest, AdminPagedResult<AdminDocumentAttemptItemDto>> handler,
            [FromQuery] string? state,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken ct = default) =>
        {
            var request = new AdminGetDocumentAttemptsRequest(
                Scope: scope,
                DocumentId: id,
                State: state,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page ?? 1,
                PageSize: pageSize ?? 20);

            var result = await handler.ExecuteAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
        })
        .WithName("GetAdminDocumentAttempts")
        .WithSummary("Get paged attempt processing history for a document")
        .Produces<AdminPagedResult<AdminDocumentAttemptItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // 4. POST /api/v1/admin/documents/{scope}/{id}/retry
        adminGroup.MapPost("/{scope}/{id:guid}/retry", async (
            string scope,
            Guid id,
            [FromBody] AdminRetryPayload payload,
            ClaimsPrincipal principal,
            [FromServices] IUseCase<AdminRetryDocumentCommand, AdminRetryDocumentResponseDto> handler,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var command = new AdminRetryDocumentCommand(
                Scope: scope,
                DocumentId: id,
                AttemptId: payload.AttemptId,
                ExpectedVersion: payload.ExpectedVersion,
                RequestId: payload.RequestId,
                Reason: payload.Reason,
                ActorUserId: userId.Value);

            var result = await handler.ExecuteAsync(command, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/v1/admin/documents/{scope}/{id}", result.Value)
                : result.Error.ToProblem();
        })
        .WithName("RetryAdminDocumentIngestion")
        .WithSummary("Request an audited retry of failed/interrupted document ingestion")
        .Produces<AdminRetryDocumentResponseDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // 5. POST /api/v1/library/books/{id}/source/pdf (Personal owner replacement)
        app.MapPost("/api/v1/library/books/{id:guid}/source/pdf", async (
            Guid id,
            [FromForm] IFormFile file,
            [FromForm] string requestId,
            [FromForm] int expectedVersion,
            [FromForm] string? explicitLanguage,
            ClaimsPrincipal principal,
            [FromServices] IUseCase<ReplacePersonalDocumentSourceCommand, SourceReplacementResponseDto> handler,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { code = "Source.EmptyFile", message = "A valid PDF file is required." });
            }

            await using var stream = file.OpenReadStream();
            var command = new ReplacePersonalDocumentSourceCommand(
                DocumentId: id,
                RequestingUserId: userId.Value,
                ExpectedVersion: expectedVersion,
                RequestId: requestId,
                ExplicitLanguage: explicitLanguage,
                SourceStream: stream,
                FileName: file.FileName,
                ByteLength: file.Length);

            var result = await handler.ExecuteAsync(command, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/v1/library/books/{id}", result.Value)
                : result.Error.ToProblem();
        })
        .WithName("ReplacePersonalDocumentSource")
        .WithSummary("Owner provide source and retry for a failed personal book")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces<SourceReplacementResponseDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // 6. POST /api/v1/admin/catalog/documents/{id}/source/pdf (Catalog admin replacement)
        app.MapPost("/api/v1/admin/catalog/documents/{id:guid}/source/pdf", async (
            Guid id,
            [FromForm] IFormFile file,
            [FromForm] string requestId,
            [FromForm] int expectedVersion,
            [FromForm] string reason,
            ClaimsPrincipal principal,
            [FromServices] IUseCase<ReplaceCatalogDocumentSourceCommand, SourceReplacementResponseDto> handler,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(principal);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { code = "Source.EmptyFile", message = "A valid PDF file is required." });
            }

            await using var stream = file.OpenReadStream();
            var command = new ReplaceCatalogDocumentSourceCommand(
                DocumentId: id,
                ActorUserId: userId.Value,
                ExpectedVersion: expectedVersion,
                RequestId: requestId,
                Reason: reason,
                SourceStream: stream,
                FileName: file.FileName,
                ByteLength: file.Length);

            var result = await handler.ExecuteAsync(command, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/v1/admin/catalog/documents/{id}", result.Value)
                : result.Error.ToProblem();
        })
        .WithName("ReplaceCatalogDocumentSource")
        .WithSummary("Catalog admin replace source for a failed catalog document")
        .RequireAuthorization("CatalogAdmin")
        .DisableAntiforgery()
        .Produces<SourceReplacementResponseDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
