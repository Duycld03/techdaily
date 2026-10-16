using DeepPace.Application.Features.Admin.Categories.DTOs;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.InterviewQuiz.GetQuizReviewQueue;

public class GetQuizReviewQueueHandler : IUseCase<GetQuizReviewQueueRequest, GetQuizReviewQueueResponse>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<GetQuizReviewQueueRequest> _validator;

    public GetQuizReviewQueueHandler(
        IDeepPaceDbContext dbContext,
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
            .Where(p => p.UserId == request.UserId && !p.IsMastered && !p.Question.IsDeleted &&
                (p.Question.CreatedByUserId == null || p.Question.CreatedByUserId == request.UserId));

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.Question.CategoryId == request.CategoryId.Value);
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

        var rawItems = await query
            .OrderByDescending(p => p.LastAttemptedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                p.Question.Id,
                p.Question.Topic,
                p.Question.CategoryId,
                p.Question.Category,
                p.Question.Level,
                p.Question.QuestionText,
                p.Question.Options,
                p.Question.CorrectOptionIndex,
                p.Question.ExplanationMarkdown,
                p.Question.Tags,
                p.IsMastered,
                p.LastSelectedOptionIndex,
                p.IsLastAnswerCorrect,
                p.CorrectCount,
                p.IncorrectCount
            })
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(p => new QuizQuestionDto
        {
            Id = p.Id,
            Topic = p.Topic,
            CategoryId = p.CategoryId,
            Category = p.Category!.ToDescriptor(),
            Level = p.Level,
            QuestionText = p.QuestionText,
            Options = p.Options,
            CorrectOptionIndex = p.CorrectOptionIndex,
            ExplanationMarkdown = p.ExplanationMarkdown,
            Tags = p.Tags,
            IsMastered = p.IsMastered,
            LastSelectedOptionIndex = p.LastSelectedOptionIndex,
            IsLastAnswerCorrect = p.IsLastAnswerCorrect,
            CorrectCount = p.CorrectCount,
            IncorrectCount = p.IncorrectCount
        }).ToList();

        return new GetQuizReviewQueueResponse(items, totalCount, request.Page, request.PageSize);
    }
}
