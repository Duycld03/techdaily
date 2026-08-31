using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Services;

public class TermExplanationService : ITermExplanationService
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<TermExplanationService> _logger;

    public TermExplanationService(
        ITechDailyDbContext dbContext,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TermExplanationService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
    }

    public async Task<Result<string>> ExplainTermAsync(
        string term,
        string category,
        string context,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var normalizedTerm = term.Trim().ToLowerInvariant();

        // 1. Check DB Cache
        var cached = await _dbContext.TermExplanationCaches
            .FirstOrDefaultAsync(t => t.Term == normalizedTerm && t.Locale == locale, cancellationToken);

        if (cached != null)
        {
            cached.IncrementHit();
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Term '{Term}' returned from cache (hits: {Hits})", term, cached.HitCount);
            return cached.ExplanationText;
        }

        // 2. Generate via Gemini Flash
        string explanation;
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                var prompt = $@"
Explain the technical term '{term}' in the context of '{category}'.
Surrounding text context: ""{context}""
Target language: {locale}
Provide a concise, crystal-clear 2-sentence explanation suitable for a Senior Engineer popup tooltip.
";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(responseJson);
                    explanation = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString()?.Trim() ?? GetFallbackExplanation(term, category, locale);
                }
                else
                {
                    explanation = GetFallbackExplanation(term, category, locale);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API error during term explanation. Using fallback.");
                explanation = GetFallbackExplanation(term, category, locale);
            }
        }
        else
        {
            explanation = GetFallbackExplanation(term, category, locale);
        }

        // 3. Save to DB Cache
        var newCache = new TermExplanationCache
        {
            Term = normalizedTerm,
            Category = category,
            Locale = locale,
            ExplanationText = explanation,
            HitCount = 1
        };

        await _dbContext.TermExplanationCaches.AddAsync(newCache, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return explanation;
    }

    private static string GetFallbackExplanation(string term, string category, string locale)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        return isVi
            ? $"Thuật ngữ '{term}' trong {category}: Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại và hành vi tài nguyên của hệ thống."
            : $"The term '{term}' in {category} represents a core runtime or architectural mechanism governing system performance and data flow.";
    }
}
