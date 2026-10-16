using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
