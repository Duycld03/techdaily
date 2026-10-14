using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class CatalogAdminHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly List<string> _tempFilesToCleanup = new();

    public CatalogAdminHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        foreach (var file in _tempFilesToCleanup)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }
        _db.Dispose();
        _connection.Dispose();
    }

    private CatalogDocument CreateSeedDoc(CatalogVisibility visibility = CatalogVisibility.Draft, ProcessingStatus status = ProcessingStatus.Ready)
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Admin Test Document",
            Description = "Admin description",
            Category = Category.BackendRuntime,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Attribution text",
            PermissionBasis = "Permission basis",
            PermissionEvidence = "Evidence doc",
            ConfirmedDistributionRights = true,
            ConfirmedAiProcessingRights = true,
            Visibility = visibility,
            ProcessingStatus = status,
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
    public async Task UploadPdf_WithMissingRights_ShouldFailValidation()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"upload-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });
        _tempFilesToCleanup.Add(tempFile);

        var queue = new TestQueue();
        var handler = new AdminUploadCatalogPdfHandler(_db, queue, new AdminUploadCatalogPdfValidator());

        var request = new AdminUploadCatalogPdfRequest(
            Title: "Test Book",
            Description: "Desc",
            Category: Category.BackendRuntime,
            Language: "en",
            AuthorOrSourceUrl: null,
            AttributionText: "Attr",
            PermissionBasis: "Basis",
            PermissionEvidence: "Evidence",
            ConfirmedDistributionRights: false, // Invalid!
            ConfirmedAiProcessingRights: true,
            TempFilePath: tempFile,
            AdminUserId: Guid.NewGuid());

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Validation.Failed");
        queue.EnqueuedJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadPdf_WithValidPayload_ShouldPersistDraftAndEnqueue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"upload-{Guid.NewGuid():N}.pdf");
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });
        _tempFilesToCleanup.Add(tempFile);

        var queue = new TestQueue();
        var handler = new AdminUploadCatalogPdfHandler(_db, queue, new AdminUploadCatalogPdfValidator());

        var request = new AdminUploadCatalogPdfRequest(
            Title: "Test Book",
            Description: "Desc",
            Category: Category.BackendRuntime,
            Language: "en",
            AuthorOrSourceUrl: "https://example.com/source",
            AttributionText: "Attr",
            PermissionBasis: "Basis",
            PermissionEvidence: "Evidence",
            ConfirmedDistributionRights: true,
            ConfirmedAiProcessingRights: true,
            TempFilePath: tempFile,
            AdminUserId: Guid.NewGuid());

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Visibility.Should().Be(CatalogVisibility.Draft);
        result.Value.ProcessingStatus.Should().Be(ProcessingStatus.Pending);

        queue.EnqueuedJobs.Should().ContainSingle();
        var job = queue.EnqueuedJobs.First();
        job.DestinationType.Should().Be(IngestionDestinationType.CatalogDocument);
        job.TargetId.Should().Be(result.Value.Id);
    }

    [Fact]
    public async Task UpdateMetadata_WhenPublished_ShouldReturnPublishedImmutableError()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Published);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminUpdateCatalogMetadataHandler(_db, new AdminUpdateCatalogMetadataValidator());

        var request = new AdminUpdateCatalogMetadataRequest(
            Id: doc.Id,
            Title: "Changed Title",
            Description: doc.Description,
            Category: doc.Category,
            Language: doc.Language,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            PermissionBasis: doc.PermissionBasis,
            PermissionEvidence: doc.PermissionEvidence,
            ConfirmedDistributionRights: true,
            ConfirmedAiProcessingRights: true,
            ExpectedConcurrencyVersion: doc.ConcurrencyVersion);

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.PublishedImmutable");
    }

    [Fact]
    public async Task UpdateMetadata_WhenDraft_ShouldIncrementRevisionAndInvalidateReview()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminUpdateCatalogMetadataHandler(_db, new AdminUpdateCatalogMetadataValidator());

        var request = new AdminUpdateCatalogMetadataRequest(
            Id: doc.Id,
            Title: "Changed Title",
            Description: doc.Description,
            Category: doc.Category,
            Language: doc.Language,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            PermissionBasis: doc.PermissionBasis,
            PermissionEvidence: doc.PermissionEvidence,
            ConfirmedDistributionRights: true,
            ConfirmedAiProcessingRights: true,
            ExpectedConcurrencyVersion: doc.ConcurrencyVersion);

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Revision.Should().Be(2);
        result.Value.IsEligibleForPublication.Should().BeFalse();
        result.Value.UnmetPublicationRequirements.Should().Contain(r => r.Contains("stale") || r.Contains("review"));
    }

    [Fact]
    public async Task Publish_WithStaleReview_ShouldRejectWithPublicationIneligible()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = Guid.NewGuid();
        doc.Review(adminId);
        doc.Revision = 2; // Stale!

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPublishCatalogDocumentHandler(_db);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.PublicationIneligible");
        result.Error.Message.Should().Contain("stale");
    }

    [Fact]
    public async Task Publish_ValidReviewedDoc_ShouldSucceedAndRecordPublishData()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = Guid.NewGuid();
        doc.Review(adminId);

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPublishCatalogDocumentHandler(_db);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Visibility.Should().Be(CatalogVisibility.Published);
        result.Value.PublishedByUserId.Should().Be(adminId);
        result.Value.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Hide_PublishedDoc_ShouldSucceedAndRecordHiddenData()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Published);
        var adminId = Guid.NewGuid();

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminHideCatalogDocumentHandler(_db);

        var request = new AdminHideCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Visibility.Should().Be(CatalogVisibility.Hidden);
        result.Value.HiddenByUserId.Should().Be(adminId);
        result.Value.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Prepare_WhenActiveLease_ShouldReturnBusyError()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft, status: ProcessingStatus.Processing);
        doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPrepareCatalogHandler(_db, new TestQueue());
        var result = await handler.ExecuteAsync(new AdminPrepareCatalogRequest(doc.Id));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.Busy");
    }

    private class TestQueue : IPdfIngestionQueue
    {
        public List<PdfIngestJob> EnqueuedJobs { get; } = new();

        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default)
        {
            EnqueuedJobs.Add(job);
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<PdfIngestJob> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            foreach (var j in EnqueuedJobs) yield return j;
        }
    }
}
