using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.DailyFocus.GetTodayFocus;

public record GetTodayFocusRequest(Guid? UserId = null, int? DayOrder = null, DateOnly? TargetDate = null, string Locale = "en");

public class GetTodayFocusResponse
{
    public TopicDto Topic { get; set; } = null!;
    public InterviewQuestionDto Question { get; set; } = null!;
    public DocumentChunkDto? DocumentChunk { get; set; }
    public DailyDrillDto Drill { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int FreezeCreditsRemaining { get; set; }
}

public class GetTodayFocusHandler : IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetTodayFocusHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetTodayFocusResponse>> ExecuteAsync(
        GetTodayFocusRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var isAuthenticated = request.UserId.HasValue && request.UserId.Value != Guid.Empty;

        // 1. If Guest (unauthenticated), serve public preview for requested DayOrder (or Day 1)
        if (!isAuthenticated)
        {
            var requestedDay = request.DayOrder ?? 1;
            var previewTopic = await _dbContext.Topics
                .Include(t => t.InterviewQuestions)
                .FirstOrDefaultAsync(t => t.DayOrder == requestedDay, cancellationToken)
                ?? await _dbContext.Topics.Include(t => t.InterviewQuestions).FirstOrDefaultAsync(cancellationToken);

            if (previewTopic == null || !previewTopic.InterviewQuestions.Any())
            {
                return Error.NotFound;
            }

            var previewQuestion = previewTopic.InterviewQuestions.First();
            var previewChunk = await _dbContext.DocumentChunks
                .FirstOrDefaultAsync(c => c.ChunkOrder == previewTopic.DayOrder, cancellationToken);

            var previewDrill = new DailyDrill
            {
                Id = Guid.Empty,
                UserId = Guid.Empty,
                QuestionId = previewQuestion.Id,
                DocumentChunkId = previewChunk?.Id,
                ScheduledDate = today,
                Status = DrillStatus.Pending,
                Question = previewQuestion,
                DocumentChunk = previewChunk
            };
            previewDrill.Question.Topic = previewTopic;

            var guestStreak = StreakRecord.Create(Guid.Empty);

            return MapResponse(previewDrill, guestStreak);
        }

        var userId = request.UserId!.Value;

        // 2. Authenticated user: Get or create user's streak record
        var streak = await _dbContext.StreakRecords
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (streak == null)
        {
            streak = StreakRecord.Create(userId);
            await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Determine the curriculum day based on request.DayOrder or total drills completed
        int targetDayOrder;
        if (request.DayOrder.HasValue && request.DayOrder.Value >= 1 && request.DayOrder.Value <= 30)
        {
            targetDayOrder = request.DayOrder.Value;
        }
        else
        {
            var totalCompleted = streak.TotalDrillsCompleted;
            targetDayOrder = (totalCompleted % 30) + 1;
        }

        var topic = await _dbContext.Topics
            .Include(t => t.InterviewQuestions)
            .FirstOrDefaultAsync(t => t.DayOrder == targetDayOrder, cancellationToken)
            ?? await _dbContext.Topics.Include(t => t.InterviewQuestions).FirstOrDefaultAsync(cancellationToken);

        if (topic == null || !topic.InterviewQuestions.Any())
        {
            return Error.NotFound;
        }

        var question = topic.InterviewQuestions.First();
        var documentChunk = await _dbContext.DocumentChunks
            .FirstOrDefaultAsync(c => c.ChunkOrder == targetDayOrder, cancellationToken);

        // 4. Check if a DailyDrill already exists for this user and question
        var existingDrill = await _dbContext.DailyDrills
            .Include(d => d.Question)
                .ThenInclude(q => q.Topic)
            .Include(d => d.DocumentChunk)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id, cancellationToken);

        if (existingDrill != null)
        {
            return MapResponse(existingDrill, streak);
        }

        // 5. Create new idempotent DailyDrill record for this topic
        var newDrill = new DailyDrill
        {
            UserId = userId,
            QuestionId = question.Id,
            DocumentChunkId = documentChunk?.Id,
            ScheduledDate = today,
            Status = DrillStatus.Pending
        };

        await _dbContext.DailyDrills.AddAsync(newDrill, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        newDrill.Question = question;
        newDrill.Question.Topic = topic;
        newDrill.DocumentChunk = documentChunk;

        return MapResponse(newDrill, streak);
    }

    private static GetTodayFocusResponse MapResponse(DailyDrill drill, StreakRecord streak)
    {
        var topic = drill.Question.Topic;
        var question = drill.Question;
        var chunk = drill.DocumentChunk;

        var isReviewed = drill.Status == DrillStatus.Reviewed;

        return new GetTodayFocusResponse
        {
            Topic = new TopicDto
            {
                Id = topic.Id,
                Slug = topic.Slug,
                Title = topic.Title,
                Category = topic.Category,
                Difficulty = topic.Difficulty,
                DayOrder = topic.DayOrder,
                Summary = topic.Summary,
                DeepDiveMarkdown = topic.DeepDiveMarkdown,
                BenchmarkSnippet = topic.BenchmarkSnippet
            },
            Question = new InterviewQuestionDto
            {
                Id = question.Id,
                QuestionText = question.QuestionText,
                Options = question.Options,
                CorrectOptionIndex = isReviewed ? question.CorrectOptionIndex : null,
                ExplanationMarkdown = isReviewed ? question.ExplanationMarkdown : null,
                ExpectedKeyPoints = question.ExpectedKeyPoints,
                ModelAnswerMarkdown = isReviewed ? question.ModelAnswerMarkdown : string.Empty,
                Difficulty = question.Difficulty
            },
            DocumentChunk = chunk == null ? null : new DocumentChunkDto
            {
                Id = chunk.Id,
                ChunkOrder = chunk.ChunkOrder,
                ChapterTitle = chunk.ChapterTitle,
                OriginalTextMarkdown = chunk.OriginalTextMarkdown,
                SummaryMarkdown = chunk.SummaryMarkdown,
                KeyTakeaways = chunk.KeyTakeaways,
                MicroQuiz = chunk.MicroQuiz,
                Language = chunk.Language,
                EstimatedReadMinutes = chunk.EstimatedReadMinutes
            },
            Drill = new DailyDrillDto
            {
                Id = drill.Id,
                ScheduledDate = drill.ScheduledDate,
                Status = drill.Status,
                SelectedOptionIndex = drill.SelectedOptionIndex,
                IsCorrect = drill.IsCorrect,
                Score = drill.Score,
                AttemptCount = drill.AttemptCount,
                SubmittedAt = drill.SubmittedAt
            },
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            FreezeCreditsRemaining = streak.FreezeCreditsRemaining
        };
    }
}
