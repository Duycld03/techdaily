using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.InterviewQuiz.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.InterviewQuiz.GetQuizStats;

public class GetQuizStatsHandler : IUseCase<GetQuizStatsRequest, GetQuizStatsResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<GetQuizStatsRequest> _validator;

    public GetQuizStatsHandler(
        ITechDailyDbContext dbContext,
        IValidator<GetQuizStatsRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<GetQuizStatsResponse>> ExecuteAsync(
        GetQuizStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new Error("Error.Validation", errors);
        }

        var progresses = await _dbContext.UserQuizProgresses
            .AsNoTracking()
            .Include(p => p.Question)
            .Where(p => p.UserId == request.UserId && !p.Question.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalAnswered = progresses.Count;
        var masteredCount = progresses.Count(p => p.IsMastered);
        var reviewQueueCount = progresses.Count(p => !p.IsMastered);

        var totalAttempts = progresses.Sum(p => p.CorrectCount + p.IncorrectCount);
        var totalCorrect = progresses.Sum(p => p.CorrectCount);
        var accuracyRate = totalAttempts > 0 ? Math.Round((decimal)totalCorrect / totalAttempts * 100, 1) : 0m;

        var levelBreakdown = Enum.GetValues<QuizLevel>().Select(lvl =>
        {
            var lvlProgs = progresses.Where(p => p.Question.Level == lvl).ToList();
            var lvlAttempts = lvlProgs.Sum(p => p.CorrectCount + p.IncorrectCount);
            var lvlCorrect = lvlProgs.Sum(p => p.CorrectCount);
            return new LevelStatDto(
                lvl,
                lvlProgs.Count,
                lvlProgs.Count(p => p.IsMastered),
                lvlAttempts > 0 ? Math.Round((decimal)lvlCorrect / lvlAttempts * 100, 1) : 0m
            );
        }).ToList();

        var topicBreakdown = progresses
            .GroupBy(p => p.Question.Topic)
            .Select(g =>
            {
                var tAttempts = g.Sum(p => p.CorrectCount + p.IncorrectCount);
                var tCorrect = g.Sum(p => p.CorrectCount);
                return new TopicStatDto(
                    g.Key,
                    g.Count(),
                    g.Count(p => p.IsMastered),
                    tAttempts > 0 ? Math.Round((decimal)tCorrect / tAttempts * 100, 1) : 0m
                );
            })
            .OrderByDescending(t => t.AnsweredCount)
            .Take(10)
            .ToList();

        return new GetQuizStatsResponse(
            totalAnswered,
            masteredCount,
            reviewQueueCount,
            accuracyRate,
            levelBreakdown,
            topicBreakdown
        );
    }
}
