using DeepPace.Domain.Common;
using DeepPace.Domain.Enums;

namespace DeepPace.Domain.Entities;

public class CatalogDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public string Language { get; set; } = "en";
    public SourceType SourceType { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public string AttributionText { get; set; } = string.Empty;

    // Permission and rights metadata
    public string PermissionBasis { get; set; } = string.Empty;
    public string PermissionEvidence { get; set; } = string.Empty;
    public bool ConfirmedDistributionRights { get; set; } = false;
    public bool ConfirmedAiProcessingRights { get; set; } = false;

    // Visibility and processing lifecycle
    public CatalogVisibility Visibility { get; set; } = CatalogVisibility.Draft;
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;
    public int TotalSlices { get; set; } = 0;
    public int EstimatedReadMinutes { get; set; } = 0;

    // Revision and concurrency
    public int Revision { get; set; } = 1;
    public int ConcurrencyVersion { get; set; } = 1;

    // Processing lease and recovery
    public long ProcessingGeneration { get; set; } = 0;
    public DateTimeOffset? ProcessingLeaseExpiresAt { get; set; }
    public string? ProcessingStatusMessage { get; set; }
    public string? ProcessingErrorMessage { get; set; }

    // Ownership and audit fields
    public Guid? CreatedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public int? ReviewedRevision { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? HiddenByUserId { get; set; }
    public DateTimeOffset? HiddenAt { get; set; }

    // Navigation properties
    public ICollection<CatalogSlice> Slices { get; set; } = new List<CatalogSlice>();

    public void Review(Guid reviewerUserId)
    {
        ReviewedByUserId = reviewerUserId;
        ReviewedRevision = Revision;
        ReviewedAt = DateTimeOffset.UtcNow;
        MarkUpdated();
    }

    public (bool IsEligible, List<string> UnmetRequirements) CheckPublicationEligibility()
    {
        var unmet = new List<string>();

        if (ProcessingStatus != ProcessingStatus.Ready)
        {
            unmet.Add("Document processing must be complete and in Ready status.");
        }

        if (Slices.Count == 0)
        {
            unmet.Add("Document must contain at least one extracted slice.");
        }
        else
        {
            var orderedSlices = Slices.OrderBy(s => s.SliceOrder).ToList();
            if (orderedSlices.Any(s => string.IsNullOrWhiteSpace(s.OriginalTextMarkdown)))
            {
                unmet.Add("All extracted slices must contain readable source content.");
            }

            int initialFormattingTarget = Math.Min(3, orderedSlices.Count);
            var initialUnformatted = orderedSlices.Take(initialFormattingTarget).Where(s => !s.IsAiFormatted).ToList();
            if (initialUnformatted.Any())
            {
                unmet.Add($"First {initialFormattingTarget} initial slices require AI initial formatting before publication.");
            }
        }

        if (string.IsNullOrWhiteSpace(AttributionText))
        {
            unmet.Add("Source attribution text is required.");
        }

        if (string.IsNullOrWhiteSpace(PermissionBasis))
        {
            unmet.Add("Permission basis is required.");
        }

        if (string.IsNullOrWhiteSpace(PermissionEvidence))
        {
            unmet.Add("Permission evidence is required.");
        }

        if (!ConfirmedDistributionRights)
        {
            unmet.Add("Confirmed distribution rights are required.");
        }

        if (!ConfirmedAiProcessingRights)
        {
            unmet.Add("Confirmed AI processing rights are required.");
        }

        if (ReviewedByUserId == null)
        {
            unmet.Add("Explicit administrator review is required before publication.");
        }
        else if (ReviewedRevision != Revision)
        {
            unmet.Add("Administrator review is stale because content or metadata changed since last review.");
        }

        return (unmet.Count == 0, unmet);
    }

    public void Publish(Guid publisherUserId)
    {
        var (isEligible, unmet) = CheckPublicationEligibility();
        if (!isEligible)
        {
            throw new InvalidOperationException($"Cannot publish catalog document: {string.Join(" ", unmet)}");
        }

        Visibility = CatalogVisibility.Published;
        PublishedByUserId = publisherUserId;
        PublishedAt = DateTimeOffset.UtcNow;
        ConcurrencyVersion++;
        MarkUpdated();
    }

    public void Hide(Guid hiddenByUserId)
    {
        if (Visibility != CatalogVisibility.Published)
        {
            return;
        }

        Visibility = CatalogVisibility.Hidden;
        HiddenByUserId = hiddenByUserId;
        HiddenAt = DateTimeOffset.UtcNow;
        ConcurrencyVersion++;
        MarkUpdated();
    }

    public void UpdateMetadata(
        string title,
        string description,
        Category category,
        string language,
        string? authorOrSourceUrl,
        string attributionText,
        string permissionBasis,
        string permissionEvidence,
        bool confirmedDistributionRights,
        bool confirmedAiProcessingRights)
    {
        if (Visibility == CatalogVisibility.Published)
        {
            throw new InvalidOperationException("Published documents are immutable. The document must be hidden first before editing metadata.");
        }
        Title = title;
        Description = description;
        Category = category;
        Language = language;
        AuthorOrSourceUrl = authorOrSourceUrl;
        AttributionText = attributionText;
        PermissionBasis = permissionBasis;
        PermissionEvidence = permissionEvidence;
        ConfirmedDistributionRights = confirmedDistributionRights;
        ConfirmedAiProcessingRights = confirmedAiProcessingRights;

        Revision++;
        ConcurrencyVersion++;
        // Review is now stale because Revision was incremented
        MarkUpdated();
    }

    public void InvalidateReviewDueToContentChange()
    {
        Revision++;
        ConcurrencyVersion++;
        MarkUpdated();
    }
}
