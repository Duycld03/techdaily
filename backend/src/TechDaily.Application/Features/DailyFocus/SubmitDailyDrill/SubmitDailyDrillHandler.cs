using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Features.DailyFocus.SubmitDailyDrill;

public record SubmitDailyDrillRequest(
    Guid DrillId,
    Guid UserId,
    string? AnswerText,
    byte[]? AudioBytes = null,
    string? AudioMimeType = null,
    string Locale = "en");

public class SubmitDailyDrillResponse
{
    public AiReviewDto Review { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalDrillsCompleted { get; set; }
    public decimal AverageScore { get; set; }
    public string? AudioUrl { get; set; }
}

public class SubmitDailyDrillValidator : AbstractValidator<SubmitDailyDrillRequest>
{
    public SubmitDailyDrillValidator()
    {
        RuleFor(x => x.DrillId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.AnswerText) || (x.AudioBytes != null && x.AudioBytes.Length > 0))
            .WithMessage("Either written answer text or voice recording must be provided.");

        When(x => !string.IsNullOrWhiteSpace(x.AnswerText), () =>
        {
            RuleFor(x => x.AnswerText).MinimumLength(10)
                .WithMessage("Answer text must be at least 10 characters long.");
        });
    }
}

public class SubmitDailyDrillHandler : IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IAiReviewService _aiReviewService;
    private readonly IAudioStorageService _audioStorageService;
    private readonly IValidator<SubmitDailyDrillRequest> _validator;

    public SubmitDailyDrillHandler(
        ITechDailyDbContext dbContext,
        IAiReviewService aiReviewService,
        IAudioStorageService audioStorageService,
        IValidator<SubmitDailyDrillRequest> validator)
    {
        _dbContext = dbContext;
        _aiReviewService = aiReviewService;
        _audioStorageService = audioStorageService;
        _validator = validator;
    }

    public async Task<Result<SubmitDailyDrillResponse>> ExecuteAsync(
        SubmitDailyDrillRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Error.Custom("Validation.Failed", firstError);
        }

        var drill = await _dbContext.DailyDrills
            .Include(d => d.Question)
                .ThenInclude(q => q.Topic)
            .Include(d => d.AiReview)
            .FirstOrDefaultAsync(d => d.Id == request.DrillId && d.UserId == request.UserId, cancellationToken);

        if (drill == null)
        {
            return Error.NotFound;
        }

        // 1. Save audio file to local storage if provided
        string? audioRelativePath = null;
        if (request.AudioBytes != null && request.AudioBytes.Length > 0)
        {
            using var memoryStream = new MemoryStream(request.AudioBytes);
            var extension = request.AudioMimeType?.Contains("wav") == true ? ".wav" : ".webm";
            audioRelativePath = await _audioStorageService.SaveAudioAsync(drill.Id, memoryStream, extension, cancellationToken);
        }

        // 2. Perform 1-Pass Multimodal AI Evaluation via Gemini Flash
        var question = drill.Question;
        var evaluationResult = await _aiReviewService.EvaluateSubmissionAsync(
            questionText: question.QuestionText,
            expectedKeyPoints: question.ExpectedKeyPoints,
            modelAnswer: question.ModelAnswerMarkdown,
            userAnswerText: request.AnswerText,
            audioBytes: request.AudioBytes,
            audioMimeType: request.AudioMimeType ?? "audio/webm",
            locale: request.Locale,
            cancellationToken: cancellationToken);

        if (evaluationResult.IsFailure)
        {
            return evaluationResult.Error;
        }

        var reviewDto = evaluationResult.Value;

        // 3. Update drill state
        drill.Submit(request.AnswerText, audioRelativePath ?? drill.UserAudioUrl);
        drill.MarkReviewed();

        if (drill.AiReview == null)
        {
            var aiReview = new AiReview
            {
                DailyDrillId = drill.Id,
                Score = reviewDto.Score,
                SummaryFeedback = reviewDto.SummaryFeedback,
                Strengths = reviewDto.Strengths,
                MissingPoints = reviewDto.MissingPoints,
                ImprovedAnswerMarkdown = reviewDto.ImprovedAnswerMarkdown,
                AiModelUsed = reviewDto.AiModelUsed
            };
            await _dbContext.AiReviews.AddAsync(aiReview, cancellationToken);
            drill.AiReview = aiReview;
        }
        else
        {
            drill.AiReview.Score = reviewDto.Score;
            drill.AiReview.SummaryFeedback = reviewDto.SummaryFeedback;
            drill.AiReview.Strengths = reviewDto.Strengths;
            drill.AiReview.MissingPoints = reviewDto.MissingPoints;
            drill.AiReview.ImprovedAnswerMarkdown = reviewDto.ImprovedAnswerMarkdown;
            drill.AiReview.AiModelUsed = reviewDto.AiModelUsed;
            drill.AiReview.MarkUpdated();
        }

        // 4. Update Streak Record
        var streak = await _dbContext.StreakRecords
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (streak == null)
        {
            streak = StreakRecord.Create(request.UserId);
            await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        streak.RecordCompletion(today, reviewDto.Score);

        // 5. Ensure Spaced Repetition Card exists if score < 7 or concepts missed
        if (reviewDto.Score < 7 || reviewDto.MissingPoints.Any())
        {
            var card = await _dbContext.SpacedRepetitionCards
                .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.TopicId == question.TopicId, cancellationToken);

            if (card == null)
            {
                card = SpacedRepetitionCard.Create(request.UserId, question.TopicId, today.AddDays(1));
                await _dbContext.SpacedRepetitionCards.AddAsync(card, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitDailyDrillResponse
        {
            Review = reviewDto,
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            TotalDrillsCompleted = streak.TotalDrillsCompleted,
            AverageScore = streak.AverageScore,
            AudioUrl = audioRelativePath != null ? _audioStorageService.GetAudioUrl(audioRelativePath) : null
        };
    }
}
