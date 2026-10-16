using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.Prepare;

public record AdminPrepareCatalogRequest(Guid Id, string? ReplacementTempFilePath = null, Guid? AdminUserId = null, int PresentedSessionVersion = 1);

public class AdminPrepareCatalogHandler(
    IDeepPaceDbContext dbContext,
    IPdfIngestionQueue pdfQueue,
    ICategoryAssignmentGuard categoryGuard,
    IAdministrativeUnitOfWork unitOfWork,
    IDocumentMutationGuard mutationGuard) : IUseCase<AdminPrepareCatalogRequest, AdminCatalogDocumentDto>
{
    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(AdminPrepareCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mutationGuard.ExecuteWithMutationGuardAsync(request.Id, IngestionTargetScope.Catalog,
            () => unitOfWork.ExecutePrivilegedMutationAsync(request.AdminUserId ?? Guid.Empty, request.PresentedSessionVersion,
                new[] { AdminPermission.ManageCatalog }, async () =>
                {
                    var candidate = await dbContext.CatalogDocuments.AsNoTracking().IgnoreAutoIncludes().FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
                    if (candidate == null || candidate.IsDeleted) return Error.Custom("Catalog.NotFound", "Catalog document not found.");
                    return await categoryGuard.ExecuteForCatalogAsync<AdminCatalogDocumentDto>(request.Id, candidate.CategoryId, async (doc, category) =>
                    {
                        if (doc.ProcessingStatus == ProcessingStatus.Processing && doc.ProcessingLeaseExpiresAt > DateTimeOffset.UtcNow)
                            return Error.Custom("Catalog.Busy", "Preparation is currently in progress. Wait until the lease expires.");
                        if (doc.SourceType == SourceType.PdfBook && (string.IsNullOrWhiteSpace(request.ReplacementTempFilePath) || !File.Exists(request.ReplacementTempFilePath)))
                            return Error.Custom("Catalog.MissingPdfFile", "Temporary upload file is unavailable. Upload the PDF again to prepare.");
                        if (dbContext is DbContext context) await context.Entry(doc).Collection(d => d.Slices).LoadAsync(cancellationToken);
                        doc.Category = category;
                        doc.ProcessingGeneration++;
                        doc.ProcessingStatus = ProcessingStatus.Pending;
                        doc.ProcessingStatusMessage = "Preparation explicitly enqueued by administrator";
                        doc.ProcessingErrorMessage = null;
                        doc.ProcessingLeaseExpiresAt = null;
                        await dbContext.AdminAuditEvents.AddAsync(AdminAuditEvent.Create(request.AdminUserId!.Value,
                            AdminAuditActions.CatalogPrepare, "CatalogDocument", doc.Id.ToString(), AdminAuditOutcome.Accepted,
                            correlationId: doc.Id.ToString(), changedFields: new[] { "ProcessingStatus", "ProcessingGeneration" },
                            statePayload: JsonSerializer.Serialize(new { doc.ProcessingStatus, doc.ProcessingGeneration })), cancellationToken);
                        return doc.ToAdminDto();
                    }, cancellationToken);
                }, cancellationToken), cancellationToken);

        if (!result.IsSuccess)
        {
            if (request.AdminUserId is { } actor && actor != Guid.Empty && result.Error.Code is "Catalog.Busy" or "Catalog.MissingPdfFile")
                await unitOfWork.AppendCleanAuditEventAsync(AdminAuditEvent.Create(actor, AdminAuditActions.CatalogPrepare,
                    "CatalogDocument", request.Id.ToString(), AdminAuditOutcome.Rejected, errorCode: result.Error.Code), cancellationToken);
            return result;
        }
        var document = result.Value;
        if (document.SourceType == SourceType.PdfBook && !string.IsNullOrWhiteSpace(request.ReplacementTempFilePath))
        {
            try
            {
                await pdfQueue.EnqueueAsync(new PdfIngestJob(document.Id, request.ReplacementTempFilePath, document.Title,
                    document.CategoryId, document.Language, IngestionDestinationType.CatalogDocument, document.ProcessingGeneration), cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                await unitOfWork.AppendCleanAuditEventAsync(AdminAuditEvent.Create(request.AdminUserId!.Value,
                    AdminAuditActions.CatalogDispatchFailed, "CatalogDocument", document.Id.ToString(), AdminAuditOutcome.Failed,
                    errorCode: "DISPATCH_FAILED", correlationId: document.Id.ToString()), cancellationToken);
            }
        }
        return result;
    }
}
