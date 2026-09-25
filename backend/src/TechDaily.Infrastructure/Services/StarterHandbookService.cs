using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Persistence.Seeders;

namespace TechDaily.Infrastructure.Services;

public class StarterHandbookService : IStarterHandbookService
{
    private readonly TechDailyDbContext _dbContext;
    private readonly ILogger<StarterHandbookService> _logger;

    public StarterHandbookService(TechDailyDbContext dbContext, ILogger<StarterHandbookService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProvisionForUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var alreadyExists = await _dbContext.DocumentBooks
            .AnyAsync(b => b.CreatedByUserId == userId && b.Slug.StartsWith("senior-engineering-craft-handbook") && !b.IsDeleted, ct);

        if (alreadyExists)
        {
            _logger.LogInformation("Starter handbook already provisioned for user {UserId}", userId);
            return;
        }

        var bookId = Guid.NewGuid();
        var templateItems = CurriculumSeeder.GetCurriculumItems(bookId);

        var book = new DocumentBook
        {
            Id = bookId,
            CreatedByUserId = userId,
            Title = "Senior Engineering Craft Handbook",
            Slug = $"senior-engineering-craft-handbook-{userId.ToString("N")[..8]}",
            SourceType = SourceType.MarkdownSeries,
            Category = Category.EngineeringCraft,
            TotalChunks = templateItems.Count,
            AuthorOrSourceUrl = "https://techdaily.dev/handbook",
            IsPublished = true,
            IsFeatured = true,
            Status = ProcessingStatus.Ready,
            ProgressPercentage = 100,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.DocumentBooks.AddAsync(book, ct);

        foreach (var (_, seededQuestion, seededChunk) in templateItems)
        {
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = bookId,
                ChunkOrder = seededChunk.ChunkOrder,
                ChapterTitle = seededChunk.ChapterTitle,
                OriginalTextMarkdown = seededChunk.OriginalTextMarkdown,
                SummaryMarkdown = seededChunk.SummaryMarkdown,
                KeyTakeaways = seededChunk.KeyTakeaways,
                Language = seededChunk.Language,
                EstimatedReadMinutes = seededChunk.EstimatedReadMinutes,
                IsAiFormatted = true,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.DocumentChunks.AddAsync(chunk, ct);

            var question = new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                DocumentChunkId = chunk.Id,
                QuestionText = seededQuestion.QuestionText,
                Options = seededQuestion.Options,
                CorrectOptionIndex = seededQuestion.CorrectOptionIndex,
                ExplanationMarkdown = seededQuestion.ExplanationMarkdown,
                ExpectedKeyPoints = seededQuestion.ExpectedKeyPoints,
                ModelAnswerMarkdown = seededQuestion.ModelAnswerMarkdown,
                Difficulty = seededQuestion.Difficulty,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.InterviewQuestions.AddAsync(question, ct);
        }

        // Deactivate any existing active pacers for other books if needed, and activate the starter handbook
        var existingPacers = await _dbContext.UserBookPacers
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(ct);

        foreach (var p in existingPacers)
        {
            p.IsActive = false;
        }

        var pacer = new UserBookPacer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentBookId = bookId,
            CurrentChunkOrder = 1,
            DailyPaceChunks = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.UserBookPacers.AddAsync(pacer, ct);

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully provisioned Senior Engineering Craft Handbook for user {UserId}", userId);
    }
}
