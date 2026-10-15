using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public class ReplaceCatalogDocumentSourceHandler : IUseCase<ReplaceCatalogDocumentSourceCommand, SourceReplacementResponseDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IIngestionSourceStorage _sourceStorage;
    private readonly IIngestionQuotaService _quotaService;
    private readonly IAdminPermissionResolver _permissionResolver;
    private readonly IDocumentMutationGuard _mutationGuard;
    private readonly IAdministrativeUnitOfWork? _unitOfWork;

    public ReplaceCatalogDocumentSourceHandler(
        IDeepPaceDbContext dbContext,
        IIngestionSourceStorage sourceStorage,
        IIngestionQuotaService quotaService,
        IAdminPermissionResolver permissionResolver,
        IDocumentMutationGuard mutationGuard,
        IAdministrativeUnitOfWork? unitOfWork = null)
    {
        _dbContext = dbContext;
        _sourceStorage = sourceStorage;
        _quotaService = quotaService;
        _permissionResolver = permissionResolver;
        _mutationGuard = mutationGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SourceReplacementResponseDto>> ExecuteAsync(
        ReplaceCatalogDocumentSourceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId))
        {
            return new Error("Command.RequestIdRequired", "A request ID is required for catalog source replacement.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return new Error("Command.ReasonRequired", "A reason is required to replace catalog document source.");
        }

        if (command.ByteLength <= 0)
        {
            return new Error("Source.InvalidLength", "Source stream byte length must be positive.");
        }

        var capabilitiesResult = await _permissionResolver.GetCapabilitiesAsync(command.ActorUserId, cancellationToken);
        if (capabilitiesResult.IsFailure)
        {
            return capabilitiesResult.Error;
        }

        var capabilities = capabilitiesResult.Value;
        if (!capabilities.CanManageCatalog)
        {
            return new Error("Authorization.Denied", "Catalog management permission is required to replace catalog document source.");
        }

        return await _mutationGuard.ExecuteWithMutationGuardAsync<Result<SourceReplacementResponseDto>>(
            command.DocumentId,
            IngestionTargetScope.Catalog,
            async () =>
            {
                var doc = await _dbContext.CatalogDocuments
                    .FirstOrDefaultAsync(c => c.Id == command.DocumentId, cancellationToken);

                if (doc == null || doc.IsDeleted)
                {
                    return new Error("Document.NotFound", "Catalog document was not found or is deleted.");
                }

                if (doc.Visibility == CatalogVisibility.Published)
                {
                    return new Error("Document.PublishedCatalog", "Cannot replace source of a published catalog document.");
                }

                if (doc.ProcessingStatus == ProcessingStatus.Ready)
                {
                    return new Error("Document.AlreadyReady", "Cannot replace source of an already ready catalog document.");
                }

                if (doc.ConcurrencyVersion != command.ExpectedVersion)
                {
                    return new Error("Document.VersionMismatch", "Catalog document concurrency version mismatch.");
                }

                var hasActiveAttempt = await _dbContext.IngestionAttempts
                    .AnyAsync(a => a.TargetScope == IngestionTargetScope.Catalog &&
                                   a.TargetDocumentId == doc.Id &&
                                   (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                        cancellationToken);

                if (hasActiveAttempt)
                {
                    return new Error("Document.ActiveAttemptInProgress", "An active ingestion attempt is currently in progress.");
                }

                // Reserve storage capacity under actor's quota account
                StorageReservation reservation;
                try
                {
                    reservation = await _quotaService.ReserveStorageAsync(command.ActorUserId, command.ByteLength, cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    return new Error("Quota.Exceeded", ex.Message);
                }

                string storageIdentity;
                string integrityHash;
                try
                {
                    using var memoryStream = new MemoryStream();
                    await command.SourceStream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    integrityHash = Convert.ToHexString(SHA256.HashData(memoryStream.ToArray()));
                    memoryStream.Position = 0;

                    storageIdentity = await _sourceStorage.StoreSourceAsync(memoryStream, command.FileName, cancellationToken);
                    await _quotaService.CommitReservationAsync(reservation.ReservationToken, command.ByteLength, cancellationToken);
                }
                catch (Exception)
                {
                    await _quotaService.ReleaseReservationAsync(reservation.ReservationToken, cancellationToken);
                    throw;
                }

                if (_unitOfWork != null)
                {
                    return await _unitOfWork.ExecutePrivilegedMutationAsync<SourceReplacementResponseDto>(
                        command.ActorUserId,
                        command.PresentedSessionVersion,
                        new[] { AdminPermission.ManageCatalog },
                        async () =>
                        {
                            var now = DateTimeOffset.UtcNow;
                            var source = IngestionSource.Create(
                                IngestionSourceKind.Pdf,
                                storageIdentity,
                                command.ByteLength,
                                integrityHash,
                                command.ActorUserId,
                                doc.Language,
                                now);

                            _dbContext.IngestionSources.Add(source);

                            var attempt = IngestionAttempt.CreateQueued(
                                IngestionTargetScope.Catalog,
                                doc.Id,
                                IngestionAttemptOrigin.AdminCatalogReplacement,
                                command.RequestId,
                                generation: doc.ProcessingGeneration + 1,
                                sourceId: source.Id,
                                predecessorAttemptId: doc.CurrentAttemptId,
                                initiatorUserId: command.ActorUserId,
                                queuedAtUtc: now);

                            _dbContext.IngestionAttempts.Add(attempt);

                            doc.ProcessingStatus = ProcessingStatus.Pending;
                            doc.AssignAttempt(attempt.Id);

                            var auditEvent = AdminAuditEvent.Create(
                                actorUserId: command.ActorUserId,
                                action: AdminAuditActions.DocumentSourceReplaced,
                                targetType: "CatalogDocument",
                                targetId: doc.Id.ToString(),
                                outcome: AdminAuditOutcome.Accepted,
                                reason: command.Reason.Trim(),
                                correlationId: command.RequestId,
                                changedFields: new[] { "ProcessingStatus", "CurrentAttemptId", "ConcurrencyVersion" });

                            _dbContext.AdminAuditEvents.Add(auditEvent);
                            await _dbContext.SaveChangesAsync(cancellationToken);

                            return Result<SourceReplacementResponseDto>.Success(new SourceReplacementResponseDto(
                                attempt.Id,
                                doc.Id,
                                attempt.State.ToString(),
                                doc.ConcurrencyVersion));
                        },
                        cancellationToken);
                }
                else
                {
                    var now = DateTimeOffset.UtcNow;
                    var source = IngestionSource.Create(
                        IngestionSourceKind.Pdf,
                        storageIdentity,
                        command.ByteLength,
                        integrityHash,
                        command.ActorUserId,
                        doc.Language,
                        now);

                    _dbContext.IngestionSources.Add(source);

                    var attempt = IngestionAttempt.CreateQueued(
                        IngestionTargetScope.Catalog,
                        doc.Id,
                        IngestionAttemptOrigin.AdminCatalogReplacement,
                        command.RequestId,
                        generation: doc.ProcessingGeneration + 1,
                        sourceId: source.Id,
                        predecessorAttemptId: doc.CurrentAttemptId,
                        initiatorUserId: command.ActorUserId,
                        queuedAtUtc: now);

                    _dbContext.IngestionAttempts.Add(attempt);

                    doc.ProcessingStatus = ProcessingStatus.Pending;
                    doc.AssignAttempt(attempt.Id);

                    var auditEvent = AdminAuditEvent.Create(
                        actorUserId: command.ActorUserId,
                        action: AdminAuditActions.DocumentSourceReplaced,
                        targetType: "CatalogDocument",
                        targetId: doc.Id.ToString(),
                        outcome: AdminAuditOutcome.Accepted,
                        reason: command.Reason.Trim(),
                        correlationId: command.RequestId,
                        changedFields: new[] { "ProcessingStatus", "CurrentAttemptId", "ConcurrencyVersion" });

                    _dbContext.AdminAuditEvents.Add(auditEvent);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return new SourceReplacementResponseDto(
                        attempt.Id,
                        doc.Id,
                        attempt.State.ToString(),
                        doc.ConcurrencyVersion);
                }
            },
            cancellationToken);
    }
}
