using FluentValidation;
using TechDaily.Application.Features.InterviewQuiz.DTOs;

namespace TechDaily.Application.Features.InterviewQuiz.GenerateQuiz;

public class GenerateQuizValidator : AbstractValidator<GenerateQuizRequest>
{
    public GenerateQuizValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required.")
            .MinimumLength(2).WithMessage("Topic must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Topic must not exceed 100 characters.");

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 10).WithMessage("Question count must be between 1 and 10.");
    }
}
