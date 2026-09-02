using FluentValidation;
using TechDaily.Application.Features.InterviewQuiz.DTOs;

namespace TechDaily.Application.Features.InterviewQuiz.GetQuizReviewQueue;

public class GetQuizReviewQueueValidator : AbstractValidator<GetQuizReviewQueueRequest>
{
    public GetQuizReviewQueueValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
