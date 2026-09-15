using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.AskBook;

public record AskBookRequest(
    Guid BookId,
    string Question,
    Guid? CurrentChunkId = null,
    string Locale = "en");

public class BookCitationDto
{
    public int ChunkOrder { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string Excerpt { get; set; } = string.Empty;
}

public class AskBookResponse
{
    public string AnswerMarkdown { get; set; } = string.Empty;
    public List<BookCitationDto> Citations { get; set; } = new();
}

public class AskBookValidator : AbstractValidator<AskBookRequest>
{
    public AskBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Question).NotEmpty()
            .MinimumLength(3).WithMessage("Question must be at least 3 characters.")
            .MaximumLength(300).WithMessage("Question cannot exceed 300 characters.");
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
    }
}

public class AskBookHandler : IUseCase<AskBookRequest, AskBookResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IBookQAService _bookQAService;
    private readonly IValidator<AskBookRequest> _validator;

    public AskBookHandler(
        ITechDailyDbContext dbContext,
        IEmbeddingService embeddingService,
        IBookQAService bookQAService,
        IValidator<AskBookRequest> validator)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _bookQAService = bookQAService;
        _validator = validator;
    }

    public async Task<Result<AskBookResponse>> ExecuteAsync(
        AskBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var book = await _dbContext.DocumentBooks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        var contexts = new List<(int ChunkOrder, string ChapterTitle, string Text)>();
        var citations = new List<BookCitationDto>();

        // 1. Try Vector Retrieval if embeddings exist
        var queryVectorResult = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);
        if (queryVectorResult.IsSuccess)
        {
            try
            {
                var qVec = queryVectorResult.Value;
                var matchedChunks = await _dbContext.DocumentChunks
                    .AsNoTracking()
                    .Where(c => c.DocumentBookId == request.BookId && c.Embedding != null)
                    .OrderBy(c => c.Embedding!.CosineDistance(qVec))
                    .Take(3)
                    .Select(c => new
                    {
                        c.ChunkOrder,
                        c.ChapterTitle,
                        c.OriginalTextMarkdown,
                        c.SummaryMarkdown,
                        Distance = c.Embedding!.CosineDistance(qVec)
                    })
                    .ToListAsync(cancellationToken);

                foreach (var mc in matchedChunks)
                {
                    var text = !string.IsNullOrWhiteSpace(mc.OriginalTextMarkdown) ? mc.OriginalTextMarkdown : mc.SummaryMarkdown;
                    contexts.Add((mc.ChunkOrder, mc.ChapterTitle, text));

                    // Cosine similarity ~ 1 - distance
                    var sim = Math.Max(0.0, Math.Min(1.0, 1.0 - mc.Distance));
                    var excerptSnippet = !string.IsNullOrWhiteSpace(mc.SummaryMarkdown)
                        ? mc.SummaryMarkdown
                        : (text.Length > 200 ? text[..200] + "..." : text);

                    citations.Add(new BookCitationDto
                    {
                        ChunkOrder = mc.ChunkOrder,
                        ChapterTitle = mc.ChapterTitle,
                        RelevanceScore = Math.Round(sim, 2),
                        Excerpt = excerptSnippet
                    });
                }
            }
            catch
            {
                // Fallback for providers that don't support pgvector translation (e.g. SQLite unit test provider)
            }
        }

        // 2. Fallback to current chunk or initial chunks if no vector matches yet
        if (contexts.Count == 0)
        {
            var fallbackQuery = _dbContext.DocumentChunks
                .AsNoTracking()
                .Where(c => c.DocumentBookId == request.BookId);

            if (request.CurrentChunkId.HasValue)
            {
                var currentChunk = await fallbackQuery
                    .FirstOrDefaultAsync(c => c.Id == request.CurrentChunkId.Value, cancellationToken);

                if (currentChunk != null)
                {
                    var text = !string.IsNullOrWhiteSpace(currentChunk.OriginalTextMarkdown)
                        ? currentChunk.OriginalTextMarkdown
                        : currentChunk.SummaryMarkdown;
                    contexts.Add((currentChunk.ChunkOrder, currentChunk.ChapterTitle, text));
                    citations.Add(new BookCitationDto
                    {
                        ChunkOrder = currentChunk.ChunkOrder,
                        ChapterTitle = currentChunk.ChapterTitle,
                        RelevanceScore = 1.0,
                        Excerpt = currentChunk.SummaryMarkdown
                    });
                }
            }

            if (contexts.Count == 0)
            {
                var initialChunks = await fallbackQuery
                    .OrderBy(c => c.ChunkOrder)
                    .Take(3)
                    .ToListAsync(cancellationToken);

                foreach (var chunk in initialChunks)
                {
                    var text = !string.IsNullOrWhiteSpace(chunk.OriginalTextMarkdown)
                        ? chunk.OriginalTextMarkdown
                        : chunk.SummaryMarkdown;
                    contexts.Add((chunk.ChunkOrder, chunk.ChapterTitle, text));
                    citations.Add(new BookCitationDto
                    {
                        ChunkOrder = chunk.ChunkOrder,
                        ChapterTitle = chunk.ChapterTitle,
                        RelevanceScore = 0.8,
                        Excerpt = chunk.SummaryMarkdown
                    });
                }
            }
        }

        // 3. Grounded Answer Synthesis via Gemini LLM
        var answerResult = await _bookQAService.AnswerQuestionAsync(
            book.Title,
            request.Question,
            contexts,
            request.Locale,
            cancellationToken);

        if (answerResult.IsFailure)
        {
            return answerResult.Error;
        }

        return new AskBookResponse
        {
            AnswerMarkdown = answerResult.Value,
            Citations = citations
        };
    }
}
