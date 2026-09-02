using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.InterviewQuiz.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.InterviewQuiz.GetQuizReviewQueue;

public class GetQuizReviewQueueHandler : IUseCase<GetQuizReviewQueueRequest, GetQuizReviewQueueResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<GetQuizReviewQueueRequest> _validator;

    public GetQuizReviewQueueHandler(
        ITechDailyDbContext dbContext,
        IValidator<GetQuizReviewQueueRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<GetQuizReviewQueueResponse>> ExecuteAsync(
        GetQuizReviewQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new Error("Error.Validation", errors);
        }

        var query = _dbContext.UserQuizProgresses
            .AsNoTracking()
            .Include(p => p.Question)
            .Where(p => p.UserId == request.UserId && !p.IsMastered && !p.Question.IsDeleted);

        if (request.Category.HasValue)
        {
            query = query.Where(p => p.Question.Category == request.Category.Value);
        }

        if (request.Level.HasValue)
        {
            query = query.Where(p => p.Question.Level == request.Level.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Topic))
        {
            var topicLower = request.Topic.Trim().ToLower();
            query = query.Where(p => p.Question.Topic.ToLower().Contains(topicLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.LastAttemptedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new QuizQuestionDto
            {
                Id = p.Question.Id,
                Topic = p.Question.Topic,
                Category = p.Question.Category,
                Level = p.Question.Level,
                QuestionText = p.Question.QuestionText,
                Options = p.Question.Options,
                CorrectOptionIndex = p.Question.CorrectOptionIndex,
                ExplanationMarkdown = p.Question.ExplanationMarkdown,
                Tags = p.Question.Tags,
                IsMastered = p.IsMastered,
                LastSelectedOptionIndex = p.LastSelectedOptionIndex,
                IsLastAnswerCorrect = p.IsLastAnswerCorrect,
                CorrectCount = p.CorrectCount,
                IncorrectCount = p.IncorrectCount
            })
            .ToListAsync(cancellationToken);

        return new GetQuizReviewQueueResponse(items, totalCount, request.Page, request.PageSize);
    }
}
