using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.DTOs;

public class BookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public Category Category { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public int TotalChunks { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public int ProgressPercentage { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class BookIngestionStatusDto
{
    public Guid Id { get; set; }
    public ProcessingStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalChunks { get; set; }
}

public class BookDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public Category Category { get; set; }
    public string? AuthorOrSourceUrl { get; set; }
    public int TotalChunks { get; set; }
    public bool IsFeatured { get; set; }
    public ProcessingStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? StatusMessage { get; set; }
    public List<ChunkSummaryDto> Chunks { get; set; } = new();
}

public class ChunkSummaryDto
{
    public Guid Id { get; set; }
    public int ChunkOrder { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public string OriginalTextMarkdown { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public int EstimatedReadMinutes { get; set; }
    public bool IsAiFormatted { get; set; }
}
