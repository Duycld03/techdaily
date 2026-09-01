using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Persistence.Configurations;

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
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(255).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.GoogleSubjectId).HasMaxLength(255);
        builder.Property(u => u.TelegramChatId);
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

        builder.Property(c => c.KeyTakeaways)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.OwnsOne(c => c.MicroQuiz, b =>
        {
            b.Property(q => q.Options)
                .HasConversion(
                    v => ConfigurationHelpers.SerializeStringList(v),
                    v => ConfigurationHelpers.DeserializeStringList(v))
                .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);
        });
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
        builder.Property(d => d.SelectedOptionIndex);
        builder.Property(d => d.IsCorrect);
        builder.Property(d => d.Score);

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
        builder.Property(r => r.AiModelUsed).HasMaxLength(100).IsRequired();

        builder.Property(r => r.Strengths)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);

        builder.Property(r => r.MissingPoints)
            .HasConversion(
                v => ConfigurationHelpers.SerializeStringList(v),
                v => ConfigurationHelpers.DeserializeStringList(v))
            .Metadata.SetValueComparer(ConfigurationHelpers.StringListComparer);
    }
}

public class SpacedRepetitionCardConfiguration : IEntityTypeConfiguration<SpacedRepetitionCard>
{
    public void Configure(EntityTypeBuilder<SpacedRepetitionCard> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.EaseFactor).HasPrecision(5, 2).HasDefaultValue(2.50m);
        builder.Property(c => c.IntervalDays).HasDefaultValue(1);
        builder.Property(c => c.RepetitionCount).HasDefaultValue(0);
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
