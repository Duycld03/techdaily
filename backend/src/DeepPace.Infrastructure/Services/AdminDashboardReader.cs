using System.Data;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class AdminDashboardReader(DeepPaceDbContext db, TimeProvider timeProvider) : IAdminDashboardReader
{
    public async Task<Result<AdminDashboardOverviewDto>> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // PostgreSQL repeatable-read keeps all aggregate queries on one snapshot.
            // SQLite's serializable transaction provides the equivalent test boundary.
            var isolation = db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"
                ? IsolationLevel.Serializable : IsolationLevel.RepeatableRead;
            await using var transaction = await db.Database.BeginTransactionAsync(isolation, cancellationToken);

            var accountCount = await db.Users.AsNoTracking().LongCountAsync(cancellationToken);
            var personalCounts = await db.DocumentBooks.AsNoTracking()
                .Where(book => book.CreatedByUserId != null)
                .GroupBy(book => book.Status)
                .Select(group => new { Status = group.Key, Count = group.LongCount() })
                .ToListAsync(cancellationToken);
            var catalogCounts = await db.CatalogDocuments.AsNoTracking()
                .GroupBy(document => new { document.Visibility, document.ProcessingStatus })
                .Select(group => new
                {
                    group.Key.Visibility,
                    Status = group.Key.ProcessingStatus,
                    Count = group.LongCount()
                })
                .ToListAsync(cancellationToken);

            DocumentProcessingCountsDto PersonalProcessing() => new(
                personalCounts.Where(item => item.Status == ProcessingStatus.Pending).Sum(item => item.Count),
                personalCounts.Where(item => item.Status == ProcessingStatus.Processing).Sum(item => item.Count),
                personalCounts.Where(item => item.Status == ProcessingStatus.Ready).Sum(item => item.Count),
                personalCounts.Where(item => item.Status == ProcessingStatus.Failed).Sum(item => item.Count));
            var catalogProcessing = new DocumentProcessingCountsDto(
                catalogCounts.Where(item => item.Status == ProcessingStatus.Pending).Sum(item => item.Count),
                catalogCounts.Where(item => item.Status == ProcessingStatus.Processing).Sum(item => item.Count),
                catalogCounts.Where(item => item.Status == ProcessingStatus.Ready).Sum(item => item.Count),
                catalogCounts.Where(item => item.Status == ProcessingStatus.Failed).Sum(item => item.Count));

            var overview = new AdminDashboardOverviewDto(
                accountCount,
                PersonalProcessing(),
                new AdminCatalogCountsDto(
                    catalogCounts.Sum(item => item.Count),
                    catalogCounts.Where(item => item.Visibility == CatalogVisibility.Draft).Sum(item => item.Count),
                    catalogCounts.Where(item => item.Visibility == CatalogVisibility.Published).Sum(item => item.Count),
                    catalogCounts.Where(item => item.Visibility == CatalogVisibility.Hidden).Sum(item => item.Count),
                    catalogProcessing),
                timeProvider.GetUtcNow());
            await transaction.CommitAsync(cancellationToken);
            return overview;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Error.Custom("ADMIN_DASHBOARD_UNAVAILABLE", "Dashboard metrics are temporarily unavailable.");
        }
    }
}
