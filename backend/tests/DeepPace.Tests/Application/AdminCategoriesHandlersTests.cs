using FluentAssertions;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Application.Features.Admin.Categories;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.Admin.Categories.Validators;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class AdminCategoriesHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly AdministrativeUnitOfWork _uow;
    private readonly CategoryAssignmentGuard _guard;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public AdminCategoriesHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        _uow = new AdministrativeUnitOfWork(_db, options);
        _guard = new CategoryAssignmentGuard(_db);

        // Seed Admin user
        var admin = new User
        {
            Id = _adminUserId,
            Email = "admin@example.com",
            Name = "Admin User",
            Status = AccountStatus.Active,
            RoleId = RoleDefaults.AdminRoleId,
            AuthenticationVersion = 1,
            AdminControlVersion = 1
        };
        _db.Users.Add(admin);

        // Ensure Admin has ManageCategories permission
        var hasManageCat = _db.RolePermissions.Any(rp => rp.RoleId == RoleDefaults.AdminRoleId && rp.PermissionKey == PermissionKeys.ManageCategories);
        if (!hasManageCat)
        {
            _db.RolePermissions.Add(RolePermission.Create(RoleDefaults.AdminRoleId, PermissionKeys.ManageCategories));
        }

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ValidateAsync_WithActiveCategory_ShouldReturnSuccess()
    {
        // Act
        var result = await _guard.ValidateAsync(CategoryDefaults.FrontendWebId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be(CategoryDefaults.FrontendWebSlug);
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownCategory_ShouldReturnValidationFailure()
    {
        // Act
        var result = await _guard.ValidateAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.ValidationFailed");
    }

    [Fact]
    public async Task ValidateAsync_WithInactiveCategory_ShouldReturnInactive()
    {
        // Arrange
        var inactive = DocumentCategory.Create("inactive-cat", "Inactive", "Không hoạt động", 10, isActive: false);
        _db.DocumentCategories.Add(inactive);
        await _db.SaveChangesAsync();

        // Act
        var result = await _guard.ValidateAsync(inactive.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.Inactive");
    }

    [Fact]
    public async Task ExecuteForBookAsync_WithOwnedInactiveAssignment_ShouldRetainCategoryAndSaveTitle()
    {
        // Arrange
        var inactive = DocumentCategory.Create("inactive-cat-2", "Inactive 2", "Không hoạt động 2", 10, isActive: false);
        _db.DocumentCategories.Add(inactive);
        var book = new DocumentBook
        {
            Id = Guid.NewGuid(), CreatedByUserId = _adminUserId, CategoryId = inactive.Id,
            Title = "Existing title", Slug = "owned-inactive-book", SourceType = SourceType.MarkdownSeries
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        // Act
        var result = await _guard.ExecuteForBookAsync(
            book.Id, _adminUserId, inactive.Id,
            (owned, category) =>
            {
                owned.Title = "Retained inactive assignment";
                return Task.FromResult(Result<Guid>.Success(category.Id));
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(inactive.Id);
        var saved = await _db.DocumentBooks.AsNoTracking().SingleAsync(item => item.Id == book.Id);
        saved.CategoryId.Should().Be(inactive.Id);
        saved.Title.Should().Be("Retained inactive assignment");
    }

    [Fact]
    public async Task CreateCategoryHandler_WithValidRequest_ShouldCreateCategoryAndAuditEvent()
    {
        // Arrange
        var handler = new CreateCategoryHandler(_db, _uow, new CreateCategoryValidator());
        var request = new CreateCategoryRequest(
            Slug: "cloud-devops",
            NameEn: "Cloud & DevOps",
            NameVi: "Điện toán đám mây",
            SortOrder: 7,
            IsActive: true,
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("cloud-devops");
        result.Value.NameEn.Should().Be("Cloud & DevOps");
        result.Value.Version.Should().Be(1);

        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.TargetId == result.Value.Id.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.CategoryCreate);
        audit.Outcome.Should().Be(AdminAuditOutcome.Accepted);
    }

    [Fact]
    public async Task CreateCategoryHandler_WithDuplicateSlug_ShouldReturnSlugConflict()
    {
        // Arrange
        var handler = new CreateCategoryHandler(_db, _uow, new CreateCategoryValidator());
        var request = new CreateCategoryRequest(
            Slug: CategoryDefaults.FrontendWebSlug,
            NameEn: "Duplicate Frontend",
            NameVi: "Frontend trùng",
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.SlugConflict");
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenValuesIdentical_ShouldReturnNoOpWithoutIncrementingVersion()
    {
        // Arrange
        var handler = new UpdateCategoryHandler(_uow, new UpdateCategoryValidator());
        var category = await _db.DocumentCategories.FirstAsync(c => c.Id == CategoryDefaults.FrontendWebId);

        var request = new UpdateCategoryRequest(
            Id: category.Id,
            NameEn: category.NameEn,
            NameVi: category.NameVi,
            SortOrder: category.SortOrder,
            IsActive: category.IsActive,
            ExpectedVersion: category.ConcurrencyVersion,
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(category.ConcurrencyVersion);
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
        (await _db.DocumentCategories.AsNoTracking().SingleAsync(c => c.Id == category.Id))
            .ConcurrencyVersion.Should().Be(request.ExpectedVersion);
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenIdenticalMetadataHasStaleVersion_RejectsConflict()
    {
        var category = await _db.DocumentCategories.AsNoTracking()
            .FirstAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        var handler = new UpdateCategoryHandler(_uow, new UpdateCategoryValidator());
        var request = new UpdateCategoryRequest(
            category.Id, category.NameEn, category.NameVi, category.SortOrder, category.IsActive,
            ExpectedVersion: category.ConcurrencyVersion + 1, ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.ConcurrencyConflict");
        (await _db.DocumentCategories.AsNoTracking().SingleAsync(c => c.Id == category.Id))
            .ConcurrencyVersion.Should().Be(category.ConcurrencyVersion);
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenIdenticalMetadataActorLostPermission_RejectsMutation()
    {
        var actor = await _db.Users.SingleAsync(u => u.Id == _adminUserId);
        actor.RoleId = RoleDefaults.LearnerRoleId;
        await _db.SaveChangesAsync();
        var category = await _db.DocumentCategories.AsNoTracking()
            .FirstAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        var handler = new UpdateCategoryHandler(_uow, new UpdateCategoryValidator());
        var request = new UpdateCategoryRequest(
            category.Id, category.NameEn, category.NameVi, category.SortOrder, category.IsActive,
            category.ConcurrencyVersion, ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenIdenticalMetadataSessionWasRevoked_RejectsMutation()
    {
        await _db.Users.Where(u => u.Id == _adminUserId)
            .ExecuteUpdateAsync(update => update.SetProperty(u => u.AuthenticationVersion, 2));
        var category = await _db.DocumentCategories.FirstAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        var request = new UpdateCategoryRequest(
            category.Id, category.NameEn, category.NameVi, category.SortOrder, category.IsActive,
            category.ConcurrencyVersion, ActorUserId: _adminUserId);

        var result = await new UpdateCategoryHandler(_uow, new UpdateCategoryValidator()).ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenTrackedActorLostPermission_UsesFreshDatabaseRole()
    {
        await _db.Users.Where(user => user.Id == _adminUserId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.RoleId, RoleDefaults.LearnerRoleId));
        var category = await _db.DocumentCategories.FirstAsync(item => item.Id == CategoryDefaults.FrontendWebId);
        var request = new UpdateCategoryRequest(
            category.Id, category.NameEn, category.NameVi, category.SortOrder, category.IsActive,
            category.ConcurrencyVersion, ActorUserId: _adminUserId);

        var result = await new UpdateCategoryHandler(_uow, new UpdateCategoryValidator()).ExecuteAsync(request);

        result.Error.Code.Should().Be("AUTH_FORBIDDEN");
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpdateCategoryHandler_WhenTrackedVersionIsStale_UsesFreshLockedVersion()
    {
        var category = await _db.DocumentCategories.FirstAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        await _db.DocumentCategories.Where(c => c.Id == category.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.ConcurrencyVersion, 2));
        var request = new UpdateCategoryRequest(
            category.Id, category.NameEn, category.NameVi, category.SortOrder, category.IsActive,
            ExpectedVersion: 1, ActorUserId: _adminUserId);

        var result = await new UpdateCategoryHandler(_uow, new UpdateCategoryValidator()).ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.ConcurrencyConflict");
    }

    [Fact]
    public async Task CategoryCommand_WhenStagedMutationIsRejected_DiscardsChangesBeforeIndependentAudit()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        var uow = new AdministrativeUnitOfWork(_db, options);
        var result = await uow.ExecuteCategoryCommandAsync<bool>(
            _adminUserId, 1, new[] { AdminPermission.ManageCategories },
            CategoryDefaults.FrontendWebId, 1, AdminAuditActions.CategoryUpdateMetadata, "staged-rejection",
            (_, target) =>
            {
                target!.UpdateMetadata("Must roll back", "Phải hoàn tác", 99, false, 1);
                return Task.FromResult(Result<(bool, AdminAuditEvent?)>.Failure(
                    Error.Custom("Category.InUse", "Cannot change this category.")));
            });

        result.Error.Code.Should().Be("Category.InUse");
        _db.Database.CurrentTransaction.Should().BeNull();
        var current = await _db.DocumentCategories.AsNoTracking().SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        current.NameEn.Should().NotBe("Must roll back");
        current.ConcurrencyVersion.Should().Be(1);
        var audit = await _db.AdminAuditEvents.SingleAsync();
        audit.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit.ErrorCode.Should().Be("Category.InUse");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateCategoryHandler_WhenAuditPersistenceFails_ReturnsSafeUnavailableAndRollsBack(bool rejectionFails)
    {
        var interceptor = new FailingAuditInterceptor();
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(interceptor).Options;
        await using var db = new DeepPaceDbContext(options);
        var handler = new UpdateCategoryHandler(new AdministrativeUnitOfWork(db, options), new UpdateCategoryValidator());
        var request = new UpdateCategoryRequest(
            CategoryDefaults.FrontendWebId, "Must roll back", "Phải hoàn tác", 99, false,
            ExpectedVersion: rejectionFails ? 999 : 1, ActorUserId: _adminUserId);

        var result = await handler.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");
        result.Error.Message.Should().NotContain("secret-provider-detail");
        db.Database.CurrentTransaction.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
        var current = await _db.DocumentCategories.AsNoTracking().SingleAsync(c => c.Id == CategoryDefaults.FrontendWebId);
        current.NameEn.Should().NotBe("Must roll back");
        current.ConcurrencyVersion.Should().Be(1);
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("23505", "IX_DocumentCategories_Slug", "Category.SlugConflict")]
    [InlineData("23505", "unrelated-constraint", "AUDIT_PERSISTENCE_UNAVAILABLE")]
    [InlineData("40001", null, "Category.ConcurrencyConflict")]
    [InlineData("40P01", null, "Category.ConcurrencyConflict")]
    public async Task CategoryCommand_WhenSaveFails_MapsOnlyKnownConflictsAndDiscardsStagedMutation(
        string sqlState, string? constraintName, string expectedError)
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection)
            .AddInterceptors(new FailedAcceptedSaveInterceptor(sqlState, constraintName)).Options;
        await using var db = new DeepPaceDbContext(options);
        var handler = new UpdateCategoryHandler(new AdministrativeUnitOfWork(db, options), new UpdateCategoryValidator());

        var result = await handler.ExecuteAsync(new UpdateCategoryRequest(
            CategoryDefaults.FrontendWebId, "Must roll back", "Phải hoàn tác", 99, false, 1, ActorUserId: _adminUserId));

        result.Error.Code.Should().Be(expectedError);
        result.Error.Message.Should().NotContain("secret-provider-detail");
        db.Database.CurrentTransaction.Should().BeNull();
        var current = await _db.DocumentCategories.AsNoTracking().SingleAsync(category => category.Id == CategoryDefaults.FrontendWebId);
        current.ConcurrencyVersion.Should().Be(1);
        current.NameEn.Should().NotBe("Must roll back");
        (await _db.AdminAuditEvents.CountAsync(audit => audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(0);
        (await _db.AdminAuditEvents.CountAsync(audit => audit.Outcome == AdminAuditOutcome.Rejected))
            .Should().Be(expectedError == "AUDIT_PERSISTENCE_UNAVAILABLE" ? 0 : 1);
    }

    private sealed class FailedAcceptedSaveInterceptor(string sqlState, string? constraintName) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AdminAuditEvent>()
                .Any(entry => entry.Entity.Outcome == AdminAuditOutcome.Accepted))
                throw new DbUpdateException("secret-provider-detail",
                    new PostgresException("secret-provider-detail", "ERROR", "ERROR", sqlState, constraintName: constraintName));
            return ValueTask.FromResult(result);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCategoryHandler_WhenExpectedVersionIsInvalid_ReturnsAuditedValidationFailure(int expectedVersion)
    {
        var result = await new UpdateCategoryHandler(_uow, new UpdateCategoryValidator()).ExecuteAsync(
            new UpdateCategoryRequest(CategoryDefaults.FrontendWebId, "Name", "Tên", 1, true,
                expectedVersion, ActorUserId: _adminUserId));

        result.Error.Code.Should().Be("Category.ValidationFailed");
        var rejection = await _db.AdminAuditEvents.SingleAsync();
        rejection.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        rejection.ErrorCode.Should().Be("Category.ValidationFailed");
    }

    [Fact]
    public async Task CreateCategoryHandler_WhenAuthorityReadFails_ReturnsSafeUnavailable()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(new FailingReadInterceptor()).Options;
        await using var db = new DeepPaceDbContext(options);
        var handler = new CreateCategoryHandler(db, new AdministrativeUnitOfWork(db, options), new CreateCategoryValidator());

        var result = await handler.ExecuteAsync(new CreateCategoryRequest(
            "unavailable-create", "Unavailable", "Không khả dụng", ActorUserId: _adminUserId));

        result.Error.Code.Should().Be("Category.Unavailable");
        result.Error.Message.Should().NotContain("secret-provider-detail");
        db.Database.CurrentTransaction.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
        (await _db.DocumentCategories.CountAsync(category => category.Slug == "unavailable-create")).Should().Be(0);
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateCategoryHandler_WhenAuditFails_DoesNotPersistCategory()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(new FailingAuditInterceptor()).Options;
        await using var db = new DeepPaceDbContext(options);
        var handler = new CreateCategoryHandler(db, new AdministrativeUnitOfWork(db, options), new CreateCategoryValidator());

        var result = await handler.ExecuteAsync(new CreateCategoryRequest(
            "audit-failed-create", "Audit failure", "Lỗi kiểm toán", ActorUserId: _adminUserId));

        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");
        (await _db.DocumentCategories.CountAsync(category => category.Slug == "audit-failed-create")).Should().Be(0);
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteCategoryHandler_WhenAuditFails_DoesNotDeleteOrBumpVersion()
    {
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(new FailingAuditInterceptor()).Options;
        await using var db = new DeepPaceDbContext(options);
        var handler = new DeleteCategoryHandler(db, new AdministrativeUnitOfWork(db, options), new DeleteCategoryValidator());

        var result = await handler.ExecuteAsync(new DeleteCategoryRequest(
            CategoryDefaults.FrontendWebId, 1, ActorUserId: _adminUserId));

        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");
        var category = await _db.DocumentCategories.AsNoTracking().SingleAsync(item => item.Id == CategoryDefaults.FrontendWebId);
        category.IsDeleted.Should().BeFalse();
        category.ConcurrencyVersion.Should().Be(1);
        (await _db.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    private sealed class FailingReadInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<DbDataReader>>(new SqliteException("secret-provider-detail", 14));
    }

    private sealed class FailingAuditInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AdminAuditEvent>().Any())
                throw new DbUpdateException("secret-provider-detail");
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task UpdateCategoryHandler_WithChangedMetadata_ShouldUpdateAndIncrementVersion()
    {
        // Arrange
        var handler = new UpdateCategoryHandler(_uow, new UpdateCategoryValidator());
        var request = new UpdateCategoryRequest(
            Id: CategoryDefaults.DatabaseStorageId,
            NameEn: "Modern Databases",
            NameVi: "Cơ sở dữ liệu hiện đại",
            SortOrder: 2,
            IsActive: true,
            ExpectedVersion: 1,
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NameEn.Should().Be("Modern Databases");
        result.Value.Version.Should().Be(2);

        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.TargetId == CategoryDefaults.DatabaseStorageId.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.CategoryUpdateMetadata);
    }

    [Fact]
    public async Task UpdateCategoryHandler_WithConcurrencyMismatch_ShouldFailAndRecordRejection()
    {
        // Arrange
        var handler = new UpdateCategoryHandler(_uow, new UpdateCategoryValidator());
        var request = new UpdateCategoryRequest(
            Id: CategoryDefaults.SystemDesignId,
            NameEn: "New Name",
            NameVi: "Tên mới",
            SortOrder: 3,
            IsActive: true,
            ExpectedVersion: 999, // Stale version
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.ConcurrencyConflict");

        var rejectionAudit = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.TargetId == CategoryDefaults.SystemDesignId.ToString() && a.Outcome == AdminAuditOutcome.Rejected);
        rejectionAudit.Should().NotBeNull();
        rejectionAudit!.ErrorCode.Should().Be("Category.ConcurrencyConflict");
    }

    [Fact]
    public async Task DeleteCategoryHandler_WhenReferencedByBook_ShouldReturnCategoryInUse()
    {
        // Arrange
        var handler = new DeleteCategoryHandler(_db, _uow, new DeleteCategoryValidator());

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Test Book",
            Slug = "test-book",
            CategoryId = CategoryDefaults.BackendRuntimeId,
            SourceType = SourceType.PdfBook,
            IsPublished = true
        };
        _db.DocumentBooks.Add(book);
        await _db.SaveChangesAsync();

        var request = new DeleteCategoryRequest(
            Id: CategoryDefaults.BackendRuntimeId,
            ExpectedVersion: 1,
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.InUse");
        var audit = await _db.AdminAuditEvents.SingleAsync();
        audit.Outcome.Should().Be(AdminAuditOutcome.Rejected);
        audit.ErrorCode.Should().Be("Category.InUse");
    }

    [Fact]
    public async Task DeleteCategoryHandler_WhenUnreferenced_ShouldSoftDeleteCategory()
    {
        // Arrange
        var unused = DocumentCategory.Create("unused-to-delete", "Unused", "Không dùng", 99, true);
        _db.DocumentCategories.Add(unused);
        await _db.SaveChangesAsync();

        var handler = new DeleteCategoryHandler(_db, _uow, new DeleteCategoryValidator());
        var request = new DeleteCategoryRequest(
            Id: unused.Id,
            ExpectedVersion: 1,
            ActorUserId: _adminUserId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deleted = await _db.DocumentCategories.IgnoreQueryFilters().FirstAsync(c => c.Id == unused.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.ConcurrencyVersion.Should().Be(2);

        var audit = await _db.AdminAuditEvents.FirstOrDefaultAsync(a => a.TargetId == unused.Id.ToString());
        audit.Should().NotBeNull();
        audit!.Action.Should().Be(AdminAuditActions.CategoryDelete);
    }

    [Fact]
    public async Task GetAdminCategoryByIdHandler_WithValidId_ShouldReturnUsageCountsAndCanDelete()
    {
        // Arrange
        var handler = new GetAdminCategoryByIdHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(CategoryDefaults.FrontendWebId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Slug.Should().Be(CategoryDefaults.FrontendWebSlug);
        result.Value.CanDelete.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoriesHandler_ShouldReturnAllNonDeletedCategoriesOrderedBySortOrderThenSlug()
    {
        // Arrange
        var handler = new GetCategoriesHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetCategoriesRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCountGreaterOrEqualTo(7);
        result.Value.Should().BeInAscendingOrder(c => c.SortOrder);
    }
}
