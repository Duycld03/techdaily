using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.Prepare;

public record AdminPrepareCatalogRequest(
    Guid Id,
    string? ReplacementTempFilePath = null,
    Guid? AdminUserId = null);

public class AdminPrepareCatalogHandler : IUseCase<AdminPrepareCatalogRequest, AdminCatalogDocumentDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IPdfIngestionQueue _pdfQueue;

    public AdminPrepareCatalogHandler(
        IDeepPaceDbContext dbContext,
        IPdfIngestionQueue pdfQueue)
    {
        _dbContext = dbContext;
        _pdfQueue = pdfQueue;
    }

    public async Task<Result<AdminCatalogDocumentDto>> ExecuteAsync(
        AdminPrepareCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.CatalogDocuments
            .Include(d => d.Slices)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doc == null || doc.IsDeleted)
        {
            return Error.Custom("Catalog.NotFound", "Catalog document not found.");
        }

        // Check if currently busy processing with active lease
        if (doc.ProcessingStatus == ProcessingStatus.Processing
            && doc.ProcessingLeaseExpiresAt.HasValue
            && doc.ProcessingLeaseExpiresAt.Value > DateTimeOffset.UtcNow)
        {
            if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
            {
                var rejectAudit = AdminAuditEvent.Create(
                    actorUserId: request.AdminUserId.Value,
                    action: AdminAuditActions.CatalogPrepare,
                    targetType: "CatalogDocument",
                    targetId: doc.Id.ToString(),
                    outcome: AdminAuditOutcome.Rejected,
                    errorCode: "Catalog.Busy");
                await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return Error.Custom("Catalog.Busy", "Preparation is currently in progress for this document. Please wait or retry after lease expires.");
        }

        if (doc.SourceType == SourceType.PdfBook)
        {
            if (string.IsNullOrWhiteSpace(request.ReplacementTempFilePath) || !File.Exists(request.ReplacementTempFilePath))
            {
                if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
                {
                    var rejectAudit = AdminAuditEvent.Create(
                        actorUserId: request.AdminUserId.Value,
                        action: AdminAuditActions.CatalogPrepare,
                        targetType: "CatalogDocument",
                        targetId: doc.Id.ToString(),
                        outcome: AdminAuditOutcome.Rejected,
                        errorCode: "Catalog.MissingPdfFile");
                    await _dbContext.AdminAuditEvents.AddAsync(rejectAudit, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                return Error.Custom("Catalog.MissingPdfFile",
                    "Temporary upload file is no longer available. Please upload the PDF file again to prepare.");
            }
        }

        // Increment processing generation to invalidate any stale worker
        doc.ProcessingGeneration++;
        doc.ProcessingStatus = ProcessingStatus.Pending;
        doc.ProcessingStatusMessage = "Preparation explicitly enqueued by administrator";
        doc.ProcessingErrorMessage = null;
        doc.ProcessingLeaseExpiresAt = null;

        if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
        {
            var audit = AdminAuditEvent.Create(
                actorUserId: request.AdminUserId.Value,
                action: AdminAuditActions.CatalogPrepare,
                targetType: "CatalogDocument",
                targetId: doc.Id.ToString(),
                outcome: AdminAuditOutcome.Accepted,
                correlationId: doc.Id.ToString(),
                changedFields: new[] { "ProcessingStatus", "ProcessingGeneration" },
                statePayload: JsonSerializer.Serialize(new { ProcessingStatus = doc.ProcessingStatus.ToString(), ProcessingGeneration = doc.ProcessingGeneration }));
            await _dbContext.AdminAuditEvents.AddAsync(audit, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (doc.SourceType == SourceType.PdfBook && !string.IsNullOrWhiteSpace(request.ReplacementTempFilePath))
        {
            try
            {
                await _pdfQueue.EnqueueAsync(new PdfIngestJob(
                    TargetId: doc.Id,
                    TempFilePath: request.ReplacementTempFilePath,
                    DocumentTitle: doc.Title,
                    Category: doc.Category,
                    Language: doc.Language,
                    DestinationType: IngestionDestinationType.CatalogDocument,
                    Generation: doc.ProcessingGeneration), cancellationToken);
            }
            catch (Exception)
            {
                if (request.AdminUserId.HasValue && request.AdminUserId.Value != Guid.Empty)
                {
                    var failAudit = AdminAuditEvent.Create(
                        actorUserId: request.AdminUserId.Value,
                        action: AdminAuditActions.CatalogDispatchFailed,
                        targetType: "CatalogDocument",
                        targetId: doc.Id.ToString(),
                        outcome: AdminAuditOutcome.Failed,
                        errorCode: "DISPATCH_FAILED",
                        correlationId: doc.Id.ToString());
                    await _dbContext.AdminAuditEvents.AddAsync(failAudit, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return doc.ToAdminDto();
}
}
