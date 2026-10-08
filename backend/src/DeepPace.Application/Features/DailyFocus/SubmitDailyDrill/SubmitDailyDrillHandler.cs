using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.DailyFocus.SubmitDailyDrill;

public record SubmitDailyDrillRequest(
    Guid DrillId,
    Guid UserId,
    int SelectedOptionIndex,
    string Locale = "en");

public class SubmitDailyDrillResponse
{
    public bool IsCorrect { get; set; }
    public int SelectedOptionIndex { get; set; }
    public int CorrectOptionIndex { get; set; }
    public int Score { get; set; }
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalDrillsCompleted { get; set; }
    public decimal AverageScore { get; set; }
}

public class SubmitDailyDrillValidator : AbstractValidator<SubmitDailyDrillRequest>
{
    public SubmitDailyDrillValidator()
    {
        RuleFor(x => x.DrillId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SelectedOptionIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Selected option index must be non-negative.");
    }
}

public class SubmitDailyDrillHandler : IUseCase<SubmitDailyDrillRequest, SubmitDailyDrillResponse>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<SubmitDailyDrillRequest> _validator;

    public SubmitDailyDrillHandler(
        IDeepPaceDbContext dbContext,
        IValidator<SubmitDailyDrillRequest> validator)
    {
        _dbContext = dbContext;
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

        var selectedIndex = request.SelectedOptionIndex;
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
            if (question.DocumentChunkId.HasValue)
            {
                var chunkId = question.DocumentChunkId.Value;
                var card = await _dbContext.SpacedRepetitionCards
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceDocumentChunkId == chunkId, cancellationToken);

                var sbFront = new System.Text.StringBuilder();
                sbFront.AppendLine(question.QuestionText);
                if (question.Options != null && question.Options.Count > 0)
                {
                    sbFront.AppendLine();
                    for (int i = 0; i < question.Options.Count; i++)
                    {
                        var label = (char)('A' + i);
                        sbFront.AppendLine($"- **{label}.** {question.Options[i]}");
                    }
                }
                var front = sbFront.ToString().Trim();

                var sbBack = new System.Text.StringBuilder();
                if (question.Options != null && question.CorrectOptionIndex >= 0 && question.CorrectOptionIndex < question.Options.Count)
                {
                    var label = (char)('A' + question.CorrectOptionIndex);
                    sbBack.AppendLine($"**Correct Option: {label}. {question.Options[question.CorrectOptionIndex]}**");
                    sbBack.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(question.ExplanationMarkdown))
                {
                    sbBack.AppendLine(question.ExplanationMarkdown);
                }
                var back = sbBack.ToString().Trim();

                if (card == null)
                {
                    card = SpacedRepetitionCard.CreateFromDrillMistake(request.UserId, chunkId, front, back, today.AddDays(1));
                    await _dbContext.SpacedRepetitionCards.AddAsync(card, cancellationToken);
                }
                else
                {
                    card.UpdateContent(front, back);
                    card.ResetProgression(today.AddDays(1));
                }
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
}
