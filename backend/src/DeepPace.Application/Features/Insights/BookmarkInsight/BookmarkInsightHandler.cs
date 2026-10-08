using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Insights.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Insights.BookmarkInsight;

public class BookmarkInsightHandler : IUseCase<BookmarkInsightRequest, BookmarkInsightResponse>
{
    private readonly IDeepPaceDbContext _dbContext;

    public BookmarkInsightHandler(IDeepPaceDbContext dbContext)
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

        var existingBookmark = await _dbContext.UserInsightBookmarks
            .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.InsightId == request.InsightId, cancellationToken);

        bool isBookmarked;
        if (existingBookmark != null)
        {
            // Toggle off: remove bookmark
            _dbContext.UserInsightBookmarks.Remove(existingBookmark);
            insight.BookmarksCount = Math.Max(0, insight.BookmarksCount - 1);
            isBookmarked = false;
        }
        else
        {
            // Toggle on: add bookmark
            var newBookmark = new UserInsightBookmark
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                InsightId = request.InsightId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _dbContext.UserInsightBookmarks.AddAsync(newBookmark, cancellationToken);
            insight.BookmarksCount++;
            isBookmarked = true;
        }

        insight.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new BookmarkInsightResponse(insight.Id, isBookmarked, insight.BookmarksCount);
    }
}
