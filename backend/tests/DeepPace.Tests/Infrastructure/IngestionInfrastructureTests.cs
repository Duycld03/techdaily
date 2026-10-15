using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class IngestionInfrastructureTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SqliteConnection _connection;
    private readonly DeepPaceDbContext _db;
    private readonly IConfiguration _config;
    private readonly Guid _userId = Guid.NewGuid();

    public IngestionInfrastructureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"deeppace_sources_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DeepPaceDbContext(options);
        _db.Database.EnsureCreated();

        var dict = new Dictionary<string, string?>
        {
            ["Ingestion:SourceStoragePath"] = _tempDir,
            ["Ingestion:GlobalSourceQuotaBytes"] = (100 * 1024).ToString(), // 100 KB global
            ["Ingestion:PerAccountSourceQuotaBytes"] = (20 * 1024).ToString() // 20 KB per account
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup error
        }
    }

    #region Task 4.2: Storage Tests

    [Fact]
    public async Task Storage_StoreAndOpenAndSafeDelete_ShouldSucceed()
    {
        var storage = new LocalFileIngestionSourceStorage(_config, NullLogger<LocalFileIngestionSourceStorage>.Instance);
        storage.IsStorageAccessible().Should().BeTrue();

        var content = "This is authoritative PDF source text.";
        using var inStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var identity = await storage.StoreSourceAsync(inStream, "sample.pdf");
        identity.Should().NotBeNullOrWhiteSpace();
        identity.Should().EndWith(".pdf");

        // Read back
        using var readStream = await storage.OpenSourceStreamAsync(identity);
        using var reader = new StreamReader(readStream, Encoding.UTF8);
        var readContent = await reader.ReadToEndAsync();
        readContent.Should().Be(content);

        // Delete
        var deleted = await storage.DeleteSourceAsync(identity);
        deleted.Should().BeTrue();

        // Second delete is safe/returns false
        var deletedAgain = await storage.DeleteSourceAsync(identity);
        deletedAgain.Should().BeFalse();
    }

    [Fact]
    public async Task Storage_PathTraversal_ShouldBeRejected()
    {
        var storage = new LocalFileIngestionSourceStorage(_config, NullLogger<LocalFileIngestionSourceStorage>.Instance);

        var act = () => storage.OpenSourceStreamAsync("../../../etc/passwd");
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Task 4.3: Quota Service Tests

    [Fact]
    public async Task QuotaService_ReserveStorage_ShouldEnforceLimits()
    {
        var quotaService = new IngestionQuotaService(_db, _config);

        // 10 KB within 20 KB limit succeeds
        var reservation = await quotaService.ReserveStorageAsync(_userId, 10 * 1024);
        reservation.Should().NotBeNull();
        reservation.ReservedBytes.Should().Be(10 * 1024);

        // Additional 15 KB exceeds 20 KB per-account limit -> throws
        var actExceed = () => quotaService.ReserveStorageAsync(_userId, 15 * 1024);
        await actExceed.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Account source quota exceeded*");
    }

    [Fact]
    public async Task QuotaService_CommitAndRelease_ShouldBalanceCorrectly()
    {
        var quotaService = new IngestionQuotaService(_db, _config);

        var reservation = await quotaService.ReserveStorageAsync(_userId, 10 * 1024);

        // Commit reservation for actual 8 KB
        await quotaService.CommitReservationAsync(reservation.ReservationToken, 8 * 1024);

        var quota = await _db.AccountSourceQuotas.FirstOrDefaultAsync(q => q.AccountId == _userId);
        quota.Should().NotBeNull();
        quota!.ReservedBytes.Should().Be(0);
        quota.ChargedBytes.Should().Be(8 * 1024);
        quota.TotalAllocatedBytes.Should().Be(8 * 1024);

        // Release charged bytes on physical deletion
        await quotaService.ReleaseChargedBytesAsync(_userId, 8 * 1024);
        quota = await _db.AccountSourceQuotas.FirstOrDefaultAsync(q => q.AccountId == _userId);
        quota!.ChargedBytes.Should().Be(0);
        quota.TotalAllocatedBytes.Should().Be(0);
    }

    #endregion

    #region Task 4.4: Claim Service Tests

    [Fact]
    public async Task ClaimService_TryClaimNextAttempt_ShouldPrioritizeQueuedAndRenewLease()
    {
        var claimService = new IngestionClaimService(_db);

        var attempt = IngestionAttempt.CreateQueued(
            IngestionTargetScope.Personal,
            Guid.NewGuid(),
            IngestionAttemptOrigin.UserUpload,
            "req-c1",
            generation: 1);

        _db.IngestionAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var claimed = await claimService.TryClaimNextAttemptAsync("worker-token-A", TimeSpan.FromMinutes(10));
        claimed.Should().NotBeNull();
        claimed!.Id.Should().Be(attempt.Id);
        claimed.State.Should().Be(IngestionAttemptState.Running);
        claimed.ClaimToken.Should().Be("worker-token-A");

        // Renew lease
        var renewed = await claimService.RenewClaimAsync(claimed.Id, "worker-token-A", TimeSpan.FromMinutes(15));
        renewed.Should().BeTrue();
    }

    #endregion
}
