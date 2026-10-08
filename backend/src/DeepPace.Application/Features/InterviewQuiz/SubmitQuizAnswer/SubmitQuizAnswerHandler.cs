using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.InterviewQuiz.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.InterviewQuiz.SubmitQuizAnswer;

public class SubmitQuizAnswerHandler : IUseCase<SubmitQuizAnswerRequest, SubmitQuizAnswerResponse>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<SubmitQuizAnswerRequest> _validator;

    public SubmitQuizAnswerHandler(
        IDeepPaceDbContext dbContext,
        IValidator<SubmitQuizAnswerRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<SubmitQuizAnswerResponse>> ExecuteAsync(
        SubmitQuizAnswerRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new Error("Error.Validation", errors);
        }

        var question = await _dbContext.QuizQuestions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId && !q.IsDeleted, cancellationToken);

        if (question == null)
        {
            return Error.NotFound;
        }

        var isCorrect = request.SelectedOptionIndex == question.CorrectOptionIndex;

        var progress = await _dbContext.UserQuizProgresses
            .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.QuestionId == request.QuestionId, cancellationToken);

        if (progress == null)
        {
            progress = new UserQuizProgress
            {
                UserId = request.UserId,
                QuestionId = request.QuestionId
            };
            await _dbContext.UserQuizProgresses.AddAsync(progress, cancellationToken);
        }

        progress.RecordAttempt(request.SelectedOptionIndex, isCorrect);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitQuizAnswerResponse(
            isCorrect,
            question.CorrectOptionIndex,
            question.ExplanationMarkdown,
            progress.IsMastered,
            progress.CorrectCount,
            progress.IncorrectCount
        );
    }
}
