using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Features.Notes.CreateHighlight;

public record CreateHighlightRequest(
    Guid UserId,
    Guid DocumentChunkId,
    string SelectedText,
    string? Note = null,
    List<string>? Tags = null);

public class CreateHighlightResponse
{
    public HighlightDto Highlight { get; set; } = null!;
}

public class CreateHighlightValidator : AbstractValidator<CreateHighlightRequest>
{
    public CreateHighlightValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DocumentChunkId).NotEmpty();
        RuleFor(x => x.SelectedText).NotEmpty().MaximumLength(2000);
    }
}

public class CreateHighlightHandler : IUseCase<CreateHighlightRequest, CreateHighlightResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<CreateHighlightRequest> _validator;

    public CreateHighlightHandler(
        ITechDailyDbContext dbContext,
        IValidator<CreateHighlightRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<CreateHighlightResponse>> ExecuteAsync(
        CreateHighlightRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var highlight = new UserHighlight
        {
            UserId = request.UserId,
            DocumentChunkId = request.DocumentChunkId,
            SelectedText = request.SelectedText,
            Note = request.Note,
            Tags = request.Tags ?? new()
        };

        await _dbContext.UserHighlights.AddAsync(highlight, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateHighlightResponse
        {
            Highlight = new HighlightDto
            {
                Id = highlight.Id,
                DocumentChunkId = highlight.DocumentChunkId,
                SelectedText = highlight.SelectedText,
                Note = highlight.Note,
                Tags = highlight.Tags,
                CreatedAt = highlight.CreatedAt
            }
        };
    }
}
