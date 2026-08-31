using System.Text.RegularExpressions;
using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Application.Features.Library.UploadPdf;

public record UploadPdfRequest(
    Stream FileStream,
    string FileName,
    long FileLength,
    string? Title,
    Category Category,
    string Language = "en");

public class UploadPdfResponse
{
    public BookDto Book { get; set; } = null!;
}

public class UploadPdfValidator : AbstractValidator<UploadPdfRequest>
{
    private const long MaxFileSize = 209_715_200; // 200 MB (50-60% of Gemini capacity)

    public UploadPdfValidator()
    {
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("File size must not exceed 200 MB.");
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .pdf files are supported.");
    }
}

public class UploadPdfHandler : IUseCase<UploadPdfRequest, UploadPdfResponse>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IPdfExtractor _pdfExtractor;
    private readonly IValidator<UploadPdfRequest> _validator;

    public UploadPdfHandler(
        ITechDailyDbContext dbContext,
        IPdfExtractor pdfExtractor,
        IValidator<UploadPdfRequest> validator)
    {
        _dbContext = dbContext;
        _pdfExtractor = pdfExtractor;
        _validator = validator;
    }

    public async Task<Result<UploadPdfResponse>> ExecuteAsync(
        UploadPdfRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        PdfExtractionResult extraction;
        try
        {
            extraction = await _pdfExtractor.ExtractSlicesAsync(
                request.FileStream,
                request.Title,
                maxPages: 800,
                cancellationToken);
        }
        catch (Exception ex)
        {
            return Error.Custom("PdfExtraction.Failed", $"Could not process PDF: {ex.Message}");
        }

        var bookTitle = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title
            : extraction.DocumentTitle;

        var slug = GenerateSlug(bookTitle);

        var book = new DocumentBook
        {
            Title = bookTitle,
            Slug = slug,
            Category = request.Category,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = request.FileName,
            IsPublished = true,
            TotalChunks = extraction.Slices.Count
        };

        foreach (var slice in extraction.Slices)
        {
            var summary = slice.ContentMarkdown.Length > 300
                ? slice.ContentMarkdown.Substring(0, 300) + "..."
                : slice.ContentMarkdown;

            var chunk = new DocumentChunk
            {
                DocumentBookId = book.Id,
                ChunkOrder = slice.Order,
                ChapterTitle = slice.ChapterTitle,
                OriginalTextMarkdown = slice.ContentMarkdown,
                SummaryMarkdown = summary,
                Language = request.Language,
                EstimatedReadMinutes = slice.EstimatedReadMinutes,
                KeyTakeaways = slice.KeyTakeaways,
                MicroQuiz = new MicroQuizVo
                {
                    Question = $"What is the primary technical concept discussed in {slice.ChapterTitle}?",
                    Options = new()
                    {
                        "Core Architecture Invariant & Implementation",
                        "Deprecated Legacy Behavior",
                        "Third-party Library Bug",
                        "Unused Abstract Syntax"
                    },
                    AnswerIndex = 0,
                    Explanation = $"Understanding {slice.ChapterTitle} reinforces core technical architecture principles."
                }
            };

            book.Chunks.Add(chunk);
        }

        await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UploadPdfResponse
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

    private static string GenerateSlug(string title)
    {
        var clean = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        var slug = Regex.Replace(clean, @"\s+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "document";
        }
        return slug + "-" + Guid.NewGuid().ToString().Substring(0, 6);
    }
}
