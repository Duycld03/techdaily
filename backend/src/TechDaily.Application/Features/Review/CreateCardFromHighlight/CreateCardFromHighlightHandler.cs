using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
namespace TechDaily.Application.Features.Review.CreateCardFromHighlight;

public record CreateCardFromHighlightRequest(Guid HighlightId, Guid UserId, string Locale = "en");

public class CreateCardFromHighlightResponse
{
    public Guid CardId { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;

    public CreateCardFromHighlightResponse() { }

    public CreateCardFromHighlightResponse(Guid cardId, string front, string back)
    {
        CardId = cardId;
        Front = front;
        Back = back;
    }
}

public class CreateCardFromHighlightHandler : IUseCase<CreateCardFromHighlightRequest, CreateCardFromHighlightResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IGeminiAiService _geminiAiService;
    private readonly ILogger<CreateCardFromHighlightHandler>? _logger;

    public CreateCardFromHighlightHandler(
        ITechDailyDbContext dbContext,
        IGeminiAiService geminiAiService,
        ILogger<CreateCardFromHighlightHandler>? logger = null)
    {
        _dbContext = dbContext;
        _geminiAiService = geminiAiService;
        _logger = logger;
    }

    public async Task<Result<CreateCardFromHighlightResponse>> ExecuteAsync(
        CreateCardFromHighlightRequest request,
        CancellationToken cancellationToken = default)
    {
        var highlight = await _dbContext.UserHighlights
            .Include(h => h.DocumentChunk)
            .FirstOrDefaultAsync(h => h.Id == request.HighlightId && h.UserId == request.UserId, cancellationToken);

        if (highlight == null)
        {
            return Error.NotFound;
        }

        // Idempotency: return existing card if already created
        var existingCard = await _dbContext.SpacedRepetitionCards
            .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.SourceHighlightId == request.HighlightId, cancellationToken);

        if (existingCard != null)
        {
            return new CreateCardFromHighlightResponse(
                existingCard.Id,
                existingCard.FrontMarkdown ?? string.Empty,
                existingCard.BackMarkdown ?? string.Empty);
        }

        var chapterTitle = highlight.DocumentChunk?.ChapterTitle ?? "Technical Guide";
        var cardResult = await _geminiAiService.SynthesizeActiveRecallCardAsync(
            highlight.SelectedText,
            highlight.Note,
            chapterTitle,
            request.Locale,
            cancellationToken);

        if (!cardResult.IsSuccess || string.IsNullOrWhiteSpace(cardResult.Value.Front) || string.IsNullOrWhiteSpace(cardResult.Value.Back))
        {
            _logger?.LogWarning("AI recall synthesis failed: {Error} for highlight {HighlightId}.", cardResult.Error?.Message ?? "Unknown", highlight.Id);
            return Result<CreateCardFromHighlightResponse>.Failure(cardResult.Error ?? Error.Custom("AiService.RecallFailed", "Active recall synthesis failed."));
        }

        var (front, back) = cardResult.Value;
        var card = SpacedRepetitionCard.CreateFromHighlight(
            request.UserId,
            highlight.Id,
            front,
            back);

        await _dbContext.SpacedRepetitionCards.AddAsync(card, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateCardFromHighlightResponse(card.Id, front, back);
    }
}
