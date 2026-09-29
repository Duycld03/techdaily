using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Features.Review.GetReviewAnalytics;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class GetReviewAnalyticsHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public GetReviewAnalyticsHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Guid> SeedUserAsync()
    {
        var userId = Guid.NewGuid();
        await _db.Users.AddAsync(new User
        {
            Id = userId,
            Email = $"dev_{Guid.NewGuid()}@techdaily.local",
            Name = "Retention Analytics Dev"
        });
        await _db.SaveChangesAsync();
        return userId;
    }

    private static SpacedRepetitionCard Card(Guid userId, CardSourceType source, string front)
        => new()
        {
            UserId = userId,
            SourceType = source,
            FrontMarkdown = front,
            BackMarkdown = $"{front} back"
        };

    [Fact]
    public async Task GetReviewAnalytics_ShouldComputeAtRiskMaturityDifficultyAndSourceBreakdown()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var baseDate = new DateOnly(2026, 6, 1);

        // A: DocumentChunk, overdue AND leech (both) -> tests union dedup.
        //    One blackout drops EaseFactor 2.50 -> 1.70; due 06-02.
        var a = Card(userId, CardSourceType.DocumentChunk, "A drill");
        a.ApplyReview(0, baseDate);

        // B: QuizMistake, leech only (EaseFactor 1.70, due exactly on target date -> not overdue).
        var b = Card(userId, CardSourceType.QuizMistake, "B quiz");
        b.ApplyReview(0, baseDate.AddDays(9));

        // C: QuizMistake, overdue only (EaseFactor 2.36, Reviewing, due 06-02).
        var c = Card(userId, CardSourceType.QuizMistake, "C quiz");
        c.ApplyReview(3, baseDate);

        // D: Highlight, mastered and comfortable, not overdue (four perfect recalls).
        var d = Card(userId, CardSourceType.Highlight, "D highlight");
        d.ApplyReview(5, baseDate);
        d.ApplyReview(5, baseDate);
        d.ApplyReview(5, baseDate);
        d.ApplyReview(5, baseDate);

        // E: Highlight, developing bucket (EaseFactor 1.96), not overdue.
        var e = Card(userId, CardSourceType.Highlight, "E highlight");
        e.ApplyReview(1, baseDate.AddDays(9));

        // Deleted card (ignored) and another user's card (ignored).
        var deleted = Card(userId, CardSourceType.Highlight, "deleted");
        deleted.SoftDelete();

        var otherUserId = Guid.NewGuid();
        await _db.Users.AddAsync(new User
        {
            Id = otherUserId,
            Email = $"other_{otherUserId}@techdaily.local",
            Name = "Other Dev"
        });
        var otherCard = Card(otherUserId, CardSourceType.DocumentChunk, "other");
        otherCard.ApplyReview(0, baseDate);

        await _db.SpacedRepetitionCards.AddRangeAsync(a, b, c, d, e, deleted, otherCard);
        await _db.SaveChangesAsync();

        var handler = new GetReviewAnalyticsHandler(_db);

        // Act — evaluate as of 06-11 so overdue classification is deterministic.
        var result = await handler.ExecuteAsync(
            new GetReviewAnalyticsRequest(userId, baseDate.AddDays(10)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var r = result.Value;

        // Scoped to the user's active cards only.
        r.TotalCards.Should().Be(5);

        // Maturity distribution.
        r.LearningCount.Should().Be(3);   // A, B, E
        r.ReviewingCount.Should().Be(1);  // C
        r.MasteredCount.Should().Be(1);   // D

        // At-risk segment (union deduplicated: A is both overdue and a leech, counted once).
        r.OverdueCount.Should().Be(2);    // A, C
        r.LeechCount.Should().Be(2);      // A, B
        r.AtRiskCount.Should().Be(3);     // A, B, C

        // Difficulty buckets — boundary at 1.70 (inclusive struggling) and 2.10 (inclusive comfortable).
        r.StrugglingCount.Should().Be(2);   // A (1.70), B (1.70)
        r.DevelopingCount.Should().Be(1);   // E (1.96)
        r.ComfortableCount.Should().Be(2);  // C (2.36), D (2.50)

        // Source-channel breakdown ordered by source type (Highlight, QuizMistake, DocumentChunk).
        r.SourceBreakdown.Select(x => x.SourceType).Should().Equal(
            CardSourceType.Highlight, CardSourceType.QuizMistake, CardSourceType.DocumentChunk);

        var highlight = r.SourceBreakdown.Single(x => x.SourceType == CardSourceType.Highlight);
        highlight.Total.Should().Be(2);
        highlight.Learning.Should().Be(1);
        highlight.Reviewing.Should().Be(0);
        highlight.Mastered.Should().Be(1);
        highlight.AverageEaseFactor.Should().Be(2.23m); // (2.50 + 1.96) / 2

        var quiz = r.SourceBreakdown.Single(x => x.SourceType == CardSourceType.QuizMistake);
        quiz.Total.Should().Be(2);
        quiz.Learning.Should().Be(1);
        quiz.Reviewing.Should().Be(1);
        quiz.AverageEaseFactor.Should().Be(2.03m); // (1.70 + 2.36) / 2

        var drill = r.SourceBreakdown.Single(x => x.SourceType == CardSourceType.DocumentChunk);
        drill.Total.Should().Be(1);
        drill.Learning.Should().Be(1);
        drill.AverageEaseFactor.Should().Be(1.70m);
    }

    [Fact]
    public async Task GetReviewAnalytics_EmptyDeck_ShouldReturnZeroedAggregates()
    {
        // Arrange
        var userId = await SeedUserAsync();
        var handler = new GetReviewAnalyticsHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetReviewAnalyticsRequest(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var r = result.Value;
        r.TotalCards.Should().Be(0);
        r.LearningCount.Should().Be(0);
        r.ReviewingCount.Should().Be(0);
        r.MasteredCount.Should().Be(0);
        r.OverdueCount.Should().Be(0);
        r.LeechCount.Should().Be(0);
        r.AtRiskCount.Should().Be(0);
        r.StrugglingCount.Should().Be(0);
        r.DevelopingCount.Should().Be(0);
        r.ComfortableCount.Should().Be(0);
        r.SourceBreakdown.Should().BeEmpty();
    }
}
