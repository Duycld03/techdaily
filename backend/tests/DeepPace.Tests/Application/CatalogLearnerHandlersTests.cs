using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.AdoptDocument;
using DeepPace.Application.Features.Catalog.Learner.GetDocumentById;
using DeepPace.Application.Features.Catalog.Learner.GetDocuments;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Application;

public class CatalogLearnerHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;

    public CatalogLearnerHandlersTests()
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
        _db.Dispose();
        _connection.Dispose();
    }

    private CatalogDocument CreateDoc(string title, CatalogVisibility visibility, DateTimeOffset? publishedAt = null)
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = $"Description for {title}",
            CategoryId = CategoryDefaults.DatabaseStorageId,
            Language = "en",
            SourceType = SourceType.PdfBook,
            AttributionText = "Attribution text",
            Visibility = visibility,
            ProcessingStatus = ProcessingStatus.Ready,
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow,
            TotalSlices = 3
        };

        for (int i = 1; i <= 3; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}",
                OriginalTextMarkdown = $"Original text {i}",
                SummaryMarkdown = $"Summary {i}",
                KeyTakeaways = new() { $"T{i}" },
                EstimatedReadMinutes = 3,
                IsAiFormatted = true
            });
        }

        return doc;
    }

    [Fact]
    public async Task GetPublishedCatalogDocuments_ExcludesDraftAndHidden()
    {
        var published1 = CreateDoc("Pub 1", CatalogVisibility.Published);
        var published2 = CreateDoc("Pub 2", CatalogVisibility.Published);
        var draft = CreateDoc("Draft 1", CatalogVisibility.Draft);
        var hidden = CreateDoc("Hidden 1", CatalogVisibility.Hidden);

        await _db.CatalogDocuments.AddRangeAsync(published1, published2, draft, hidden);
        await _db.SaveChangesAsync();

        var handler = new GetPublishedCatalogDocumentsHandler(_db);
        var response = await handler.ExecuteAsync(new GetPublishedCatalogDocumentsRequest());

        response.IsSuccess.Should().BeTrue();
        response.Value.TotalCount.Should().Be(2);
        response.Value.Items.Select(i => i.Title).Should().BeEquivalentTo(new[] { "Pub 1", "Pub 2" });
    }

    [Fact]
    public async Task GetPublishedCatalogDocuments_MapsExistingBookId_WhenAdopted()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Name = "User" };
        var publishedDoc = CreateDoc("Published Book", CatalogVisibility.Published);
        var personalBook = new DocumentBook
        {
            CategoryId = publishedDoc.CategoryId,
            Id = Guid.NewGuid(),
            Title = "Published Book",
            Slug = "published-book-copy",
            CreatedByUserId = user.Id,
            SourceCatalogDocumentId = publishedDoc.Id,
            IsDeleted = false
        };

        await _db.Users.AddAsync(user);
        await _db.CatalogDocuments.AddAsync(publishedDoc);
        await _db.DocumentBooks.AddAsync(personalBook);
        await _db.SaveChangesAsync();

        var handler = new GetPublishedCatalogDocumentsHandler(_db);
        var response = await handler.ExecuteAsync(new GetPublishedCatalogDocumentsRequest(UserId: user.Id));

        response.IsSuccess.Should().BeTrue();
        response.Value.Items.Should().ContainSingle();
        response.Value.Items.First().ExistingBookId.Should().Be(personalBook.Id);
    }

    [Fact]
    public async Task GetPublishedCatalogDocumentById_WhenHidden_Returns404()
    {
        var hiddenDoc = CreateDoc("Hidden Book", CatalogVisibility.Hidden);
        await _db.CatalogDocuments.AddAsync(hiddenDoc);
        await _db.SaveChangesAsync();

        var handler = new GetPublishedCatalogDocumentByIdHandler(_db);
        var response = await handler.ExecuteAsync(new GetPublishedCatalogDocumentByIdRequest(hiddenDoc.Id));

        response.IsSuccess.Should().BeFalse();
        response.Error.Code.Should().Be("Catalog.NotFound");
    }

    [Fact]
    public async Task GetPublishedCatalogDocumentById_OnlyExposesFirstSlicePreview_HidesLaterSlicesAndPrivateData()
    {
        var doc = CreateDoc("Detailed Book", CatalogVisibility.Published);
        await _db.CatalogDocuments.AddAsync(doc);
        await _db.SaveChangesAsync();

        var handler = new GetPublishedCatalogDocumentByIdHandler(_db);
        var response = await handler.ExecuteAsync(new GetPublishedCatalogDocumentByIdRequest(doc.Id));

        response.IsSuccess.Should().BeTrue();
        var detail = response.Value;

        // TOC lists all slices
        detail.TableOfContents.Should().HaveCount(3);
        detail.TableOfContents.Select(t => t.SliceOrder).Should().BeEquivalentTo(new[] { 1, 2, 3 });

        // Preview ONLY contains slice 1
        detail.FirstSlicePreview.Should().NotBeNull();
        detail.FirstSlicePreview!.SliceOrder.Should().Be(1);
        detail.FirstSlicePreview.OriginalTextMarkdown.Should().Be("Original text 1");

        // Learner DTO has NO private fields
        detail.GetType().GetProperty("PermissionBasis").Should().BeNull();
        detail.GetType().GetProperty("PermissionEvidence").Should().BeNull();
    }

    [Fact]
    public async Task AdoptCatalogDocumentHandler_DelegatesToAdoptionService()
    {
        var fakeService = new FakeAdoptionService();
        var handler = new AdoptCatalogDocumentHandler(fakeService);

        var catalogId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = await handler.ExecuteAsync(new AdoptCatalogDocumentRequest(catalogId, userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyAdded.Should().BeFalse();
        result.Value.BookId.Should().NotBeEmpty();
        fakeService.CalledCatalogId.Should().Be(catalogId);
        fakeService.CalledUserId.Should().Be(userId);
    }

    private class FakeAdoptionService : ICatalogAdoptionService
    {
        public Guid CalledCatalogId { get; private set; }
        public Guid CalledUserId { get; private set; }

        public Task<Result<CatalogAdoptionResult>> AdoptAsync(Guid catalogDocumentId, Guid userId, CancellationToken cancellationToken = default)
        {
            CalledCatalogId = catalogDocumentId;
            CalledUserId = userId;
            return Task.FromResult(Result<CatalogAdoptionResult>.Success(new CatalogAdoptionResult(Guid.NewGuid(), false)));
        }
    }
}
