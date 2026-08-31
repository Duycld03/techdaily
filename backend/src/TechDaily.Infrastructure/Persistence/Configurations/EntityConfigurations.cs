using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PreferredLocale).HasMaxLength(10).HasDefaultValue("en");

        builder.HasOne(u => u.StreakRecord)
            .WithOne(s => s.User)
            .HasForeignKey<StreakRecord>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).HasMaxLength(255).IsRequired();
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Difficulty).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.DayOrder).IsRequired();
        builder.HasIndex(t => t.DayOrder);

        builder.HasMany(t => t.InterviewQuestions)
            .WithOne(q => q.Topic)
            .HasForeignKey(q => q.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionText).IsRequired();
        builder.Property(q => q.ModelAnswerMarkdown).IsRequired();
        builder.Property(q => q.Difficulty).HasConversion<string>().HasMaxLength(50).IsRequired();
    }
}

public class DocumentBookConfiguration : IEntityTypeConfiguration<DocumentBook>
{
    public void Configure(EntityTypeBuilder<DocumentBook> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(255).IsRequired();
        builder.Property(b => b.Slug).HasMaxLength(255).IsRequired();
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.Property(b => b.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(b => b.Category).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasMany(b => b.Chunks)
            .WithOne(c => c.DocumentBook)
            .HasForeignKey(c => c.DocumentBookId)
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

        // Vector embedding (768 dimensions for Gemini text-embedding-004)
        builder.Property(c => c.Embedding).HasColumnType("vector(768)");

        // JSONB mapping for MicroQuizVo
        builder.OwnsOne(c => c.MicroQuiz, b => b.ToJson());
    }
}

public class DailyDrillConfiguration : IEntityTypeConfiguration<DailyDrill>
{
    public void Configure(EntityTypeBuilder<DailyDrill> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.ScheduledDate).IsRequired();
        builder.Property(d => d.UserAudioUrl).HasMaxLength(500);

        // Unique constraint: 1 daily drill per user per day per question
        builder.HasIndex(d => new { d.UserId, d.ScheduledDate, d.QuestionId }).IsUnique();

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

        builder.HasOne(d => d.AiReview)
            .WithOne(r => r.DailyDrill)
            .HasForeignKey<AiReview>(r => r.DailyDrillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AiReviewConfiguration : IEntityTypeConfiguration<AiReview>
{
    public void Configure(EntityTypeBuilder<AiReview> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Score).IsRequired();
        builder.Property(r => r.SummaryFeedback).IsRequired();
        builder.Property(r => r.ImprovedAnswerMarkdown).IsRequired();
        builder.Property(r => r.AiModelUsed).HasMaxLength(50).HasDefaultValue("gemini-2.5-flash");
    }
}

public class SpacedRepetitionCardConfiguration : IEntityTypeConfiguration<SpacedRepetitionCard>
{
    public void Configure(EntityTypeBuilder<SpacedRepetitionCard> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.EaseFactor).HasPrecision(5, 2).HasDefaultValue(2.50m);
        builder.Property(c => c.IntervalDays).HasDefaultValue(1);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.NextReviewDate).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.TopicId }).IsUnique();
        builder.HasIndex(c => new { c.UserId, c.NextReviewDate });

        builder.HasOne(c => c.User)
            .WithMany(u => u.SpacedRepetitionCards)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Topic)
            .WithMany(t => t.SpacedRepetitionCards)
            .HasForeignKey(c => c.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
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
        builder.Property(s => s.AverageScore).HasPrecision(4, 2).HasDefaultValue(0.00m);
        builder.HasIndex(s => s.UserId).IsUnique();
    }
}

public class UserHighlightConfiguration : IEntityTypeConfiguration<UserHighlight>
{
    public void Configure(EntityTypeBuilder<UserHighlight> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.SelectedText).IsRequired();

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
        builder.Property(t => t.Term).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(10).HasDefaultValue("en").IsRequired();
        builder.Property(t => t.ExplanationText).IsRequired();
        builder.Property(t => t.Embedding).HasColumnType("vector(768)");

        builder.HasIndex(t => new { t.Term, t.Locale }).IsUnique();
    }
}
