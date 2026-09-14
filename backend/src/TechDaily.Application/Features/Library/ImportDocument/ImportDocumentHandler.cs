using System.Text.RegularExpressions;
using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Application.Features.Library.ImportDocument;

public record ImportDocumentRequest(
    string Title,
    string MarkdownContent,
    Category Category,
    string? SourceUrl = null,
    string Language = "en");

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
    private readonly IValidator<ImportDocumentRequest> _validator;

    public ImportDocumentHandler(
        ITechDailyDbContext dbContext,
        IValidator<ImportDocumentRequest> validator)
    {
        _dbContext = dbContext;
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
            IsPublished = true
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
                MicroQuiz = new MicroQuizVo
                {
                    Question = $"What is the primary architectural takeaway from {title}?",
                    Options = new() { "Performance & Reliability", "Unnecessary Overhead", "Deprecation Notice", "Syntax sugar only" },
                    AnswerIndex = 0,
                    Explanation = "Core principle emphasizes robustness and performance efficiency."
                }
            };

            book.Chunks.Add(chunk);
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
