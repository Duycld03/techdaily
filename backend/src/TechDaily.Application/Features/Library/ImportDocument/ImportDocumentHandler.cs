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

            var chunk = new DocumentChunk
            {
                DocumentBookId = book.Id,
                ChunkOrder = order++,
                ChapterTitle = title,
                OriginalTextMarkdown = chunkContent,
                SummaryMarkdown = chunkContent.Length > 300 ? chunkContent.Substring(0, 300) + "..." : chunkContent,
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
        var headingRegex = new Regex(@"(?=^#{1,3}\s+)", RegexOptions.Multiline);
        var sections = headingRegex.Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return sections.Any() ? sections : new List<string> { text };
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
