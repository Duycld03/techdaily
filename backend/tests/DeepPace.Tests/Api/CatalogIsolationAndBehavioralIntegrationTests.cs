using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;
using DeepPace.Api.Authorization;
using DeepPace.Api.Endpoints;
using DeepPace.Application;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Catalog.Admin.ImportUrl;
using DeepPace.Application.Features.Catalog.Admin.Publish;
using DeepPace.Application.Features.Catalog.DTOs;
using DeepPace.Application.Features.Catalog.Learner.GetDocuments;
using DeepPace.Application.Features.DailyFocus.DTOs;
using DeepPace.Application.Features.DailyFocus.SwitchBook;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Features.Library.ExportBookMarkdown;
using DeepPace.Application.Features.Library.GetBookById;
using DeepPace.Application.Features.Library.GetBookSlice;
using DeepPace.Application.Features.Library.GetBooks;
using DeepPace.Application.Features.Library.ImportDocument;
using DeepPace.Application.Features.Notes.CreateHighlight;
using DeepPace.Application.Features.Notes.GetHighlights;
using DeepPace.Application.Features.Review.DTOs;
using DeepPace.Application.Features.Review.GetReviewCards;
using DeepPace.Application.Features.Review.GetReviewDeck;
using DeepPace.Application.Features.Review.GradeReviewCard;
using DeepPace.Application.Features.Review.CreateCardFromHighlight;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

/// <summary>
/// Verifies Task 6.1 and 6.2 end-to-end integration and behavioral invariants:
/// - Admin import, prepare, review, and publish flow with permitted document.
/// - Two distinct learners adopting the same catalog document getting completely isolated books and chunks.
/// - Cross-user data isolation across reading, highlighting, and spaced repetition practice.
/// - Document hiding preserves existing learner adoptions while denying new adoptions.
/// - Soft-deletion and re-adoption semantics.
/// - Personal book import coexistence alongside catalog-adopted documents.
/// </summary>
public class CatalogIsolationAndBehavioralIntegrationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DeepPaceDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _learner1UserId = Guid.NewGuid();
    private readonly Guid _learner2UserId = Guid.NewGuid();
    private readonly Guid _learner3UserId = Guid.NewGuid();

    public CatalogIsolationAndBehavioralIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initDb = new DeepPaceDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var adminUser = new User { Id = _adminUserId, Email = "admin@deeppace.test", Name = "Curator Admin", IsDeleted = false };
            var learner1 = new User { Id = _learner1UserId, Email = "learner1@deeppace.test", Name = "Learner One", IsDeleted = false };
            var learner2 = new User { Id = _learner2UserId, Email = "learner2@deeppace.test", Name = "Learner Two", IsDeleted = false };
            var learner3 = new User { Id = _learner3UserId, Email = "learner3@deeppace.test", Name = "Learner Three", IsDeleted = false };

            await db.Users.AddRangeAsync(adminUser, learner1, learner2, learner3);
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Configure CatalogAdministration allowlist with _adminUserId
        builder.Configuration["CatalogAdministration:UserIds:0"] = _adminUserId.ToString();

        builder.Services.AddRouting();
        builder.Services.AddOpenApi();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(_dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddApplicationServices();

        builder.Services.AddScoped<ICatalogAdoptionService, CatalogAdoptionService>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IPdfIngestionQueue, FakePdfQueue>();
        builder.Services.AddScoped<IWebArticleCrawler, FakeArticleCrawler>();
        builder.Services.AddScoped<IAiMarkdownFormatter, FakeAiMarkdownFormatter>();
        builder.Services.AddScoped<IGeminiAiService, FakeGeminiAiService>();
        builder.Services.AddScoped<IEmbeddingService, FakeEmbeddingService>();
        builder.Services.AddScoped<IGoogleCloudTtsService, MockTtsService>();

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();

        builder.Services.AddAuthentication(defaultScheme: "Test")
            .AddScheme<AuthenticationSchemeOptions, DynamicTestAuthHandler>("Test", _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CatalogAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CatalogAdminRequirement());
            });
        });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapCatalogEndpoints();
        _app.MapLibraryEndpoints();
        _app.MapNotesEndpoints();
        _app.MapGroup("/api/v1/review").MapReviewEndpoints();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _connection.Dispose();
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri, Guid userId)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        return request;
    }

    private CatalogDocument SeedPublishedDocument(DeepPaceDbContext db)
    {
        var doc = new CatalogDocument
        {
            Id = Guid.NewGuid(),
            Title = "Distributed Systems Invariants",
            Description = "A masterclass in replicated log safety and consensus.",
            Category = Category.BackendRuntime,
            Language = "en",
            SourceType = SourceType.WebDocUrl,
            AuthorOrSourceUrl = "https://principles.systems/raft",
            AttributionText = "Distributed Systems Research Group",
            Visibility = CatalogVisibility.Draft,
            ProcessingStatus = ProcessingStatus.Ready,
            TotalSlices = 2,
            Revision = 1
        };

        for (int i = 1; i <= 2; i++)
        {
            doc.Slices.Add(new CatalogSlice
            {
                Id = Guid.NewGuid(),
                CatalogDocumentId = doc.Id,
                SliceOrder = i,
                ChapterTitle = $"Chapter {i}: Consensus Safety",
                OriginalTextMarkdown = $"Source text markdown for chapter {i} outlining consensus safety.",
                SummaryMarkdown = $"Summary of chapter {i}",
                KeyTakeaways = new() { $"Key invariant {i}", $"Safety theorem {i}" },
                EstimatedReadMinutes = 4,
                IsAiFormatted = true
            });
        }

        doc.Review(_adminUserId);
        doc.Publish(_adminUserId);

        db.CatalogDocuments.Add(doc);
        db.SaveChanges();
        return doc;
    }

    [Fact]
    public async Task Task6_1_AdminImportPreparePublishAndLearnerAdoptsAndReads_EndToEndFlow()
    {
        // 1. Admin imports a permitted document via URL
        var importUrlRequest = new AdminImportCatalogUrlApiRequest(
            Url: "https://principles.systems/raft",
            Title: "Raft Consensus and Replicated State Machines",
            Description: "Formal verification of leader election and log invariants.",
            Category: Category.BackendRuntime,
            AuthorOrSourceUrl: "https://principles.systems/raft",
            Language: "en",
            AttributionText: "Stanford & USENIX ATC 2014");

        var importMessage = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/admin/catalog/documents/import-url", _adminUserId);
        importMessage.Content = JsonContent.Create(importUrlRequest);

        var importRes = await _client.SendAsync(importMessage);
        importRes.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var importResult = await importRes.Content.ReadFromJsonAsync<AdminCatalogDocumentDto>();
        importResult.Should().NotBeNull();
        var docId = importResult!.Id;
        importResult.ProcessingStatus.Should().Be(ProcessingStatus.Ready);
        importResult.Visibility.Should().Be(CatalogVisibility.Draft);
        importResult.TotalSlices.Should().Be(2);

        // 2. Admin reviews the slices to confirm curation quality
        var reviewMessage = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/admin/catalog/documents/{docId}/slices/1/review", _adminUserId);
        var reviewRes = await _client.SendAsync(reviewMessage);
        reviewRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Admin publishes the document
        var publishMessage = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/admin/catalog/documents/{docId}/publish", _adminUserId);
        publishMessage.Content = JsonContent.Create(new AdminPublishCatalogApiRequest(ExpectedRevision: 1));

        var publishRes = await _client.SendAsync(publishMessage);
        publishRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishedDto = await publishRes.Content.ReadFromJsonAsync<AdminCatalogDocumentDto>();
        publishedDto!.Visibility.Should().Be(CatalogVisibility.Published);

        // 4. Learner 1 discovers the published catalog document
        var discoverMessage = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/catalog/documents", _learner1UserId);
        var discoverRes = await _client.SendAsync(discoverMessage);
        discoverRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var discoverResult = await discoverRes.Content.ReadFromJsonAsync<GetPublishedCatalogDocumentsResponse>();
        discoverResult.Should().NotBeNull();
        var discoveredDoc = discoverResult!.Items.FirstOrDefault(d => d.Id == docId);
        discoveredDoc.Should().NotBeNull();
        discoveredDoc!.ExistingBookId.Should().BeNull();

        // 5. Learner 1 adopts the document
        var adoptMessage = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{docId}/adopt", _learner1UserId);
        var adoptRes = await _client.SendAsync(adoptMessage);
        adoptRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var adoptResult = await adoptRes.Content.ReadFromJsonAsync<CatalogAdoptionResult>();
        adoptResult.Should().NotBeNull();
        adoptResult!.AlreadyAdded.Should().BeFalse();
        var adoptedBookId = adoptResult.BookId;
        adoptedBookId.Should().NotBeEmpty();

        // 6. Learner 1 immediately reads the adopted book
        var readBookMessage = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{adoptedBookId}", _learner1UserId);
        var readBookRes = await _client.SendAsync(readBookMessage);
        readBookRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var bookResult = await readBookRes.Content.ReadFromJsonAsync<GetBookByIdResponse>();
        bookResult.Should().NotBeNull();
        bookResult!.Book.Title.Should().Be("Raft Consensus and Replicated State Machines");
        bookResult.Book.TotalChunks.Should().Be(2);

        // 7. Learner 1 reads slice 1
        var readSliceMessage = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{adoptedBookId}/slices/1", _learner1UserId);
        var readSliceRes = await _client.SendAsync(readSliceMessage);
        readSliceRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var sliceResult = await readSliceRes.Content.ReadFromJsonAsync<GetBookSliceResponse>();
        sliceResult.Should().NotBeNull();
        sliceResult!.Slice.OriginalTextMarkdown.Should().Contain("Consensus protocols enable replicated state machines");

        // 8. Learner 1 exports markdown
        var exportMessage = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{adoptedBookId}/export-markdown", _learner1UserId);
        var exportRes = await _client.SendAsync(exportMessage);
        exportRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var markdownContent = await exportRes.Content.ReadAsStringAsync();
        markdownContent.Should().Contain("Chapter 1: Consensus Fundamentals");
        markdownContent.Should().Contain("Chapter 2: Raft Invariants");
    }

    [Fact]
    public async Task Task6_2_TwoLearners_AdoptSameDocument_GetCompletelyIsolatedBooksAndChunks()
    {
        CatalogDocument doc;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            doc = SeedPublishedDocument(db);
        }

        // Learner 1 adopts
        var adopt1Msg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId);
        var adopt1Res = await _client.SendAsync(adopt1Msg);
        adopt1Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var adopt1Result = await adopt1Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>();

        // Learner 2 adopts
        var adopt2Msg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner2UserId);
        var adopt2Res = await _client.SendAsync(adopt2Msg);
        adopt2Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var adopt2Result = await adopt2Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>();

        // 1. Verify distinct BookIds
        var book1Id = adopt1Result!.BookId;
        var book2Id = adopt2Result!.BookId;
        book1Id.Should().NotBe(book2Id);

        // 2. Query database directly to verify complete physical isolation
        using (var verifyDb = new DeepPaceDbContext(_dbOptions))
        {
            var book1 = await verifyDb.DocumentBooks
                .Include(b => b.Chunks)
                .FirstOrDefaultAsync(b => b.Id == book1Id);

            var book2 = await verifyDb.DocumentBooks
                .Include(b => b.Chunks)
                .FirstOrDefaultAsync(b => b.Id == book2Id);

            book1.Should().NotBeNull();
            book2.Should().NotBeNull();

            // Ownership verified
            book1!.CreatedByUserId.Should().Be(_learner1UserId);
            book1.SourceCatalogDocumentId.Should().Be(doc.Id);

            book2!.CreatedByUserId.Should().Be(_learner2UserId);
            book2.SourceCatalogDocumentId.Should().Be(doc.Id);

            // Chunk isolation
            book1.Chunks.Should().HaveCount(2);
            book2.Chunks.Should().HaveCount(2);

            var chunkIds1 = book1.Chunks.Select(c => c.Id).ToHashSet();
            var chunkIds2 = book2.Chunks.Select(c => c.Id).ToHashSet();
            var catalogSliceIds = doc.Slices.Select(s => s.Id).ToHashSet();

            // Zero ID collision across learners and catalog
            chunkIds1.Intersect(chunkIds2).Should().BeEmpty();
            chunkIds1.Intersect(catalogSliceIds).Should().BeEmpty();
            chunkIds2.Intersect(catalogSliceIds).Should().BeEmpty();

            // Slices retain order and chapter structure
            book1.Chunks.OrderBy(c => c.ChunkOrder).Select(c => c.ChapterTitle)
                .Should().Equal(doc.Slices.OrderBy(s => s.SliceOrder).Select(s => s.ChapterTitle));

            book2.Chunks.OrderBy(c => c.ChunkOrder).Select(c => c.ChapterTitle)
                .Should().Equal(doc.Slices.OrderBy(s => s.SliceOrder).Select(s => s.ChapterTitle));
        }
    }

    [Fact]
    public async Task Task6_2_TwoLearners_NotesReviewAndPractice_StrictlyIsolated()
    {
        CatalogDocument doc;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            doc = SeedPublishedDocument(db);
        }

        // Both learners adopt the same catalog document
        var adopt1Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId));
        var adopt2Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner2UserId));

        var book1Id = (await adopt1Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;
        var book2Id = (await adopt2Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;

        // 1. Learner 2 cannot read Learner 1's book or slices
        var learner2ReadBook1 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book1Id}", _learner2UserId));
        learner2ReadBook1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var learner2ReadSlice1 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book1Id}/slices/1", _learner2UserId));
        learner2ReadSlice1.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 2. Learner 1 creates a highlight on their own copy
        Guid chunk1Id;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            chunk1Id = (await db.DocumentChunks.FirstAsync(c => c.DocumentBookId == book1Id && c.ChunkOrder == 1)).Id;
        }

        var createHighlightMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/notes/highlights", _learner1UserId);
        createHighlightMsg.Content = JsonContent.Create(new CreateHighlightApiRequest(
            DocumentChunkId: chunk1Id,
            SelectedText: "Source text markdown for chapter 1",
            Note: "Critical invariant for replicated log consistency.",
            Tags: new() { "Raft", "Consensus" }));

        var createHighlightRes = await _client.SendAsync(createHighlightMsg);
        createHighlightRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var highlightId = (await createHighlightRes.Content.ReadFromJsonAsync<CreateHighlightResponse>())!.Highlight.Id;

        // 3. Learner 2 queries highlights -> 0 highlights returned (no data leakage)
        var learner2GetHighlights = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/notes/highlights", _learner2UserId));
        learner2GetHighlights.StatusCode.Should().Be(HttpStatusCode.OK);
        var learner2Highlights = await learner2GetHighlights.Content.ReadFromJsonAsync<GetHighlightsResponse>();
        learner2Highlights!.Highlights.Should().BeEmpty();

        // 4. Learner 2 attempts to mutate or delete Learner 1's highlight -> 404 NotFound
        var updateMsg = CreateAuthenticatedRequest(HttpMethod.Put, $"/api/v1/notes/highlights/{highlightId}", _learner2UserId);
        updateMsg.Content = JsonContent.Create(new UpdateHighlightApiRequest(Note: "Tampered note", Tags: new() { "Hacked" }));
        var updateRes = await _client.SendAsync(updateMsg);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var deleteMsg = CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/v1/notes/highlights/{highlightId}", _learner2UserId);
        var deleteRes = await _client.SendAsync(deleteMsg);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 5. Learner 1 creates a spaced repetition flashcard from highlight
        var createCardMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/review/cards/from-highlight", _learner1UserId);
        createCardMsg.Content = JsonContent.Create(new CreateCardFromHighlightJsonRequest { HighlightId = highlightId, Locale = "en" });
        var createCardRes = await _client.SendAsync(createCardMsg);
        createCardRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var cardId = (await createCardRes.Content.ReadFromJsonAsync<CreateCardFromHighlightResponse>())!.CardId;

        // 6. Learner 1 grades the review card (quality 5)
        var gradeMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/review/cards/{cardId}/grade", _learner1UserId);
        gradeMsg.Content = JsonContent.Create(new GradeCardJsonRequest { QualityGrade = 5 });
        var gradeRes = await _client.SendAsync(gradeMsg);
        gradeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Learner 2 queries review deck and cards -> 0 cards (no cross-user leakage)
        var learner2DeckRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/review/deck", _learner2UserId));
        learner2DeckRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var learner2Deck = await learner2DeckRes.Content.ReadFromJsonAsync<GetReviewDeckResponse>();
        learner2Deck!.DueCards.Should().BeEmpty();

        var learner2CardsRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/review/cards", _learner2UserId));
        learner2CardsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var learner2Cards = await learner2CardsRes.Content.ReadFromJsonAsync<GetReviewCardsResponse>();
        learner2Cards!.TotalCount.Should().Be(0);

        // 8. Learner 1 switches active book; verify no automatic pacer switch for Learner 2
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var switchHandler = new SwitchBookHandler(db, new SwitchBookValidator());
            var switchResult = await switchHandler.ExecuteAsync(new SwitchBookRequest(_learner1UserId, book1Id));
            switchResult.IsSuccess.Should().BeTrue();

            var learner1Pacers = await db.UserBookPacers.Where(p => p.UserId == _learner1UserId).ToListAsync();
            var learner2Pacers = await db.UserBookPacers.Where(p => p.UserId == _learner2UserId).ToListAsync();

            learner1Pacers.Should().ContainSingle(p => p.DocumentBookId == book1Id && p.IsActive);
            learner2Pacers.Should().BeEmpty(); // Learner 2's pacer state untouched
        }
    }

    [Fact]
    public async Task Task6_2_AdminHideDocument_PreservesExistingAdoptedCopies_AndRejectsNewAdoptions()
    {
        CatalogDocument doc;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            doc = SeedPublishedDocument(db);
        }

        // Learner 1 and 2 adopt
        var adopt1Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId));
        var adopt2Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner2UserId));

        var book1Id = (await adopt1Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;
        var book2Id = (await adopt2Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;

        // Administrator hides the catalog document
        var hideMsg = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/admin/catalog/documents/{doc.Id}/hide", _adminUserId);
        hideMsg.Content = JsonContent.Create(new AdminHideCatalogApiRequest(ExpectedConcurrencyVersion: doc.ConcurrencyVersion));
        var hideRes = await _client.SendAsync(hideMsg);
        hideRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Document no longer visible in learner catalog discovery
        var discoverRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/catalog/documents", _learner1UserId));
        var discoverResult = await discoverRes.Content.ReadFromJsonAsync<GetPublishedCatalogDocumentsResponse>();
        discoverResult!.Items.Should().NotContain(d => d.Id == doc.Id);

        // Learner 1 can still read book details
        var readBook1 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book1Id}", _learner1UserId));
        readBook1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Learner 1 can still read slice 1
        var readSlice1 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book1Id}/slices/1", _learner1UserId));
        readSlice1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Learner 1 can still export markdown
        var exportBook1 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book1Id}/export-markdown", _learner1UserId));
        exportBook1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Learner 2 can still read their book
        var readBook2 = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/library/books/{book2Id}", _learner2UserId));
        readBook2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Learner 3 attempts to adopt the hidden document -> 404 NotFound
        var adopt3Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner3UserId));
        adopt3Res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Task6_2_SoftDeleteAndReAdoption_CreatesNewIndependentCopy()
    {
        CatalogDocument doc;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            doc = SeedPublishedDocument(db);
        }

        // 1. First adoption
        var adopt1Res = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId));
        adopt1Res.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBookId = (await adopt1Res.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;

        // 2. Learner 1 soft-deletes the book
        var deleteRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/v1/library/books/{firstBookId}", _learner1UserId));
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm soft-delete in DB
        using (var verifyDb = new DeepPaceDbContext(_dbOptions))
        {
            var deletedBook = await verifyDb.DocumentBooks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == firstBookId);
            deletedBook.Should().NotBeNull();
            deletedBook!.IsDeleted.Should().BeTrue();
        }

        // 3. Learner 1 re-adopts the still-published catalog document
        var reAdoptRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId));
        reAdoptRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var reAdoptResult = await reAdoptRes.Content.ReadFromJsonAsync<CatalogAdoptionResult>();
        reAdoptResult!.AlreadyAdded.Should().BeFalse();
        var secondBookId = reAdoptResult.BookId;

        secondBookId.Should().NotBe(firstBookId);

        // Confirm in DB that active book count is 1
        using (var verifyDb = new DeepPaceDbContext(_dbOptions))
        {
            var activeBooks = await verifyDb.DocumentBooks
                .Where(b => b.CreatedByUserId == _learner1UserId && b.SourceCatalogDocumentId == doc.Id && !b.IsDeleted)
                .ToListAsync();

            activeBooks.Should().HaveCount(1);
            activeBooks.First().Id.Should().Be(secondBookId);
        }
    }

    [Fact]
    public async Task Task6_2_PersonalImport_CoexistsSeamlesslyWithCatalogAdoptions()
    {
        CatalogDocument doc;
        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            doc = SeedPublishedDocument(db);
        }

        // Learner 1 adopts catalog document
        var adoptRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/catalog/documents/{doc.Id}/adopt", _learner1UserId));
        var catalogBookId = (await adoptRes.Content.ReadFromJsonAsync<CatalogAdoptionResult>())!.BookId;

        // Learner 1 imports a personal markdown document
        var personalDocReq = new ImportDocumentRequest(
            Title: "Personal Microservice Design Notes",
            MarkdownContent: "# Microservice Architecture\n\nDatabase per service pattern ensures loose coupling and independent scalability across bounded contexts.",
            Category: Category.BackendRuntime);

        var importMsg = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/library/import", _learner1UserId);
        importMsg.Content = JsonContent.Create(personalDocReq);
        var importRes = await _client.SendAsync(importMsg);
        importRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var personalBookId = (await importRes.Content.ReadFromJsonAsync<ImportDocumentResponse>())!.Book.Id;

        // Verify in DB that personal book has SourceCatalogDocumentId == null
        using (var verifyDb = new DeepPaceDbContext(_dbOptions))
        {
            var personalBook = await verifyDb.DocumentBooks.FindAsync(personalBookId);
            personalBook!.SourceCatalogDocumentId.Should().BeNull();

            var catalogBook = await verifyDb.DocumentBooks.FindAsync(catalogBookId);
            catalogBook!.SourceCatalogDocumentId.Should().Be(doc.Id);
        }

        // Learner 1 library listing contains BOTH books
        var learner1BooksRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/library/books", _learner1UserId));
        learner1BooksRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var learner1Books = await learner1BooksRes.Content.ReadFromJsonAsync<GetBooksResponse>();
        learner1Books!.Books.Select(b => b.Id).Should().Contain(new[] { catalogBookId, personalBookId });

        // Learner 2 library listing contains ZERO books (neither catalog nor personal of learner 1)
        var learner2BooksRes = await _client.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/library/books", _learner2UserId));
        learner2BooksRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var learner2Books = await learner2BooksRes.Content.ReadFromJsonAsync<GetBooksResponse>();
        learner2Books!.Books.Should().BeEmpty();
    }

    #region Test Fakes & Auth Handler

    public class DynamicTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static Guid DefaultUserId { get; set; } = Guid.NewGuid();
        public static bool IsAuthenticated { get; set; } = true;

        public DynamicTestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!IsAuthenticated || Context.Request.Headers.ContainsKey("X-Test-Anonymous"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = DefaultUserId;
            if (Context.Request.Headers.TryGetValue("X-Test-UserId", out var headerVal) &&
                Guid.TryParse(headerVal.ToString(), out var parsedGuid))
            {
                userId = parsedGuid;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, $"{userId:N}@deeppace.test")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class FakePdfQueue : IPdfIngestionQueue
    {
        public ValueTask EnqueueAsync(PdfIngestJob job, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public IAsyncEnumerable<PdfIngestJob> ReadAllAsync(CancellationToken cancellationToken = default) => EmptyAsyncEnumerable();

        private static async IAsyncEnumerable<PdfIngestJob> EmptyAsyncEnumerable()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    public class FakeArticleCrawler : IWebArticleCrawler
    {
        public Task<CrawlArticleResult> CrawlUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            var markdown = @"# Chapter 1: Consensus Fundamentals

Consensus protocols enable replicated state machines to agree on a sequence of state transitions despite network partitions.

# Chapter 2: Raft Invariants

Raft safety guarantees election safety, leader append-only, and log matching invariants across all cluster nodes.";

            return Task.FromResult(new CrawlArticleResult(
                Title: "Raft Consensus and Replicated State Machines",
                MarkdownContent: markdown,
                SourceUrl: url,
                EstimatedWordCount: 80,
                IsPdfDetected: false));
        }
    }

    public class FakeAiMarkdownFormatter : IAiMarkdownFormatter
    {
        public Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
            string rawText,
            string chapterTitle,
            string language = "en",
            Category? category = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<AiFormattedSliceResult>.Success(new AiFormattedSliceResult(
                FormattedMarkdown: $"# {chapterTitle}\n\n{rawText}",
                SummaryMarkdown: $"Summary of {chapterTitle}",
                KeyTakeaways: new List<string> { "Core Consensus Invariant", "Leader Election Safety" },
                EstimatedReadMinutes: 4)));
        }
    }

    public class FakeGeminiAiService : IGeminiAiService
    {
        public Task<Result<(string Front, string Back)>> SynthesizeActiveRecallCardAsync(
            string quote,
            string? note,
            string chapterTitle,
            string locale = "en",
            CancellationToken ct = default)
        {
            return Task.FromResult(Result<(string Front, string Back)>.Success((
                "What is the primary invariant of Raft leader election?",
                "At most one leader can be elected in a given term.")));
        }
    }

    public class FakeEmbeddingService : IEmbeddingService
    {
        private readonly int _dimensions = 768;

        public Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<Vector>.Success(new Vector(new float[_dimensions])));
        }

        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            var list = texts.Select(_ => new Vector(new float[_dimensions])).ToList();
            return Task.FromResult(Result<List<Vector>>.Success(list));
        }
    }

    public class MockTtsService : IGoogleCloudTtsService
    {
        public Task<Result<GoogleCloudTtsResult>> SynthesizeAsync(string text, string voiceId, string? languageCode = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<GoogleCloudTtsResult>.Success(new GoogleCloudTtsResult(new byte[] { 1, 2, 3 }, text.Length, 1.0)));
        }
    }

    #endregion
}
