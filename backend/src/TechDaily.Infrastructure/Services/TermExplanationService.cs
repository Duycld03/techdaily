using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Services;

public class TermExplanationService : ITermExplanationService
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<TermExplanationService> _logger;

    public TermExplanationService(
        ITechDailyDbContext dbContext,
        IEmbeddingService embeddingService,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TermExplanationService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-3.1-flash-lite";
    }

    public async Task<Result<TermExplanationResult>> ExplainTermAsync(
        string term,
        string category,
        string context,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var normalizedTerm = term.Trim().ToLowerInvariant();
        var safeCategory = category.Length > 200 ? category[..200] : category;

        // 1. Tier 1: Check Exact DB Cache via B-Tree Index (<5ms)
        var cached = await _dbContext.TermExplanationCaches
            .FirstOrDefaultAsync(t => t.Term == normalizedTerm && t.Locale == locale, cancellationToken);

        if (cached != null)
        {
            cached.IncrementHit();
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Term '{Term}' returned from exact cache (hits: {Hits})", term, cached.HitCount);
            return new TermExplanationResult(cached.ExplanationText, true);
        }

        // 2. Tier 2: Check Semantic Vector Cache via pgvector HNSW Index (<150ms)
        Pgvector.Vector? termVector = null;
        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync($"[{safeCategory}] {normalizedTerm}", cancellationToken);
        if (embeddingResult.IsSuccess)
        {
            try
            {
                termVector = embeddingResult.Value;
                var semanticMatch = await _dbContext.TermExplanationCaches
                    .Where(t => t.Locale == locale && t.Embedding != null)
                    .OrderBy(t => t.Embedding!.CosineDistance(termVector))
                    .Select(t => new
                    {
                        Entity = t,
                        Distance = t.Embedding!.CosineDistance(termVector)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                // Cosine distance <= 0.08 represents >= 92% semantic similarity
                if (semanticMatch != null && semanticMatch.Distance <= 0.08)
                {
                    semanticMatch.Entity.IncrementHit();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Term '{Term}' returned from semantic cache matching '{MatchedTerm}' (distance: {Distance:F4}, hits: {Hits})",
                        term, semanticMatch.Entity.Term, semanticMatch.Distance, semanticMatch.Entity.HitCount);
                    return new TermExplanationResult(semanticMatch.Entity.ExplanationText, true);
                }
            }
            catch
            {
                // Fallback for providers that don't support pgvector (e.g. SQLite unit test provider)
            }
        }

        // 3. Tier 3: Full Cache Miss - Generate via Gemini Flash
        string explanation = string.Empty;
        bool isLlmGenerated = false;

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

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-goog-api-key", _apiKey);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(responseJson);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var parts = candidates[0].GetProperty("content").GetProperty("parts");
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textProp))
                            {
                                var t = textProp.GetString();
                                if (!string.IsNullOrWhiteSpace(t))
                                {
                                    explanation = t.Trim();
                                    isLlmGenerated = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Gemini API call returned status {Status}: {Error}", response.StatusCode, errBody);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Gemini API error during term explanation. Using fallback.");
            }
        }

        if (!isLlmGenerated || string.IsNullOrWhiteSpace(explanation))
        {
            var fallback = GetFallbackExplanation(term, safeCategory, locale);
            _logger.LogWarning("Gemini API call was unavailable or failed for term '{Term}'. Returning transient fallback without caching.", term);
            return new TermExplanationResult(fallback, false);
        }

        // 4. Save to DB Cache with Vector Embedding (only for LLM-generated explanations)
        if (termVector == null)
        {
            var embRes = await _embeddingService.GenerateEmbeddingAsync($"[{safeCategory}] {normalizedTerm}", cancellationToken);
            if (embRes.IsSuccess)
            {
                termVector = embRes.Value;
            }
        }

        var newCache = new TermExplanationCache
        {
            Term = normalizedTerm,
            Category = safeCategory,
            Locale = locale,
            ExplanationText = explanation,
            Embedding = termVector,
            HitCount = 1
        };

        await _dbContext.TermExplanationCaches.AddAsync(newCache, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TermExplanationResult(explanation, false);
    }

    private static string GetFallbackExplanation(string term, string category, string locale)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        return isVi
            ? $"Thuật ngữ '{term}' trong {category}: Khái niệm kỹ thuật quan trọng mô tả cơ chế hoạt động nội tại và hành vi tài nguyên của hệ thống."
            : $"The term '{term}' in {category} represents a core runtime or architectural mechanism governing system performance and data flow.";
    }
}
