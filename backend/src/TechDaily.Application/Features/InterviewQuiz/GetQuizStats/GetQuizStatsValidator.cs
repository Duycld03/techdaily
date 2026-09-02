using FluentValidation;
using TechDaily.Application.Features.InterviewQuiz.DTOs;

namespace TechDaily.Application.Features.InterviewQuiz.GetQuizStats;

public class GetQuizStatsValidator : AbstractValidator<GetQuizStatsRequest>
{
    public GetQuizStatsValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
