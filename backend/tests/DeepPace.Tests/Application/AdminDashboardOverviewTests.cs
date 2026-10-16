using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DeepPace.Application.Features.AdminDashboard.GetOverview;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Application;

public class AdminDashboardOverviewTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly AggregateInterceptor _interceptor = new();
    private readonly DbContextOptions<DeepPaceDbContext> _options;

    public AdminDashboardOverviewTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection).AddInterceptors(_interceptor).Options;
        using var db = new DeepPaceDbContext(_options);
        db.Database.EnsureCreated();
        _interceptor.Commands.Clear();
    }

    [Fact]
    public async Task Overview_SeparatesCatalogFromAdoptedCopiesAndExcludesDeletedAndUnownedRows()
    {
        await using var db = new DeepPaceDbContext(_options);
        var user = new User { Email = "learner@example.test", Name = "Learner" };
        var secondUser = new User { Email = "second@example.test", Name = "Second" };
        db.Users.AddRange(user, secondUser, new User { Email = "deleted@example.test", IsDeleted = true });
        var catalog = new CatalogDocument
        {
            CategoryId = CategoryDefaults.SystemDesignId,
            Title = "Source", Visibility = CatalogVisibility.Published,
            ProcessingStatus = ProcessingStatus.Ready
        };
        db.CatalogDocuments.AddRange(catalog,
            new CatalogDocument { CategoryId = CategoryDefaults.SystemDesignId, Title = "Draft", ProcessingStatus = ProcessingStatus.Pending },
            new CatalogDocument { CategoryId = CategoryDefaults.SystemDesignId, Title = "Hidden", Visibility = CatalogVisibility.Hidden, ProcessingStatus = ProcessingStatus.Failed },
            new CatalogDocument { CategoryId = CategoryDefaults.SystemDesignId, Title = "Processing", ProcessingStatus = ProcessingStatus.Processing },
            new CatalogDocument { CategoryId = CategoryDefaults.SystemDesignId, Title = "Deleted", IsDeleted = true });
        db.DocumentBooks.AddRange(
            DocumentBook.CreateFromCatalogSnapshot(catalog, user.Id, "first-copy"),
            DocumentBook.CreateFromCatalogSnapshot(catalog, secondUser.Id, "second-copy"),
            new DocumentBook { CategoryId = CategoryDefaults.SystemDesignId, Slug = "personal", CreatedByUserId = user.Id, Status = ProcessingStatus.Failed },
            new DocumentBook { CategoryId = CategoryDefaults.SystemDesignId, Slug = "pending", CreatedByUserId = user.Id, Status = ProcessingStatus.Pending },
            new DocumentBook { CategoryId = CategoryDefaults.SystemDesignId, Slug = "processing", CreatedByUserId = user.Id, Status = ProcessingStatus.Processing },
            new DocumentBook { CategoryId = CategoryDefaults.SystemDesignId, Slug = "legacy", Status = ProcessingStatus.Failed },
            new DocumentBook { CategoryId = CategoryDefaults.SystemDesignId, Slug = "deleted", CreatedByUserId = user.Id, IsDeleted = true });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        _interceptor.Commands.Clear();

        var handler = new GetAdminDashboardOverviewHandler(new AdminDashboardReader(db, TimeProvider.System));
        var result = await handler.ExecuteAsync(new GetAdminDashboardOverviewRequest());

        result.IsSuccess.Should().BeTrue();
        var overview = result.Value;
        overview.AccountCount.Should().Be(2);
        overview.PersonalDocuments.Total.Should().Be(5);
        overview.PersonalDocuments.Ready.Should().Be(2);
        overview.PersonalDocuments.Pending.Should().Be(1);
        overview.PersonalDocuments.Processing.Should().Be(1);
        overview.PersonalDocuments.Failed.Should().Be(1);
        overview.CatalogDocuments.Total.Should().Be(4);
        overview.CatalogDocuments.Draft.Should().Be(2);
        overview.CatalogDocuments.Published.Should().Be(1);
        overview.CatalogDocuments.Hidden.Should().Be(1);
        overview.CatalogDocuments.Processing.Total.Should().Be(4);
        overview.ObservedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        db.ChangeTracker.Entries().Should().BeEmpty();

        // Relational queries fetch aggregates only, within the same transaction.
        _interceptor.Commands.Should().HaveCount(3);
        _interceptor.Commands.Select(command => command.Transaction).Should().OnlyContain(tx => tx != null);
        _interceptor.Commands.Select(command => command.Transaction).Distinct().Should().HaveCount(1);
        var sql = string.Join("\n", _interceptor.Commands.Select(command => command.Sql));
        sql.Should().NotContain("OriginalTextMarkdown").And.NotContain("PasswordHash").And.NotContain("Email");
    }

    [Fact]
    public async Task Overview_EmptyDatabaseReturnsMeasuredZeroCounts()
    {
        await using var db = new DeepPaceDbContext(_options);
        var result = await new AdminDashboardReader(db, TimeProvider.System).ReadAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.AccountCount.Should().Be(0);
        result.Value.PersonalDocuments.Total.Should().Be(0);
        result.Value.CatalogDocuments.Total.Should().Be(0);
    }

    [Fact]
    public async Task Overview_DatabaseFailureReturnsUnavailableWithoutZeroMetrics()
    {
        await using var db = new DeepPaceDbContext(_options);
        await db.DisposeAsync();
        var result = await new AdminDashboardReader(db, TimeProvider.System).ReadAsync();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ADMIN_DASHBOARD_UNAVAILABLE");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Overview_PropagatesCancellation()
    {
        await using var db = new DeepPaceDbContext(_options);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var action = () => new AdminDashboardReader(db, TimeProvider.System).ReadAsync(cancellation.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class AggregateInterceptor : DbCommandInterceptor
    {
        public List<(string Sql, DbTransaction? Transaction)> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandText, command.Transaction));
            return ValueTask.FromResult(result);
        }
    }
}
