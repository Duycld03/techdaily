using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class AdminSystemStatusService(DeepPaceDbContext db, TimeProvider timeProvider) : IAdminSystemStatusService
{
    public async Task<AdminDashboardStatusDto> ObserveAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var stopwatch = Stopwatch.StartNew();
        var connected = false;
        string? errorCode = null;
        try
        {
            connected = await db.Database.CanConnectAsync(linked.Token).WaitAsync(linked.Token);
            if (!connected) errorCode = "DATABASE_UNAVAILABLE";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            errorCode = "DATABASE_TIMEOUT";
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            errorCode = "DATABASE_UNAVAILABLE";
        }
        cancellationToken.ThrowIfCancellationRequested();
        var observedAt = timeProvider.GetUtcNow();
        return new AdminDashboardStatusDto(
            new ServiceObservationDto(connected ? "healthy" : "unavailable", observedAt,
                stopwatch.ElapsedMilliseconds, errorCode), observedAt);
    }
}
