using TechDaily.Domain.Common;
using TechDaily.Domain.Enums;

namespace TechDaily.Domain.Entities;

public class TechInsight : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Category Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public string SummaryMarkdown { get; set; } = string.Empty;
    public string ProblemSnippet { get; set; } = string.Empty;
    public string SolutionSnippet { get; set; } = string.Empty;
    public string UnderTheHoodMarkdown { get; set; } = string.Empty;
    public string BenchmarkStats { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public int LikesCount { get; set; } = 0;
    public int BookmarksCount { get; set; } = 0;
    public bool IsPublished { get; set; } = true;
}
