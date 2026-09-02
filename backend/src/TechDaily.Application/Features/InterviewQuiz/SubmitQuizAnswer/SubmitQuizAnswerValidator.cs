using FluentValidation;
using TechDaily.Application.Features.InterviewQuiz.DTOs;

namespace TechDaily.Application.Features.InterviewQuiz.SubmitQuizAnswer;

public class SubmitQuizAnswerValidator : AbstractValidator<SubmitQuizAnswerRequest>
{
    public SubmitQuizAnswerValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("QuestionId is required.");

        RuleFor(x => x.SelectedOptionIndex)
            .InclusiveBetween(0, 3).WithMessage("SelectedOptionIndex must be between 0 and 3.");
    }
}
