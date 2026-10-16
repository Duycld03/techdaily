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
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class CatalogAdminHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly AdministrativeUnitOfWork _uow;
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
        _db.Users.Add(new User
        {
            Id = _adminUserId,
            Email = "catalog-admin@example.test",
            Name = "Catalog administrator",
            RoleId = RoleDefaults.AdminRoleId,
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        });
        _db.SaveChanges();
        _uow = new AdministrativeUnitOfWork(_db);
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
            CategoryId = CategoryDefaults.BackendRuntimeId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Attribution text",
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
    public async Task UploadPdf_WithNonExistentTempFile_ShouldFailValidation()
    {
        var queue = new TestQueue();
        var handler = new AdminUploadCatalogPdfHandler(_db, queue, new AdminUploadCatalogPdfValidator(), new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db));

        var request = new AdminUploadCatalogPdfRequest(
            Title: "Test Book",
            Description: "Desc",
            CategoryId : CategoryDefaults.BackendRuntimeId,
            Language: "en",
            AuthorOrSourceUrl: null,
            AttributionText: "Attr",
            TempFilePath: "/tmp/non-existent-pdf-file.pdf",
            AdminUserId: _adminUserId);

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
        var handler = new AdminUploadCatalogPdfHandler(_db, queue, new AdminUploadCatalogPdfValidator(), new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db));

        var request = new AdminUploadCatalogPdfRequest(
            Title: "Test Book",
            Description: "Desc",
            CategoryId : CategoryDefaults.BackendRuntimeId,
            Language: "en",
            AuthorOrSourceUrl: "https://example.com/source",
            AttributionText: "Attr",
            TempFilePath: tempFile,
            AdminUserId: _adminUserId);

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

        var handler = new AdminUpdateCatalogMetadataHandler(_db, new AdminUpdateCatalogMetadataValidator(), new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));

        var request = new AdminUpdateCatalogMetadataRequest(
            Id: doc.Id,
            Title: "Changed Title",
            Description: doc.Description,
            CategoryId: doc.CategoryId,
            Language: doc.Language,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            ExpectedConcurrencyVersion: doc.ConcurrencyVersion,
            AdminUserId: _adminUserId);

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.PublishedImmutable");
    }

    [Fact]
    public async Task UpdateMetadata_WhenDraft_ShouldIncrementRevisionAndInvalidateReview()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = _adminUserId;
        doc.Review(adminId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminUpdateCatalogMetadataHandler(_db, new AdminUpdateCatalogMetadataValidator(), new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));

        var request = new AdminUpdateCatalogMetadataRequest(
            Id: doc.Id,
            Title: "Changed Title",
            Description: doc.Description,
            CategoryId: doc.CategoryId,
            Language: doc.Language,
            AuthorOrSourceUrl: doc.AuthorOrSourceUrl,
            AttributionText: doc.AttributionText,
            ExpectedConcurrencyVersion: doc.ConcurrencyVersion,
            AdminUserId: _adminUserId);

        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Revision.Should().Be(2);
        result.Value.IsEligibleForPublication.Should().BeTrue();
    }

    [Fact]
    public async Task Publish_WhenNoSlices_ShouldRejectWithPublicationIneligible()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        doc.Slices.Clear();
        var adminId = _adminUserId;

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPublishCatalogDocumentHandler(_db, _uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Catalog.PublicationIneligible");
        result.Error.Message.Should().Contain("slice");
    }

    [Fact]
    public async Task Publish_ValidReviewedDoc_ShouldSucceedAndRecordPublishData()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = _adminUserId;
        doc.Review(adminId);

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPublishCatalogDocumentHandler(_db, _uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
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
        var adminId = _adminUserId;

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminHideCatalogDocumentHandler(_db, _uow);

        var request = new AdminHideCatalogDocumentRequest(doc.Id, adminId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Visibility.Should().Be(CatalogVisibility.Hidden);
        result.Value.HiddenByUserId.Should().Be(adminId);
        result.Value.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Review_DraftDoc_ShouldSucceedAndRecordReviewData()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var adminId = _adminUserId;

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminReviewCatalogHandler(_db, _uow);

        var request = new AdminReviewCatalogRequest(doc.Id, adminId, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReviewedByUserId.Should().Be(adminId);
        result.Value.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_WithUnitOfWork_EligibleNonInitialSessionVersion_ShouldSucceed()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var admin = await _db.Users.FindAsync(_adminUserId);
        admin!.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        doc.Review(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminPublishCatalogDocumentHandler(_db, uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion, PresentedSessionVersion: 2);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Visibility.Should().Be(CatalogVisibility.Published);
    }

    [Fact]
    public async Task Publish_WithUnitOfWork_StaleSessionVersion_ShouldFailWithAuthForbidden()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var admin = await _db.Users.FindAsync(_adminUserId);
        admin!.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        doc.Review(_adminUserId);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminPublishCatalogDocumentHandler(_db, uow);

        var request = new AdminPublishCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task Hide_WithUnitOfWork_StaleSessionVersion_ShouldFailWithAuthForbidden()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Published);
        var admin = await _db.Users.FindAsync(_adminUserId);
        admin!.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminHideCatalogDocumentHandler(_db, uow);

        var request = new AdminHideCatalogDocumentRequest(doc.Id, _adminUserId, doc.ConcurrencyVersion, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task Review_WithUnitOfWork_StaleSessionVersion_ShouldFailWithAuthForbidden()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft);
        var admin = await _db.Users.FindAsync(_adminUserId);
        admin!.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var uow = new AdministrativeUnitOfWork(_db);
        var handler = new AdminReviewCatalogHandler(_db, uow);

        var request = new AdminReviewCatalogRequest(doc.Id, _adminUserId, PresentedSessionVersion: 1);
        var result = await handler.ExecuteAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task Prepare_WhenActiveLease_ShouldReturnBusyError()
    {
        var doc = CreateSeedDoc(visibility: CatalogVisibility.Draft, status: ProcessingStatus.Processing);
        doc.ProcessingLeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new AdminPrepareCatalogHandler(_db, new TestQueue(), new DeepPace.Infrastructure.Services.CategoryAssignmentGuard(_db), new DeepPace.Infrastructure.Services.AdministrativeUnitOfWork(_db), new DeepPace.Infrastructure.Services.DocumentMutationGuard(_db));
        var result = await handler.ExecuteAsync(new AdminPrepareCatalogRequest(doc.Id, AdminUserId: _adminUserId));

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
