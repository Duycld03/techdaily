using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.Admin.Hide;
using DeepPace.Application.Features.Catalog.Admin.Prepare;
using DeepPace.Application.Features.Catalog.Admin.PreviewUrl;
using DeepPace.Application.Features.Catalog.Admin.Publish;
using DeepPace.Application.Features.Catalog.Admin.Review;
using DeepPace.Application.Features.Catalog.Admin.UpdateMetadata;
using DeepPace.Application.Features.Catalog.Admin.UploadPdf;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class CatalogMutationAuditIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public CatalogMutationAuditIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
        _db.Users.Add(new User
        {
            Id = _adminUserId,
            Email = "catalog-audit-admin@example.test",
            Name = "Catalog administrator",
            RoleId = RoleDefaults.AdminRoleId,
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private class ThrowingPdfQueue : IPdfIngestionQueue
    {
        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated queue worker failure");
        }

        public async IAsyncEnumerable<PdfIngestJob> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }
    }

    private class DummyPdfQueue : IPdfIngestionQueue
    {
        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<PdfIngestJob> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }
    }

    [Fact]
    public async Task PreviewUrl_ShouldNotCreateAnyAuditEvent()
    {
        var crawlerMock = new MockCrawler();
        var handler = new AdminPreviewCatalogUrlHandler(crawlerMock, new AdminPreviewCatalogUrlValidator());
        var result = await handler.ExecuteAsync(new AdminPreviewCatalogUrlRequest("https://example.com/clean-code"));

        result.IsSuccess.Should().BeTrue();
        var auditEvents = await _db.AdminAuditEvents.ToListAsync();
        auditEvents.Should().BeEmpty("Reads and previews must not create administrative mutation history");
    }

    [Fact]
    public async Task UpdateMetadata_Success_ShouldCreateSucceededAuditEvent()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Description = "Desc",
            CategoryId = CategoryDefaults.SystemDesignId,
            Language = "en",
            SourceType = SourceType.WebDocUrl,
            Visibility = CatalogVisibility.Draft,
            ConcurrencyVersion = 1
        };
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var validator = new AdminUpdateCatalogMetadataValidator();
        var handler = new AdminUpdateCatalogMetadataHandler(_db, validator, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));

        var res = await handler.ExecuteAsync(new AdminUpdateCatalogMetadataRequest(
            doc.Id,
            "Updated Title",
            "Updated Desc",
            CategoryDefaults.SystemDesignId,
            "en",
            null,
            "Attribution",
            1,
            _adminUserId));

        res.IsSuccess.Should().BeTrue();

        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.TargetId == doc.Id.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.CatalogUpdateMetadata);
        audit.Outcome.Should().Be(AdminAuditOutcome.Succeeded);
        audit.ActorUserId.Should().Be(_adminUserId);
    }

    [Fact]
    public async Task UpdateMetadata_PublishedImmutable_ShouldCreateRejectedAuditEvent()
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Published Title",
            Description = "Desc",
            CategoryId = CategoryDefaults.SystemDesignId,
            Language = "en",
            SourceType = SourceType.WebDocUrl,
            Visibility = CatalogVisibility.Published,
            ConcurrencyVersion = 1
        };
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var validator = new AdminUpdateCatalogMetadataValidator();
        var handler = new AdminUpdateCatalogMetadataHandler(_db, validator, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));

        var res = await handler.ExecuteAsync(new AdminUpdateCatalogMetadataRequest(
            doc.Id,
            "Updated Title",
            "Updated Desc",
            CategoryDefaults.SystemDesignId,
            "en",
            null,
            "Attribution",
            1,
            _adminUserId));

        res.IsSuccess.Should().BeFalse();
        res.Error.Code.Should().Be("Catalog.PublishedImmutable");

        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(e => e.TargetId == doc.Id.ToString());
        audit.Should().NotBeNull();
        audit!.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit.ErrorCode.Should().Be("Catalog.PublishedImmutable");
    }

    [Fact]
    public async Task Prepare_QueueFailure_ShouldPreserveAcceptedEventAndAppendFailedDispatchEvent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test-prepare-{Guid.NewGuid():N}.pdf");
        await File.WriteAllTextAsync(tempFile, "fake pdf content");

        try
        {
            var doc = new CatalogDocument
            {
                Id = Guid.NewGuid(),
                Title = "Doc to Prepare",
                Description = "Desc",
                CategoryId = CategoryDefaults.EngineeringCraftId,
                Language = "en",
                SourceType = SourceType.PdfBook,
                Visibility = CatalogVisibility.Draft,
                ProcessingStatus = ProcessingStatus.Pending,
                ConcurrencyVersion = 1
            };
            await _db.CatalogDocuments.AddAsync(doc);
            await _db.SaveChangesAsync();

            var throwingQueue = new ThrowingPdfQueue();
            var handler = new AdminPrepareCatalogHandler(_db, throwingQueue, new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));

            var res = await handler.ExecuteAsync(new AdminPrepareCatalogRequest(doc.Id, tempFile, _adminUserId));

            // Return recoverable response referencing accepted draft
            res.IsSuccess.Should().BeTrue();

            var events = await _db.AdminAuditEvents.Where(e => e.TargetId == doc.Id.ToString()).ToListAsync();
            events.Should().HaveCount(2);

            var acceptedEvent = events.FirstOrDefault(e => e.Outcome == AdminAuditOutcome.Accepted);
            acceptedEvent.Should().NotBeNull();
            acceptedEvent!.Action.Should().Be(AdminAuditActions.CatalogPrepare);

            var failedEvent = events.FirstOrDefault(e => e.Outcome == AdminAuditOutcome.Failed);
            failedEvent.Should().NotBeNull();
            failedEvent!.Action.Should().Be(AdminAuditActions.CatalogDispatchFailed);
            failedEvent.ErrorCode.Should().Be("DISPATCH_FAILED");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private CatalogDocument CreateSeedDoc(CatalogVisibility visibility = CatalogVisibility.Draft)
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Admin Test Document",
            Description = "Admin description",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Attribution text",
            Visibility = visibility,
            ProcessingStatus = ProcessingStatus.Ready,
            Revision = 1,
            ConcurrencyVersion = 1,
            TotalSlices = 2
        };

        for (int i = 1; i <= 2; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}",
                OriginalTextMarkdown = $"Source text {i}",
                SummaryMarkdown = $"Summary {i}",
                KeyTakeaways = new() { $"T{i}" },
                EstimatedReadMinutes = 4,
                IsAiFormatted = true
            });
        }

        return doc;
    }

    [Fact]
    public async Task Publish_StaleRevision_ShouldPersistOneDurableRejectedEvent_AndLeaveDocUnchanged()
    {
        var doc = CreateSeedDoc(CatalogVisibility.Draft);
        doc.Review(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminPublishCatalogDocumentHandler(_db, uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion + 99, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.ConcurrencyConflict");

        var docReread = await _db.CatalogDocuments.FindAsync(doc.Id);
        docReread!.Visibility.Should().Be(CatalogVisibility.Draft);
        docReread.PublishedAt.Should().BeNull();

        var audits = await _db.AdminAuditEvents.Where(a => a.TargetId == doc.Id.ToString()).ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audits[0].Action.Should().Be(AdminAuditActions.CatalogPublish);
        audits[0].ErrorCode.Should().Be("Catalog.ConcurrencyConflict");
    }

    [Fact]
    public async Task Publish_UnmetRequirements_ShouldPersistOneDurableRejectedEvent_AndLeaveDocUnchanged()
    {
        var doc = CreateSeedDoc(CatalogVisibility.Draft);
        doc.Slices.Clear();
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminPublishCatalogDocumentHandler(_db, uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.PublicationIneligible");

        var docReread = await _db.CatalogDocuments.FindAsync(doc.Id);
        docReread!.Visibility.Should().Be(CatalogVisibility.Draft);

        var audits = await _db.AdminAuditEvents.Where(a => a.TargetId == doc.Id.ToString()).ToListAsync();
        audits.Should().HaveCount(1);
        audits[0].Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audits[0].Action.Should().Be(AdminAuditActions.CatalogPublish);
        audits[0].ErrorCode.Should().Be("Catalog.PublicationIneligible");
    }

    [Fact]
    public async Task Publish_RejectionPersistenceFailure_ShouldReturnUnavailableWithoutSilentFailure()
    {
        var doc = CreateSeedDoc(CatalogVisibility.Draft);
        doc.Slices.Clear();
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var brokenOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite("DataSource=file::memory:?cache=shared;Mode=ReadOnly")
            .Options;
        var brokenUow = new AdministrativeUnitOfWork(_db, brokenOptions);
        var handler = new AdminPublishCatalogDocumentHandler(_db, brokenUow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");

        var docReread = await _db.CatalogDocuments.FindAsync(doc.Id);
        docReread!.Visibility.Should().Be(CatalogVisibility.Draft);
    }

    [Fact]
    public async Task Hide_StaleRevision_ShouldPersistOneDurableRejectedEvent_AndLeaveDocUnchanged()
    {
        var doc = CreateSeedDoc();
        doc.Publish(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();
        await _db.Entry(doc).ReloadAsync();
        var handler = new AdminHideCatalogDocumentHandler(_db, new AdministrativeUnitOfWork(_db));

        var result = await handler.ExecuteAsync(new AdminHideCatalogDocumentRequest(
            doc.Id, _adminUserId, doc.ConcurrencyVersion - 1, PresentedSessionVersion: 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Catalog.ConcurrencyConflict");
        result.Error.Message.Should().Be("Document has been modified by another administrator. Please reload.");
        _db.Database.CurrentTransaction.Should().BeNull();
        await AssertHideDidNotChangeDocumentAsync(doc);

        var audits = await _db.AdminAuditEvents.AsNoTracking()
            .Where(a => a.TargetId == doc.Id.ToString()).ToListAsync();
        audits.Should().ContainSingle();
        audits[0].ActorUserId.Should().Be(_adminUserId);
        audits[0].Action.Should().Be(AdminAuditActions.CatalogHide);
        audits[0].TargetType.Should().Be("CatalogDocument");
        audits[0].Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audits[0].ErrorCode.Should().Be("Catalog.ConcurrencyConflict");
    }

    [Fact]
    public async Task Hide_RejectionPersistenceFailure_ShouldReturnSafeUnavailableWithoutMutationOrHistory()
    {
        var doc = CreateSeedDoc();
        doc.Publish(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();
        await _db.Entry(doc).ReloadAsync();
        var auditOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new FailingRejectionAuditInterceptor())
            .Options;
        var handler = new AdminHideCatalogDocumentHandler(
            _db, new AdministrativeUnitOfWork(_db, auditOptions));

        var result = await handler.ExecuteAsync(new AdminHideCatalogDocumentRequest(
            doc.Id, _adminUserId, doc.ConcurrencyVersion - 1, PresentedSessionVersion: 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");
        result.Error.Message.Should().NotContain("secret-provider-detail");
        _db.Database.CurrentTransaction.Should().BeNull();
        await AssertHideDidNotChangeDocumentAsync(doc);
        (await _db.AdminAuditEvents.AsNoTracking().ToListAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public async Task Hide_DeniedOrStaleActor_ShouldNotCreateAttributedRejection(
        bool removePermission, int presentedSessionVersion)
    {
        var actor = await _db.Users.SingleAsync(user => user.Id == _adminUserId);
        actor.AuthenticationVersion = 2;
        if (removePermission)
        {
            actor.RoleId = RoleDefaults.LearnerRoleId;
        }
        await _db.SaveChangesAsync();
        var doc = CreateSeedDoc();
        doc.Publish(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();
        await _db.Entry(doc).ReloadAsync();
        var handler = new AdminHideCatalogDocumentHandler(_db, new AdministrativeUnitOfWork(_db));

        var result = await handler.ExecuteAsync(new AdminHideCatalogDocumentRequest(
            doc.Id, _adminUserId, doc.ConcurrencyVersion - 1, presentedSessionVersion));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
        _db.Database.CurrentTransaction.Should().BeNull();
        await AssertHideDidNotChangeDocumentAsync(doc);
        (await _db.AdminAuditEvents.AsNoTracking().ToListAsync()).Should().BeEmpty();
    }

    private async Task AssertHideDidNotChangeDocumentAsync(CatalogDocument expected)
    {
        var persisted = await _db.CatalogDocuments.AsNoTracking().SingleAsync(d => d.Id == expected.Id);
        persisted.Visibility.Should().Be(CatalogVisibility.Published);
        persisted.ConcurrencyVersion.Should().Be(expected.ConcurrencyVersion);
        persisted.Revision.Should().Be(expected.Revision);
        persisted.HiddenAt.Should().BeNull();
        persisted.HiddenByUserId.Should().BeNull();
        persisted.PublishedAt.Should().Be(expected.PublishedAt);
        persisted.PublishedByUserId.Should().Be(expected.PublishedByUserId);
    }

    private sealed class FailingRejectionAuditInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AdminAuditEvent>()
                .Any(entry => entry.Entity.Outcome == AdminAuditOutcome.Rejected))
            {
                throw new DbUpdateException("secret-provider-detail");
            }
            return ValueTask.FromResult(result);
        }
    }

    private class MockCrawler : IWebArticleCrawler
    {
        public Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CrawlArticleResult(
                Title: "Crawled Title",
                SourceUrl: url,
                MarkdownContent: "# Heading\nParagraph content for system design.",
                EstimatedWordCount: 500,
                IsPdfDetected: false,
                DetectedPdfUrl: null));
        }
    }
}
