using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using DeepPace.Api.Http;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.Admin.GetDocumentById;
using DeepPace.Application.Features.Catalog.Admin.GetDocuments;
using DeepPace.Application.Features.Catalog.Admin.GetSlice;
using DeepPace.Application.Features.Catalog.Admin.Hide;
using DeepPace.Application.Features.Catalog.Admin.ImportUrl;
using DeepPace.Application.Features.Catalog.Admin.Prepare;
using DeepPace.Application.Features.Catalog.Admin.PreviewUrl;
using DeepPace.Application.Features.Catalog.Admin.Publish;
using DeepPace.Application.Features.Catalog.Admin.Review;
using DeepPace.Application.Features.Catalog.Admin.UpdateMetadata;
using DeepPace.Application.Features.Catalog.Admin.UploadPdf;
using DeepPace.Application.Features.Catalog.AdoptDocument;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Features.Catalog.Learner.GetDocumentById;
using DeepPace.Application.Features.Catalog.Learner.GetDocuments;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Learner Discovery Endpoints (Authenticated)
        var learnerGroup = app.MapGroup("/api/v1/catalog")
            .WithTags("Curated Document Catalog")
            .RequireAuthorization();

        // GET /api/v1/catalog/capabilities
        learnerGroup.MapGet("/capabilities", async (
            ClaimsPrincipal userClaims,
            [FromServices] ICatalogAdminService adminService,
            [FromServices] IAuthorizationReadinessService readinessService,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var readiness = await readinessService.GetReadinessAsync(ct);
            if (!readiness.IsInitialized)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Authorization Service Unavailable",
                    detail: "Privileged authorization has not been initialized.");
            }

            var auth = await adminService.CheckCatalogAdminAsync(userId.Value, ct);
            if (auth.IsUnavailable)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Authorization Service Unavailable",
                    detail: auth.Reason ?? "Privileged authorization is currently unavailable.");
            }

            return Results.Ok(new CatalogCapabilitiesDto(auth.IsAuthorized));
        })
        .WithName("GetCatalogCapabilities")
        .WithSummary("Get Catalog Management Capability")
        .WithDescription("Checks whether the authenticated user has catalog administration permissions.")
        .Produces<CatalogCapabilitiesDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/v1/catalog/documents
        learnerGroup.MapGet("/documents", async (
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<GetPublishedCatalogDocumentsRequest, GetPublishedCatalogDocumentsResponse> handler,
            [FromQuery] string? search,
            [FromQuery] Category? category,
            [FromQuery] string? language,
            [FromQuery] SourceType? sourceType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            var request = new GetPublishedCatalogDocumentsRequest(search, category, language, sourceType, page, pageSize, userId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("GetPublishedCatalogDocuments")
        .WithSummary("Browse Published Catalog Documents")
        .WithDescription("Retrieves paginated published catalog documents with conjunctive filters and adoption status.")
        .Produces<GetPublishedCatalogDocumentsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/v1/catalog/documents/{id}
        learnerGroup.MapGet("/documents/{id:guid}", async (
            Guid id,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<GetPublishedCatalogDocumentByIdRequest, LearnerCatalogDetailDto> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            var result = await handler.ExecuteAsync(new GetPublishedCatalogDocumentByIdRequest(id, userId), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("GetPublishedCatalogDocumentById")
        .WithSummary("Get Published Catalog Document Details")
        .WithDescription("Fetches published catalog document metadata, table of contents, first-slice preview, and adoption status.")
        .Produces<LearnerCatalogDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /api/v1/catalog/documents/{id}/adopt
        learnerGroup.MapPost("/documents/{id:guid}/adopt", async (
            Guid id,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdoptCatalogDocumentRequest, DeepPace.Application.Interfaces.CatalogAdoptionResult> handler,
            CancellationToken ct) =>
        {
            var userId = GetUserIdFromClaims(userClaims);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var result = await handler.ExecuteAsync(new AdoptCatalogDocumentRequest(id, userId.Value), ct);
            return result.Match(
                success => success.AlreadyAdded
                    ? Results.Ok(success)
                    : Results.Created($"/api/v1/library/books/{success.BookId}", success),
                error => error.ToProblem()
            );
        })
        .WithName("AdoptCatalogDocument")
        .WithSummary("Adopt Catalog Document to Personal Library")
        .WithDescription("Creates an independent personal copy of the published catalog document or returns existing copy.")
        .Produces<CatalogAdoptionResult>(StatusCodes.Status201Created)
        .Produces<CatalogAdoptionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // 2. Administrator Catalog Endpoints (CatalogAdmin Policy Enforced)
        var adminGroup = app.MapGroup("/api/v1/admin/catalog")
            .WithTags("Curated Catalog Administration")
            .RequireAuthorization("CatalogAdmin");

        // GET /api/v1/admin/catalog/documents
        adminGroup.MapGet("/documents", async (
            [FromServices] IUseCase<AdminGetCatalogDocumentsRequest, AdminGetCatalogDocumentsResponse> handler,
            [FromQuery] string? search,
            [FromQuery] CatalogVisibility? visibility,
            [FromQuery] ProcessingStatus? status,
            [FromQuery] Category? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default) =>
        {
            var result = await handler.ExecuteAsync(new AdminGetCatalogDocumentsRequest(search, visibility, status, category, page, pageSize), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminGetCatalogDocuments")
        .WithSummary("Admin Browse Catalog Documents")
        .WithDescription("Retrieves all catalog documents (drafts, published, hidden) with processing status.")
        .Produces<AdminGetCatalogDocumentsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/upload-pdf
        adminGroup.MapPost("/documents/upload-pdf", async (
            IFormFile file,
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] Category category,
            [FromForm] string? authorOrSourceUrl,
            [FromForm] string? language,
            [FromForm] string? attributionText,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminUploadCatalogPdfRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;

            var tempFilePath = Path.GetTempFileName();
            await using (var fileStream = File.Create(tempFilePath))
            {
                await file.CopyToAsync(fileStream, ct);
            }

            var request = new AdminUploadCatalogPdfRequest(
                title,
                description,
                category,
                language ?? "en",
                authorOrSourceUrl,
                attributionText,
                tempFilePath,
                adminUserId);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Accepted($"/api/v1/admin/catalog/documents/{success.Id}", success),
                error => error.ToProblem()
            );
        })
        .DisableAntiforgery()
        .WithName("AdminUploadCatalogPdf")
        .WithSummary("Admin Upload Catalog PDF")
        .WithDescription("Uploads a PDF document draft to the catalog and initiates asynchronous extraction and processing.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/preview-url
        adminGroup.MapPost("/preview-url", async (
            [FromBody] AdminPreviewCatalogUrlApiRequest body,
            [FromServices] IUseCase<AdminPreviewCatalogUrlRequest, AdminPreviewCatalogUrlResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new AdminPreviewCatalogUrlRequest(body.Url), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminPreviewCatalogUrl")
        .WithSummary("Admin Preview Web URL Extraction")
        .WithDescription("Extracts readable markdown from a URL without creating a catalog document or enqueuing ingestion.")
        .Produces<AdminPreviewCatalogUrlResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/import-url
        adminGroup.MapPost("/documents/import-url", async (
            [FromBody] AdminImportCatalogUrlApiRequest body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminImportCatalogUrlRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;
            var request = new AdminImportCatalogUrlRequest(
                body.Url,
                body.Title,
                body.Description,
                body.Category,
                body.Language ?? "en",
                body.AuthorOrSourceUrl,
                body.AttributionText,
                adminUserId);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Accepted($"/api/v1/admin/catalog/documents/{success.Id}", success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminImportCatalogUrl")
        .WithSummary("Admin Import Catalog URL")
        .WithDescription("Validates, scrapes, chunks, and initiates AI processing for a web document into the catalog.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /api/v1/admin/catalog/documents/{id}
        adminGroup.MapGet("/documents/{id:guid}", async (
            Guid id,
            [FromServices] IUseCase<AdminGetCatalogDocumentByIdRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new AdminGetCatalogDocumentByIdRequest(id), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminGetCatalogDocumentById")
        .WithSummary("Admin Get Catalog Document")
        .WithDescription("Retrieves detailed catalog document information, revision metadata, and slices for review.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /api/v1/admin/catalog/documents/{id}/slices/{order:int}
        adminGroup.MapGet("/documents/{id:guid}/slices/{order:int}", async (
            Guid id,
            int order,
            [FromServices] IUseCase<AdminGetCatalogSliceRequest, AdminCatalogSliceDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new AdminGetCatalogSliceRequest(id, order), ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminGetCatalogSlice")
        .WithSummary("Admin Get Catalog Slice")
        .WithDescription("Retrieves a single catalog slice for administrative inspection and review.")
        .Produces<AdminCatalogSliceDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // PUT /api/v1/admin/catalog/documents/{id}
        adminGroup.MapPut("/documents/{id:guid}", async (
            Guid id,
            [FromBody] AdminUpdateCatalogMetadataApiRequest body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminUpdateCatalogMetadataRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;
            var request = new AdminUpdateCatalogMetadataRequest(
                id,
                body.Title,
                body.Description,
                body.Category,
                body.Language,
                body.AuthorOrSourceUrl,
                body.AttributionText,
                body.ExpectedVersion,
                adminUserId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminUpdateCatalogMetadata")
        .WithSummary("Admin Update Catalog Metadata")
        .WithDescription("Updates catalog document metadata and permission statements. Rejected if document is published.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/{id}/prepare
        adminGroup.MapPost("/documents/{id:guid}/prepare", async (
            Guid id,
            [FromBody] AdminPrepareCatalogApiRequest? body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminPrepareCatalogRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims);
            var request = new AdminPrepareCatalogRequest(
                id,
                body?.ReplacementTempFilePath,
                adminUserId);
            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Accepted($"/api/v1/admin/catalog/documents/{success.Id}", success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminPrepareCatalog")
        .WithSummary("Admin Prepare or Retry Catalog Ingestion")
        .WithDescription("Initiates or retries asynchronous processing for initial slices of a catalog draft.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/{id}/slices/{order:int}/review
        adminGroup.MapPost("/documents/{id:guid}/slices/{order:int}/review", async (
            Guid id,
            int order,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminReviewCatalogRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;
            var request = new AdminReviewCatalogRequest(id, adminUserId);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminReviewCatalogSlice")
        .WithSummary("Admin Confirm Slice Review")
        .WithDescription("Records administrator review for a catalog draft at the current revision.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/{id}/publish
        adminGroup.MapPost("/documents/{id:guid}/publish", async (
            Guid id,
            [FromBody] AdminPublishCatalogApiRequest body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminPublishCatalogDocumentRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;
            var request = new AdminPublishCatalogDocumentRequest(
                id,
                adminUserId,
                body.ExpectedRevision);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminPublishCatalogDocument")
        .WithSummary("Admin Publish Catalog Document")
        .WithDescription("Publishes a fully prepared and reviewed catalog document, making it available for discovery.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/catalog/documents/{id}/hide
        adminGroup.MapPost("/documents/{id:guid}/hide", async (
            Guid id,
            [FromBody] AdminHideCatalogApiRequest? body,
            ClaimsPrincipal userClaims,
            [FromServices] IUseCase<AdminHideCatalogDocumentRequest, AdminCatalogDocumentDto> handler,
            CancellationToken ct) =>
        {
            var adminUserId = GetUserIdFromClaims(userClaims) ?? Guid.Empty;
            var expectedVersion = body?.ExpectedConcurrencyVersion ?? 0;
            var request = new AdminHideCatalogDocumentRequest(id, adminUserId, expectedVersion);

            var result = await handler.ExecuteAsync(request, ct);
            return result.Match(
                success => Results.Ok(success),
                error => error.ToProblem()
            );
        })
        .WithName("AdminHideCatalogDocument")
        .WithSummary("Admin Hide Catalog Document")
        .WithDescription("Hides a published catalog document from learner discovery without affecting existing adoptions.")
        .Produces<AdminCatalogDocumentDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

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

public record AdminPreviewCatalogUrlApiRequest(string Url);
public record AdminImportCatalogUrlApiRequest(
    string Url,
    string Title,
    string Description,
    Category Category,
    string? AuthorOrSourceUrl,
    string Language,
    string? AttributionText);

public record AdminUpdateCatalogMetadataApiRequest(
    string Title,
    string Description,
    Category Category,
    string Language,
    string? AuthorOrSourceUrl,
    string? AttributionText,
    int ExpectedVersion);

public record AdminPrepareCatalogApiRequest(string? ReplacementTempFilePath = null);
public record AdminPublishCatalogApiRequest(int ExpectedRevision);
public record AdminHideCatalogApiRequest(int ExpectedConcurrencyVersion = 0);
