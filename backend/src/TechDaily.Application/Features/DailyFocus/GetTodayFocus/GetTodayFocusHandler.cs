using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Application.Features.DailyFocus.GetTodayFocus;

public record GetTodayFocusRequest(
    Guid? UserId = null,
    Guid? BookId = null,
    int? ChunkOrder = null,
    int? DayOrder = null,
    DateOnly? TargetDate = null,
    string Locale = "en");

public class GetTodayFocusResponse
{
    public TopicDto Topic { get; set; } = null!;
    public InterviewQuestionDto Question { get; set; } = null!;
    public DocumentChunkDto? DocumentChunk { get; set; }
    public DailyDrillDto Drill { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int FreezeCreditsRemaining { get; set; }
    public PacerDto? Pacer { get; set; }
    public bool IsGeneratingQuestion { get; set; } = false;
}

public class GetTodayFocusHandler : IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly ILookAheadBufferService? _lookAheadService;

    public GetTodayFocusHandler(
        ITechDailyDbContext dbContext,
        ILookAheadBufferService? lookAheadService = null)
    {
        _dbContext = dbContext;
        _lookAheadService = lookAheadService;
    }

    public async Task<Result<GetTodayFocusResponse>> ExecuteAsync(
        GetTodayFocusRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var isAuthenticated = request.UserId.HasValue && request.UserId.Value != Guid.Empty;
        var userId = isAuthenticated ? request.UserId!.Value : Guid.Empty;

        // Check if any books are ready in the library
        var readyBooks = await _dbContext.DocumentBooks
            .Where(b => b.Status == ProcessingStatus.Ready)
            .OrderByDescending(b => b.IsFeatured)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

        if (readyBooks.Count > 0)
        {
            return await HandleBookPacerModeAsync(request, readyBooks, userId, isAuthenticated, today, cancellationToken);
        }

        // Fallback: Legacy 30-Day Topic Curriculum Mode (when no DocumentBooks exist yet)
        return await HandleLegacyTopicModeAsync(request, userId, isAuthenticated, today, cancellationToken);
    }

    private async Task<Result<GetTodayFocusResponse>> HandleBookPacerModeAsync(
        GetTodayFocusRequest request,
        List<DocumentBook> readyBooks,
        Guid userId,
        bool isAuthenticated,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        DocumentBook targetBook;
        UserBookPacer? activePacer = null;

        if (isAuthenticated)
        {
            var userPacers = await _dbContext.UserBookPacers
                .Where(p => p.UserId == userId)
                .ToListAsync(cancellationToken);

            if (request.BookId.HasValue)
            {
                targetBook = readyBooks.FirstOrDefault(b => b.Id == request.BookId.Value) ?? readyBooks[0];
                foreach (var p in userPacers) p.IsActive = false;
                activePacer = userPacers.FirstOrDefault(p => p.DocumentBookId == targetBook.Id);
                if (activePacer == null)
                {
                    activePacer = new UserBookPacer
                    {
                        UserId = userId,
                        DocumentBookId = targetBook.Id,
                        CurrentChunkOrder = 1,
                        DailyPaceChunks = 1,
                        IsActive = true,
                        LastReadDate = today
                    };
                    await _dbContext.UserBookPacers.AddAsync(activePacer, cancellationToken);
                }
                else
                {
                    activePacer.IsActive = true;
                    activePacer.LastReadDate = today;
                }
            }
            else
            {
                activePacer = userPacers.FirstOrDefault(p => p.IsActive);
                if (activePacer != null)
                {
                    targetBook = readyBooks.FirstOrDefault(b => b.Id == activePacer.DocumentBookId) ?? readyBooks[0];
                }
                else
                {
                    targetBook = readyBooks.FirstOrDefault(b => b.IsFeatured) ?? readyBooks[0];
                    activePacer = userPacers.FirstOrDefault(p => p.DocumentBookId == targetBook.Id);
                    if (activePacer == null)
                    {
                        activePacer = new UserBookPacer
                        {
                            UserId = userId,
                            DocumentBookId = targetBook.Id,
                            CurrentChunkOrder = 1,
                            DailyPaceChunks = 1,
                            IsActive = true,
                            LastReadDate = today
                        };
                        await _dbContext.UserBookPacers.AddAsync(activePacer, cancellationToken);
                    }
                    else
                    {
                        activePacer.IsActive = true;
                        activePacer.LastReadDate = today;
                    }
                }
            }
        }
        else
        {
            // Guest mode
            targetBook = request.BookId.HasValue
                ? readyBooks.FirstOrDefault(b => b.Id == request.BookId.Value) ?? readyBooks[0]
                : readyBooks.FirstOrDefault(b => b.IsFeatured) ?? readyBooks[0];
        }

        int targetChunkOrder = request.ChunkOrder
            ?? (activePacer?.CurrentChunkOrder)
            ?? request.DayOrder
            ?? 1;

        if (targetBook.TotalChunks > 0)
        {
            targetChunkOrder = Math.Clamp(targetChunkOrder, 1, targetBook.TotalChunks);
        }

        if (isAuthenticated && activePacer != null)
        {
            activePacer.CurrentChunkOrder = targetChunkOrder;
            activePacer.LastReadDate = today;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var documentChunk = await _dbContext.DocumentChunks
            .Include(c => c.InterviewQuestions)
            .FirstOrDefaultAsync(c => c.DocumentBookId == targetBook.Id && c.ChunkOrder == targetChunkOrder, cancellationToken);

        InterviewQuestion? question = documentChunk?.InterviewQuestions.FirstOrDefault();
        bool isGenerating = false;

        if (question == null && documentChunk != null && _lookAheadService != null)
        {
            question = await _lookAheadService.PromoteChunkPriorityAsync(targetBook.Id, targetChunkOrder, cancellationToken);
            if (question == null)
            {
                isGenerating = true;
            }
        }

        if (_lookAheadService != null && documentChunk != null)
        {
            _ = _lookAheadService.EnsureBufferDepthAsync(targetBook.Id, targetChunkOrder);
        }

        if (question == null)
        {
            // Graceful fallback question so UI never crashes
            question = await _dbContext.InterviewQuestions.FirstOrDefaultAsync(cancellationToken)
                ?? new InterviewQuestion
                {
                    Id = Guid.NewGuid(),
                    QuestionText = $"Analyze the architectural trade-offs described in {documentChunk?.ChapterTitle ?? targetBook.Title}.",
                    Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                    CorrectOptionIndex = 0,
                    Difficulty = Difficulty.Senior,
                    ModelAnswerMarkdown = "Under review."
                };
        }

        StreakRecord streak;
        DailyDrill drill;

        if (isAuthenticated)
        {
            streak = await _dbContext.StreakRecords.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
                ?? StreakRecord.Create(userId);

            if (streak.Id == Guid.Empty)
            {
                await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var existingDrill = await _dbContext.DailyDrills
                .Include(d => d.Question)
                    .ThenInclude(q => q.Topic)
                .Include(d => d.DocumentChunk)
                .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id, cancellationToken);

            if (existingDrill != null)
            {
                drill = existingDrill;
            }
            else
            {
                drill = new DailyDrill
                {
                    UserId = userId,
                    QuestionId = question.Id,
                    DocumentChunkId = documentChunk?.Id,
                    ScheduledDate = today,
                    Status = DrillStatus.Pending,
                    Question = question,
                    DocumentChunk = documentChunk
                };
                await _dbContext.DailyDrills.AddAsync(drill, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            streak = StreakRecord.Create(Guid.Empty);
            drill = new DailyDrill
            {
                Id = Guid.Empty,
                UserId = Guid.Empty,
                QuestionId = question.Id,
                DocumentChunkId = documentChunk?.Id,
                ScheduledDate = today,
                Status = DrillStatus.Pending,
                Question = question,
                DocumentChunk = documentChunk
            };
        }

        // Build PacerDto
        var userPacersList = isAuthenticated
            ? await _dbContext.UserBookPacers.Where(p => p.UserId == userId).ToListAsync(cancellationToken)
            : new List<UserBookPacer>();
        var pacerMap = userPacersList.ToDictionary(p => p.DocumentBookId);

        var availableBooks = readyBooks.Select(b =>
        {
            var p = pacerMap.GetValueOrDefault(b.Id);
            var currentOrder = p?.CurrentChunkOrder ?? 1;
            var pct = b.TotalChunks > 0 ? (int)Math.Round((double)currentOrder / b.TotalChunks * 100) : 0;
            return new PacerBookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                TotalChunks = b.TotalChunks,
                CurrentChunkOrder = currentOrder,
                ProgressPercentage = Math.Clamp(pct, 0, 100),
                IsActive = b.Id == targetBook.Id
            };
        }).ToList();

        var total = Math.Max(1, targetBook.TotalChunks);
        var progressPct = (int)Math.Round((double)targetChunkOrder / total * 100);

        var pacer = new PacerDto
        {
            BookId = targetBook.Id,
            BookTitle = targetBook.Title,
            ChapterTitle = documentChunk?.ChapterTitle ?? "Overview",
            CurrentChunkOrder = targetChunkOrder,
            TotalChunks = targetBook.TotalChunks,
            ProgressPercentage = Math.Clamp(progressPct, 0, 100),
            HasPrevious = targetChunkOrder > 1,
            HasNext = targetChunkOrder < targetBook.TotalChunks,
            AvailableBooks = availableBooks
        };

        var response = MapResponse(drill, streak, question, documentChunk);
        response.Pacer = pacer;
        response.IsGeneratingQuestion = isGenerating;
        return response;
    }

    private async Task<Result<GetTodayFocusResponse>> HandleLegacyTopicModeAsync(
        GetTodayFocusRequest request,
        Guid userId,
        bool isAuthenticated,
        DateOnly today,
        CancellationToken cancellationToken)
    {
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

            return MapResponse(previewDrill, StreakRecord.Create(Guid.Empty), previewQuestion, previewChunk, previewTopic);
        }

        var streak = await _dbContext.StreakRecords
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (streak == null)
        {
            streak = StreakRecord.Create(userId);
            await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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

        var existingDrill = await _dbContext.DailyDrills
            .Include(d => d.Question)
                .ThenInclude(q => q.Topic)
            .Include(d => d.DocumentChunk)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id, cancellationToken);

        if (existingDrill != null)
        {
            return MapResponse(existingDrill, streak, question, documentChunk, topic);
        }

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

        return MapResponse(newDrill, streak, question, documentChunk, topic);
    }

    private static GetTodayFocusResponse MapResponse(
        DailyDrill drill,
        StreakRecord streak,
        InterviewQuestion question,
        DocumentChunk? chunk,
        Topic? explicitTopic = null)
    {
        var topic = explicitTopic ?? question.Topic;
        var isReviewed = drill.Status == DrillStatus.Reviewed;

        return new GetTodayFocusResponse
        {
            Topic = new TopicDto
            {
                Id = topic?.Id ?? chunk?.Id ?? Guid.Empty,
                Slug = topic?.Slug ?? "doc-slice",
                Title = topic?.Title ?? chunk?.ChapterTitle ?? "Technical Guide",
                Category = topic?.Category ?? chunk?.DocumentBook?.Category ?? Category.BackendDotNet,
                Difficulty = topic?.Difficulty ?? question.Difficulty,
                DayOrder = topic?.DayOrder ?? chunk?.ChunkOrder ?? 1,
                Summary = topic?.Summary ?? chunk?.SummaryMarkdown ?? string.Empty,
                DeepDiveMarkdown = topic?.DeepDiveMarkdown ?? chunk?.OriginalTextMarkdown ?? string.Empty,
                BenchmarkSnippet = topic?.BenchmarkSnippet
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
