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
    int? SelectedOptionIndex = null,
    string? AnswerText = null,
    byte[]? AudioBytes = null,
    string? AudioMimeType = null,
    string Locale = "en");

public class SubmitDailyDrillResponse
{
    public bool IsCorrect { get; set; }
    public int? SelectedOptionIndex { get; set; }
    public int CorrectOptionIndex { get; set; }
    public int Score { get; set; }
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public AiReviewDto? Review { get; set; }
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
            .Must(x => x.SelectedOptionIndex.HasValue || !string.IsNullOrWhiteSpace(x.AnswerText) || (x.AudioBytes != null && x.AudioBytes.Length > 0))
            .WithMessage("Either a selected option, written answer text, or voice recording must be provided.");

        When(x => x.SelectedOptionIndex.HasValue, () =>
        {
            RuleFor(x => x.SelectedOptionIndex!.Value)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Selected option index must be non-negative.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.AnswerText) && !x.SelectedOptionIndex.HasValue, () =>
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

        var question = drill.Question;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Fetch or create user streak
        var streak = await _dbContext.StreakRecords
            .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

        if (streak == null)
        {
            streak = StreakRecord.Create(request.UserId);
            await _dbContext.StreakRecords.AddAsync(streak, cancellationToken);
        }

        // Branch A: Scenario Multiple-Choice Option Submission
        if (request.SelectedOptionIndex.HasValue)
        {
            var selectedIndex = request.SelectedOptionIndex.Value;
            if (question.Options.Count > 0 && (selectedIndex < 0 || selectedIndex >= question.Options.Count))
            {
                return Error.Custom("Validation.InvalidOption", $"Selected option index {selectedIndex} is out of bounds (0..{question.Options.Count - 1}).");
            }

            var isCorrect = selectedIndex == question.CorrectOptionIndex;
            var score = isCorrect ? 10 : 0;

            drill.SubmitOption(selectedIndex, isCorrect, score);
            streak.RecordCompletion(today, score);

            if (!isCorrect)
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
                IsCorrect = isCorrect,
                SelectedOptionIndex = selectedIndex,
                CorrectOptionIndex = question.CorrectOptionIndex,
                Score = score,
                ExplanationMarkdown = question.ExplanationMarkdown,
                CurrentStreak = streak.CurrentStreak,
                LongestStreak = streak.LongestStreak,
                TotalDrillsCompleted = streak.TotalDrillsCompleted,
                AverageScore = streak.AverageScore
            };
        }

        // Branch B: Free-text or Voice submission (AI Review flow)
        string? audioRelativePath = null;
        if (request.AudioBytes != null && request.AudioBytes.Length > 0)
        {
            using var memoryStream = new MemoryStream(request.AudioBytes);
            var extension = request.AudioMimeType?.Contains("wav") == true ? ".wav" : ".webm";
            audioRelativePath = await _audioStorageService.SaveAudioAsync(drill.Id, memoryStream, extension, cancellationToken);
        }

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

        streak.RecordCompletion(today, reviewDto.Score);

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
            IsCorrect = reviewDto.Score >= 7,
            CorrectOptionIndex = question.CorrectOptionIndex,
            Score = reviewDto.Score,
            ExplanationMarkdown = question.ExplanationMarkdown,
            Review = reviewDto,
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            TotalDrillsCompleted = streak.TotalDrillsCompleted,
            AverageScore = streak.AverageScore,
            AudioUrl = audioRelativePath != null ? _audioStorageService.GetAudioUrl(audioRelativePath) : null
        };
    }
}
