using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Features.Admin.Documents;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class AdminDocumentOperationsHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly IConfiguration _config;
    private readonly AdministrativeUnitOfWork _uow;
    private readonly AdminPermissionResolver _permissionResolver;
    private readonly DocumentMutationGuard _mutationGuard;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _learnerUserId = Guid.NewGuid();

    public AdminDocumentOperationsHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
        _config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _uow = new AdministrativeUnitOfWork(_db);
        _permissionResolver = new AdminPermissionResolver(_db);
        _mutationGuard = new DocumentMutationGuard(_db);

        // Seed Admin and Learner users
        var admin = new User
        {
            Id = _adminUserId,
            Email = "admin@techdaily.local",
            Name = "Admin User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId
        };

        var learner = new User
        {
            Id = _learnerUserId,
            Email = "learner@techdaily.local",
            Name = "Learner User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.LearnerRoleId
        };

        _db.Users.AddRange(admin, learner);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    #region Task 3.2: Inventory, Detail & Attempts Handlers

    [Fact]
    public async Task Inventory_ShouldReturnBothPersonalAndCatalog_ExcludingDeleted()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Personal Book Active",
            CreatedByUserId = _learnerUserId,
            Status = ProcessingStatus.Ready,
            IsDeleted = false
        };

        var deletedBook = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Personal Book Deleted",
            CreatedByUserId = _learnerUserId,
            Status = ProcessingStatus.Failed,
            IsDeleted = true
        };

        var catalogDoc = new CatalogDocument
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Catalog Doc Active",
            ProcessingStatus = ProcessingStatus.Ready,
            IsDeleted = false
        };

        _db.DocumentBooks.AddRange(book, deletedBook);
        _db.CatalogDocuments.Add(catalogDoc);
        await _db.SaveChangesAsync();

        var handler = new AdminGetDocumentInventoryHandler(_db);
        var result = await handler.ExecuteAsync(new AdminDocumentInventoryQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().Contain(i => i.Id == book.Id && i.Scope == "Personal");
        result.Value.Items.Should().Contain(i => i.Id == catalogDoc.Id && i.Scope == "Catalog");
        result.Value.Items.Should().NotContain(i => i.Id == deletedBook.Id);
    }

    [Fact]
    public async Task Detail_PersonalBook_ShouldSanitizeUrlAndComputeEligibility()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Algorithms",
            CreatedByUserId = _learnerUserId,
            Status = ProcessingStatus.Failed,
            AuthorOrSourceUrl = "https://user:secret@example.com/source.pdf?token=secret#section1",
            IngestionControlVersion = 1,
            IsDeleted = false
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/file.pdf", 5000, "hash", _learnerUserId, "en", DateTimeOffset.UtcNow);

        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            book.Id,
            IngestionAttemptOrigin.UserUpload,
            "req-1",
            generation: 1,
            sourceId: source.Id);
        attempt.Claim("token", DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow);
        attempt.Fail("token", "ExtractError", "Failed extraction", DateTimeOffset.UtcNow);

        book.AssignAttempt(attempt.Id);

        _db.DocumentBooks.Add(book);
        _db.IngestionSources.Add(source);
        _db.IngestionAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var handler = new AdminGetDocumentDetailHandler(_db, _mutationGuard);
        var result = await handler.ExecuteAsync(new AdminGetDocumentDetailRequest("personal", book.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(book.Id);
        result.Value.Scope.Should().Be("Personal");
        result.Value.IsRetryEligible.Should().BeTrue();
        // Sanitized URL check: user credentials, query string, fragment removed
        result.Value.SanitizedSourceUrl.Should().Be("https://example.com/source.pdf");
    }

    [Fact]
    public async Task Attempts_ShouldRespectTimeRangeAndSortDeterministically()
    {
        var bookId = Guid.NewGuid();
        var t1 = new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 10, 15, 11, 0, 0, TimeSpan.Zero);

        var a1 = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, bookId, IngestionAttemptOrigin.UserUpload, "r1", 1, queuedAtUtc: t1);
        a1.Claim("tok", t1.AddMinutes(15), t1);
        a1.Fail("tok", "InitFailed", "Failed attempt", t1.AddMinutes(10));
        var a2 = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, bookId, IngestionAttemptOrigin.AdminRetry, "r2", 2, queuedAtUtc: t2);
        _db.IngestionAttempts.AddRange(a1, a2);
        await _db.SaveChangesAsync();

        var handler = new AdminGetDocumentAttemptsHandler(_db);

        // Valid range query
        var query = new AdminGetDocumentAttemptsRequest(
            "personal",
            bookId,
            FromUtc: t1,
            ToUtc: t2.AddMinutes(30));

        var result = await handler.ExecuteAsync(query);
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        // Ordering: QueuedAtUtc descending -> a2 first, then a1
        result.Value.Items[0].AttemptId.Should().Be(a2.Id);
        result.Value.Items[1].AttemptId.Should().Be(a1.Id);

        // Invalid range query (From >= To)
        var invalidQuery = new AdminGetDocumentAttemptsRequest(
            "personal",
            bookId,
            FromUtc: t2,
            ToUtc: t1);

        var invalidResult = await handler.ExecuteAsync(invalidQuery);
        invalidResult.IsFailure.Should().BeTrue();
        invalidResult.Error.Code.Should().Be("Document.InvalidTimeRange");
    }

    #endregion

    #region Task 3.3: AdminRetryDocumentHandler

    [Fact]
    public async Task Retry_EligiblePersonalBook_ShouldCreateQueuedAttemptAndAudit()
    {
        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Kafka Internals",
            CreatedByUserId = _learnerUserId,
            Status = ProcessingStatus.Failed,
            IngestionControlVersion = 2,
            IsDeleted = false
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/kafka.pdf", 4096, "hash-123", _learnerUserId, "en", DateTimeOffset.UtcNow);

        var failedAttempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            book.Id,
            IngestionAttemptOrigin.UserUpload,
            "req-init",
            generation: 1,
            sourceId: source.Id);
        failedAttempt.Claim("tok", DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow);
        failedAttempt.Fail("tok", "CorruptPdf", "Corrupted file", DateTimeOffset.UtcNow);

        book.AssignAttempt(failedAttempt.Id);

        _db.DocumentBooks.Add(book);
        _db.IngestionSources.Add(source);
        _db.IngestionAttempts.Add(failedAttempt);
        await _db.SaveChangesAsync();

        var handler = new AdminRetryDocumentHandler(
            _db,
            _uow,
            _permissionResolver,
            _mutationGuard);

        var command = new AdminRetryDocumentCommand(
            Scope: "personal",
            DocumentId: book.Id,
            AttemptId: failedAttempt.Id,
            ExpectedVersion: book.IngestionControlVersion,
            RequestId: "retry-req-1",
            Reason: "Retry after transient network glitch",
            ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetDocumentId.Should().Be(book.Id);
        result.Value.State.Should().Be("Queued");
        result.Value.ControlVersion.Should().Be(book.IngestionControlVersion);

        // Verify audit event persisted
        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.CorrelationId == "retry-req-1");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.DocumentIngestionRetryRequested);
        audit.TargetId.Should().Be(book.Id.ToString());
    }

    [Fact]
    public async Task Retry_PublishedCatalog_ShouldBeRejected()
    {
        var doc = new CatalogDocument
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Published Guide",
            Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Failed,
            ConcurrencyVersion = 1,
            IsDeleted = false
        };

        var source = IngestionSource.Create(
            IngestionSourceKind.Pdf, "sources/guide.pdf", 4096, "hash-123", _adminUserId, "en", DateTimeOffset.UtcNow);

        var failedAttempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Catalog, doc.Id, IngestionAttemptOrigin.AdminCatalogUpload, "req-1", 1, sourceId: source.Id);
        failedAttempt.Claim("tok", DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow);
        failedAttempt.Fail("tok", "Error", "Error", DateTimeOffset.UtcNow);

        _db.CatalogDocuments.Add(doc);
        _db.IngestionSources.Add(source);
        _db.IngestionAttempts.Add(failedAttempt);
        await _db.SaveChangesAsync();

        var handler = new AdminRetryDocumentHandler(
            _db,
            _uow,
            _permissionResolver,
            _mutationGuard);

        var command = new AdminRetryDocumentCommand(
            Scope: "catalog",
            DocumentId: doc.Id,
            AttemptId: failedAttempt.Id,
            ExpectedVersion: 1,
            RequestId: "retry-cat-1",
            Reason: "Retry catalog",
            ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DocumentRetryEligibility.ReasonPublishedCatalog);
    }

    [Fact]
    public async Task Retry_Replay_ConflictingParameters_ShouldReturn409()
    {
        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            Guid.NewGuid(),
            IngestionAttemptOrigin.AdminRetry,
            "existing-request-id",
            generation: 1,
            commandFingerprint: "fingerprint-aaa");

        _db.IngestionAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var handler = new AdminRetryDocumentHandler(
            _db,
            _uow,
            _permissionResolver,
            _mutationGuard);

        var command = new AdminRetryDocumentCommand(
            Scope: "personal",
            DocumentId: Guid.NewGuid(),
            AttemptId: Guid.NewGuid(),
            ExpectedVersion: 1,
            RequestId: "existing-request-id", // Same request ID
            Reason: "Different reason here",   // Produces different fingerprint
            ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Command.RequestIdConflict");
    }


    #region Task 3.6: Source Replacement Handlers

    [Fact]
    public async Task ReplacePersonalSource_ValidOwner_ShouldCreateQueuedAttemptAndAdvanceControlVersion()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"source_replace_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var storageConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ingestion:SourceStoragePath"] = tempDir
        }).Build();
        var storage = new LocalFileIngestionSourceStorage(storageConfig, Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalFileIngestionSourceStorage>.Instance);
        var quotaService = new IngestionQuotaService(_db, _config);

        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Rust Concurrency",
            CreatedByUserId = _learnerUserId,
            Status = ProcessingStatus.Failed,
            DeclaredLanguage = "en",
            IngestionControlVersion = 2,
            IsDeleted = false
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        var handler = new ReplacePersonalDocumentSourceHandler(_db, storage, quotaService, _mutationGuard);

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 mock pdf content");
        using var stream = new MemoryStream(pdfBytes);

        var command = new ReplacePersonalDocumentSourceCommand(
            DocumentId: book.Id,
            RequestingUserId: _learnerUserId,
            ExpectedVersion: 2,
            RequestId: "replace-req-1",
            ExplicitLanguage: null,
            SourceStream: stream,
            FileName: "rust.pdf",
            ByteLength: pdfBytes.Length);

        var result = await handler.ExecuteAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be(book.Id);
        result.Value.State.Should().Be("Queued");
        result.Value.ResultingVersion.Should().Be(3);

        // Verify attempt and source records
        var attempt = await _db.IngestionAttempts.FirstOrDefaultAsync(a => a.Id == result.Value.AttemptId);
        attempt.Should().NotBeNull();
        attempt!.Origin.Should().Be(IngestionAttemptOrigin.UserReplacement);
        attempt.SourceId.Should().NotBeNull();

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task ReplacePersonalSource_CrossOwner_ShouldReturn404()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"source_cross_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var storageConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ingestion:SourceStoragePath"] = tempDir
        }).Build();
        var storage = new LocalFileIngestionSourceStorage(storageConfig, Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalFileIngestionSourceStorage>.Instance);
        var quotaService = new IngestionQuotaService(_db, _config);

        var book = new DocumentBook
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Secrets",
            CreatedByUserId = _learnerUserId, // Owned by learner
            Status = ProcessingStatus.Failed,
            IngestionControlVersion = 1,
            IsDeleted = false
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        var handler = new ReplacePersonalDocumentSourceHandler(_db, storage, quotaService, _mutationGuard);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new ReplacePersonalDocumentSourceCommand(
            DocumentId: book.Id,
            RequestingUserId: _adminUserId, // Different user attempting replacement
            ExpectedVersion: 1,
            RequestId: "cross-owner-req",
            ExplicitLanguage: null,
            SourceStream: stream,
            FileName: "file.pdf",
            ByteLength: 3);

        var result = await handler.ExecuteAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound"); // Cross-owner 404 security contract

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task ReplaceCatalogSource_ValidAdmin_ShouldCreateQueuedAttemptAndAudit()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"cat_replace_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var storageConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ingestion:SourceStoragePath"] = tempDir
        }).Build();
        var storage = new LocalFileIngestionSourceStorage(storageConfig, Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalFileIngestionSourceStorage>.Instance);
        var quotaService = new IngestionQuotaService(_db, _config);

        var doc = new CatalogDocument
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Id = Guid.NewGuid(),
            Title = "Catalog Microservices",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Failed,
            ConcurrencyVersion = 2,
            IsDeleted = false
        };
        _db.CatalogDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var handler = new ReplaceCatalogDocumentSourceHandler(_db, storage, quotaService, _permissionResolver, _mutationGuard);

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 mock catalog pdf");
        using var stream = new MemoryStream(pdfBytes);

        var command = new ReplaceCatalogDocumentSourceCommand(
            DocumentId: doc.Id,
            ActorUserId: _adminUserId,
            ExpectedVersion: 2,
            RequestId: "cat-replace-1",
            Reason: "Provide corrected PDF layout",
            SourceStream: stream,
            FileName: "microservices.pdf",
            ByteLength: pdfBytes.Length);

        var result = await handler.ExecuteAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentId.Should().Be(doc.Id);
        result.Value.State.Should().Be("Queued");
        result.Value.ResultingVersion.Should().Be(3);

        // Verify audit event persisted
        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.CorrelationId == "cat-replace-1");
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.DocumentSourceReplaced);
        audit.TargetId.Should().Be(doc.Id.ToString());

        Directory.Delete(tempDir, true);
    }

    #endregion
    #endregion
}
