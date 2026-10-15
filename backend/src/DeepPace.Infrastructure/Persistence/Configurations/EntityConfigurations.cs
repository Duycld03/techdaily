using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
namespace DeepPace.Infrastructure.Persistence.Configurations;

internal static class ConfigurationHelpers
{
    public static readonly ValueComparer<List<string>> StringListComparer = new(
        (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
        c => c.ToList());

    public static string SerializeStringList(List<string>? list)
    {
        return JsonSerializer.Serialize(list ?? new List<string>(), (JsonSerializerOptions)null!);
    }

    public static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions)null!) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static Category ParseCategory(string? v)
    {
        if (string.Equals(v, "BackendDotNet", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "DotNet", StringComparison.OrdinalIgnoreCase))
        {
            return Category.BackendRuntime;
        }

        return Enum.TryParse<Category>(v, true, out var cat) ? cat : Category.BackendRuntime;
    }

    public static readonly ValueConverter<Category, string> CategoryStringConverter = new(
        v => v.ToString(),
        v => ParseCategory(v)
    );
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(u => u.Name).HasMaxLength(255).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.GoogleSubjectId).HasMaxLength(255);
        builder.Property(u => u.PreferredLocale).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(u => u.TimeZone).HasMaxLength(100).HasDefaultValue("UTC");
        builder.Property(u => u.IsPushEnabled).HasDefaultValue(false);
        builder.Property(u => u.PreferredStudyTime);
        builder.Property(u => u.StreakAlertTime);
        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountStatus.Active)
            .IsRequired();
        builder.Property(u => u.AuthenticationVersion)
            .HasDefaultValue(1)
            .IsRequired();
        builder.Property(u => u.AdminControlVersion)
            .HasDefaultValue(1)
            .IsConcurrencyToken()
            .IsRequired();
        builder.Property(u => u.SuspendedByUserId);
        builder.Property(u => u.SuspendedAt);
        builder.Property(u => u.SuspensionReason).HasMaxLength(500);
        builder.Property(u => u.RoleId)
            .HasDefaultValue(RoleDefaults.LearnerRoleId)
            .IsRequired();
        builder.HasIndex(u => new { u.RoleId, u.Status });
        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.StreakRecord)
            .WithOne(s => s.User)
            .HasForeignKey<StreakRecord>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


public class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.DocumentChunkId).IsRequired(false);
        builder.HasIndex(q => q.DocumentChunkId);

        builder.HasOne(q => q.DocumentChunk)
            .WithMany(c => c.InterviewQuestions)
            .HasForeignKey(q => q.DocumentChunkId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.QuestionText).IsRequired();
        builder.Property(q => q.ModelAnswerMarkdown).IsRequired();
        builder.Property(q => q.Difficulty).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(q => q.CorrectOptionIndex).HasDefaultValue(0);
        builder.Property(q => q.ExplanationMarkdown).HasDefaultValue(string.Empty);

        builder.Property(q => q.Options)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.Property(q => q.ExpectedKeyPoints)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);
    }
}

public class DocumentBookConfiguration : IEntityTypeConfiguration<DocumentBook>
{
    public void Configure(EntityTypeBuilder<DocumentBook> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(255).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(255).IsRequired();
        builder.HasIndex(b => b.Slug)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(b => b.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(b => b.Category).HasConversion(ConfigurationHelpers.CategoryStringConverter).HasMaxLength(50).IsRequired();

        builder.Property(b => b.IsFeatured).HasDefaultValue(false);
        builder.HasIndex(b => b.IsFeatured);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(b => b.ProgressPercentage).HasDefaultValue(0);
        builder.Property(b => b.StatusMessage).HasMaxLength(500);
        builder.Property(b => b.ErrorMessage);
        builder.Property(b => b.IngestionControlVersion).HasDefaultValue(1).IsConcurrencyToken();
        builder.Property(b => b.CurrentAttemptId);
        builder.Property(b => b.CurrentAttemptGeneration).HasDefaultValue(0);
        builder.Property(b => b.DeclaredLanguage).HasMaxLength(10);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.CurrentAttemptId);
        builder.HasMany(b => b.Chunks)
            .WithOne(c => c.DocumentBook)
            .HasForeignKey(c => c.DocumentBookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.CreatedByUserId);
        builder.HasIndex(b => b.CreatedByUserId);
        builder.HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(b => b.SourceAttribution).HasMaxLength(2000);
        builder.HasIndex(b => new { b.CreatedByUserId, b.SourceCatalogDocumentId })
            .IsUnique()
            .HasFilter("\"CreatedByUserId\" IS NOT NULL AND \"SourceCatalogDocumentId\" IS NOT NULL AND \"IsDeleted\" = false");
    }
}

public class UserBookPacerConfiguration : IEntityTypeConfiguration<UserBookPacer>
{
    public void Configure(EntityTypeBuilder<UserBookPacer> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CurrentChunkOrder).HasDefaultValue(1);
        builder.Property(p => p.DailyPaceChunks).HasDefaultValue(1);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.LastReadDate);
        builder.Property(p => p.CompletedAt);

        builder.HasIndex(p => new { p.UserId, p.DocumentBookId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(p => new { p.UserId, p.IsActive });

        builder.HasOne(p => p.User)
            .WithMany(u => u.BookPacers)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.DocumentBook)
            .WithMany()
            .HasForeignKey(p => p.DocumentBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChapterTitle).HasMaxLength(255).IsRequired();
        builder.Property(c => c.OriginalTextMarkdown).IsRequired();
        builder.Property(c => c.SummaryMarkdown).IsRequired();
        builder.Property(c => c.Language).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(c => c.ChunkOrder).IsRequired();

        builder.Property(c => c.KeyTakeaways)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);
        builder.HasIndex(c => new { c.DocumentBookId, c.ChunkOrder })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class DailyDrillConfiguration : IEntityTypeConfiguration<DailyDrill>
{
    public void Configure(EntityTypeBuilder<DailyDrill> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.ScheduledDate).IsRequired();
        builder.Property(d => d.SelectedOptionIndex);
        builder.Property(d => d.IsCorrect);
        builder.Property(d => d.Score);

        builder.HasIndex(d => new { d.UserId, d.ScheduledDate, d.QuestionId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(d => d.User)
            .WithMany(u => u.DailyDrills)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Question)
            .WithMany(q => q.DailyDrills)
            .HasForeignKey(d => d.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DocumentChunk)
            .WithMany(c => c.DailyDrills)
            .HasForeignKey(d => d.DocumentChunkId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SpacedRepetitionCardConfiguration : IEntityTypeConfiguration<SpacedRepetitionCard>
{
    public void Configure(EntityTypeBuilder<SpacedRepetitionCard> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.FrontMarkdown);
        builder.Property(c => c.BackMarkdown);
        builder.Property(c => c.EaseFactor).HasPrecision(5, 2).HasDefaultValue(2.50m);
        builder.Property(c => c.IntervalDays).HasDefaultValue(1);
        builder.Property(c => c.RepetitionCount).HasDefaultValue(0);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.NextReviewDate).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.NextReviewDate });
        builder.HasIndex(c => new { c.UserId, c.SourceHighlightId });
        builder.HasIndex(c => new { c.UserId, c.SourceQuizQuestionId });
        builder.HasIndex(c => new { c.UserId, c.SourceDocumentChunkId });

        builder.HasOne(c => c.User)
            .WithMany(u => u.SpacedRepetitionCards)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasOne(c => c.SourceHighlight)
            .WithMany()
            .HasForeignKey(c => c.SourceHighlightId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.SourceQuizQuestion)
            .WithMany()
            .HasForeignKey(c => c.SourceQuizQuestionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.SourceDocumentChunk)
            .WithMany()
            .HasForeignKey(c => c.SourceDocumentChunkId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StreakRecordConfiguration : IEntityTypeConfiguration<StreakRecord>
{
    public void Configure(EntityTypeBuilder<StreakRecord> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CurrentStreak).HasDefaultValue(0);
        builder.Property(s => s.LongestStreak).HasDefaultValue(0);
        builder.Property(s => s.FreezeCreditsRemaining).HasDefaultValue(2);
        builder.Property(s => s.TotalDrillsCompleted).HasDefaultValue(0);
        builder.Property(s => s.AverageScore).HasPrecision(5, 2).HasDefaultValue(0.00m);

        builder.HasIndex(s => s.UserId).IsUnique();
    }
}

public class UserHighlightConfiguration : IEntityTypeConfiguration<UserHighlight>
{
    public void Configure(EntityTypeBuilder<UserHighlight> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.SelectedText).IsRequired();
        builder.Property(h => h.Note);

        builder.Property(h => h.Tags)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.HasOne(h => h.User)
            .WithMany(u => u.UserHighlights)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.DocumentChunk)
            .WithMany(c => c.Highlights)
            .HasForeignKey(h => h.DocumentChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TermExplanationCacheConfiguration : IEntityTypeConfiguration<TermExplanationCache>
{
    public void Configure(EntityTypeBuilder<TermExplanationCache> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Term).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(t => t.ExplanationText).IsRequired();
        builder.Property(t => t.HitCount).HasDefaultValue(1);

        builder.HasIndex(t => new { t.Term, t.Locale });
    }
}

public class TechInsightConfiguration : IEntityTypeConfiguration<TechInsight>
{
    public void Configure(EntityTypeBuilder<TechInsight> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).HasMaxLength(255).IsRequired();
        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Category).IsRequired();
        builder.Property(t => t.SummaryMarkdown).IsRequired();
        builder.Property(t => t.ProblemSnippet).IsRequired();
        builder.Property(t => t.SolutionSnippet).IsRequired();
        builder.Property(t => t.UnderTheHoodMarkdown).IsRequired();
        builder.Property(t => t.BenchmarkStats).HasMaxLength(255);
        builder.Property(t => t.SourceUrl).HasMaxLength(500);
        builder.Property(t => t.BookmarksCount).HasDefaultValue(0);
        builder.Property(t => t.IsPublished).HasDefaultValue(true);

        builder.Property(t => t.Tags)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.HasIndex(t => new { t.Category, t.IsPublished });
    }
}

public class UserInsightBookmarkConfiguration : IEntityTypeConfiguration<UserInsightBookmark>
{
    public void Configure(EntityTypeBuilder<UserInsightBookmark> builder)
    {
        builder.ToTable("UserInsightBookmarks");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.UserId, b.InsightId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Insight)
            .WithMany()
            .HasForeignKey(b => b.InsightId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("QuizQuestions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Topic).HasMaxLength(255).IsRequired();
        builder.Property(q => q.Category).IsRequired();
        builder.Property(q => q.Level).IsRequired();
        builder.Property(q => q.QuestionText).IsRequired();
        builder.Property(q => q.CorrectOptionIndex).HasDefaultValue(0);
        builder.Property(q => q.ExplanationMarkdown).IsRequired();

        builder.Property(q => q.Options)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.Property(q => q.Tags)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.HasIndex(q => new { q.Topic, q.Level });
        builder.HasIndex(q => new { q.Category, q.Level });

        builder.HasOne(q => q.CreatedByUser)
            .WithMany()
            .HasForeignKey(q => q.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(q => q.UserProgresses)
            .WithOne(p => p.Question)
            .HasForeignKey(p => p.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserQuizProgressConfiguration : IEntityTypeConfiguration<UserQuizProgress>
{
    public void Configure(EntityTypeBuilder<UserQuizProgress> builder)
    {
        builder.ToTable("UserQuizProgresses");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.IsMastered).HasDefaultValue(false);
        builder.Property(p => p.CorrectCount).HasDefaultValue(0);
        builder.Property(p => p.IncorrectCount).HasDefaultValue(0);

        builder.HasIndex(p => new { p.UserId, p.QuestionId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(p => new { p.UserId, p.IsMastered });

        builder.HasOne(p => p.User)
            .WithMany(u => u.QuizProgresses)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Question)
            .WithMany(q => q.UserProgresses)
            .HasForeignKey(p => p.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPushSubscriptionConfiguration : IEntityTypeConfiguration<UserPushSubscription>
{
    public void Configure(EntityTypeBuilder<UserPushSubscription> builder)
    {
        builder.ToTable("UserPushSubscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Endpoint).IsRequired();
        builder.Property(s => s.P256dh).IsRequired();
        builder.Property(s => s.Auth).IsRequired();
        builder.Property(s => s.UserAgent).HasMaxLength(500);

        builder.HasIndex(s => new { s.UserId, s.Endpoint })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(s => s.User)
            .WithMany(u => u.PushSubscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.Property(r => r.FamilyId).IsRequired();
        builder.HasIndex(r => r.FamilyId);
        builder.HasIndex(r => r.UserId);
        builder.Property(r => r.IsPersistent).IsRequired().HasDefaultValue(true);
        builder.Property(r => r.AuthenticationVersion).IsRequired().HasDefaultValue(1);

        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReplacedByToken)
            .WithMany()
            .HasForeignKey(r => r.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmailOtpConfiguration : IEntityTypeConfiguration<EmailOtp>
{
    public void Configure(EntityTypeBuilder<EmailOtp> builder)
    {
        builder.ToTable("EmailOtps");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Email).HasMaxLength(255).IsRequired();
        builder.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(o => o.PendingName).HasMaxLength(255);
        builder.Property(o => o.PendingPasswordHash).HasMaxLength(500);
        builder.Property(o => o.PendingLocale).HasMaxLength(10);
        builder.HasIndex(o => new { o.Email, o.Purpose });
    }
}

public class DocumentChunkAudioConfiguration : IEntityTypeConfiguration<DocumentChunkAudio>
{
    public void Configure(EntityTypeBuilder<DocumentChunkAudio> builder)
    {
        builder.ToTable("DocumentChunkAudios");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ContentHash).HasMaxLength(128).IsRequired();
        builder.Property(a => a.VoiceId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(100).HasDefaultValue("audio/mpeg").IsRequired();
        builder.Property(a => a.AudioData).IsRequired();
        builder.Property(a => a.CharacterCount).IsRequired();
        builder.Property(a => a.DurationSeconds).HasDefaultValue(0.0);

        builder.HasIndex(a => new { a.DocumentChunkId, a.ContentHash, a.VoiceId })
            .IsUnique()
            .HasDatabaseName("IX_DocumentChunkAudios_Lookup")
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(a => a.DocumentChunk)
            .WithMany(c => c.Audios)
            .HasForeignKey(a => a.DocumentChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CatalogDocumentConfiguration : IEntityTypeConfiguration<CatalogDocument>
{
    public void Configure(EntityTypeBuilder<CatalogDocument> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Description).IsRequired();
        builder.Property(c => c.Category).HasConversion(ConfigurationHelpers.CategoryStringConverter).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Language).HasMaxLength(10).HasDefaultValue("en").IsRequired();
        builder.Property(c => c.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.AuthorOrSourceUrl).HasMaxLength(1000);
        builder.Property(c => c.AttributionText).HasMaxLength(2000).IsRequired();


        builder.Property(c => c.Visibility).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Visibility);

        builder.Property(c => c.ProcessingStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.ProcessingStatus);

        builder.Property(c => c.TotalSlices).HasDefaultValue(0);
        builder.Property(c => c.EstimatedReadMinutes).HasDefaultValue(0);
        builder.Property(c => c.Revision).HasDefaultValue(1);
        builder.Property(c => c.ConcurrencyVersion).HasDefaultValue(1).IsConcurrencyToken();
        builder.Property(c => c.ProcessingGeneration).HasDefaultValue(0);
        builder.Property(c => c.ProcessingStatusMessage).HasMaxLength(500);
        builder.Property(c => c.CurrentAttemptId);
        builder.HasIndex(c => c.CurrentAttemptId);
        builder.HasMany(c => c.Slices)
            .WithOne(s => s.CatalogDocument)
            .HasForeignKey(s => s.CatalogDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.Visibility, c.PublishedAt });
    }
}

public class CatalogSliceConfiguration : IEntityTypeConfiguration<CatalogSlice>
{
    public void Configure(EntityTypeBuilder<CatalogSlice> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ChapterTitle).HasMaxLength(255).IsRequired();
        builder.Property(s => s.OriginalTextMarkdown).IsRequired();
        builder.Property(s => s.SummaryMarkdown).IsRequired();
        builder.Property(s => s.Language).HasMaxLength(10).HasDefaultValue("en");
        builder.Property(s => s.SliceOrder).IsRequired();

        builder.Property(s => s.KeyTakeaways)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.HasIndex(s => new { s.CatalogDocumentId, s.SliceOrder })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class AdminAuditEventConfiguration : IEntityTypeConfiguration<AdminAuditEvent>
{
    public void Configure(EntityTypeBuilder<AdminAuditEvent> builder)
    {
        builder.ToTable("AdminAuditEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TargetType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TargetId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Outcome).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.ChangedFields)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);
        builder.Property(e => e.StatePayload);

        builder.HasIndex(e => new { e.OccurredAt, e.Id });
        builder.HasIndex(e => new { e.ActorUserId, e.OccurredAt, e.Id });
        builder.HasIndex(e => new { e.TargetType, e.TargetId, e.OccurredAt, e.Id });
    }
}

public class IngestionAttemptConfiguration : IEntityTypeConfiguration<IngestionAttempt>
{
    public void Configure(EntityTypeBuilder<IngestionAttempt> builder)
    {
        builder.ToTable("IngestionAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TargetScope).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.TargetDocumentId).IsRequired();
        builder.Property(a => a.Origin).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.RequestId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.CommandFingerprint).HasMaxLength(256);
        builder.Property(a => a.Generation).IsRequired();
        builder.Property(a => a.Stage).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.State).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.ProgressPercent).HasDefaultValue(0);
        builder.Property(a => a.ClaimToken).HasMaxLength(100);
        builder.Property(a => a.ErrorCode).HasMaxLength(100);
        builder.Property(a => a.SafeErrorGuidance).HasMaxLength(1000);

        builder.HasIndex(a => new { a.TargetScope, a.TargetDocumentId });
        builder.HasIndex(a => a.QueuedAtUtc);
        builder.HasIndex(a => new { a.State, a.ClaimExpiresAtUtc });
        builder.HasIndex(a => new { a.TargetScope, a.TargetDocumentId })
            .IsUnique()
            .HasFilter("\"State\" IN ('Queued', 'Running')");
    }
}

public class IngestionSourceConfiguration : IEntityTypeConfiguration<IngestionSource>
{
    public void Configure(EntityTypeBuilder<IngestionSource> builder)
    {
        builder.ToTable("IngestionSources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SourceKind).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.StorageIdentity).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ByteLength).IsRequired();
        builder.Property(s => s.IntegritySha256).HasMaxLength(128).IsRequired();
        builder.Property(s => s.QuotaAccountId).IsRequired();
        builder.Property(s => s.DeclaredLanguage).HasMaxLength(10).HasDefaultValue("en").IsRequired();
        builder.Property(s => s.Availability).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(s => s.DeletionState).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(s => s.QuotaAccountId);
        builder.HasIndex(s => new { s.RetentionExpiresAtUtc, s.Availability });
    }
}

public class AccountSourceQuotaConfiguration : IEntityTypeConfiguration<AccountSourceQuota>
{
    public void Configure(EntityTypeBuilder<AccountSourceQuota> builder)
    {
        builder.ToTable("AccountSourceQuotas");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.AccountId).IsRequired();
        builder.Property(q => q.ChargedBytes).HasDefaultValue(0);
        builder.Property(q => q.ReservedBytes).HasDefaultValue(0);

        builder.HasIndex(q => q.AccountId).IsUnique();
    }
}

public class StorageReservationConfiguration : IEntityTypeConfiguration<StorageReservation>
{
    public void Configure(EntityTypeBuilder<StorageReservation> builder)
    {
        builder.ToTable("StorageReservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReservationToken).HasMaxLength(100).IsRequired();
        builder.Property(r => r.AccountId).IsRequired();
        builder.Property(r => r.ReservedBytes).IsRequired();
        builder.Property(r => r.IsReleased).HasDefaultValue(false);
        builder.Property(r => r.IsCommitted).HasDefaultValue(false);

        builder.HasIndex(r => r.ReservationToken).IsUnique();
        builder.HasIndex(r => new { r.AccountId, r.ExpiresAtUtc });
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Key).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => r.Key)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.Property(r => r.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(r => r.NameVi).HasMaxLength(100).IsRequired();
        builder.Property(r => r.IsProtected).HasDefaultValue(false).IsRequired();

        var seedCreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new
            {
                Id = RoleDefaults.LearnerRoleId,
                Key = RoleDefaults.LearnerKey,
                NameEn = "Learner",
                NameVi = "Người học",
                IsProtected = true,
                CreatedAt = seedCreatedAt,
                IsDeleted = false
            },
            new
            {
                Id = RoleDefaults.AdminRoleId,
                Key = RoleDefaults.AdminKey,
                NameEn = "Administrator",
                NameVi = "Quản trị viên",
                IsProtected = true,
                CreatedAt = seedCreatedAt,
                IsDeleted = false
            }
        );
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionKey });
        builder.Property(rp => rp.PermissionKey).HasMaxLength(64).IsRequired();
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.Permissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ViewAdminDashboard },
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ManageCatalog },
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ManageAccounts },
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ReadAudit },
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ManageDocumentOperations },
            new { RoleId = RoleDefaults.AdminRoleId, PermissionKey = PermissionKeys.ManageRoles }
        );
    }
}

public class AuthorizationInitializationConfiguration : IEntityTypeConfiguration<AuthorizationInitialization>
{
    public void Configure(EntityTypeBuilder<AuthorizationInitialization> builder)
    {
        builder.HasKey(ai => ai.Scope);
        builder.Property(ai => ai.Scope).HasMaxLength(64).IsRequired();
        builder.Property(ai => ai.CompletedAt).IsRequired();
        builder.Property(ai => ai.ImportedAdminUserIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Guid>(), (JsonSerializerOptions)null!),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Guid>()
                    : JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions)null!) ?? new List<Guid>())
            .HasColumnType("text")
            .IsRequired();
    }
}
