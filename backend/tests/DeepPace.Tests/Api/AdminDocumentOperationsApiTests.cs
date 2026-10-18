using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeepPace.Api.Authorization;
using DeepPace.Api.Endpoints;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Features.Admin.Documents;
using DeepPace.Application.Features.Admin.Documents.DTOs;
using DeepPace.Application.Features.Library.GetBookById;
using DeepPace.Application.Features.Library.GetBookSlice;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class AdminDocumentOperationsApiTests : IAsyncLifetime
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();
    private readonly Guid _attemptId = Guid.NewGuid();
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        await using (var db = new DeepPaceDbContext(dbOptions))
        {
            await db.Database.EnsureCreatedAsync();

            var admin = new User { Id = _adminId, Email = "admin@example.com", Name = "Admin", Status = AccountStatus.Active, RoleId = RoleDefaults.AdminRoleId };
            var learner = new User { Id = _learnerId, Email = "learner@example.com", Name = "Learner", Status = AccountStatus.Active };
            var book = new DocumentBook
            {
                CategoryId = CategoryDefaults.SystemDesignId,
                Id = _bookId,
                Title = "Kafka Architecture",
                CreatedByUserId = _learnerId,
                Status = ProcessingStatus.Failed,
                IngestionControlVersion = 2,
                IsDeleted = false
            };
            var source = IngestionSource.Create(IngestionSourceKind.Pdf, "sources/k.pdf", 4096, "hash", _learnerId, "en", DateTimeOffset.UtcNow);
            var attempt = IngestionAttempt.CreateQueued(IngestionTargetScope.Personal, _bookId, IngestionAttemptOrigin.UserUpload, "req-init", 1, sourceId: source.Id);
            attempt.Claim("tok", DateTimeOffset.UtcNow.AddMinutes(15), DateTimeOffset.UtcNow);
            attempt.Fail("tok", "Err", "Failed", DateTimeOffset.UtcNow.AddMinutes(5));

            book.AssignAttempt(attempt.Id);

            db.Users.AddRange(admin, learner);
            db.DocumentBooks.Add(book);
            db.IngestionSources.Add(source);
            db.IngestionAttempts.Add(attempt);
            await db.SaveChangesAsync();
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        builder.Services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        builder.Services.AddScoped<IAdminPermissionResolver, AdminPermissionResolver>();
        builder.Services.AddScoped<IAdministrativeUnitOfWork, AdministrativeUnitOfWork>();
        builder.Services.AddScoped<IDocumentMutationGuard, DocumentMutationGuard>();
        builder.Services.AddSingleton<IIngestionSourceStorage, LocalFileIngestionSourceStorage>();
        builder.Services.AddScoped<IIngestionQuotaService, IngestionQuotaService>();
        builder.Services.AddScoped<IIngestionClaimService, IngestionClaimService>();

        builder.Services.AddScoped<IUseCase<AdminDocumentInventoryQuery, AdminPagedResult<AdminDocumentInventoryItemDto>>, AdminGetDocumentInventoryHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetDocumentDetailRequest, AdminDocumentDetailDto>, AdminGetDocumentDetailHandler>();
        builder.Services.AddScoped<IUseCase<AdminGetDocumentAttemptsRequest, AdminPagedResult<AdminDocumentAttemptItemDto>>, AdminGetDocumentAttemptsHandler>();
        builder.Services.AddScoped<IUseCase<AdminRetryDocumentCommand, AdminRetryDocumentResponseDto>, AdminRetryDocumentHandler>();
        builder.Services.AddScoped<IUseCase<ReplacePersonalDocumentSourceCommand, SourceReplacementResponseDto>, ReplacePersonalDocumentSourceHandler>();
        builder.Services.AddScoped<IUseCase<ReplaceCatalogDocumentSourceCommand, SourceReplacementResponseDto>, ReplaceCatalogDocumentSourceHandler>();
        builder.Services.AddScoped<IUseCase<GetBookByIdRequest, GetBookByIdResponse>, GetBookByIdHandler>();
        builder.Services.AddScoped<IUseCase<GetBookSliceRequest, GetBookSliceResponse>, GetBookSliceHandler>();

        builder.Services.AddScoped<IAuthorizationHandler, CatalogAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AccountAdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AuditReadAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, DocumentOperationsAdminAuthorizationHandler>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("CatalogAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CatalogAdminRequirement());
            });
            options.AddPolicy("AccountAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AccountAdminRequirement());
            });
            options.AddPolicy("AuditRead", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AuditReadRequirement());
            });
            options.AddPolicy("DocumentOperationsAdmin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new DocumentOperationsAdminRequirement());
            });
        });

        builder.Services.AddAuthentication("TestAuth")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", _ => { });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapAdminDocumentOperationsEndpoints();
        _app.MapLibraryEndpoints();
        await _app.StartAsync();

        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetInventory_AsAdmin_Returns200WithItems()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminPagedResult<AdminDocumentInventoryItemDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
        body.Items[0].Title.Should().Be("Kafka Architecture");
    }

    [Theory]
    [InlineData(CatalogVisibility.Draft, ProcessingStatus.Processing)]
    [InlineData(CatalogVisibility.Published, ProcessingStatus.Ready)]
    [InlineData(CatalogVisibility.Hidden, ProcessingStatus.Ready)]
    public async Task GetInventoryAndDetail_CatalogVisibilityIsIndependentAndPersonalVisibilityIsNull(
        CatalogVisibility visibility, ProcessingStatus status)
    {
        var catalog = new CatalogDocument
        {
            Title = "Catalog visibility", CategoryId = CategoryDefaults.SystemDesignId,
            Visibility = CatalogVisibility.Published, ProcessingStatus = ProcessingStatus.Ready
        };
        var adopted = DocumentBook.CreateFromCatalogSnapshot(catalog, _learnerId, "visibility-adopted");
        catalog.Visibility = visibility;
        catalog.ProcessingStatus = status;
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            db.CatalogDocuments.Add(catalog);
            db.DocumentBooks.Add(adopted);
            await db.SaveChangesAsync();
        }
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var inventory = await _client.GetAsync("/api/v1/admin/documents");
        inventory.StatusCode.Should().Be(HttpStatusCode.OK);
        using var inventoryJson = JsonDocument.Parse(await inventory.Content.ReadAsStringAsync());
        var items = inventoryJson.RootElement.GetProperty("items").EnumerateArray().ToArray();
        items.Should().HaveCount(3);
        foreach (var id in new[] { catalog.Id, _bookId, adopted.Id })
        {
            var item = items.Single(row => row.GetProperty("id").GetGuid() == id);
            var documentScope = id == catalog.Id ? "catalog" : "personal";
            var detail = await _client.GetAsync($"/api/v1/admin/documents/{documentScope}/{id}");
            detail.StatusCode.Should().Be(HttpStatusCode.OK);
            using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
            detailJson.RootElement.GetProperty("id").GetGuid().Should().Be(id);
            foreach (var metadata in new[] { item, detailJson.RootElement })
            {
                if (id == catalog.Id)
                {
                    metadata.GetProperty("visibility").GetString().Should().Be(visibility.ToString());
                    metadata.GetProperty("status").GetString().Should().Be(status.ToString());
                }
                else
                {
                    metadata.GetProperty("visibility").ValueKind.Should().Be(JsonValueKind.Null);
                    metadata.GetProperty("scope").GetString().Should().Be("Personal");
                }
            }
        }
    }

    [Fact]
    public async Task GetInventory_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInventory_AsLearner_Returns403()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _learnerId.ToString());

        var response = await _client.GetAsync("/api/v1/admin/documents");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("personal", false)]
    [InlineData("catalog", false)]
    [InlineData("personal", true)]
    [InlineData("catalog", true)]
    public async Task GetDetail_WithoutOperationsPermission_Returns401Or403(string documentScope, bool authenticated)
    {
        var catalog = new CatalogDocument
        {
            Title = "Restricted catalog metadata", CategoryId = CategoryDefaults.SystemDesignId,
            Visibility = CatalogVisibility.Hidden, ProcessingStatus = ProcessingStatus.Ready
        };
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            db.CatalogDocuments.Add(catalog);
            await db.SaveChangesAsync();
        }
        if (authenticated)
            _client.DefaultRequestHeaders.Add("X-Test-UserId", _learnerId.ToString());
        var id = documentScope == "personal" ? _bookId : catalog.Id;

        var response = await _client.GetAsync($"/api/v1/admin/documents/{documentScope}/{id}");

        response.StatusCode.Should().Be(authenticated ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("Kafka Architecture");
        body.Should().NotContain(catalog.Title);
    }

    [Fact]
    public async Task GetInventoryAndDetail_DeletedPersonalAndCatalog_AreExcludedAndReturn404()
    {
        var catalog = new CatalogDocument
        {
            Title = "Deleted catalog metadata", CategoryId = CategoryDefaults.SystemDesignId,
            Visibility = CatalogVisibility.Hidden, ProcessingStatus = ProcessingStatus.Ready
        };
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            db.CatalogDocuments.Add(catalog);
            await db.SaveChangesAsync();
        }
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());
        var before = await _client.GetAsync("/api/v1/admin/documents");
        before.StatusCode.Should().Be(HttpStatusCode.OK);
        using var beforeJson = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        beforeJson.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(new[] { _bookId, catalog.Id });
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var personal = await db.DocumentBooks.FindAsync(_bookId);
            var source = await db.CatalogDocuments.FindAsync(catalog.Id);
            personal!.IsDeleted = true;
            source!.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var inventory = await _client.GetAsync("/api/v1/admin/documents");
        inventory.StatusCode.Should().Be(HttpStatusCode.OK);
        using var inventoryJson = JsonDocument.Parse(await inventory.Content.ReadAsStringAsync());
        inventoryJson.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
        inventoryJson.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
        foreach (var (documentScope, id) in new[] { ("personal", _bookId), ("catalog", catalog.Id) })
        {
            var response = await _client.GetAsync($"/api/v1/admin/documents/{documentScope}/{id}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("status").GetInt32().Should().Be(404);
            json.RootElement.GetProperty("code").GetString().Should().Be("Document.NotFound");
        }
    }

    [Fact]
    public async Task GetInventoryAndDetail_AllowlistedMetadata_ExcludesPrivateContentAndSourceSecrets()
    {
        const string privateText = "PRIVATE_SOURCE_BODY_SENTINEL";
        const string privateSummary = "PRIVATE_SUMMARY_SENTINEL";
        const string secret = "PRIVATE_SOURCE_SECRET_SENTINEL";
        var catalog = new CatalogDocument
        {
            Title = "Sanitized catalog metadata", CategoryId = CategoryDefaults.SystemDesignId,
            Visibility = CatalogVisibility.Hidden, ProcessingStatus = ProcessingStatus.Ready,
            AuthorOrSourceUrl = $"https://reader:{secret}@example.test/catalog.pdf?token={secret}#{secret}",
            Slices = [new CatalogSlice { SliceOrder = 1, OriginalTextMarkdown = privateText, SummaryMarkdown = privateSummary }]
        };
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var book = await db.DocumentBooks.FindAsync(_bookId);
            book!.AuthorOrSourceUrl = $"https://reader:{secret}@example.test/personal.pdf?token={secret}#{secret}";
            book.ErrorMessage = secret;
            book.StatusMessage = secret;
            book.SourceAttribution = privateText;
            db.DocumentChunks.Add(new DocumentChunk
            {
                DocumentBookId = _bookId, ChunkOrder = 1,
                OriginalTextMarkdown = privateText, SummaryMarkdown = privateSummary,
                KeyTakeaways = [privateSummary]
            });
            db.CatalogDocuments.Add(catalog);
            await db.SaveChangesAsync();
        }
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        foreach (var path in new[]
                 {
                     "/api/v1/admin/documents",
                     $"/api/v1/admin/documents/personal/{_bookId}",
                     $"/api/v1/admin/documents/catalog/{catalog.Id}"
                 })
        {
            var response = await _client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(privateText);
            body.Should().NotContain(privateSummary);
            body.Should().NotContain(secret);
            using var json = JsonDocument.Parse(body);
            if (path.Contains("/personal/"))
                json.RootElement.GetProperty("sanitizedSourceUrl").GetString().Should().Be("https://example.test/personal.pdf");
            else if (path.Contains("/catalog/"))
                json.RootElement.GetProperty("sanitizedSourceUrl").GetString().Should().Be("https://example.test/catalog.pdf");
        }
    }

    [Fact]
    public async Task OperationsOnlyAccount_CanReadMetadata_ButCannotMutateCatalogOrReadPrivateContent()
    {
        const string privateText = "OPERATIONS_PRIVATE_BODY_SENTINEL";
        var role = Role.Create("operations-only", "Operations only", "Vận hành", isProtected: false);
        var operatorUser = new User
        {
            Email = "operations-only@example.test", Name = "Operations only",
            Status = AccountStatus.Active, RoleId = role.Id
        };
        var catalog = new CatalogDocument
        {
            Title = "Hidden catalog metadata", CategoryId = CategoryDefaults.SystemDesignId,
            Visibility = CatalogVisibility.Hidden, ProcessingStatus = ProcessingStatus.Ready
        };
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            db.Roles.Add(role);
            db.RolePermissions.Add(RolePermission.Create(role.Id, PermissionKeys.ManageDocumentOperations));
            db.Users.Add(operatorUser);
            db.CatalogDocuments.Add(catalog);
            db.DocumentChunks.Add(new DocumentChunk
            {
                DocumentBookId = _bookId, ChunkOrder = 1,
                OriginalTextMarkdown = privateText, SummaryMarkdown = privateText
            });
            await db.SaveChangesAsync();
        }
        _client.DefaultRequestHeaders.Add("X-Test-UserId", operatorUser.Id.ToString());

        var inventory = await _client.GetAsync("/api/v1/admin/documents");
        inventory.StatusCode.Should().Be(HttpStatusCode.OK);
        using var inventoryJson = JsonDocument.Parse(await inventory.Content.ReadAsStringAsync());
        inventoryJson.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(new[] { _bookId, catalog.Id });
        foreach (var (documentScope, id) in new[] { ("personal", _bookId), ("catalog", catalog.Id) })
        {
            var detail = await _client.GetAsync($"/api/v1/admin/documents/{documentScope}/{id}");
            detail.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await detail.Content.ReadAsStringAsync();
            body.Should().NotContain(privateText);
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("id").GetGuid().Should().Be(id);
            if (documentScope == "catalog")
            {
                json.RootElement.GetProperty("visibility").GetString().Should().Be("Hidden");
                json.RootElement.GetProperty("status").GetString().Should().Be("Ready");
            }
            else
            {
                json.RootElement.GetProperty("ownerUserId").GetGuid().Should().Be(_learnerId);
                json.RootElement.GetProperty("visibility").ValueKind.Should().Be(JsonValueKind.Null);
            }
        }

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]), "file", "replacement.pdf");
        form.Add(new StringContent("operations-only-replacement"), "requestId");
        form.Add(new StringContent("1"), "expectedVersion");
        form.Add(new StringContent("Operations permission must not grant catalog mutation"), "reason");
        var mutation = await _client.PostAsync($"/api/v1/admin/catalog/documents/{catalog.Id}/source/pdf", form);
        mutation.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        foreach (var path in new[] { $"/api/v1/library/books/{_bookId}", $"/api/v1/library/books/{_bookId}/slices/1" })
        {
            var privateRead = await _client.GetAsync(path);
            privateRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await privateRead.Content.ReadAsStringAsync()).Should().NotContain(privateText);
        }
        using var ownerClient = _app.GetTestClient();
        ownerClient.DefaultRequestHeaders.Add("X-Test-UserId", _learnerId.ToString());
        var ownerRead = await ownerClient.GetAsync($"/api/v1/library/books/{_bookId}/slices/1");
        ownerRead.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownerRead.Content.ReadAsStringAsync()).Should().Contain(privateText);
        using var verifyScope = _app.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var unchanged = await verifyDb.CatalogDocuments.FindAsync(catalog.Id);
        unchanged!.Visibility.Should().Be(CatalogVisibility.Hidden);
        unchanged.ProcessingStatus.Should().Be(ProcessingStatus.Ready);
        unchanged.CurrentAttemptId.Should().BeNull();
        (await verifyDb.IngestionAttempts.CountAsync(attempt => attempt.TargetDocumentId == catalog.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GetDetail_AsAdmin_Returns200()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync($"/api/v1/admin/documents/personal/{_bookId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminDocumentDetailDto>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(_bookId);
        body.IsRetryEligible.Should().BeTrue();
    }

    [Fact]
    public async Task GetAttempts_AsAdmin_Returns200()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        var response = await _client.GetAsync($"/api/v1/admin/documents/personal/{_bookId}/attempts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminPagedResult<AdminDocumentAttemptItemDto>>();
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Retry_AsAdmin_Eligible_Returns202Accepted()
    {
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());

        // Get current attempt id from db
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var attempt = await db.IngestionAttempts.FirstAsync();
        var book = await db.DocumentBooks.FirstAsync();

        var payload = new AdminRetryPayload(
            AttemptId: attempt.Id,
            ExpectedVersion: book.IngestionControlVersion,
            RequestId: "api-retry-req-1",
            Reason: "Retry from API test");

        var response = await _client.PostAsJsonAsync($"/api/v1/admin/documents/personal/{_bookId}/retry", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<AdminRetryDocumentResponseDto>();
        body.Should().NotBeNull();
        body!.TargetDocumentId.Should().Be(_bookId);
        body.State.Should().Be("Queued");
    }

    [Fact]
    public async Task Retry_WithFreshSessionVersionGreaterThanOne_Returns202Accepted_AndQueuesAttempt()
    {
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var admin = await db.Users.FindAsync(_adminId);
            admin!.AuthenticationVersion = 3;
            await db.SaveChangesAsync();
        }

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Session-Version", "3");

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var attempt = await db.IngestionAttempts.FirstAsync();
            var book = await db.DocumentBooks.FirstAsync();

            var payload = new AdminRetryPayload(
                AttemptId: attempt.Id,
                ExpectedVersion: book.IngestionControlVersion,
                RequestId: "api-retry-req-fresh-v3",
                Reason: "Retry with fresh session version 3");

            var response = await client.PostAsJsonAsync($"/api/v1/admin/documents/personal/{_bookId}/retry", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var body = await response.Content.ReadFromJsonAsync<AdminRetryDocumentResponseDto>();
            body.Should().NotBeNull();
            body!.TargetDocumentId.Should().Be(_bookId);
            body.State.Should().Be("Queued");
        }

        using (var verifyScope = _app.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var queuedAttempt = await verifyDb.IngestionAttempts
                .FirstOrDefaultAsync(a => a.RequestId == "api-retry-req-fresh-v3");
            queuedAttempt.Should().NotBeNull();
            queuedAttempt!.State.Should().Be(IngestionAttemptState.Queued);
        }
    }

    [Fact]
    public async Task Retry_WithStaleSessionVersion_Returns403Forbidden_AndDoesNotMutateOrQueueAttempt()
    {
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var admin = await db.Users.FindAsync(_adminId);
            admin!.AuthenticationVersion = 3;
            await db.SaveChangesAsync();
        }

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Session-Version", "2"); // Stale: DB is 3

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var attempt = await db.IngestionAttempts.FirstAsync();
            var book = await db.DocumentBooks.FirstAsync();

            var payload = new AdminRetryPayload(
                AttemptId: attempt.Id,
                ExpectedVersion: book.IngestionControlVersion,
                RequestId: "api-retry-req-stale-v2",
                Reason: "Retry with stale session version 2");

            var response = await client.PostAsJsonAsync($"/api/v1/admin/documents/personal/{_bookId}/retry", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        using (var verifyScope = _app.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var queuedAttempt = await verifyDb.IngestionAttempts
                .FirstOrDefaultAsync(a => a.RequestId == "api-retry-req-stale-v2");
            queuedAttempt.Should().BeNull("Stale session requests cannot mutate state or queue attempts");
        }
    }

    [Fact]
    public async Task ReplaceCatalogDocumentSource_WithFreshSessionVersionGreaterThanOne_Returns202Accepted_AndPersistsOutcome()
    {
        var catalogDocId = Guid.NewGuid();
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var admin = await db.Users.FindAsync(_adminId);
            admin!.AuthenticationVersion = 4;

            var catDoc = new CatalogDocument
            {
                Id = catalogDocId,
                Title = "Rust Concurrency",
                Description = "Systems programming guide",
                CategoryId = CategoryDefaults.BackendRuntimeId,
                ProcessingStatus = ProcessingStatus.Failed,
                Visibility = CatalogVisibility.Draft,
                ConcurrencyVersion = 1,
                AttributionText = "DeepPace Docs"
            };
            db.CatalogDocuments.Add(catDoc);
            await db.SaveChangesAsync();
        }

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Session-Version", "4");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }); // %PDF-1.4
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "rust-concurrency.pdf");
        form.Add(new StringContent("cat-replace-req-fresh-v4"), "requestId");
        form.Add(new StringContent("1"), "expectedVersion");
        form.Add(new StringContent("Source replacement under fresh session version 4"), "reason");

        var response = await client.PostAsync($"/api/v1/admin/catalog/documents/{catalogDocId}/source/pdf", form);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var result = await response.Content.ReadFromJsonAsync<SourceReplacementResponseDto>();
        result.Should().NotBeNull();
        result!.State.Should().Be("Queued");

        using (var verifyScope = _app.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var doc = await verifyDb.CatalogDocuments.FindAsync(catalogDocId);
            doc!.ProcessingStatus.Should().Be(ProcessingStatus.Pending);
            doc.CurrentAttemptId.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ReplaceCatalogDocumentSource_WithStaleSessionVersion_Returns403Forbidden_AndDoesNotMutate()
    {
        var catalogDocId = Guid.NewGuid();
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var admin = await db.Users.FindAsync(_adminId);
            admin!.AuthenticationVersion = 4;

            var catDoc = new CatalogDocument
            {
                Id = catalogDocId,
                Title = "Go Concurrency",
                Description = "Goroutines guide",
                CategoryId = CategoryDefaults.BackendRuntimeId,
                ProcessingStatus = ProcessingStatus.Failed,
                Visibility = CatalogVisibility.Draft,
                ConcurrencyVersion = 1,
                AttributionText = "DeepPace Docs"
            };
            db.CatalogDocuments.Add(catDoc);
            await db.SaveChangesAsync();
        }

        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", _adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Session-Version", "3"); // Stale: DB is 4

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "go-concurrency.pdf");
        form.Add(new StringContent("cat-replace-req-stale-v3"), "requestId");
        form.Add(new StringContent("1"), "expectedVersion");
        form.Add(new StringContent("Source replacement under stale session"), "reason");

        var response = await client.PostAsync($"/api/v1/admin/catalog/documents/{catalogDocId}/source/pdf", form);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var verifyScope = _app.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            var doc = await verifyDb.CatalogDocuments.FindAsync(catalogDocId);
            doc!.ProcessingStatus.Should().Be(ProcessingStatus.Failed, "Stale session cannot mutate document status");
            doc.CurrentAttemptId.Should().BeNull();
        }
    }

    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdValues) ||
                !Guid.TryParse(userIdValues.FirstOrDefault(), out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var sessionVersion = "1";
            if (Request.Headers.TryGetValue("X-Test-Session-Version", out var svValues) &&
                !string.IsNullOrWhiteSpace(svValues.FirstOrDefault()))
            {
                sessionVersion = svValues.FirstOrDefault()!;
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim("session_version", sessionVersion)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
