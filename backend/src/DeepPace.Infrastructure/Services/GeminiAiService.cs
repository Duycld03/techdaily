using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;

namespace DeepPace.Infrastructure.Services;

public class GeminiAiService : ITechInsightGenerator, IQuizGeneratorService, IAiMarkdownFormatter, IGeminiAiService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _model;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"]
               ?? configuration["GEMINI_API_KEY"]
               ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _model = configuration["Gemini:Model"]
              ?? configuration["GEMINI_MODEL"]
              ?? Environment.GetEnvironmentVariable("GEMINI_MODEL");
    }

    public async Task<Result<TechInsight>> GenerateInsightAsync(
        Guid categoryId,
        string? preferredTopic,
        List<string>? existingTitlesToAvoid = null,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var topicPrompt = string.IsNullOrWhiteSpace(preferredTopic)
            ? "a focused practical concept explained with accurate, concrete examples"
            : preferredTopic;

        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_model))
        {
            _logger.LogError("Gemini API key or model is not configured.");
            return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed: API key or model is not configured."));
        }

        try
        {
            var cleanTopic = topicPrompt.Trim();

            var antiDuplicationClause = existingTitlesToAvoid != null && existingTitlesToAvoid.Any()
                ? $@"
CRITICAL ANTI-DUPLICATION RULE:
Do NOT generate insights covering the exact same title, code pattern, or redundant explanation as any of these existing insights:
- {string.Join("\n- ", existingTitlesToAvoid.Take(25))}
If the user's topic relates to an existing insight, you MUST explore a fresh sub-system, complementary edge-case, architectural pitfall, or advanced runtime detail."
                : string.Empty;

            var systemInstruction = $@"
You are an expert educator. Generate a precise, authoritative insight strictly about '{cleanTopic}'.
Let the requested topic determine the subject matter, examples, terminology, and practical trade-offs.
Do not substitute a software engineering, productivity, or other curriculum for the requested topic.
Use code only if relevant and in the requested language.
{antiDuplicationClause}
Language of explanations: {(isVi ? "Vietnamese" : "English")}.
Respond strictly in valid JSON, without markdown wrapping, with these fields:
title (string), tags (string array), summaryMarkdown (string), problemSnippet (string),
solutionSnippet (string), underTheHoodMarkdown (string), benchmarkStats (string), sourceUrl (string or null).
Do not invent measurements or sources.";
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            var promptText = $"User requested topic: '{topicPrompt}'. Generate a unique, authoritative, non-repetitive insight.";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var response = await PostGeminiWithRetryAsync(requestUri, jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed."));
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseInsightResponse(responseBody, categoryId, topicPrompt, isVi);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Exception while generating insight with Gemini API.");
            return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed."));
        }
    }

    private Result<TechInsight> ParseInsightResponse(
        string responseBody,
        Guid categoryId,
        string topic,
        bool isVi)
    {
        string? rawText = null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini API returned 0 candidates for insight topic '{Topic}'.", topic);
                return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed."));
            }

            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");

            // Look through parts to find the one containing text (skipping thought/reasoning objects)
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    var partText = textProp.GetString();
                    if (!string.IsNullOrWhiteSpace(partText))
                    {
                        rawText = partText;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("Gemini API returned empty text part for insight topic '{Topic}'.", topic);
                return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed."));
            }

            var cleanJson = rawText.Trim();
            if (cleanJson.StartsWith("```"))
            {
                var firstLineBreak = cleanJson.IndexOf('\n');
                if (firstLineBreak != -1)
                {
                    cleanJson = cleanJson.Substring(firstLineBreak + 1);
                }
                var lastBacktick = cleanJson.LastIndexOf("```");
                if (lastBacktick != -1)
                {
                    cleanJson = cleanJson.Substring(0, lastBacktick);
                }
                cleanJson = cleanJson.Trim();
            }

            cleanJson = ExtractJsonObject(cleanJson);

            using var insightDoc = JsonDocument.Parse(cleanJson);
            var root = insightDoc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Senior Architecture Insight" : "Senior Architecture Insight";

            var tags = new List<string>();
            if (root.TryGetProperty("tags", out var tagsElem) && tagsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tagsElem.EnumerateArray())
                {
                    var val = tag.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) tags.Add(val);
                }
            }

            var summaryMarkdown = root.TryGetProperty("summaryMarkdown", out var s) ? s.GetString() ?? "" : "";
            var problemSnippet = root.TryGetProperty("problemSnippet", out var p) ? p.GetString() ?? "" : "";
            var solutionSnippet = root.TryGetProperty("solutionSnippet", out var sol) ? sol.GetString() ?? "" : "";
            var underTheHood = root.TryGetProperty("underTheHoodMarkdown", out var u) ? u.GetString() ?? "" : "";
            var benchmarkStats = root.TryGetProperty("benchmarkStats", out var b) ? b.GetString() ?? "⚡ Optimized" : "⚡ Optimized";
            var sourceUrl = root.TryGetProperty("sourceUrl", out var src) ? src.GetString() : null;

            var slug = GenerateSlug(title);

            return new TechInsight
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                Title = title,
                CategoryId = categoryId,
                Tags = tags,
                SummaryMarkdown = summaryMarkdown,
                ProblemSnippet = problemSnippet,
                SolutionSnippet = solutionSnippet,
                UnderTheHoodMarkdown = underTheHood,
                BenchmarkStats = benchmarkStats,
                SourceUrl = sourceUrl,
                IsPublished = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while parsing Gemini insight response for topic '{Topic}'. Raw snippet: {Snippet}", topic, rawText ?? responseBody.Substring(0, Math.Min(responseBody.Length, 300)));
            return Result<TechInsight>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI insight generation failed."));
        }
    }

    private async Task<HttpResponseMessage> PostGeminiWithRetryAsync(
        string requestUri,
        string jsonPayload,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Add("x-goog-api-key", _apiKey);
            }

            response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            // Retry if transient 503 (ServiceUnavailable) or 429 (TooManyRequests)
            if ((response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429) && attempt < 3)
            {
                var delayMs = attempt * 1500;
                _logger.LogWarning("Gemini API transient error ({StatusCode}) on attempt {Attempt}/3. Retrying after {Delay}ms...", response.StatusCode, attempt, delayMs);
                response.Dispose();
                await Task.Delay(delayMs, cancellationToken);
                continue;
            }

            break;
        }

        return response!;
    }

    public static string ExtractJsonArray(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var startIdx = text.IndexOf('[');
        if (startIdx < 0) return text;

        var depth = 0;
        var inString = false;
        var isEscaped = false;

        for (var i = startIdx; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (c == '\\')
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (c == '"')
            {
                inString = true;
            }
            else if (c == '[')
            {
                depth++;
            }
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIdx, i - startIdx + 1);
                }
            }
        }

        var lastBracket = text.LastIndexOf(']');
        return (lastBracket > startIdx) ? text.Substring(startIdx, lastBracket - startIdx + 1) : text;
    }

    public static string ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var startIdx = text.IndexOf('{');
        if (startIdx < 0) return text;

        var depth = 0;
        var inString = false;
        var isEscaped = false;

        for (var i = startIdx; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (c == '\\')
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (c == '"')
            {
                inString = true;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIdx, i - startIdx + 1);
                }
            }
        }

        var lastBrace = text.LastIndexOf('}');
        return (lastBrace > startIdx) ? text.Substring(startIdx, lastBrace - startIdx + 1) : text;
    }

    private static string GenerateSlug(string title)
    {
        var clean = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace(".", "")
            .Replace(",", "");
        return $"{clean}-{Guid.NewGuid().ToString("N")[..6]}";
    }


    public async Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(
        string topic,
        Guid categoryId,
        QuizLevel level,
        int count,
        List<string> existingTitlesToAvoid,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        var levelName = level switch
        {
            QuizLevel.Foundation => "Foundation (Core Principles, Definitions, Primary Mechanisms)",
            QuizLevel.Applied => "Applied (Practical Real-World Application, Common Traps & Antipatterns)",
            QuizLevel.Advanced => "Advanced (Complex Systems, Cross-Discipline Integration, Nuanced Trade-offs)",
            QuizLevel.Mastery => "Mastery (High-Leverage Architecture, Under-the-Hood Dynamics, Systemic Optimization)",
            _ => "Mastery"
        };

        const string roleTitle = "Expert educator";
        const string questionFocus = "the requested topic's principles, practical applications, and nuanced trade-offs";

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_model))
        {
            _logger.LogError("Gemini API key or model is not configured.");
            return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed: API key or model is not configured."));
        }

        try
        {
            var systemInstruction = $@"
You are a {roleTitle}.
Your task is to generate exactly {count} realistic, challenging, high-quality multiple-choice Decision Drill questions for the mastery level '{levelName}'.
Rules:
1. Each question MUST test {questionFocus}.
2. Each question MUST have EXACTLY 4 distinct option strings in the `options` array (no fewer, no more).
3. `correctOptionIndex` MUST be an integer from 0 to 3 pointing to the single optimal/correct answer.
4. `explanationMarkdown` MUST be detailed markdown (using bold, code backticks, bullet points) explaining:
   - Why the correct choice is optimal.
   - Why the other 3 choices are flawed, suboptimal, or misconceptions.
5. If the topic specifies a language (e.g. C#, TypeScript, Python, Go, Rust, PostgreSQL), ensure code syntax in questions/options matches that language.
6. Language of questions and explanations: {(isVi ? "Vietnamese (technical terms and code in English with clear Vietnamese explanations)" : "English")}.
{(existingTitlesToAvoid.Any() ? $"7. Do NOT generate any questions similar to these existing ones:\n- {string.Join("\n- ", existingTitlesToAvoid.Take(20))}" : "")}

Respond strictly in valid JSON adhering to this schema:
[
  {{
    ""questionText"": ""Clear question text or code scenario"",
    ""options"": [
      ""Option A text"",
      ""Option B text"",
      ""Option C text"",
      ""Option D text""
    ],
    ""correctOptionIndex"": 0,
    ""explanationMarkdown"": ""### Deep Dive Explanation\n- **Why optimal:** ...\n- **Distractor Analysis:** ..."",
    ""tags"": [""tag1"", ""tag2""]
  }}
]
No markdown backticks around JSON.";

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            var promptText = $"Generate {count} multiple-choice Decision Drill questions on topic '{topic}' for mastery level '{levelName}'.";
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var response = await PostGeminiWithRetryAsync(requestUri, jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseQuizResponse(responseBody, topic, categoryId, level, count, isVi);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Exception while generating quiz with Gemini API.");
            return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
        }
    }

    private Result<List<QuizQuestion>> ParseQuizResponse(
        string responseBody,
        string topic,
        Guid categoryId,
        QuizLevel level,
        int count,
        bool isVi)
    {
        string? rawText = null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini API returned 0 candidates for topic '{Topic}'.", topic);
                return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
            }

            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");

            // Look through parts to find the one containing text (skipping any thought objects from reasoning models)
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    var partText = textProp.GetString();
                    if (!string.IsNullOrWhiteSpace(partText))
                    {
                        rawText = partText;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("Gemini API returned empty text part for topic '{Topic}'.", topic);
                return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
            }

            var cleanJson = rawText.Trim();

            // Strip markdown code fences if present
            if (cleanJson.StartsWith("```"))
            {
                var firstLineBreak = cleanJson.IndexOf('\n');
                if (firstLineBreak != -1)
                {
                    cleanJson = cleanJson.Substring(firstLineBreak + 1);
                }
                var lastBacktick = cleanJson.LastIndexOf("```");
                if (lastBacktick != -1)
                {
                    cleanJson = cleanJson.Substring(0, lastBacktick);
                }
                cleanJson = cleanJson.Trim();
            }

            // Extract the outermost JSON array `[...]` using balanced bracket matching to discard any leading/trailing commentary, backticks, or extra tokens
            cleanJson = ExtractJsonArray(cleanJson);

            using var quizDoc = JsonDocument.Parse(cleanJson);
            var root = quizDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Gemini response is not a JSON array for topic '{Topic}'. Cleaned text: {CleanJson}", topic, cleanJson);
                return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
            }

            var list = new List<QuizQuestion>();
            foreach (var item in root.EnumerateArray())
            {
                var qText = item.TryGetProperty("questionText", out var qt) ? qt.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(qText)) continue;

                var options = new List<string>();
                if (item.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var opt in opts.EnumerateArray())
                    {
                        var str = opt.GetString();
                        if (!string.IsNullOrWhiteSpace(str)) options.Add(str);
                    }
                }

                // Enforce exactly 4 options
                if (options.Count < 4)
                {
                    while (options.Count < 4) options.Add($"Option {options.Count + 1}");
                }
                else if (options.Count > 4)
                {
                    options = options.Take(4).ToList();
                }

                var correctIdx = 0;
                if (item.TryGetProperty("correctOptionIndex", out var ci) && ci.TryGetInt32(out var parsedIdx))
                {
                    correctIdx = Math.Clamp(parsedIdx, 0, 3);
                }

                var explanation = item.TryGetProperty("explanationMarkdown", out var exp) ? exp.GetString() ?? "" : "";
                var tags = new List<string>();
                if (item.TryGetProperty("tags", out var tg) && tg.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tg.EnumerateArray())
                    {
                        var s = t.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) tags.Add(s);
                    }
                }

                list.Add(new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    Topic = topic,
                    CategoryId = categoryId,
                    Level = level,
                    QuestionText = qText,
                    Options = options,
                    CorrectOptionIndex = correctIdx,
                    ExplanationMarkdown = explanation,
                    Tags = tags,
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                });
            }

            if (list.Count == 0)
            {
                _logger.LogWarning("No valid questions parsed from Gemini response for topic '{Topic}'.", topic);
                return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while parsing Gemini quiz response for topic '{Topic}'. Raw response snippet: {Snippet}", topic, rawText ?? responseBody.Substring(0, Math.Min(responseBody.Length, 300)));
            return Result<List<QuizQuestion>>.Failure(Error.Custom("AiService.Unavailable", "Gemini AI quiz generation failed."));
        }
    }


    public async Task<Result<AiFormattedSliceResult>> FormatSliceAsync(
        string rawText,
        string chapterTitle,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new AiFormattedSliceResult(
                FormattedMarkdown: $"# {chapterTitle}\n\nNo content available for this section.",
                SummaryMarkdown: "Empty section.",
                KeyTakeaways: new List<string> { "Empty chapter section" },
                EstimatedReadMinutes: 1);
        }

        // Check if Gemini is configured
        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_model))
        {
            _logger.LogWarning("Gemini API key or model is not configured for '{Title}'.", chapterTitle);
            return new Error("Gemini.MissingConfiguration", "Gemini API key or model is not configured.");
        }

        try
        {
            var inputSample = rawText;
            var systemInstruction = $@"
You are a faithful Markdown restoration editor for documents on any subject.
Restore the supplied source as Markdown, headed '# {chapterTitle}'.
100% Verbatim Text Retention & Structural Restoration: retain every original word, story,
example, quotation, and code snippet. ZERO SUMMARIZATION of the source body; do not shorten,
paraphrase, omit, translate, or substitute a different curriculum.
Repair extraction line breaks and paragraph spacing. Detect Run-in Headings and promote them
to appropriate Markdown headings. Eliminate Duplicated Heading Echo from extraction artifacts.
Preserve Spoken Dialogue & Quotes. Use syntax-tagged code fences only for actual source code.
Let source content alone determine terminology and scenarios, never an assumed subject area.
Preserve the author's original language ({language}); generated additions must use that language.
Keep auxiliary notes, summaries and exactly three source-grounded takeaways separate from the restored reading body.
Return valid JSON without markdown wrapping:
{{
  ""formattedMarkdown"": ""The complete restored source Markdown"",
  ""summaryMarkdown"": ""A separate concise overview, not a replacement for the source"",
  ""keyTakeaways"": [""Three source-grounded takeaways""],
  ""estimatedReadMinutes"": 5,
  ""scenarioDrill"": {{
    ""questionText"": ""A practical question grounded in the source subject"",
    ""options"": [""A"", ""B"", ""C"", ""D""],
    ""correctOptionIndex"": 0,
    ""explanationMarkdown"": ""Source-grounded explanation"",
    ""expectedKeyPoints"": [""Relevant criteria""]
  }}
}}";
            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = $"Chapter Title: '{chapterTitle}'\n\nRaw Text to Format:\n{inputSample}" }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var response = await PostGeminiWithRetryAsync(requestUri, jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API error ({StatusCode}) during slice formatting: {Error}.", response.StatusCode, errorBody);
                return new Error("Gemini.ApiError", $"Gemini API returned {response.StatusCode}: {errorBody}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseSliceResponse(responseBody, rawText, chapterTitle);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Exception during AI slice formatting for '{Title}'.", chapterTitle);
            return new Error("Gemini.Exception", ex.Message);
        }
    }

    private Result<AiFormattedSliceResult> ParseSliceResponse(string responseBody, string rawText, string chapterTitle)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                return new Error("Gemini.NoCandidates", "Gemini returned no candidates.");
            }

            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            string? rawJson = null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    rawJson = textProp.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new Error("Gemini.EmptyResponse", "Gemini returned empty text.");
            }

            var cleanJson = ExtractJsonObject(rawJson.Trim());
            using var sliceDoc = JsonDocument.Parse(cleanJson);
            var root = sliceDoc.RootElement;

            var rawFormatted = root.TryGetProperty("formattedMarkdown", out var fm) ? fm.GetString() : null;
            var formattedMarkdown = NormalizeEscapedNewlines(rawFormatted);
            var summaryMarkdown = NormalizeEscapedNewlines(root.TryGetProperty("summaryMarkdown", out var sm) ? sm.GetString() : null);
            var takeaways = new List<string>();
            if (root.TryGetProperty("keyTakeaways", out var kt) && kt.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in kt.EnumerateArray())
                {
                    var item = el.GetString();
                    if (!string.IsNullOrWhiteSpace(item)) takeaways.Add(NormalizeEscapedNewlines(item.Trim()));
                }
            }

            int estimatedReadMinutes = root.TryGetProperty("estimatedReadMinutes", out var em) && em.TryGetInt32(out var min)
                ? min
                : Math.Max(1, (formattedMarkdown ?? rawText).Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length / 200);

            if (string.IsNullOrWhiteSpace(formattedMarkdown))
            {
                return new Error("Gemini.MissingFormattedMarkdown", "AI response did not include formattedMarkdown.");
            }


            AiScenarioDrillVo? scenarioDrill = null;
            if (root.TryGetProperty("scenarioDrill", out var sd) && sd.ValueKind == JsonValueKind.Object)
            {
                var qText = sd.TryGetProperty("questionText", out var qp) ? qp.GetString() : null;
                var optList = new List<string>();
                if (sd.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var op in opts.EnumerateArray())
                    {
                        var optStr = op.GetString();
                        if (!string.IsNullOrWhiteSpace(optStr)) optList.Add(NormalizeEscapedNewlines(optStr.Trim()));
                    }
                }
                var cIdx = sd.TryGetProperty("correctOptionIndex", out var cp) && cp.TryGetInt32(out var ci) ? ci : 0;
                var expl = sd.TryGetProperty("explanationMarkdown", out var ep) ? ep.GetString() : "";
                var kpList = new List<string>();
                if (sd.TryGetProperty("expectedKeyPoints", out var kps) && kps.ValueKind == JsonValueKind.Array)
                {
                    foreach (var kp in kps.EnumerateArray())
                    {
                        var kpStr = kp.GetString();
                        if (!string.IsNullOrWhiteSpace(kpStr)) kpList.Add(NormalizeEscapedNewlines(kpStr.Trim()));
                    }
                }

                if (!string.IsNullOrWhiteSpace(qText) && optList.Count >= 2)
                {
                    scenarioDrill = new AiScenarioDrillVo(
                        QuestionText: NormalizeEscapedNewlines(qText.Trim()),
                        Options: optList,
                        CorrectOptionIndex: Math.Clamp(cIdx, 0, optList.Count - 1),
                        ExplanationMarkdown: NormalizeEscapedNewlines(expl ?? ""),
                        ExpectedKeyPoints: kpList);
                }
            }

            return new AiFormattedSliceResult(
                FormattedMarkdown: formattedMarkdown,
                SummaryMarkdown: string.IsNullOrWhiteSpace(summaryMarkdown) ? $"Executive summary for {chapterTitle}." : summaryMarkdown,
                KeyTakeaways: takeaways,
                EstimatedReadMinutes: Math.Max(1, estimatedReadMinutes),
                ScenarioDrill: scenarioDrill);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI slice response for '{Title}'.", chapterTitle);
            return new Error("Gemini.JsonParseError", ex.Message);
        }
    }

    private static string NormalizeEscapedNewlines(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Contains(@"\n"))
        {
            return text.Replace(@"\r\n", "\n").Replace(@"\n", "\n").Replace(@"\t", "\t");
        }
        return text;
    }

    public async Task<Result<(string Front, string Back)>> SynthesizeActiveRecallCardAsync(
        string quote,
        string? note,
        string chapterTitle,
        string locale = "en",
        CancellationToken ct = default)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        var noteContext = string.IsNullOrWhiteSpace(note) ? "None provided" : note.Trim();

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_model))
        {
            _logger.LogError("Gemini API key or model is not configured for recall card synthesis.");
            return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", "Gemini API key or model is not configured."));
        }

        try
        {
            var systemInstruction = $@"
You are an expert technical curriculum designer for senior software engineers.
Given this quote highlighted by an engineer from chapter '{chapterTitle}':
QUOTE: ""{quote}""
ENGINEER'S NOTE: ""{noteContext}""

Create an active recall flashcard:
1. Front: A concise, direct conceptual question testing understanding of the underlying engineering mechanism or trade-off in {(isVi ? "Vietnamese" : "English")}. Do NOT ask ""What did the author say about X?"". Instead ask: ""Why does X achieve Y?"" or ""How does mechanism X handle failure condition Y?"".
2. Back: A clear, authoritative explanation (2-3 sentences max) detailing the answer, architectural trade-offs, and operational invariants in {(isVi ? "Vietnamese" : "English")}.

Respond strictly in valid JSON without markdown wrapping:
{{
  ""front"": ""Concise conceptual question..."",
  ""back"": ""Authoritative 2-3 sentence explanation...""
}}";

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = $"Chapter: '{chapterTitle}'\nQuote: '{quote}'\nNote: '{noteContext}'" }
                        }
                    }
                },
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 1024,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            var response = await PostGeminiWithRetryAsync(requestUri, jsonContent, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API error ({StatusCode}) during recall card synthesis: {Error}", response.StatusCode, errorBody);
                return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", $"Gemini API returned status {response.StatusCode}."));
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                _logger.LogError("Gemini API returned 0 candidates during recall card synthesis.");
                return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", "AI model returned no candidates."));
            }

            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            string? rawJson = null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    rawJson = textProp.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                _logger.LogError("Gemini API returned empty text part during recall card synthesis.");
                return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", "AI model returned empty text."));
            }

            var cleanJson = ExtractJsonObject(rawJson.Trim());
            using var cardDoc = JsonDocument.Parse(cleanJson);
            var root = cardDoc.RootElement;

            var front = root.TryGetProperty("front", out var fp) ? fp.GetString() : null;
            var back = root.TryGetProperty("back", out var bp) ? bp.GetString() : null;

            if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back))
            {
                _logger.LogError("Parsed recall card JSON missing front or back properties.");
                return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", "Parsed recall card missing front or back."));
            }

            return (NormalizeEscapedNewlines(front.Trim()), NormalizeEscapedNewlines(back.Trim()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize active recall card for highlight in '{Title}'.", chapterTitle);
            return Result<(string Front, string Back)>.Failure(Error.Custom("AiService.RecallFailed", ex.Message));
        }
    }
}

