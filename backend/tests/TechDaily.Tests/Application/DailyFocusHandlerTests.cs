using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.GetTodayFocus;
using TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Application;

public class DailyFocusHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public DailyFocusHandlerTests()
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

    [Fact]
    public async Task GetTodayFocus_ShouldReturnEmptyState_WhenUserHasNoReadyBooks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);
        var request = new GetTodayFocusRequest(userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasActiveBook.Should().BeFalse();
        result.Value.DocumentChunk.Should().BeNull();
    }

    [Fact]
    public async Task GetTodayFocus_ShouldCreateIdempotentDrill_WhenReadyBookExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage,
            Status = ProcessingStatus.Ready,
            CreatedByUserId = userId,
            TotalChunks = 1
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Reliability, Scalability, and Maintainability",
            OriginalTextMarkdown = "System reliability principles...",
            SummaryMarkdown = "Summary of reliability..."
        };
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            DocumentChunkId = chunk.Id,
            QuestionText = "Explain fault tolerance vs failure",
            ModelAnswerMarkdown = "Model answer",
            Difficulty = Difficulty.Senior
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);
        var request = new GetTodayFocusRequest(userId);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasActiveBook.Should().BeTrue();
        result.Value.DocumentChunk.Should().NotBeNull();
        result.Value.DocumentChunk!.ChapterTitle.Should().Be("Reliability, Scalability, and Maintainability");
        result.Value.Question.QuestionText.Should().Be("Explain fault tolerance vs failure");
        result.Value.Drill.Status.Should().Be(DrillStatus.Pending);

        // Call again on the same day -> should return the exact same drill record
        var secondResult = await handler.ExecuteAsync(request);
        secondResult.Value.Drill.Id.Should().Be(result.Value.Drill.Id);
    }

    [Fact]
    public async Task GetTodayFocus_ShouldResolveSlice_WhenBookIdAndChunkOrderProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage,
            Status = ProcessingStatus.Ready,
            CreatedByUserId = userId,
            TotalChunks = 2
        };
        var chunk1 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Chapter 1"
        };
        var chunk2 = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 2,
            ChapterTitle = "Chapter 2 - Data Models"
        };
        var question2 = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            DocumentChunkId = chunk2.Id,
            QuestionText = "Relational vs Document data models",
            Difficulty = Difficulty.Senior
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddRangeAsync(chunk1, chunk2);
        await _db.InterviewQuestions.AddAsync(question2);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);
        var request = new GetTodayFocusRequest(userId, BookId: book.Id, ChunkOrder: 2);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentChunk.Should().NotBeNull();
        result.Value.DocumentChunk!.ChunkOrder.Should().Be(2);
        result.Value.DocumentChunk.ChapterTitle.Should().Be("Chapter 2 - Data Models");
        result.Value.Question.QuestionText.Should().Be("Relational vs Document data models");
    }

    [Fact]
    public async Task SubmitDailyDrill_CorrectOption_ShouldAwardTenPointsAndIncrementStreak()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "GC Book",
            Slug = "gc-book",
            SourceType = SourceType.MarkdownSeries,
            Category = Category.BackendRuntime,
            TotalChunks = 1,
            Status = ProcessingStatus.Ready
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Garbage Collection Internals",
            OriginalTextMarkdown = "GC text",
            SummaryMarkdown = "GC summary"
        };
        var questionId = Guid.NewGuid();
        var question = new InterviewQuestion
        {
            Id = questionId,
            DocumentChunkId = chunk.Id,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool rents reusable contiguous byte arrays.",
            Difficulty = Difficulty.Senior
        };
        var drill = new DailyDrill { Id = Guid.NewGuid(), UserId = userId, QuestionId = questionId, ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();

        var validator = new SubmitDailyDrillValidator();
        var handler = new SubmitDailyDrillHandler(_db, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, SelectedOptionIndex: 1);

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeTrue();
        result.Value.Score.Should().Be(10);
        result.Value.CorrectOptionIndex.Should().Be(1);
        result.Value.ExplanationMarkdown.Should().Contain("ArrayPool");
        result.Value.CurrentStreak.Should().Be(1);

        var updatedDrill = await _db.DailyDrills.FirstAsync(d => d.Id == drill.Id);
        updatedDrill.Status.Should().Be(DrillStatus.Reviewed);
        updatedDrill.IsCorrect.Should().BeTrue();
        updatedDrill.Score.Should().Be(10);
        updatedDrill.SelectedOptionIndex.Should().Be(1);
    }

    [Fact]
    public async Task SubmitDailyDrill_IncorrectOption_ShouldAwardZeroAndScheduleSm2Card()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "GC Book",
            Slug = "gc-book-2",
            SourceType = SourceType.MarkdownSeries,
            Category = Category.BackendRuntime,
            TotalChunks = 1,
            Status = ProcessingStatus.Ready
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Garbage Collection Internals",
            OriginalTextMarkdown = "GC text",
            SummaryMarkdown = "GC summary"
        };
        var questionId = Guid.NewGuid();
        var question = new InterviewQuestion
        {
            Id = questionId,
            DocumentChunkId = chunk.Id,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool rents reusable contiguous byte arrays.",
            Difficulty = Difficulty.Senior
        };
        var drill = new DailyDrill { Id = Guid.NewGuid(), UserId = userId, QuestionId = questionId, ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();
        var validator = new SubmitDailyDrillValidator();
        var handler = new SubmitDailyDrillHandler(_db, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, SelectedOptionIndex: 0); // Chose wrong option 0

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeFalse();
        result.Value.Score.Should().Be(0);
        result.Value.CorrectOptionIndex.Should().Be(1);

        var updatedDrill = await _db.DailyDrills.FirstAsync(d => d.Id == drill.Id);
        updatedDrill.Status.Should().Be(DrillStatus.Reviewed);
        updatedDrill.IsCorrect.Should().BeFalse();
        updatedDrill.Score.Should().Be(0);

        // Verify SM-2 card was created for unmastered concept
        var sm2Card = await _db.SpacedRepetitionCards.FirstOrDefaultAsync(c => c.UserId == userId && c.SourceDocumentChunkId == chunk.Id);
        sm2Card.Should().NotBeNull();
        sm2Card!.NextReviewDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
    }

    [Fact]
    public async Task SubmitDailyDrill_ShouldCreateSpacedRepetitionCard_WhenAnswerIsIncorrectForDocumentChunk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "reader@techdaily.local", Name = "Reader Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "Designing Data-Intensive Applications",
            Slug = "ddia",
            Category = Category.DatabaseStorage
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Reliability, Scalability, and Maintainability",
            OriginalTextMarkdown = "Content...",
            SummaryMarkdown = "Summary..."
        };
        var questionId = Guid.NewGuid();
        var question = new InterviewQuestion
        {
            Id = questionId,
            DocumentChunkId = chunk.Id,
            QuestionText = "What defines fault tolerance in distributed systems?",
            Options = new() { "Zero downtime under any failure", "Ability to anticipate faults and cope with them", "Instant auto-recovery", "Infinite redundancy" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "Fault tolerance means coping with components failing without system failure.",
            Difficulty = Difficulty.Senior
        };
        var drill = new DailyDrill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QuestionId = questionId,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.DailyDrills.AddAsync(drill);
        await _db.SaveChangesAsync();

        var validator = new SubmitDailyDrillValidator();
        var handler = new SubmitDailyDrillHandler(_db, validator);
        var request = new SubmitDailyDrillRequest(drill.Id, userId, SelectedOptionIndex: 0); // Wrong answer

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeFalse();

        var sm2Card = await _db.SpacedRepetitionCards
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SourceDocumentChunkId == chunk.Id);
        sm2Card.Should().NotBeNull();
        sm2Card!.SourceType.Should().Be(CardSourceType.DocumentChunk);
        sm2Card.NextReviewDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        sm2Card.FrontMarkdown.Should().Contain("What defines fault tolerance");
        sm2Card.BackMarkdown.Should().Contain("Fault tolerance means coping");
    }

    [Fact]
    public async Task GetTodayFocus_ShouldMaskCorrectOptionAndExplanation_WhenPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "dev@techdaily.local", Name = "Senior Dev" };
        await _db.Users.AddAsync(user);

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = "GC Book",
            Slug = "gc-book",
            Category = Category.BackendRuntime,
            Status = ProcessingStatus.Ready,
            CreatedByUserId = userId,
            TotalChunks = 1
        };
        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentBookId = book.Id,
            ChunkOrder = 1,
            ChapterTitle = "Garbage Collection"
        };
        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            DocumentChunkId = chunk.Id,
            QuestionText = "How to mitigate LOH fragmentation?",
            Options = new() { "GC.Collect()", "ArrayPool<byte>.Shared", "String concatenation", "32-bit runtime" },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = "ArrayPool explanation secret",
            Difficulty = Difficulty.Senior
        };

        await _db.DocumentBooks.AddAsync(book);
        await _db.DocumentChunks.AddAsync(chunk);
        await _db.InterviewQuestions.AddAsync(question);
        await _db.SaveChangesAsync();

        var handler = new GetTodayFocusHandler(_db);

        // Act
        var result = await handler.ExecuteAsync(new GetTodayFocusRequest(userId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Question.Options.Should().HaveCount(4);
        result.Value.Question.CorrectOptionIndex.Should().BeNull();
        result.Value.Question.ExplanationMarkdown.Should().BeNull();
    }
}
