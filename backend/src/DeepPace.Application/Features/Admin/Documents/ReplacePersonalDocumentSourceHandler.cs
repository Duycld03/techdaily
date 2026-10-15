using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Documents;

public class ReplacePersonalDocumentSourceHandler : IUseCase<ReplacePersonalDocumentSourceCommand, SourceReplacementResponseDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IIngestionSourceStorage _sourceStorage;
    private readonly IIngestionQuotaService _quotaService;
    private readonly IDocumentMutationGuard _mutationGuard;

    public ReplacePersonalDocumentSourceHandler(
        IDeepPaceDbContext dbContext,
        IIngestionSourceStorage sourceStorage,
        IIngestionQuotaService quotaService,
        IDocumentMutationGuard mutationGuard)
    {
        _dbContext = dbContext;
        _sourceStorage = sourceStorage;
        _quotaService = quotaService;
        _mutationGuard = mutationGuard;
    }

    public async Task<Result<SourceReplacementResponseDto>> ExecuteAsync(
        ReplacePersonalDocumentSourceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RequestId))
        {
            return new Error("Command.RequestIdRequired", "A request ID is required for replayable source replacement.");
        }

        if (command.ByteLength <= 0)
        {
            return new Error("Source.InvalidLength", "Source stream byte length must be positive.");
        }

        return await _mutationGuard.ExecuteWithMutationGuardAsync<Result<SourceReplacementResponseDto>>(
            command.DocumentId,
            IngestionTargetScope.Personal,
            async () =>
            {
                var book = await _dbContext.DocumentBooks
                    .FirstOrDefaultAsync(b => b.Id == command.DocumentId, cancellationToken);

                // Cross-owner 404 security contract
                if (book == null || book.IsDeleted || book.CreatedByUserId != command.RequestingUserId)
                {
                    return new Error("Document.NotFound", "Personal document was not found or access is denied.");
                }

                var hasActiveAttempt = await _dbContext.IngestionAttempts
                    .AnyAsync(a => a.TargetScope == IngestionTargetScope.Personal &&
                                   a.TargetDocumentId == book.Id &&
                                   (a.State == IngestionAttemptState.Queued || a.State == IngestionAttemptState.Running),
                        cancellationToken);

                var hasLearningActivity = await _mutationGuard.HasLearningReferencesAsync(book.Id, cancellationToken);

                var eligibility = DocumentRetryEligibility.EvaluatePersonalSourceReplacement(
                    book, command.RequestingUserId, hasActiveAttempt, hasLearningActivity, command.ExpectedVersion);

                if (!eligibility.IsEligible)
                {
                    return new Error(eligibility.IneligibleReasonCode ?? "Document.Ineligible", eligibility.LocalizedMessage ?? "Document is not eligible for source replacement.");
                }

                // Explicit language verification for unknown legacy records
                var language = book.DeclaredLanguage;
                if (string.IsNullOrWhiteSpace(language))
                {
                    if (string.IsNullOrWhiteSpace(command.ExplicitLanguage))
                    {
                        return new Error("Document.LanguageRequired", "Explicit language selection is required for documents with unknown source language.");
                    }
                    language = command.ExplicitLanguage.Trim().ToLowerInvariant();
                }

                // Reserve storage capacity
                StorageReservation reservation;
                try
                {
                    reservation = await _quotaService.ReserveStorageAsync(command.RequestingUserId, command.ByteLength, cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    return new Error("Quota.Exceeded", ex.Message);
                }

                string storageIdentity;
                string integrityHash;
                try
                {
                    // Compute hash and store file
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

                var now = DateTimeOffset.UtcNow;
                var source = IngestionSource.Create(
                    IngestionSourceKind.Pdf,
                    storageIdentity,
                    command.ByteLength,
                    integrityHash,
                    command.RequestingUserId,
                    language,
                    now);

                _dbContext.IngestionSources.Add(source);

                var attempt = IngestionAttempt.CreateQueued(
                    IngestionTargetScope.Personal,
                    book.Id,
                    IngestionAttemptOrigin.UserReplacement,
                    command.RequestId,
                    generation: book.CurrentAttemptGeneration + 1,
                    sourceId: source.Id,
                    predecessorAttemptId: book.CurrentAttemptId,
                    initiatorUserId: command.RequestingUserId,
                    queuedAtUtc: now);

                _dbContext.IngestionAttempts.Add(attempt);

                book.Status = ProcessingStatus.Pending;
                book.SetDeclaredLanguage(language);
                book.AssignAttempt(attempt.Id);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return new SourceReplacementResponseDto(
                    attempt.Id,
                    book.Id,
                    attempt.State.ToString(),
                    book.IngestionControlVersion);
            },
            cancellationToken);
    }
}
