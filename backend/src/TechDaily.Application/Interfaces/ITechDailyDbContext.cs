using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Interfaces;

public interface ITechDailyDbContext
{
    DbSet<User> Users { get; }
    DbSet<Topic> Topics { get; }
    DbSet<InterviewQuestion> InterviewQuestions { get; }
    DbSet<DocumentBook> DocumentBooks { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<DailyDrill> DailyDrills { get; }
    DbSet<AiReview> AiReviews { get; }
    DbSet<SpacedRepetitionCard> SpacedRepetitionCards { get; }
    DbSet<StreakRecord> StreakRecords { get; }
    DbSet<UserHighlight> UserHighlights { get; }
    DbSet<TermExplanationCache> TermExplanationCaches { get; }
    DbSet<TechInsight> TechInsights { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
