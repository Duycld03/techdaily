using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Notes.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Notes.UpdateHighlight;

public record UpdateHighlightRequest(
    Guid HighlightId,
    Guid UserId,
    string? Note = null,
    List<string>? Tags = null);

public class UpdateHighlightResponse
{
    public HighlightDto Highlight { get; set; } = null!;

    public UpdateHighlightResponse() { }

    public UpdateHighlightResponse(HighlightDto highlight)
    {
        Highlight = highlight;
    }
}

public class UpdateHighlightValidator : AbstractValidator<UpdateHighlightRequest>
{
    public UpdateHighlightValidator()
    {
        RuleFor(x => x.HighlightId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Note)
            .MaximumLength(2000)
            .WithMessage("Note must not exceed 2000 characters.")
            .When(x => x.Note != null);
        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Count <= 10)
            .WithMessage("Tags cannot exceed 10 items.");
        RuleForEach(x => x.Tags)
            .MaximumLength(50)
            .WithMessage("Each tag must not exceed 50 characters.")
            .When(x => x.Tags != null);
    }
}

public class UpdateHighlightHandler : IUseCase<UpdateHighlightRequest, UpdateHighlightResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<UpdateHighlightRequest> _validator;

    public UpdateHighlightHandler(
        ITechDailyDbContext dbContext,
        IValidator<UpdateHighlightRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<UpdateHighlightResponse>> ExecuteAsync(
        UpdateHighlightRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var highlight = await _dbContext.UserHighlights
            .Include(h => h.DocumentChunk)
                .ThenInclude(c => c.DocumentBook)
            .FirstOrDefaultAsync(
                h => h.Id == request.HighlightId && h.UserId == request.UserId && !h.IsDeleted,
                cancellationToken);

        if (highlight == null)
        {
            return Error.NotFound;
        }

        highlight.Update(request.Note, request.Tags);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateHighlightResponse(new HighlightDto
        {
            Id = highlight.Id,
            DocumentChunkId = highlight.DocumentChunkId,
            ChapterTitle = highlight.DocumentChunk?.ChapterTitle ?? "Reading Slice",
            BookTitle = highlight.DocumentChunk?.DocumentBook?.Title ?? "Core Curriculum",
            SelectedText = highlight.SelectedText,
            Note = highlight.Note,
            Tags = highlight.Tags,
            CreatedAt = highlight.CreatedAt
        });
    }
}
