using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Catalog.Admin.Prepare;

public record AdminPrepareCatalogRequest(
    Guid Id,
    string? ReplacementTempFilePath = null);

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
            return Error.Custom("Catalog.Busy", "Preparation is currently in progress for this document. Please wait or retry after lease expires.");
        }

        // Increment processing generation to invalidate any stale worker
        doc.ProcessingGeneration++;
        doc.ProcessingStatus = ProcessingStatus.Pending;
        doc.ProcessingStatusMessage = "Preparation explicitly enqueued by administrator";
        doc.ProcessingErrorMessage = null;
        doc.ProcessingLeaseExpiresAt = null;

        if (doc.SourceType == SourceType.PdfBook)
        {
            if (string.IsNullOrWhiteSpace(request.ReplacementTempFilePath) || !File.Exists(request.ReplacementTempFilePath))
            {
                return Error.Custom("Catalog.MissingPdfFile",
                    "Temporary upload file is no longer available. Please upload the PDF file again to prepare.");
            }

            await _pdfQueue.EnqueueAsync(new PdfIngestJob(
                TargetId: doc.Id,
                TempFilePath: request.ReplacementTempFilePath,
                DocumentTitle: doc.Title,
                Category: doc.Category,
                Language: doc.Language,
                DestinationType: IngestionDestinationType.CatalogDocument,
                Generation: doc.ProcessingGeneration), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return doc.ToAdminDto();
    }
}
