using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Review.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Review.UpdateReviewCard;

public record UpdateReviewCardRequest(
    Guid CardId,
    Guid UserId,
    string FrontMarkdown,
    string BackMarkdown);

public class UpdateReviewCardResponse
{
    public ReviewCardDto Card { get; set; } = null!;

    public UpdateReviewCardResponse() { }

    public UpdateReviewCardResponse(ReviewCardDto card)
    {
        Card = card;
    }
}

public class UpdateReviewCardValidator : AbstractValidator<UpdateReviewCardRequest>
{
    public UpdateReviewCardValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FrontMarkdown)
            .NotEmpty().WithMessage("Front markdown cannot be empty.")
            .MaximumLength(5000).WithMessage("Front markdown must not exceed 5000 characters.");
        RuleFor(x => x.BackMarkdown)
            .NotEmpty().WithMessage("Back markdown cannot be empty.")
            .MaximumLength(5000).WithMessage("Back markdown must not exceed 5000 characters.");
    }
}

public class UpdateReviewCardHandler : IUseCase<UpdateReviewCardRequest, UpdateReviewCardResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<UpdateReviewCardRequest> _validator;

    public UpdateReviewCardHandler(
        ITechDailyDbContext dbContext,
        IValidator<UpdateReviewCardRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<UpdateReviewCardResponse>> ExecuteAsync(
        UpdateReviewCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var card = await _dbContext.SpacedRepetitionCards
            .Include(c => c.Topic)
            .FirstOrDefaultAsync(
                c => c.Id == request.CardId && c.UserId == request.UserId && !c.IsDeleted,
                cancellationToken);

        if (card == null)
        {
            return Error.NotFound;
        }

        card.UpdateContent(request.FrontMarkdown, request.BackMarkdown);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateReviewCardResponse(ReviewCardDto.FromEntity(card));
    }
}
