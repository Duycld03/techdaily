using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Categories;
using DeepPace.Application.Features.Admin.Categories.Validators;
using DeepPace.Application.Features.Admin.Categories.DTOs;
using DeepPace.Application.Interfaces;
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

public sealed class PostgresCategoryConcurrencyTests : IAsyncLifetime
{
    private readonly string _databaseName = $"category_concurrency_{Guid.NewGuid():N}";
    private readonly Guid _adminId = Guid.NewGuid();
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
        db.Users.Add(new User
        {
            Id = _adminId, Email = "category-admin@example.com", Name = "Category admin",
            RoleId = RoleDefaults.AdminRoleId, Status = AccountStatus.Active, AuthenticationVersion = 1
        });
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
    public async Task ConcurrentCreatesWithSameSlug_CommitExactlyOneCategoryAndRejectEveryLoser()
    {
        var barrier = new AcceptedSaveBarrier(4);
        var options = new DbContextOptionsBuilder<DeepPaceDbContext>(_options).AddInterceptors(barrier).Options;
        var tasks = Enumerable.Range(0, 4).Select(async index =>
        {
            await using var db = new DeepPaceDbContext(options);
            var handler = new CreateCategoryHandler(db, new AdministrativeUnitOfWork(db, options), new CreateCategoryValidator());
            return await handler.ExecuteAsync(new CreateCategoryRequest(
                "concurrent-slug", "Concurrent category", "Danh mục đồng thời", ActorUserId: _adminId,
                CorrelationId: $"duplicate-{index}"));
        });

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Where(result => result.IsFailure).Should().HaveCount(3)
            .And.OnlyContain(result => result.Error.Code == "Category.SlugConflict");
        await using var verify = new DeepPaceDbContext(_options);
        (await verify.DocumentCategories.CountAsync(category => category.Slug == "concurrent-slug")).Should().Be(1);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Action == AdminAuditActions.CategoryCreate && audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(1);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.ErrorCode == "Category.SlugConflict" && audit.Outcome == AdminAuditOutcome.Rejected)).Should().Be(3);
    }

    [Fact]
    public async Task ConcurrentUpdatesWithSameVersion_CommitOneAndRecordCleanConflict()
    {
        var tasks = Enumerable.Range(0, 2).Select(async index =>
        {
            await using var db = new DeepPaceDbContext(_options);
            var handler = new UpdateCategoryHandler(new AdministrativeUnitOfWork(db, _options), new UpdateCategoryValidator());
            return await handler.ExecuteAsync(new UpdateCategoryRequest(
                CategoryDefaults.FrontendWebId, $"Updated {index}", "Đã cập nhật", 10, true, 1, ActorUserId: _adminId));
        });

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30));

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Single(result => result.IsFailure).Error.Code.Should().Be("Category.ConcurrencyConflict");
        await using var verify = new DeepPaceDbContext(_options);
        (await verify.DocumentCategories.SingleAsync(category => category.Id == CategoryDefaults.FrontendWebId)).ConcurrencyVersion.Should().Be(2);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Outcome == AdminAuditOutcome.Accepted)).Should().Be(1);
        (await verify.AdminAuditEvents.CountAsync(audit => audit.Outcome == AdminAuditOutcome.Rejected)).Should().Be(1);
    }

    [Fact]
    public async Task StagedRejectedMutation_ReleasesTransactionBeforeIndependentAudit()
    {
        await using var db = new DeepPaceDbContext(_options);
        var result = await new AdministrativeUnitOfWork(db, _options).ExecuteCategoryCommandAsync<bool>(
            _adminId, 1, new[] { AdminPermission.ManageCategories }, CategoryDefaults.FrontendWebId, 1,
            AdminAuditActions.CategoryUpdateMetadata, "rejected-staged",
            (_, category) =>
            {
                category!.UpdateMetadata("Must roll back", "Phải hoàn tác", 99, false, 1);
                return Task.FromResult(Result<(bool, AdminAuditEvent?)>.Failure(Error.Custom("Category.InUse", "Category is in use.")));
            });

        result.Error.Code.Should().Be("Category.InUse");
        db.Database.CurrentTransaction.Should().BeNull();
        await using var verify = new DeepPaceDbContext(_options);
        var current = await verify.DocumentCategories.SingleAsync(category => category.Id == CategoryDefaults.FrontendWebId);
        current.NameEn.Should().NotBe("Must roll back");
        current.ConcurrencyVersion.Should().Be(1);
        (await verify.AdminAuditEvents.SingleAsync()).Outcome.Should().Be(AdminAuditOutcome.Rejected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    public async Task AcceptedOrRejectedAuditFailure_ReturnsSafeUnavailableAndPersistsNothing(int expectedVersion)
    {
        await using var db = new DeepPaceDbContext(_options);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reject_category_audit() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'secret-provider-detail'; END $$;
            CREATE TRIGGER reject_category_audit BEFORE INSERT ON "AdminAuditEvents"
            FOR EACH ROW EXECUTE FUNCTION reject_category_audit();
            """);
        var handler = new UpdateCategoryHandler(new AdministrativeUnitOfWork(db, _options), new UpdateCategoryValidator());

        var result = await handler.ExecuteAsync(new UpdateCategoryRequest(
            CategoryDefaults.FrontendWebId, "Must roll back", "Phải hoàn tác", 99, false, expectedVersion, ActorUserId: _adminId));

        result.Error.Code.Should().Be("AUDIT_PERSISTENCE_UNAVAILABLE");
        result.Error.Message.Should().NotContain("secret-provider-detail");
        db.Database.CurrentTransaction.Should().BeNull();
        db.ChangeTracker.Entries().Should().BeEmpty();
        await using var verify = new DeepPaceDbContext(_options);
        (await verify.DocumentCategories.SingleAsync(category => category.Id == CategoryDefaults.FrontendWebId)).ConcurrencyVersion.Should().Be(1);
        (await verify.AdminAuditEvents.CountAsync()).Should().Be(0);
    }

    private sealed class AcceptedSaveBarrier(int participants) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _allStaged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrived;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AdminAuditEvent>()
                .Any(entry => entry.State == EntityState.Added && entry.Entity.Outcome == AdminAuditOutcome.Accepted))
            {
                if (Interlocked.Increment(ref _arrived) == participants) _allStaged.TrySetResult();
                await _allStaged.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
