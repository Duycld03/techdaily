using System.Data.Common;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Features.Admin.Categories.Validators;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public sealed class PostgresCategoryAssignmentConcurrencyTests : IAsyncLifetime
{
    private static readonly TimeSpan BarrierTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);
    private readonly string _databaseName = $"category_assignment_{Guid.NewGuid():N}";
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();
    private readonly DocumentCategory _category = DocumentCategory.Create(
        $"assignment-race-{Guid.NewGuid():N}", "Assignment race category", "Danh mục kiểm tra đồng thời");
    private string _adminConnection = null!;
    private DbContextOptions<DeepPaceDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION must identify an explicitly provisioned PostgreSQL test server.");
        var builder = new NpgsqlConnectionStringBuilder(connection) { Database = "postgres", Pooling = false };
        _adminConnection = builder.ConnectionString;
        await using var admin = new NpgsqlConnection(_adminConnection);
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
        await create.ExecuteNonQueryAsync();
        builder.Database = _databaseName;
        _options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseNpgsql(builder.ConnectionString, postgres => postgres.UseVector()).Options;
        await using var db = new DeepPaceDbContext(_options);
        await db.Database.MigrateAsync();
        db.Users.AddRange(
            new User
            {
                Id = _adminId, Email = "assignment-admin@example.com", Name = "Assignment admin",
                RoleId = RoleDefaults.AdminRoleId, Status = AccountStatus.Active, AuthenticationVersion = 1
            },
            new User
            {
                Id = _learnerId, Email = "assignment-learner@example.com", Name = "Assignment learner",
                RoleId = RoleDefaults.LearnerRoleId, Status = AccountStatus.Active, AuthenticationVersion = 1
            });
        db.DocumentCategories.Add(_category);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_adminConnection is null) return;
        await using var admin = new NpgsqlConnection(_adminConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task DeactivationHoldingCategoryLock_RejectsWaitingAssignmentWithoutPersistingBook()
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        var held = new AsyncBarrier();
        var acceptedSave = new AcceptedCategorySaveInterceptor(_category.Id, AdminAuditActions.CategoryDeactivate, held.HoldAsync);
        var assignmentStarted = new CategoryLockStartedInterceptor(_category.Id, "FOR SHARE");
        var adminOptions = OptionsWith(acceptedSave);
        await using var admin = new DeepPaceDbContext(adminOptions);
        await using var learner = new DeepPaceDbContext(OptionsWith(assignmentStarted));
        var book = CreateBook();

        // Accepted audit staging occurs after the UOW has acquired the category FOR UPDATE lock.
        var deactivation = DeactivateAsync(admin, adminOptions, cancellation.Token);
        var (deactivated, assigned) = await CompleteContendedOperationsAsync(
            deactivation, held, () => AssignAsync(learner, book, null, cancellation.Token), assignmentStarted);

        deactivated.IsSuccess.Should().BeTrue();
        assigned.IsFailure.Should().BeTrue();
        assigned.Error.Code.Should().Be("Category.Inactive");
        await using var verify = new DeepPaceDbContext(_options);
        var category = await verify.DocumentCategories.SingleAsync(category => category.Id == _category.Id);
        category.IsActive.Should().BeFalse();
        category.ConcurrencyVersion.Should().Be(2);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryDeactivate
            && audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(1);
        await AssertBookStateAsync(verify, book, exists: false);
    }

    [Fact]
    public async Task AssignmentHoldingCategoryLock_CommitsBookBeforeWaitingDeactivation()
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        var held = new AsyncBarrier();
        var deactivationStarted = new CategoryLockStartedInterceptor(_category.Id, "FOR UPDATE");
        var book = CreateBook();
        var acceptedSave = new AcceptedCategorySaveInterceptor(
            _category.Id, AdminAuditActions.CategoryDeactivate, async ct =>
            {
                // A separate connection must see the committed book before the UOW may save or commit deactivation.
                await using var committed = new DeepPaceDbContext(_options);
                var persisted = await committed.DocumentBooks.IgnoreAutoIncludes().AsNoTracking()
                    .SingleAsync(source => source.Id == book.Id, ct);
                persisted.CategoryId.Should().Be(_category.Id);
                persisted.CreatedByUserId.Should().Be(_learnerId);
            });
        var adminOptions = OptionsWith(deactivationStarted, acceptedSave);
        await using var learner = new DeepPaceDbContext(_options);
        await using var admin = new DeepPaceDbContext(adminOptions);

        // Entering the mutation callback proves the guard already owns the category FOR SHARE lock.
        var assignment = AssignAsync(learner, book, held, cancellation.Token);
        var (assigned, deactivated) = await CompleteContendedOperationsAsync(
            assignment, held, () => DeactivateAsync(admin, adminOptions, cancellation.Token), deactivationStarted);

        assigned.IsSuccess.Should().BeTrue();
        assigned.Value.Should().Be(book.Id);
        deactivated.IsSuccess.Should().BeTrue();
        acceptedSave.Observed.Should().BeTrue();
        await using var verify = new DeepPaceDbContext(_options);
        var category = await verify.DocumentCategories.SingleAsync(category => category.Id == _category.Id);
        category.IsActive.Should().BeFalse();
        category.ConcurrencyVersion.Should().Be(2);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryDeactivate
            && audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(1);
        await AssertBookStateAsync(verify, book, exists: true);
    }

    [Fact]
    public async Task DeletionHoldingCategoryLock_RejectsWaitingAssignmentWithoutPersistingBook()
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        var held = new AsyncBarrier();
        var acceptedSave = new AcceptedCategorySaveInterceptor(_category.Id, AdminAuditActions.CategoryDelete, held.HoldAsync);
        var assignmentStarted = new CategoryLockStartedInterceptor(_category.Id, "FOR SHARE");
        var adminOptions = OptionsWith(acceptedSave);
        await using var admin = new DeepPaceDbContext(adminOptions);
        await using var learner = new DeepPaceDbContext(OptionsWith(assignmentStarted));
        var book = CreateBook();

        var deletion = DeleteAsync(admin, adminOptions, cancellation.Token);
        var (deleted, assigned) = await CompleteContendedOperationsAsync(
            deletion, held, () => AssignAsync(learner, book, null, cancellation.Token), assignmentStarted);

        deleted.IsSuccess.Should().BeTrue();
        deleted.Value.Should().BeTrue();
        assigned.IsFailure.Should().BeTrue();
        assigned.Error.Code.Should().Be("Category.ValidationFailed");
        await using var verify = new DeepPaceDbContext(_options);
        (await verify.DocumentCategories.AnyAsync(category => category.Id == _category.Id)).Should().BeFalse();
        var category = await verify.DocumentCategories.IgnoreQueryFilters().SingleAsync(category => category.Id == _category.Id);
        category.IsDeleted.Should().BeTrue();
        category.ConcurrencyVersion.Should().Be(2);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryDelete
            && audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(1);
        await AssertBookStateAsync(verify, book, exists: false);
    }

    [Fact]
    public async Task AssignmentHoldingCategoryLock_CommitsBookAndMakesWaitingDeletionRejectInUseCategory()
    {
        using var cancellation = new CancellationTokenSource(OperationTimeout);
        var held = new AsyncBarrier();
        var deletionStarted = new CategoryLockStartedInterceptor(_category.Id, "FOR UPDATE");
        var adminOptions = OptionsWith(deletionStarted);
        await using var learner = new DeepPaceDbContext(_options);
        await using var admin = new DeepPaceDbContext(adminOptions);
        var book = CreateBook();

        var assignment = AssignAsync(learner, book, held, cancellation.Token);
        var (assigned, deleted) = await CompleteContendedOperationsAsync(
            assignment, held, () => DeleteAsync(admin, adminOptions, cancellation.Token), deletionStarted);

        assigned.IsSuccess.Should().BeTrue();
        assigned.Value.Should().Be(book.Id);
        deleted.IsFailure.Should().BeTrue();
        deleted.Error.Code.Should().Be("Category.InUse");
        await using var verify = new DeepPaceDbContext(_options);
        var category = await verify.DocumentCategories.IgnoreQueryFilters().SingleAsync(category => category.Id == _category.Id);
        category.IsDeleted.Should().BeFalse();
        category.IsActive.Should().BeTrue();
        category.ConcurrencyVersion.Should().Be(1);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryDelete
            && audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(0);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryDelete
            && audit.Outcome == AdminAuditOutcome.Rejected && audit.ErrorCode == "Category.InUse")).Should().Be(1);
        await AssertBookStateAsync(verify, book, exists: true);
    }

    private DbContextOptions<DeepPaceDbContext> OptionsWith(params IInterceptor[] interceptors) =>
        new DbContextOptionsBuilder<DeepPaceDbContext>(_options).AddInterceptors(interceptors).Options;

    private DocumentBook CreateBook() => new()
    {
        Title = "Owned assignment race source",
        Slug = $"assignment-source-{Guid.NewGuid():N}",
        SourceType = SourceType.PdfBook,
        CategoryId = _category.Id,
        CreatedByUserId = _learnerId
    };

    private Task<Result<Guid>> AssignAsync(
        DeepPaceDbContext db, DocumentBook book, AsyncBarrier? held, CancellationToken ct) =>
        new CategoryAssignmentGuard(db).ExecuteAsync<Guid>(_category.Id, async category =>
        {
            category.Id.Should().Be(_category.Id);
            if (held != null) await held.HoldAsync(ct);
            db.DocumentBooks.Add(book);
            return Result<Guid>.Success(book.Id);
        }, ct);

    private Task<Result<CategoryDescriptorDto>> DeactivateAsync(
        DeepPaceDbContext db, DbContextOptions<DeepPaceDbContext> options, CancellationToken ct) =>
        new UpdateCategoryHandler(new AdministrativeUnitOfWork(db, options), new UpdateCategoryValidator())
            .ExecuteAsync(new UpdateCategoryRequest(
                _category.Id, _category.NameEn, _category.NameVi, _category.SortOrder,
                IsActive: false, ExpectedVersion: 1, ActorUserId: _adminId), ct);

    private Task<Result<bool>> DeleteAsync(
        DeepPaceDbContext db, DbContextOptions<DeepPaceDbContext> options, CancellationToken ct) =>
        new DeleteCategoryHandler(db, new AdministrativeUnitOfWork(db, options), new DeleteCategoryValidator())
            .ExecuteAsync(new DeleteCategoryRequest(_category.Id, ExpectedVersion: 1, ActorUserId: _adminId), ct);

    private async Task AssertBookStateAsync(DeepPaceDbContext verify, DocumentBook book, bool exists)
    {
        // Ignore both filters and auto-includes so an orphan cannot disappear behind its deleted category.
        var sources = await verify.DocumentBooks.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(source => source.CreatedByUserId == _learnerId || source.CategoryId == _category.Id).ToListAsync();
        if (exists)
        {
            var persisted = sources.Should().ContainSingle().Subject;
            persisted.Id.Should().Be(book.Id);
            persisted.Title.Should().Be(book.Title);
            persisted.CategoryId.Should().Be(_category.Id);
            persisted.CreatedByUserId.Should().Be(_learnerId);
        }
        else
        {
            sources.Should().BeEmpty();
        }

        var hasOrphans = await verify.DocumentBooks.IgnoreQueryFilters().IgnoreAutoIncludes()
            .AnyAsync(source => !verify.DocumentCategories.IgnoreQueryFilters()
                .Any(category => category.Id == source.CategoryId && !category.IsDeleted));
        hasOrphans.Should().BeFalse();
    }

    private static async Task<(Result<TFirst> First, Result<TSecond> Second)> CompleteContendedOperationsAsync<TFirst, TSecond>(
        Task<Result<TFirst>> first, AsyncBarrier held, Func<Task<Result<TSecond>>> startSecond,
        CategoryLockStartedInterceptor secondStarted)
    {
        Task<Result<TSecond>>? second = null;
        try
        {
            await held.Entered.WaitAsync(BarrierTimeout);
            second = startSecond();
            await secondStarted.Started.WaitAsync(BarrierTimeout);
            second.IsCompleted.Should().BeFalse("the first transaction still holds the conflicting category row lock");
        }
        finally
        {
            held.Release();
            // Drain both operations even when a coordination assertion fails, before disposing their contexts.
            if (second != null) await Task.WhenAll(first, second).WaitAsync(OperationTimeout);
            else await first.WaitAsync(OperationTimeout);
        }

        return (await first, await second!);
    }

    private sealed class AsyncBarrier
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Entered => _entered.Task;

        public async Task HoldAsync(CancellationToken ct)
        {
            _entered.TrySetResult();
            await _released.Task.WaitAsync(BarrierTimeout, ct);
        }

        public void Release() => _released.TrySetResult();
    }

    private sealed class AcceptedCategorySaveInterceptor(
        Guid categoryId, string action, Func<CancellationToken, Task> onAcceptedSave) : SaveChangesInterceptor
    {
        public bool Observed { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AdminAuditEvent>().Any(entry =>
                entry.State == EntityState.Added && entry.Entity.Outcome == AdminAuditOutcome.Accepted
                && entry.Entity.TargetType == "Category" && entry.Entity.TargetId == categoryId.ToString()
                && entry.Entity.Action == action))
            {
                eventData.Context.Database.CurrentTransaction.Should().NotBeNull();
                Observed = true;
                await onAcceptedSave(cancellationToken);
            }
            return result;
        }
    }

    private sealed class CategoryLockStartedInterceptor(Guid categoryId, string lockClause) : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Started => _started.Task;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("\"DocumentCategories\"", StringComparison.Ordinal)
                && command.CommandText.Contains(lockClause, StringComparison.Ordinal)
                && command.Parameters.Cast<DbParameter>().Any(parameter => parameter.Value is Guid id && id == categoryId))
            {
                _started.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }
}
