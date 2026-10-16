using DeepPace.Application.Features.Admin.Categories.DTOs;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Library.UploadPdf;

public record UploadPdfRequest(
    Stream FileStream,
    string FileName,
    long FileLength,
    string? Title,
    Guid CategoryId,
    string Language = "en",
    Guid? CreatedByUserId = null);

public class UploadPdfResponse
{
    public BookDto Book { get; set; } = null!;
}

public class UploadPdfValidator : AbstractValidator<UploadPdfRequest>
{
    private const long MaxFileSize = 367_001_600; // 350 MB

    public UploadPdfValidator()
    {
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("File size must not exceed 350 MB.");
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .pdf files are supported.");
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UploadPdfHandler : IUseCase<UploadPdfRequest, UploadPdfResponse>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IPdfIngestionQueue _ingestionQueue;
    private readonly IValidator<UploadPdfRequest> _validator;
    private readonly ICategoryAssignmentGuard _categoryGuard;

    public UploadPdfHandler(
        IDeepPaceDbContext dbContext,
        IPdfIngestionQueue ingestionQueue,
        IValidator<UploadPdfRequest> validator,
        ICategoryAssignmentGuard categoryGuard)
    {
        _dbContext = dbContext;
        _ingestionQueue = ingestionQueue;
        _validator = validator;
        _categoryGuard = categoryGuard;
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
        var categoryValidation = await _categoryGuard.ValidateAsync(request.CategoryId, cancellationToken);
        if (!categoryValidation.IsSuccess) return categoryValidation.Error;


        var rawTitle = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title
            : Path.GetFileNameWithoutExtension(request.FileName);

        var bookTitle = SanitizeText(rawTitle);
        if (string.IsNullOrWhiteSpace(bookTitle))
        {
            bookTitle = "Uploaded Technical Document";
        }
        var slug = GenerateSlug(bookTitle);
        var bookId = Guid.NewGuid();

        // Zero-LOH Disk Spooling (80KB buffer)
        var tempDirectory = Path.Combine(Path.GetTempPath(), "deeppace-uploads");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"{bookId}.pdf");

        try
        {
            await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await request.FileStream.CopyToAsync(fileStream, bufferSize: 81920, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return Error.Custom("PdfUpload.Failed", $"Failed to save uploaded file: {ex.Message}");
        }

        // Validate PDF magic bytes (%PDF-)
        var magicBytes = new byte[5];
        int bytesRead;
        await using (var verifyStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
        {
            bytesRead = await verifyStream.ReadAsync(magicBytes.AsMemory(0, 5), cancellationToken);
        }

        if (bytesRead < 5 || Encoding.ASCII.GetString(magicBytes) != "%PDF-")
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            return Error.Custom("INVALID_PDF_FORMAT", "The file is not a valid PDF document.");
        }

        var book = new DocumentBook
        {
            Id = bookId,
            Title = bookTitle,
            Slug = slug,
            CategoryId = request.CategoryId,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = SanitizeText(request.FileName),
            IsPublished = true,
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            StatusMessage = "File uploaded, queued for processing...",
            TotalChunks = 0,
            CreatedByUserId = request.CreatedByUserId
        };

        var admitted = await _categoryGuard.ExecuteAsync<Guid>(request.CategoryId, async category =>
        {
            book.Category = category;
            await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
            return book.Id;
        }, cancellationToken);
        if (!admitted.IsSuccess)
        {
            File.Delete(tempFilePath);
            return admitted.Error;
        }

        await _ingestionQueue.EnqueueAsync(new PdfIngestJob(
            book.Id,
            tempFilePath,
            bookTitle,
            request.CategoryId,
            request.Language), cancellationToken);

        return new UploadPdfResponse
        {
            Book = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Slug = book.Slug,
                SourceType = book.SourceType,
                CategoryId = book.CategoryId,
                Category = book.Category!.ToDescriptor(),
                AuthorOrSourceUrl = book.AuthorOrSourceUrl,
                TotalChunks = book.TotalChunks,
                IsPublished = book.IsPublished,
                IsFeatured = book.IsFeatured,
                Status = book.Status,
                ProgressPercentage = book.ProgressPercentage,
                StatusMessage = book.StatusMessage,
                ErrorMessage = book.ErrorMessage,
                CreatedAt = book.CreatedAt
            }
        };
    }

    private static string SanitizeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "").Trim();
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
