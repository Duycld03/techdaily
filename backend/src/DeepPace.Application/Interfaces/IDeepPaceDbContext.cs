using Microsoft.EntityFrameworkCore;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IDeepPaceDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<AuthorizationInitialization> AuthorizationInitializations { get; }
    DbSet<InterviewQuestion> InterviewQuestions { get; }
    DbSet<DocumentBook> DocumentBooks { get; }
    DbSet<DocumentCategory> DocumentCategories { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<DocumentChunkAudio> DocumentChunkAudios { get; }
    DbSet<DailyDrill> DailyDrills { get; }
    DbSet<SpacedRepetitionCard> SpacedRepetitionCards { get; }
    DbSet<StreakRecord> StreakRecords { get; }
    DbSet<UserHighlight> UserHighlights { get; }
    DbSet<TermExplanationCache> TermExplanationCaches { get; }
    DbSet<TechInsight> TechInsights { get; }
    DbSet<UserInsightBookmark> UserInsightBookmarks { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<UserQuizProgress> UserQuizProgresses { get; }
    DbSet<UserBookPacer> UserBookPacers { get; }
    DbSet<UserPushSubscription> UserPushSubscriptions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailOtp> EmailOtps { get; }
    DbSet<CatalogDocument> CatalogDocuments { get; }
    DbSet<CatalogSlice> CatalogSlices { get; }
    DbSet<AdminAuditEvent> AdminAuditEvents { get; }
    DbSet<IngestionAttempt> IngestionAttempts { get; }
    DbSet<IngestionSource> IngestionSources { get; }
    DbSet<AccountSourceQuota> AccountSourceQuotas { get; }
    DbSet<StorageReservation> StorageReservations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
