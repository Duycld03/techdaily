using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Review.GradeReviewCard;

public record GradeReviewCardRequest(Guid CardId, Guid UserId, int QualityGrade);

public class GradeReviewCardResponse
{
    public Guid CardId { get; set; }
    public int RepetitionCount { get; set; }
    public decimal EaseFactor { get; set; }
    public int IntervalDays { get; set; }
    public DateOnly NextReviewDate { get; set; }
    public CardStatus Status { get; set; }
}

public class GradeReviewCardValidator : AbstractValidator<GradeReviewCardRequest>
{
    public GradeReviewCardValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QualityGrade).InclusiveBetween(0, 5)
            .WithMessage("Quality grade must be between 0 (complete blackout) and 5 (perfect recall).");
    }
}

public class GradeReviewCardHandler : IUseCase<GradeReviewCardRequest, GradeReviewCardResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<GradeReviewCardRequest> _validator;

    public GradeReviewCardHandler(
        ITechDailyDbContext dbContext,
        IValidator<GradeReviewCardRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<GradeReviewCardResponse>> ExecuteAsync(
        GradeReviewCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var card = await _dbContext.SpacedRepetitionCards
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.UserId == request.UserId, cancellationToken);

        if (card == null)
        {
            return Error.NotFound;
        }

        card.ApplyReview(request.QualityGrade);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GradeReviewCardResponse
        {
            CardId = card.Id,
            RepetitionCount = card.RepetitionCount,
            EaseFactor = card.EaseFactor,
            IntervalDays = card.IntervalDays,
            NextReviewDate = card.NextReviewDate,
            Status = card.Status
        };
    }
}
