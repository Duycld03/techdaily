using DeepPace.Application.Features.Admin.Categories.DTOs;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using FluentValidation;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Features.Library.UploadPdf;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Library.ImportRemotePdf;

public record ImportRemotePdfRequest(
    string PdfUrl,
    string Title,
    Guid CategoryId,
    string Language = "en",
    Guid? CreatedByUserId = null);

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

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A valid category ID is required.");
    }
}

public class ImportRemotePdfHandler : IUseCase<ImportRemotePdfRequest, UploadPdfResponse>
{
    private const long MaxFileSize = 367_001_600; // 350 MB
    private readonly HttpClient _httpClient;
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IPdfIngestionQueue _ingestionQueue;
    private readonly IValidator<ImportRemotePdfRequest> _validator;
    private readonly ICategoryAssignmentGuard _categoryGuard;

    public ImportRemotePdfHandler(
        HttpClient httpClient,
        IDeepPaceDbContext dbContext,
        IPdfIngestionQueue ingestionQueue,
        IValidator<ImportRemotePdfRequest> validator,
        ICategoryAssignmentGuard categoryGuard)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _ingestionQueue = ingestionQueue;
        _validator = validator;
        _categoryGuard = categoryGuard;
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
        var categoryValidation = await _categoryGuard.ValidateAsync(request.CategoryId, cancellationToken);
        if (!categoryValidation.IsSuccess) return categoryValidation.Error;


        // 1. SSRF Safety Check with IP Pinning
        Uri validatedUri;
        IPAddress[] resolvedIps;
        try
        {
            (validatedUri, resolvedIps) = UrlSecurityValidator.ValidateSafeUrl(request.PdfUrl);
        }
        catch (Exception ex)
        {
            return Error.Custom("Security.SsrfBlocked", ex.Message);
        }

        var bookId = Guid.NewGuid();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "deeppace-uploads");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"{bookId:N}.pdf");

        // 2. Stream remote content into temporary file using 80KB buffer (Zero-LOH)
        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, validatedUri);
            httpRequest.Headers.Add("User-Agent", "DeepPaceCrawler/1.0 (Remote PDF Ingestion)");
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                return Error.Custom("Security.RedirectRejected", "HTTP redirects are not permitted for remote PDF downloads.");
            }
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
            CategoryId = request.CategoryId,
            SourceType = SourceType.PdfBook,
            AuthorOrSourceUrl = request.PdfUrl,
            IsPublished = true,
            Status = ProcessingStatus.Processing,
            ProgressPercentage = 0,
            StatusMessage = "Remote PDF download initiated. Processing chapters...",
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

        // 4. Enqueue into background ingestion channel
        await _ingestionQueue.EnqueueAsync(new PdfIngestJob(
            book.Id,
            tempFilePath,
            book.Title,
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
