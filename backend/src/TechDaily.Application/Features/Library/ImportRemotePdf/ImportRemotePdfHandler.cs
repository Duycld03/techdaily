using System.Text.RegularExpressions;
using FluentValidation;
using TechDaily.Application.Common;
using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Application.Features.Library.UploadPdf;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.ImportRemotePdf;

public record ImportRemotePdfRequest(
    string PdfUrl,
    string Title,
    Category Category,
    string Language = "en");

public class ImportRemotePdfValidator : AbstractValidator<ImportRemotePdfRequest>
{
    public ImportRemotePdfValidator()
    {
        RuleFor(x => x.PdfUrl)
            .NotEmpty().WithMessage("PDF URL is required.")
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A valid HTTP or HTTPS URL is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title cannot exceed 250 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid category is required.");
    }
}

public class ImportRemotePdfHandler : IUseCase<ImportRemotePdfRequest, UploadPdfResponse>
{
    private const long MaxFileSize = 367_001_600; // 350 MB
    private readonly HttpClient _httpClient;
    private readonly ITechDailyDbContext _dbContext;
    private readonly IPdfIngestionQueue _ingestionQueue;
    private readonly IValidator<ImportRemotePdfRequest> _validator;

    public ImportRemotePdfHandler(
        HttpClient httpClient,
        ITechDailyDbContext dbContext,
        IPdfIngestionQueue ingestionQueue,
        IValidator<ImportRemotePdfRequest> validator)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _ingestionQueue = ingestionQueue;
        _validator = validator;
    }

    public async Task<Result<UploadPdfResponse>> ExecuteAsync(
        ImportRemotePdfRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Error.Custom("Validation.Failed", validation.Errors.First().ErrorMessage);
        }

        // 1. SSRF Safety Check
        try
        {
            UrlSecurityValidator.ValidateSafeUrl(request.PdfUrl);
        }
        catch (Exception ex)
        {
            return Error.Custom("Security.SsrfBlocked", ex.Message);
        }

        var bookId = Guid.NewGuid();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "techdaily-uploads");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"{bookId:N}.pdf");

        // 2. Stream remote content into temporary file using 80KB buffer (Zero-LOH)
        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.PdfUrl);
            httpRequest.Headers.Add("User-Agent", "TechDailyCrawler/1.0 (Remote PDF Ingestion)");
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            return Error.Custom("RemotePdf.DownloadFailed", $"Failed to connect or download remote PDF: {ex.Message}");
        }

        using (response)
        {
            if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > MaxFileSize)
            {
                return Error.Custom("RemotePdf.FileTooLarge", "Remote PDF exceeds the maximum allowed size of 350 MB.");
            }

            try
            {
                await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

                byte[] buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;

                while ((bytesRead = await remoteStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    totalBytes += bytesRead;
                    if (totalBytes > MaxFileSize)
                    {
                        await fileStream.DisposeAsync();
                        if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                        return Error.Custom("RemotePdf.FileTooLarge", "Remote PDF exceeds the maximum allowed size of 350 MB.");
                    }
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                return Error.Custom("RemotePdf.StreamFailed", $"Failed to stream remote PDF to disk: {ex.Message}");
            }
        }

        // 3. Create DocumentBook entity
        var rawTitle = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : "Remote Technical Document";
        var bookTitle = SanitizeText(rawTitle);
        var slug = GenerateSlug(bookTitle);

        var book = new DocumentBook
        {
            Id = bookId,
            Title = bookTitle,
            Slug = slug,
            Category = request.Category,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = request.PdfUrl,
            IsPublished = true,
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            StatusMessage = "Remote PDF download initiated. Processing chapters...",
            TotalChunks = 0
        };

        await _dbContext.DocumentBooks.AddAsync(book, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Enqueue into background ingestion channel
        await _ingestionQueue.EnqueueAsync(new PdfIngestJob(
            book.Id,
            tempFilePath,
            book.Title,
            request.Category,
            request.Language), cancellationToken);

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
