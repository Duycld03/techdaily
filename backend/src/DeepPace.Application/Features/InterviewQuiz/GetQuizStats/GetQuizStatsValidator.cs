using FluentValidation;
using DeepPace.Application.Features.InterviewQuiz.DTOs;

namespace DeepPace.Application.Features.InterviewQuiz.GetQuizStats;

public class GetQuizStatsValidator : AbstractValidator<GetQuizStatsRequest>
{
    public GetQuizStatsValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
