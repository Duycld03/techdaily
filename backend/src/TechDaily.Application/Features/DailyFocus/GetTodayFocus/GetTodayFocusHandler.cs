using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.DailyFocus.GetTodayFocus;

public record GetTodayFocusRequest(
    Guid? UserId = null,
    Guid? BookId = null,
    int? ChunkOrder = null,
    DateOnly? TargetDate = null,
    string Locale = "en");

public class GetTodayFocusResponse
{
    public InterviewQuestionDto Question { get; set; } = null!;
    public DocumentChunkDto? DocumentChunk { get; set; }
    public DailyDrillDto Drill { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int FreezeCreditsRemaining { get; set; }
    public PacerDto? Pacer { get; set; }
    public bool IsGeneratingQuestion { get; set; } = false;
    public bool HasActiveBook { get; set; } = true;
}

public class GetTodayFocusHandler : IUseCase<GetTodayFocusRequest, GetTodayFocusResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly ILookAheadBufferService? _lookAheadService;
    private readonly IAiMarkdownFormatter? _aiFormatter;

    public GetTodayFocusHandler(
        ITechDailyDbContext dbContext,
        ILookAheadBufferService? lookAheadService = null,
        IAiMarkdownFormatter? aiFormatter = null)
    {
        _dbContext = dbContext;
        _lookAheadService = lookAheadService;
        _aiFormatter = aiFormatter;
    }

    public async Task<Result<GetTodayFocusResponse>> ExecuteAsync(
        GetTodayFocusRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = request.TargetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var isAuthenticated = request.UserId.HasValue && request.UserId.Value != Guid.Empty;
        var userId = isAuthenticated ? request.UserId!.Value : Guid.Empty;

        // Check if any books are ready in the user's library
        var readyBooksQuery = _dbContext.DocumentBooks
            .Where(b => b.Status == ProcessingStatus.Ready && !b.IsDeleted);

        if (isAuthenticated)
        {
            readyBooksQuery = readyBooksQuery.Where(b => b.CreatedByUserId == userId);
        }

        var readyBooks = await readyBooksQuery
            .OrderByDescending(b => b.IsFeatured)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

        if (readyBooks.Count > 0)
        {
            return await HandleBookPacerModeAsync(request, readyBooks, userId, isAuthenticated, today, cancellationToken);
        }
        var streak = isAuthenticated
            ? await _dbContext.StreakRecords.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            : null;

        return new GetTodayFocusResponse
        {
            HasActiveBook = false,
            CurrentStreak = streak?.CalculateEffectiveStreak(today) ?? 0,
            LongestStreak = streak?.LongestStreak ?? 0,
            FreezeCreditsRemaining = streak?.FreezeCreditsRemaining ?? 0,
            Question = new InterviewQuestionDto(),
            Drill = new DailyDrillDto(),
            DocumentChunk = null,
            Pacer = null
        };
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

        int targetChunkOrder;
        bool isManualNavigation = request.ChunkOrder.HasValue;

        if (isManualNavigation)
        {
            targetChunkOrder = request.ChunkOrder!.Value;
        }
        else
        {
            targetChunkOrder = activePacer?.CurrentChunkOrder ?? 1;

            if (isAuthenticated && activePacer != null)
            {
                // Calendar day transition check: subsequent day after prior activity
                if (activePacer.LastReadDate.HasValue && today > activePacer.LastReadDate.Value)
                {
                    // Check if the current slice's drill is reviewed
                    var currentChunk = await _dbContext.DocumentChunks
                        .FirstOrDefaultAsync(c => c.DocumentBookId == targetBook.Id && c.ChunkOrder == activePacer.CurrentChunkOrder, cancellationToken);

                    if (currentChunk != null)
                    {
                        var isCurrentSliceDrillReviewed = await _dbContext.DailyDrills
                            .AnyAsync(d => d.UserId == userId 
                                        && d.DocumentChunkId == currentChunk.Id 
                                        && d.Status == DrillStatus.Reviewed, cancellationToken);

                        if (isCurrentSliceDrillReviewed)
                        {
                            if (activePacer.CurrentChunkOrder < targetBook.TotalChunks)
                            {
                                activePacer.CurrentChunkOrder++;
                                targetChunkOrder = activePacer.CurrentChunkOrder;
                            }
                            else
                            {
                                activePacer.CompletedAt ??= DateTimeOffset.UtcNow;
                            }
                        }
                    }
                }

                activePacer.LastReadDate = today;
            }
        }

        if (targetBook.TotalChunks > 0)
        {
            targetChunkOrder = Math.Clamp(targetChunkOrder, 1, targetBook.TotalChunks);
        }

        if (isAuthenticated && activePacer != null)
        {
            if (!isManualNavigation)
            {
                activePacer.CurrentChunkOrder = targetChunkOrder;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var documentChunk = await _dbContext.DocumentChunks
            .Include(c => c.InterviewQuestions)
            .FirstOrDefaultAsync(c => c.DocumentBookId == targetBook.Id && c.ChunkOrder == targetChunkOrder, cancellationToken);

        // On-demand JIT AI formatting if this chunk was not yet curated by background worker
        if (documentChunk != null && !documentChunk.IsAiFormatted && _aiFormatter != null)
        {
            try
            {
                var aiResult = await _aiFormatter.FormatSliceAsync(
                    documentChunk.OriginalTextMarkdown,
                    documentChunk.ChapterTitle,
                    documentChunk.Language,
                    targetBook.Category,
                    cancellationToken);

                if (aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value.FormattedMarkdown))
                {
                    if (targetBook.Category != Category.EngineeringCraft)
                    {
                        documentChunk.OriginalTextMarkdown = aiResult.Value.FormattedMarkdown;
                    }
                    documentChunk.SummaryMarkdown = !string.IsNullOrWhiteSpace(aiResult.Value.SummaryMarkdown)
                        ? aiResult.Value.SummaryMarkdown
                        : aiResult.Value.FormattedMarkdown;
                    documentChunk.KeyTakeaways = aiResult.Value.KeyTakeaways;
                    documentChunk.EstimatedReadMinutes = aiResult.Value.EstimatedReadMinutes;
                    documentChunk.IsAiFormatted = true;

                    if (aiResult.Value.ScenarioDrill != null)
                    {
                        var scenarioDrill = aiResult.Value.ScenarioDrill;

                        var existingQ = await _dbContext.InterviewQuestions
                            .FirstOrDefaultAsync(q => q.DocumentChunkId == documentChunk.Id, cancellationToken);

                        if (existingQ != null)
                        {
                            existingQ.QuestionText = scenarioDrill.QuestionText;
                            existingQ.Options = scenarioDrill.Options;
                            existingQ.CorrectOptionIndex = scenarioDrill.CorrectOptionIndex;
                            existingQ.ExplanationMarkdown = scenarioDrill.ExplanationMarkdown;
                            existingQ.ExpectedKeyPoints = scenarioDrill.ExpectedKeyPoints;
                            existingQ.ModelAnswerMarkdown = scenarioDrill.ExplanationMarkdown;
                            existingQ.Difficulty = Difficulty.Senior;
                        }
                        else
                        {
                            var newQ = new InterviewQuestion
                            {
                                DocumentChunkId = documentChunk.Id,
                                QuestionText = scenarioDrill.QuestionText,
                                Options = scenarioDrill.Options,
                                CorrectOptionIndex = scenarioDrill.CorrectOptionIndex,
                                ExplanationMarkdown = scenarioDrill.ExplanationMarkdown,
                                ExpectedKeyPoints = scenarioDrill.ExpectedKeyPoints,
                                ModelAnswerMarkdown = scenarioDrill.ExplanationMarkdown,
                                Difficulty = Difficulty.Senior
                            };
                            await _dbContext.InterviewQuestions.AddAsync(newQ, cancellationToken);
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch
            {
                // Non-blocking fallback to existing text
            }
        }

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

            DailyDrill? existingDrill;
            if (isManualNavigation)
            {
                existingDrill = await _dbContext.DailyDrills
                    .Include(d => d.Question)
                    .Include(d => d.DocumentChunk)
                    .OrderByDescending(d => d.SubmittedAt ?? d.CreatedAt)
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id, cancellationToken);
            }
            else
            {
                existingDrill = await _dbContext.DailyDrills
                    .Include(d => d.Question)
                    .Include(d => d.DocumentChunk)
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id && d.ScheduledDate == today, cancellationToken);

                if (existingDrill == null)
                {
                    var pastReviewedDrill = await _dbContext.DailyDrills
                        .Include(d => d.Question)
                        .Include(d => d.DocumentChunk)
                        .OrderByDescending(d => d.SubmittedAt)
                        .FirstOrDefaultAsync(d => d.UserId == userId && d.QuestionId == question.Id && d.Status == DrillStatus.Reviewed, cancellationToken);

                    if (pastReviewedDrill != null)
                    {
                        existingDrill = pastReviewedDrill;
                    }
                }
            }
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
                    Status = DrillStatus.Pending
                };
                await _dbContext.DailyDrills.AddAsync(drill, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                drill.Question = question;
                drill.DocumentChunk = documentChunk;
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

        var response = MapResponse(drill, streak, question, documentChunk, today);
        response.Pacer = pacer;
        response.IsGeneratingQuestion = isGenerating;
        return response;
    }

    private static GetTodayFocusResponse MapResponse(
        DailyDrill drill,
        StreakRecord streak,
        InterviewQuestion question,
        DocumentChunk? chunk,
        DateOnly today)
    {
        var isReviewed = drill.Status == DrillStatus.Reviewed;

        return new GetTodayFocusResponse
        {
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
                Language = chunk.Language,
                EstimatedReadMinutes = chunk.EstimatedReadMinutes,
                IsAiFormatted = chunk.IsAiFormatted
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
            CurrentStreak = streak.CalculateEffectiveStreak(today),
            LongestStreak = streak.LongestStreak,
            FreezeCreditsRemaining = streak.FreezeCreditsRemaining
        };
    }
}
