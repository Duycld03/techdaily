using System.Text.RegularExpressions;
using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.ImportDocument;

public record ImportDocumentRequest(
    string Title,
    string MarkdownContent,
    Category Category,
    string? SourceUrl = null,
    string Language = "en",
    Guid? CreatedByUserId = null);

public class ImportDocumentResponse
{
    public BookDto Book { get; set; } = null!;
}

public class ImportDocumentValidator : AbstractValidator<ImportDocumentRequest>
{
    public ImportDocumentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MarkdownContent).NotEmpty().MinimumLength(50);
    }
}

public class ImportDocumentHandler : IUseCase<ImportDocumentRequest, ImportDocumentResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IValidator<ImportDocumentRequest> _validator;

    public ImportDocumentHandler(
        ITechDailyDbContext dbContext,
        IEmbeddingService embeddingService,
        IValidator<ImportDocumentRequest> validator)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _validator = validator;
    }

    public async Task<Result<ImportDocumentResponse>> ExecuteAsync(
        ImportDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        var slug = GenerateSlug(request.Title);

        var book = new DocumentBook
        {
            Title = request.Title,
            Slug = slug,
            Category = request.Category,
            SourceType = request.SourceUrl != null ? SourceType.WebDocUrl : SourceType.MarkdownSeries,
            AuthorOrSourceUrl = request.SourceUrl,
            IsPublished = true,
            CreatedByUserId = request.CreatedByUserId
        };

        // Split markdown content into logical chunks by heading or paragraphs
        var rawChunks = SplitIntoChunks(request.MarkdownContent);
        int order = 1;

        foreach (var chunkContent in rawChunks)
        {
            var title = ExtractTitle(chunkContent, order);
            var estimatedMinutes = Math.Max(1, chunkContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200);

            var contentWithoutHeading = Regex.Replace(chunkContent, @"^\s*#+\s+[^\n\r]+(\r?\n)*", "").Trim();
            var summaryText = string.IsNullOrWhiteSpace(contentWithoutHeading) ? title : contentWithoutHeading;

            var chunk = new DocumentChunk
            {
                DocumentBookId = book.Id,
                ChunkOrder = order++,
                ChapterTitle = title,
                OriginalTextMarkdown = chunkContent,
                SummaryMarkdown = summaryText.Length > 300 ? summaryText.Substring(0, 300) + "..." : summaryText,
                Language = request.Language,
                EstimatedReadMinutes = estimatedMinutes,
                KeyTakeaways = new() { "Core Architecture Principle", "System Invariant" },
            };

            book.Chunks.Add(chunk);
        }

        if (book.Chunks.Count > 0)
        {
            var chunkList = book.Chunks.ToList();
            var texts = chunkList.Select(c => $"{c.ChapterTitle}: {c.SummaryMarkdown}").ToList();
            var embResult = await _embeddingService.GenerateBatchEmbeddingsAsync(texts, cancellationToken);
            if (embResult.IsSuccess && embResult.Value.Count == chunkList.Count)
            {
                for (int i = 0; i < chunkList.Count; i++)
                {
                    chunkList[i].Embedding = embResult.Value[i];
                }
            }
        }

        book.TotalChunks = book.Chunks.Count;

        await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ImportDocumentResponse
        {
            Book = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Slug = book.Slug,
                SourceType = book.SourceType,
                Category = book.Category,
                AuthorOrSourceUrl = book.AuthorOrSourceUrl,
                TotalChunks = book.TotalChunks,
                IsPublished = book.IsPublished,
                CreatedAt = book.CreatedAt
            }
        };
    }

    private static List<string> SplitIntoChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        // Pass 1: Split on headings (#, ##, ###) outside code blocks
        var headingChunks = SplitOnHeadings(text);

        // Pass 2: Subdivide long sections exceeding 1,500 words
        var finalChunks = new List<string>();
        foreach (var chunk in headingChunks)
        {
            var wordCount = CountWords(chunk);
            if (wordCount <= 1500)
            {
                finalChunks.Add(chunk);
            }
            else
            {
                var subSlices = SubdivideLongSection(chunk, targetWords: 1000, maxWords: 1500);
                finalChunks.AddRange(subSlices);
            }
        }

        return finalChunks.Any() ? finalChunks : new List<string> { text.Trim() };
    }

    private static List<string> SplitOnHeadings(string text)
    {
        var lines = text.Split('\n');
        var chunks = new List<string>();
        var currentChunk = new System.Text.StringBuilder();
        bool inCodeBlock = false;
        var headingRegex = new Regex(@"^#{1,3}\s+", RegexOptions.Compiled);

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.TrimStart();
            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
            }

            // Only split on headings when NOT inside an active code block
            if (!inCodeBlock && headingRegex.IsMatch(trimmed) && currentChunk.Length > 0)
            {
                var chunkStr = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkStr))
                {
                    chunks.Add(chunkStr);
                }
                currentChunk.Clear();
            }

            currentChunk.AppendLine(rawLine);
        }

        if (currentChunk.Length > 0)
        {
            var chunkStr = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(chunkStr))
            {
                chunks.Add(chunkStr);
            }
        }

        return chunks.Any() ? chunks : new List<string> { text.Trim() };
    }

    private static List<string> SubdivideLongSection(string chunk, int targetWords = 1000, int maxWords = 1500)
    {
        var lines = chunk.Split('\n');
        string sectionTitle = "Section";
        bool hasHeading = false;
        int bodyStartIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var headingMatch = Regex.Match(trimmed, @"^#{1,6}\s+(.+)");
            if (headingMatch.Success)
            {
                sectionTitle = headingMatch.Groups[1].Value.Trim();
                hasHeading = true;
                bodyStartIndex = i + 1;
            }
            break;
        }

        // Parse into atomic blocks (paragraphs, code blocks, tables, blockquotes)
        var blocks = new List<string>();
        var currentBlock = new System.Text.StringBuilder();
        bool inCodeBlock = false;

        for (int i = hasHeading ? bodyStartIndex : 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var trimmed = rawLine.TrimStart();

            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
            }

            if (!inCodeBlock && string.IsNullOrWhiteSpace(rawLine))
            {
                if (currentBlock.Length > 0)
                {
                    var blockText = currentBlock.ToString().Trim();
                    if (!string.IsNullOrEmpty(blockText))
                    {
                        blocks.Add(blockText);
                    }
                    currentBlock.Clear();
                }
            }
            else
            {
                currentBlock.AppendLine(rawLine);
            }
        }

        if (currentBlock.Length > 0)
        {
            var blockText = currentBlock.ToString().Trim();
            if (!string.IsNullOrEmpty(blockText))
            {
                blocks.Add(blockText);
            }
        }

        if (blocks.Count == 0)
        {
            return new List<string> { chunk };
        }

        // Group blocks into slices aiming for ~800 to 1200 words (max 1500)
        var slices = new List<string>();
        var currentSliceBlocks = new List<string>();
        int currentWordCount = 0;

        foreach (var block in blocks)
        {
            var blockWords = CountWords(block);

            if (currentSliceBlocks.Count > 0 &&
                ((currentWordCount >= 800 && currentWordCount + blockWords > 1200) ||
                 (currentWordCount >= targetWords) ||
                 (currentWordCount + blockWords > maxWords)))
            {
                slices.Add(string.Join("\n\n", currentSliceBlocks));
                currentSliceBlocks.Clear();
                currentWordCount = 0;
            }

            currentSliceBlocks.Add(block);
            currentWordCount += blockWords;
        }

        if (currentSliceBlocks.Count > 0)
        {
            if (slices.Count > 0 && currentWordCount < 400 && (CountWords(slices[^1]) + currentWordCount <= maxWords))
            {
                slices[^1] = slices[^1] + "\n\n" + string.Join("\n\n", currentSliceBlocks);
            }
            else
            {
                slices.Add(string.Join("\n\n", currentSliceBlocks));
            }
        }

        if (slices.Count <= 1)
        {
            return new List<string> { chunk };
        }

        var result = new List<string>();
        for (int i = 0; i < slices.Count; i++)
        {
            result.Add($"# {sectionTitle} (Part {i + 1})\n\n{slices[i]}");
        }
        return result;
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string ExtractTitle(string chunk, int order)
    {
        var firstLine = chunk.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
        if (firstLine.StartsWith('#'))
        {
            return firstLine.TrimStart('#').Trim();
        }
        return $"Section {order}";
    }

    private static string GenerateSlug(string title)
    {
        var clean = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(clean, @"\s+", "-").Trim('-') + "-" + Guid.NewGuid().ToString().Substring(0, 6);
    }
}
