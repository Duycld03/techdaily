using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.DTOs;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.DailyFocus.GetTodayFocus;

public record GetTodayFocusRequest(Guid UserId, DateOnly? TargetDate = null, string Locale = "en");

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

        // 1. Get or create user's streak record
        var streak = await _dbContext.StreakRecords
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (streak == null)
        {
            streak = StreakRecord.Create(request.UserId);
            await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Check if a DailyDrill already exists for today
        var existingDrill = await _dbContext.DailyDrills
            .Include(d => d.Question)
                .ThenInclude(q => q.Topic)
            .Include(d => d.DocumentChunk)
            .Include(d => d.AiReview)
            .FirstOrDefaultAsync(d => d.UserId == request.UserId && d.ScheduledDate == today, cancellationToken);

        if (existingDrill != null)
        {
            return MapResponse(existingDrill, streak);
        }

        // 3. Determine the curriculum day based on total drills completed or DayOrder modulo 30
        var totalCompleted = streak.TotalDrillsCompleted;
        var dayOrder = (totalCompleted % 30) + 1;

        var topic = await _dbContext.Topics
            .Include(t => t.InterviewQuestions)
            .FirstOrDefaultAsync(t => t.DayOrder == dayOrder, cancellationToken)
            ?? await _dbContext.Topics.Include(t => t.InterviewQuestions).FirstOrDefaultAsync(cancellationToken);

        if (topic == null || !topic.InterviewQuestions.Any())
        {
            return Error.NotFound;
        }

        var question = topic.InterviewQuestions.First();

        var documentChunk = await _dbContext.DocumentChunks
            .FirstOrDefaultAsync(c => c.ChunkOrder == dayOrder, cancellationToken);

        // 4. Create new idempotent DailyDrill record for today
        var newDrill = new DailyDrill
        {
            UserId = request.UserId,
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
                ExpectedKeyPoints = question.ExpectedKeyPoints,
                ModelAnswerMarkdown = question.ModelAnswerMarkdown,
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
                UserAnswerText = drill.UserAnswerText,
                UserAudioUrl = drill.UserAudioUrl,
                AttemptCount = drill.AttemptCount,
                SubmittedAt = drill.SubmittedAt,
                AiReview = drill.AiReview == null ? null : new AiReviewDto
                {
                    Score = drill.AiReview.Score,
                    SummaryFeedback = drill.AiReview.SummaryFeedback,
                    Strengths = drill.AiReview.Strengths,
                    MissingPoints = drill.AiReview.MissingPoints,
                    ImprovedAnswerMarkdown = drill.AiReview.ImprovedAnswerMarkdown,
                    AiModelUsed = drill.AiReview.AiModelUsed
                }
            },
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            FreezeCreditsRemaining = streak.FreezeCreditsRemaining
        };
    }
}
