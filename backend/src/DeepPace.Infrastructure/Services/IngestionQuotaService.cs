using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Infrastructure.Services;

public class IngestionQuotaService : IIngestionQuotaService
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly long _globalQuotaBytes;
    private readonly long _perAccountQuotaBytes;

    public IngestionQuotaService(
        IDeepPaceDbContext dbContext,
        IConfiguration configuration)
    {
        _dbContext = dbContext;

        var globalCfg = configuration["Ingestion:GlobalSourceQuotaBytes"]
            ?? configuration["Ingestion__GlobalSourceQuotaBytes"];
        _globalQuotaBytes = long.TryParse(globalCfg, out var g) && g > 0
            ? g
            : 5L * 1024 * 1024 * 1024; // 5 GiB

        var perAccCfg = configuration["Ingestion:PerAccountSourceQuotaBytes"]
            ?? configuration["Ingestion__PerAccountSourceQuotaBytes"];
        _perAccountQuotaBytes = long.TryParse(perAccCfg, out var a) && a > 0
            ? Math.Min(a, _globalQuotaBytes)
            : 1024L * 1024 * 1024; // 1 GiB
    }

    public async Task<StorageReservation> ReserveStorageAsync(
        Guid accountId,
        long bytes,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));
        }

        if (bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Reservation bytes must be positive.");
        }

        var now = DateTimeOffset.UtcNow;

        var quota = await _dbContext.AccountSourceQuotas
            .FirstOrDefaultAsync(q => q.AccountId == accountId, cancellationToken);

        if (quota == null)
        {
            quota = AccountSourceQuota.Create(accountId);
            _dbContext.AccountSourceQuotas.Add(quota);
        }

        var currentGlobalAllocated = await _dbContext.AccountSourceQuotas
            .SumAsync(q => q.ChargedBytes + q.ReservedBytes, cancellationToken);

        quota.Reserve(bytes, _perAccountQuotaBytes, _globalQuotaBytes, currentGlobalAllocated);

        var token = Guid.NewGuid().ToString("N");
        var reservation = StorageReservation.Create(token, accountId, bytes, now);
        _dbContext.StorageReservations.Add(reservation);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task CommitReservationAsync(
        string reservationToken,
        long actualChargedBytes,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.StorageReservations
            .FirstOrDefaultAsync(r => r.ReservationToken == reservationToken, cancellationToken);

        if (reservation == null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        reservation.Commit(now);

        var quota = await _dbContext.AccountSourceQuotas
            .FirstOrDefaultAsync(q => q.AccountId == reservation.AccountId, cancellationToken);

        if (quota != null)
        {
            quota.CommitReservation(reservation.ReservedBytes, actualChargedBytes);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseReservationAsync(
        string reservationToken,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _dbContext.StorageReservations
            .FirstOrDefaultAsync(r => r.ReservationToken == reservationToken, cancellationToken);

        if (reservation == null)
        {
            return;
        }

        if (reservation.Release())
        {
            var quota = await _dbContext.AccountSourceQuotas
                .FirstOrDefaultAsync(q => q.AccountId == reservation.AccountId, cancellationToken);

            if (quota != null)
            {
                quota.ReleaseReservation(reservation.ReservedBytes);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ReleaseChargedBytesAsync(
        Guid accountId,
        long bytes,
        CancellationToken cancellationToken = default)
    {
        if (bytes <= 0 || accountId == Guid.Empty)
        {
            return;
        }

        var quota = await _dbContext.AccountSourceQuotas
            .FirstOrDefaultAsync(q => q.AccountId == accountId, cancellationToken);

        if (quota != null)
        {
            quota.ReleaseChargedBytes(bytes);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
