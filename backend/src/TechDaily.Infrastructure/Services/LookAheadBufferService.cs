using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Infrastructure.Services;

public class LookAheadBufferService : ILookAheadBufferService
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LookAheadBufferService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public LookAheadBufferService(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<LookAheadBufferService> logger)
    {
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";
    }

    public async Task PreGenerateInitialBufferAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();

        var firstThreeChunks = await dbContext.DocumentChunks
            .Where(c => c.DocumentBookId == bookId && c.ChunkOrder >= 1 && c.ChunkOrder <= 3)
            .OrderBy(c => c.ChunkOrder)
            .ToListAsync(cancellationToken);

        foreach (var chunk in firstThreeChunks)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var existing = await dbContext.InterviewQuestions
                .AnyAsync(q => q.DocumentChunkId == chunk.Id, cancellationToken);

            if (!existing)
            {
                _logger.LogInformation("Pre-generating initial buffer challenge for Book {BookId}, Chunk {ChunkOrder}", bookId, chunk.ChunkOrder);
                await GenerateChallengeForChunkAsync(chunk.Id, cancellationToken);
            }
        }
    }

    public async Task EnsureBufferDepthAsync(Guid bookId, int currentChunkOrder, CancellationToken cancellationToken = default)
    {
        var targetOrders = new[] { currentChunkOrder + 1, currentChunkOrder + 2, currentChunkOrder + 3 };

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();

        var chunksToBuffer = await dbContext.DocumentChunks
            .Where(c => c.DocumentBookId == bookId && targetOrders.Contains(c.ChunkOrder))
            .OrderBy(c => c.ChunkOrder)
            .ToListAsync(cancellationToken);

        foreach (var chunk in chunksToBuffer)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var hasQuestion = await dbContext.InterviewQuestions
                .AnyAsync(q => q.DocumentChunkId == chunk.Id, cancellationToken);

            if (!hasQuestion)
            {
                _logger.LogInformation("Buffer depth replenishment: Generating challenge for Book {BookId}, Chunk {ChunkOrder}", bookId, chunk.ChunkOrder);
                await GenerateChallengeForChunkAsync(chunk.Id, cancellationToken);
            }
        }
    }

    public async Task<InterviewQuestion?> PromoteChunkPriorityAsync(Guid bookId, int chunkOrder, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();

        var chunk = await dbContext.DocumentChunks
            .Include(c => c.InterviewQuestions)
            .FirstOrDefaultAsync(c => c.DocumentBookId == bookId && c.ChunkOrder == chunkOrder, cancellationToken);

        if (chunk == null)
        {
            return null;
        }

        var existingQuestion = chunk.InterviewQuestions.FirstOrDefault();
        if (existingQuestion != null)
        {
            return existingQuestion;
        }

        _logger.LogInformation("Priority Promotion Jump: Generating challenge immediately for Book {BookId}, Chunk {ChunkOrder}", bookId, chunkOrder);
        return await GenerateChallengeForChunkAsync(chunk.Id, cancellationToken);
    }

    public async Task<InterviewQuestion?> GenerateChallengeForChunkAsync(Guid chunkId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITechDailyDbContext>();

        var chunk = await dbContext.DocumentChunks
            .Include(c => c.DocumentBook)
            .Include(c => c.InterviewQuestions)
            .FirstOrDefaultAsync(c => c.Id == chunkId, cancellationToken);

        if (chunk == null)
        {
            return null;
        }

        var existing = chunk.InterviewQuestions.FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        InterviewQuestion question;
        try
        {
            question = await CallGeminiForScenarioAsync(chunk, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini scenario generation failed or timed out for Chunk {ChunkId}. Applying fallback question.", chunkId);
            question = CreateFallbackQuestion(chunk);
        }

        await dbContext.InterviewQuestions.AddAsync(question, cancellationToken);

        chunk.MicroQuiz = new MicroQuizVo
        {
            Question = question.QuestionText,
            Options = question.Options,
            AnswerIndex = question.CorrectOptionIndex,
            Explanation = question.ExplanationMarkdown
        };

        await dbContext.SaveChangesAsync(cancellationToken);
        return question;
    }

    private async Task<InterviewQuestion> CallGeminiForScenarioAsync(DocumentChunk chunk, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Falling back to local template challenge.");
            return CreateFallbackQuestion(chunk);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(6));

        var bookTitle = chunk.DocumentBook?.Title ?? "Technical Guide";
        var chapterTitle = string.IsNullOrWhiteSpace(chunk.ChapterTitle) ? "Core Concepts" : chunk.ChapterTitle;
        var excerpt = chunk.OriginalTextMarkdown.Length > 2500
            ? chunk.OriginalTextMarkdown[..2500]
            : chunk.OriginalTextMarkdown;

        var prompt = $@"
You are a Principal Software Architect interviewing a Senior/Staff Engineer.
Based on the following book chapter excerpt from '{bookTitle}' - '{chapterTitle}', create a Senior-level Architectural Trade-off Multiple-Choice Challenge.

The scenario MUST test real-world trade-offs (e.g. latency vs consistency, memory allocations vs throughput, locking vs contention, cache invalidation, network partitions).
Do NOT ask trivial trivia or memorization questions. Frame it as a realistic production design problem with 4 distinct architectural options.

Excerpt:
{excerpt}

Respond strictly in valid JSON adhering to this exact schema:
{{
  ""questionText"": ""Clear scenario question stating the production trade-off problem"",
  ""options"": [
    ""Option A (plausible trade-off)"",
    ""Option B (optimal senior choice)"",
    ""Option C (suboptimal trade-off)"",
    ""Option D (anti-pattern trade-off)""
  ],
  ""correctOptionIndex"": 1,
  ""explanationMarkdown"": ""Detailed Markdown explanation analyzing why the chosen option succeeds and the others fail or incur high architectural debt."",
  ""expectedKeyPoints"": [
    ""Key trade-off factor 1"",
    ""Key trade-off factor 2""
  ],
  ""modelAnswerMarkdown"": ""Executive summary of the architectural decision.""
}}";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 2048
            }
        };

        var jsonBody = JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cts.Token);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(responseString);

        var root = doc.RootElement;
        var candidates = root.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned empty candidates.");
        }

        var text = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        var cleanJson = GeminiAiService.ExtractJsonObject(text);
        if (string.IsNullOrWhiteSpace(cleanJson))
        {
            throw new InvalidOperationException("No valid JSON object extracted from Gemini response.");
        }

        var parsed = JsonSerializer.Deserialize<ScenarioJsonDto>(cleanJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (parsed == null || parsed.Options == null || parsed.Options.Count < 2 || string.IsNullOrWhiteSpace(parsed.QuestionText))
        {
            throw new InvalidOperationException("Gemini scenario JSON failed validation.");
        }

        var correctIndex = Math.Clamp(parsed.CorrectOptionIndex ?? 0, 0, parsed.Options.Count - 1);

        return new InterviewQuestion
        {
            DocumentChunkId = chunk.Id,
            TopicId = null,
            Difficulty = Difficulty.Senior,
            QuestionText = parsed.QuestionText.Trim(),
            Options = parsed.Options,
            CorrectOptionIndex = correctIndex,
            ExplanationMarkdown = parsed.ExplanationMarkdown ?? string.Empty,
            ExpectedKeyPoints = parsed.ExpectedKeyPoints ?? new List<string>(),
            ModelAnswerMarkdown = parsed.ModelAnswerMarkdown ?? parsed.ExplanationMarkdown ?? string.Empty
        };
    }

    private static InterviewQuestion CreateFallbackQuestion(DocumentChunk chunk)
    {
        var chapter = string.IsNullOrWhiteSpace(chunk.ChapterTitle) ? "Software Architecture" : chunk.ChapterTitle;
        return new InterviewQuestion
        {
            DocumentChunkId = chunk.Id,
            TopicId = null,
            Difficulty = Difficulty.Senior,
            QuestionText = $"In the context of '{chapter}', when designing for high availability and low latency under production workloads, which trade-off strategy is most appropriate?",
            Options = new List<string>
            {
                "Prioritize synchronous distributed transactions across all nodes to guarantee immediate consistency at all times.",
                "Adopt eventual consistency with localized optimistic locking and asynchronous replication to maximize read/write throughput.",
                "Disable index maintenance and write-ahead logging to minimize disk I/O bottlenecks during peak traffic.",
                "Enforce client-side polling with no server backpressure or rate-limiting headers."
            },
            CorrectOptionIndex = 1,
            ExplanationMarkdown = $"In high-throughput systems covered in **{chapter}**, eventual consistency combined with asynchronous replication and localized locking decouples write latency from network partitions (CAP theorem / PACELC), avoiding distributed lock contention.",
            ExpectedKeyPoints = new List<string>
            {
                "Trade-off between strong consistency and availability/throughput",
                "Impact of distributed locks on tail latencies",
                "Asynchronous replication and idempotency"
            },
            ModelAnswerMarkdown = $"For high-availability scenarios in **{chapter}**, trading strict synchronous consistency for bounded eventual consistency allows the system to sustain high throughput with sub-millisecond p99 latencies while handling intermittent network partitions gracefully."
        };
    }

    private class ScenarioJsonDto
    {
        public string? QuestionText { get; set; }
        public List<string>? Options { get; set; }
        public int? CorrectOptionIndex { get; set; }
        public string? ExplanationMarkdown { get; set; }
        public List<string>? ExpectedKeyPoints { get; set; }
        public string? ModelAnswerMarkdown { get; set; }
    }
}
