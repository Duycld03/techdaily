namespace TechDaily.Application.Features.Notes.DTOs;

public class HighlightDto
{
    public Guid Id { get; set; }
    public Guid DocumentChunkId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string SelectedText { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
