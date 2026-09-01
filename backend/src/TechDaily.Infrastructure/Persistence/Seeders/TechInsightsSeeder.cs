using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Infrastructure.Persistence.Seeders;

public static class TechInsightsSeeder
{
    private class InsightSeedItem
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Category { get; set; }
        public List<string> Tags { get; set; } = new();
        public string SummaryMarkdown { get; set; } = string.Empty;
        public string ProblemSnippet { get; set; } = string.Empty;
        public string SolutionSnippet { get; set; } = string.Empty;
        public string UnderTheHoodMarkdown { get; set; } = string.Empty;
        public string BenchmarkStats { get; set; } = string.Empty;
        public string? SourceUrl { get; set; }
    }

    public static async Task SeedAsync(TechDailyDbContext context)
    {
        var seedItems = LoadSeedItems();
        if (seedItems == null || seedItems.Count == 0)
        {
            return;
        }

        foreach (var item in seedItems)
        {
            var existing = await context.TechInsights
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Slug == item.Slug);

            if (existing == null)
            {
                var insight = new TechInsight
                {
                    Id = Guid.NewGuid(),
                    Slug = item.Slug,
                    Title = item.Title,
                    Category = (Category)item.Category,
                    Tags = item.Tags ?? new List<string>(),
                    SummaryMarkdown = item.SummaryMarkdown,
                    ProblemSnippet = item.ProblemSnippet,
                    SolutionSnippet = item.SolutionSnippet,
                    UnderTheHoodMarkdown = item.UnderTheHoodMarkdown,
                    BenchmarkStats = item.BenchmarkStats,
                    SourceUrl = item.SourceUrl,
                    IsPublished = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await context.TechInsights.AddAsync(insight);
            }
            else
            {
                existing.Title = item.Title;
                existing.Category = (Category)item.Category;
                existing.Tags = item.Tags ?? new List<string>();
                existing.SummaryMarkdown = item.SummaryMarkdown;
                existing.ProblemSnippet = item.ProblemSnippet;
                existing.SolutionSnippet = item.SolutionSnippet;
                existing.UnderTheHoodMarkdown = item.UnderTheHoodMarkdown;
                existing.BenchmarkStats = item.BenchmarkStats;
                existing.SourceUrl = item.SourceUrl;
                existing.IsPublished = true;
                existing.IsDeleted = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    private static List<InsightSeedItem> LoadSeedItems()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "TechDaily.Infrastructure.Data.tech-insights.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<InsightSeedItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<InsightSeedItem>();
        }

        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "tech-insights.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<InsightSeedItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<InsightSeedItem>();
        }

        return new List<InsightSeedItem>();
    }
}
