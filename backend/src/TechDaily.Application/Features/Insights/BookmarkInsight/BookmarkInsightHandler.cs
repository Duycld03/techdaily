using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Insights.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Insights.BookmarkInsight;

public class BookmarkInsightHandler : IUseCase<BookmarkInsightRequest, BookmarkInsightResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public BookmarkInsightHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BookmarkInsightResponse>> ExecuteAsync(
        BookmarkInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        var insight = await _dbContext.TechInsights
            .FirstOrDefaultAsync(i => i.Id == request.InsightId, cancellationToken);

        if (insight == null)
        {
            return Error.NotFound;
        }

        insight.BookmarksCount++;
        insight.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BookmarkInsightResponse(insight.Id, true, insight.BookmarksCount);
    }
}
