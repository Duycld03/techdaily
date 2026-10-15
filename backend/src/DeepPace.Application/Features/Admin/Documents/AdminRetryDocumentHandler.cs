using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public class AdminRetryDocumentHandler : IUseCase<AdminRetryDocumentCommand, AdminRetryDocumentResponseDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IAdministrativeUnitOfWork _unitOfWork;
    private readonly IAdminPermissionResolver _permissionResolver;
    private readonly IDocumentMutationGuard _mutationGuard;

    public AdminRetryDocumentHandler(
        IDeepPaceDbContext dbContext,
        IAdministrativeUnitOfWork unitOfWork,
        IAdminPermissionResolver permissionResolver,
        IDocumentMutationGuard mutationGuard)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _permissionResolver = permissionResolver;
        _mutationGuard = mutationGuard;
    }

    public async Task<Result<AdminRetryDocumentResponseDto>> ExecuteAsync(
        AdminRetryDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new Error("Command.ReasonRequired", "A reason is required to retry document ingestion.");
        }

        var trimmedReason = command.Reason.Trim();
        if (trimmedReason.Length > 500)
        {
            return new Error("Command.ReasonTooLong", "Retry reason cannot exceed 500 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.RequestId))
        {
            return new Error("Command.RequestIdRequired", "A request ID is required for replayable retry commands.");
        }

        var scopeStr = command.Scope.Trim().ToLowerInvariant();
        if (scopeStr != "personal" && scopeStr != "catalog")
        {
            return new Error("Document.InvalidScope", $"Unsupported scope '{command.Scope}'. Must be 'personal' or 'catalog'.");
        }

        var targetScope = scopeStr == "personal" ? IngestionTargetScope.Personal : IngestionTargetScope.Catalog;

        // Permissions check
        var capabilities = await _permissionResolver.GetCapabilitiesAsync(command.ActorUserId, cancellationToken);
        if (!capabilities.CanManageDocumentOperations)
        {
            return new Error("Authorization.Denied", "Actor lacks permission to manage document operations.");
        }

        if (targetScope == IngestionTargetScope.Catalog && !capabilities.CanManageCatalog)
        {
            return new Error("Authorization.Denied", "Catalog document retry requires catalog management permission.");
        }

        var canonicalData = $"{command.ActorUserId}:{scopeStr}:{command.DocumentId}:{command.AttemptId}:{command.ExpectedVersion}:{trimmedReason}:{command.RequestId.Trim()}";
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalData)));

        // Check for existing attempt with same RequestId (replay check)
        var existingWithRequestId = await _dbContext.IngestionAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.RequestId == command.RequestId.Trim(), cancellationToken);

        if (existingWithRequestId != null)
        {
            if (existingWithRequestId.CommandFingerprint == fingerprint)
            {
                // Identical replay: return existing attempt details safely
                int currentVersion = 1;
                if (targetScope == IngestionTargetScope.Personal)
                {
                    var existingBook = await _dbContext.DocumentBooks.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == command.DocumentId, cancellationToken);
                    currentVersion = existingBook?.IngestionControlVersion ?? 1;
                }
                else
                {
                    var existingCat = await _dbContext.CatalogDocuments.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == command.DocumentId, cancellationToken);
                    currentVersion = existingCat?.ConcurrencyVersion ?? 1;
                }

                return new AdminRetryDocumentResponseDto(
                    existingWithRequestId.Id,
                    existingWithRequestId.TargetDocumentId,
                    existingWithRequestId.State.ToString(),
                    currentVersion);
            }

            // Conflicting replay with different parameters
            return new Error("Command.RequestIdConflict", "Request ID was previously submitted with different command parameters.");
        }

        // Execute retry within mutation guard
        return await _mutationGuard.ExecuteWithMutationGuardAsync<Result<AdminRetryDocumentResponseDto>>(
            command.DocumentId,
            targetScope,
            async () =>
            {
                var now = DateTimeOffset.UtcNow;
                        // Retrieve predecessor attempt
                        var predecessor = await _dbContext.IngestionAttempts
                            .FirstOrDefaultAsync(a => a.Id == command.AttemptId && a.TargetDocumentId == command.DocumentId, cancellationToken);

                        if (predecessor == null)
                        {
                            return new Error("Attempt.NotFound", "The specified predecessor attempt was not found.");
                        }

                        if (predecessor.State != IngestionAttemptState.Failed && predecessor.State != IngestionAttemptState.Interrupted)
                        {
                            return new Error("Attempt.NotRetryable", $"Attempt is in state '{predecessor.State}', expected Failed or Interrupted.");
                        }
                        IngestionSource? source = null;
                        if (predecessor.SourceId.HasValue)
                        {
                            source = await _dbContext.IngestionSources
                                .FirstOrDefaultAsync(s => s.Id == predecessor.SourceId.Value, cancellationToken);
                        }

                        int resultingVersion = 1;
                        long generation = 1;

                        if (targetScope == IngestionTargetScope.Personal)
                        {
                            var book = await _dbContext.DocumentBooks
                                .FirstOrDefaultAsync(b => b.Id == command.DocumentId, cancellationToken);

                            if (book == null || book.IsDeleted)
                            {
                                return new Error("Document.NotFound", "Document was not found or is deleted.");
                            }
                            var hasActiveAttempt = await _dbContext.IngestionAttempts
                                .AnyAsync(a => a.TargetScope == IngestionTargetScope.Personal &&
                                               a.TargetDocumentId == book.Id &&
                                               (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                                    cancellationToken);

                            var hasLearningActivity = await _mutationGuard.HasLearningReferencesAsync(book.Id, cancellationToken);

                            var eligibility = DocumentRetryEligibility.EvaluatePersonalRetry(
                                book, source, hasActiveAttempt, hasLearningActivity, now, command.ExpectedVersion);

                            if (!eligibility.IsEligible)
                            {
                                return new Error(eligibility.IneligibleReasonCode ?? "Document.Ineligible", eligibility.LocalizedMessage ?? "Document is not eligible for retry.");
                            }
                            var newAttempt = IngestionAttempt.CreateQueued(
                                IngestionTargetScope.Personal,
                                book.Id,
                                IngestionAttemptOrigin.AdminRetry,
                                command.RequestId,
                                generation: book.CurrentAttemptGeneration + 1,
                                sourceId: source?.Id,
                                predecessorAttemptId: predecessor.Id,
                                initiatorUserId: command.ActorUserId,
                                commandFingerprint: fingerprint,
                                queuedAtUtc: now);

                            book.Status = ProcessingStatus.Pending;
                            book.AssignAttempt(newAttempt.Id);
                            _dbContext.IngestionAttempts.Add(newAttempt);

                            resultingVersion = book.IngestionControlVersion;
                            generation = book.CurrentAttemptGeneration;

                            var dto = new AdminRetryDocumentResponseDto(newAttempt.Id, book.Id, newAttempt.State.ToString(), resultingVersion);
                            var auditEvent = AdminAuditEvent.Create(
                                actorUserId: command.ActorUserId,
                                action: AdminAuditActions.DocumentIngestionRetryRequested,
                                targetType: "PersonalDocument",
                                targetId: book.Id.ToString(),
                                outcome: AdminAuditOutcome.Accepted,
                                reason: trimmedReason,
                                correlationId: command.RequestId,
                                changedFields: new[] { "Status", "CurrentAttemptId", "IngestionControlVersion" });

                            _dbContext.AdminAuditEvents.Add(auditEvent);
                            await _dbContext.SaveChangesAsync(cancellationToken);

                            return dto;
                        }
                        else
                        {
                            var catalogDoc = await _dbContext.CatalogDocuments
                                .FirstOrDefaultAsync(c => c.Id == command.DocumentId, cancellationToken);

                            if (catalogDoc == null || catalogDoc.IsDeleted)
                            {
                                return new Error("Document.NotFound", "Catalog document was not found or is deleted.");
                            }
                            var hasActiveAttempt = await _dbContext.IngestionAttempts
                                .AnyAsync(a => a.TargetScope == IngestionTargetScope.Catalog &&
                                               a.TargetDocumentId == catalogDoc.Id &&
                                               (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                                    cancellationToken);

                            var eligibility = DocumentRetryEligibility.EvaluateCatalogRetry(
                                catalogDoc, source, hasActiveAttempt, now, command.ExpectedVersion);

                            if (!eligibility.IsEligible)
                            {
                                return new Error(eligibility.IneligibleReasonCode ?? "Document.Ineligible", eligibility.LocalizedMessage ?? "Catalog document is not eligible for retry.");
                            }
                            var newAttempt = IngestionAttempt.CreateQueued(
                                IngestionTargetScope.Catalog,
                                catalogDoc.Id,
                                IngestionAttemptOrigin.AdminRetry,
                                command.RequestId,
                                generation: catalogDoc.ProcessingGeneration + 1,
                                sourceId: source?.Id,
                                predecessorAttemptId: predecessor.Id,
                                initiatorUserId: command.ActorUserId,
                                commandFingerprint: fingerprint,
                                queuedAtUtc: now);

                            catalogDoc.ProcessingStatus = ProcessingStatus.Pending;
                            catalogDoc.AssignAttempt(newAttempt.Id);
                            _dbContext.IngestionAttempts.Add(newAttempt);

                            resultingVersion = catalogDoc.ConcurrencyVersion;
                            generation = catalogDoc.ProcessingGeneration;

                            var dto = new AdminRetryDocumentResponseDto(newAttempt.Id, catalogDoc.Id, newAttempt.State.ToString(), resultingVersion);
                            var auditEvent = AdminAuditEvent.Create(
                                actorUserId: command.ActorUserId,
                                action: AdminAuditActions.DocumentIngestionRetryRequested,
                                targetType: "CatalogDocument",
                                targetId: catalogDoc.Id.ToString(),
                                outcome: AdminAuditOutcome.Accepted,
                                reason: trimmedReason,
                                correlationId: command.RequestId,
                                changedFields: new[] { "ProcessingStatus", "CurrentAttemptId", "ConcurrencyVersion" });

                            _dbContext.AdminAuditEvents.Add(auditEvent);
                            await _dbContext.SaveChangesAsync(cancellationToken);

                            return dto;
                        }
            },
            cancellationToken);
    }
}
