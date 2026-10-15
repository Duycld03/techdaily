using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class DocumentBook : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public Category Category { get; set; }
    public int TotalChunks { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    // Ingestion status
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public int ProgressPercentage { get; set; } = 0;
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // Control version and attempt tracking
    public int IngestionControlVersion { get; set; } = 1;
    public Guid? CurrentAttemptId { get; set; }
    public long CurrentAttemptGeneration { get; set; } = 0;
    public string? DeclaredLanguage { get; set; }

    // Ownership
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    // Curated catalog provenance
    public Guid? SourceCatalogDocumentId { get; set; }
    public int? SourceCatalogRevision { get; set; }
    public string? SourceAttribution { get; set; }

    // Navigation properties
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();


    public void AdvanceControlVersion()
    {
        IngestionControlVersion++;
        MarkUpdated();
    }

    public void SetDeclaredLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language cannot be empty.", nameof(language));
        }

        var normalized = language.Trim().ToLowerInvariant();
        if (DeclaredLanguage != normalized)
        {
            DeclaredLanguage = normalized;
            AdvanceControlVersion();
        }
    }

    public void AssignAttempt(Guid attemptId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt ID cannot be empty.", nameof(attemptId));
        }

        CurrentAttemptId = attemptId;
        CurrentAttemptGeneration++;
        AdvanceControlVersion();
    }

    public void UpdateHeartbeatProgress(int percent, string? message = null)
    {
        ProgressPercentage = Math.Clamp(percent, 0, 100);
        StatusMessage = message;
        // Progress heartbeats do not advance IngestionControlVersion
        MarkUpdated();
    }
    public static DocumentBook CreateFromCatalogSnapshot(
        CatalogDocument catalogDoc,
        Guid targetUserId,
        string uniqueSlug)
    {
        if (catalogDoc.Visibility != CatalogVisibility.Published)
        {
            throw new InvalidOperationException("Cannot create a personal book snapshot from an unpublished catalog document.");
        }

        if (catalogDoc.ProcessingStatus != ProcessingStatus.Ready)
        {
            throw new InvalidOperationException("Cannot create a personal book snapshot from a catalog document that is not ready.");
        }

        var book = new DocumentBook
        {
            Id = Guid.NewGuid(),
            Title = catalogDoc.Title,
            Slug = uniqueSlug,
            SourceType = catalogDoc.SourceType,
            Category = catalogDoc.Category,
            TotalChunks = catalogDoc.TotalSlices,
            AuthorOrSourceUrl = catalogDoc.AuthorOrSourceUrl,
            IsPublished = true,
            IsFeatured = false,
            Status = ProcessingStatus.Ready,
            ProgressPercentage = 100,
            CreatedByUserId = targetUserId,
            SourceCatalogDocumentId = catalogDoc.Id,
            SourceCatalogRevision = catalogDoc.Revision,
            SourceAttribution = catalogDoc.AttributionText
        };

        foreach (var slice in catalogDoc.Slices.OrderBy(s => s.SliceOrder))
        {
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = book.Id,
                ChunkOrder = slice.SliceOrder,
                ChapterTitle = slice.ChapterTitle,
                OriginalTextMarkdown = slice.OriginalTextMarkdown,
                SummaryMarkdown = slice.SummaryMarkdown,
                KeyTakeaways = new List<string>(slice.KeyTakeaways),
                Language = slice.Language,
                Embedding = slice.Embedding != null ? new Pgvector.Vector(slice.Embedding.ToArray()) : null,
                EstimatedReadMinutes = slice.EstimatedReadMinutes,
                IsAiFormatted = slice.IsAiFormatted
            };

            book.Chunks.Add(chunk);
        }

        return book;
    }
}
