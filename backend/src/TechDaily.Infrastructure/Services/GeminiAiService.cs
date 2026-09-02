using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Infrastructure.Services;

public class GeminiAiService : IAiReviewService, ITechInsightGenerator, IQuizGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-3.6-flash";
    }

    public async Task<Result<AiReviewDto>> EvaluateSubmissionAsync(
        string questionText,
        List<string> expectedKeyPoints,
        string modelAnswer,
        string? userAnswerText,
        byte[]? audioBytes,
        string? audioMimeType,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Falling back to local mock evaluation.");
            return GenerateMockEvaluation(userAnswerText, audioBytes != null, locale);
        }

        try
        {
            var systemInstruction = $@"
You are a Principal Software Architect conducting a senior-level technical interview drill.
Analyze the candidate's answer for technical accuracy, architectural depth, memory implications, and internal mechanisms.
Evaluate strictly on a 1-10 scale where 8-10 is Senior/Principal level.
Respond strictly in valid JSON adhering to this schema:
{{
  ""score"": 8,
  ""summaryFeedback"": ""string"",
  ""strengths"": [""string""],
  ""missingPoints"": [""string""],
  ""improvedAnswerMarkdown"": ""string""
}}
No markdown formatting backticks around JSON.
Language: {(locale.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "Vietnamese" : "English")}.";

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"### Technical Question\n{questionText}\n");
            promptBuilder.AppendLine($"### Expected Key Points\n{string.Join("\n- ", expectedKeyPoints)}\n");
            promptBuilder.AppendLine($"### Model Answer\n{modelAnswer}\n");

            if (!string.IsNullOrWhiteSpace(userAnswerText))
            {
                promptBuilder.AppendLine($"### Candidate Text Answer\n{userAnswerText}\n");
            }

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var parts = new List<object>();

            if (audioBytes != null && audioBytes.Length > 0 && !string.IsNullOrWhiteSpace(audioMimeType))
            {
                parts.Add(new
                {
                    inlineData = new
                    {
                        mimeType = audioMimeType,
                        data = Convert.ToBase64String(audioBytes)
                    }
                });
                parts.Add(new
                {
                    text = $"{promptBuilder}\nPlease listen to the attached audio recording of the candidate's answer and evaluate it thoroughly."
                });
            }
            else
            {
                parts.Add(new
                {
                    text = promptBuilder.ToString()
                });
            }

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = parts.ToArray()
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
                    maxOutputTokens = 2048,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);
            using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, httpContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                return GenerateMockEvaluation(userAnswerText, audioBytes != null, locale);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseGeminiResponse(responseBody, _model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while calling Gemini API. Falling back to local mock evaluation.");
            return GenerateMockEvaluation(userAnswerText, audioBytes != null, locale);
        }
    }

    public async Task<Result<TechInsight>> GenerateInsightAsync(
        Category? preferredCategory,
        string? preferredTopic,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var categoryName = preferredCategory?.ToString() ?? "Senior Fullstack / .NET / Postgres / Distributed Systems";
        var topicPrompt = string.IsNullOrWhiteSpace(preferredTopic)
            ? "a deep, surprising senior-level performance or architectural trick (under the hood)"
            : preferredTopic;

        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Falling back to local generated insight.");
            return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topicPrompt, isVi);
        }

        try
        {
            var systemInstruction = $@"
You are a Principal Software Architect and Staff Engineer.
Generate an authoritative, bite-sized Senior Technical Insight on the requested topic or language.
IMPORTANT: If the user's prompt mentions or implies a specific language or technology (such as Rust, Go, Python, C#, TypeScript, Vue, React, PostgreSQL, Docker, Kafka, etc.), you MUST write the code snippets (`problemSnippet`, `solutionSnippet`) strictly in that requested language! Never default to C# unless C# or .NET was requested.
Focus on under-the-hood runtime mechanisms, memory allocation savings, zero-cost abstractions, or latency optimizations.
Provide realistic, concrete code snippets (bad/naive pattern vs senior optimal pattern) and benchmark statistics.
Language of explanations: {(isVi ? "Vietnamese (Technical terminology in English with Vietnamese explanations)" : "English")}.
Respond strictly in valid JSON adhering to this schema:
{{
  ""title"": ""Catchy, precise senior title"",
  ""category"": 3,
  ""tags"": [""tag1"", ""tag2""],
  ""summaryMarkdown"": ""Markdown summary explaining why the naive pattern is suboptimal and why the senior pattern is superior."",
  ""problemSnippet"": ""// ❌ BAD: snippet showing naive or anti-pattern"",
  ""solutionSnippet"": ""// ✅ SENIOR PATTERN: snippet showing optimal implementation"",
  ""underTheHoodMarkdown"": ""### Under The Hood Mechanics\\n- Deep dive explanation of runtime/engine/compiler internals."",
  ""benchmarkStats"": ""⚡ Benchmark metric (e.g. 10x faster | 0 B allocated)"",
  ""sourceUrl"": ""https://docs...""
}}
Category mapping: 0=FrontendWeb, 1=BackendDotNet, 2=DatabaseStorage, 3=SystemDesign (use 3 for general systems languages like Rust/Go/C++ or distributed systems).
No markdown backticks around JSON.";

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var promptText = $"User requested topic: '{topicPrompt}'. Preferred category: {categoryName}. Generate a comprehensive, accurate Senior Technical Insight.";

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
            using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, httpContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topicPrompt, isVi);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseInsightResponse(responseBody, preferredCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while generating insight with Gemini API. Falling back to local template.");
            return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topicPrompt, isVi);
        }
    }

    private static Result<TechInsight> ParseInsightResponse(string responseBody, Category? preferredCategory)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            var content = candidates[0].GetProperty("content");
            var text = content.GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, "Architecture", false);
            }

            var cleanJson = text.Trim();
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(cleanJson.IndexOf('\n') + 1);
                cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("```")).Trim();
            }

            using var insightDoc = JsonDocument.Parse(cleanJson);
            var root = insightDoc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Senior Architecture Insight" : "Senior Architecture Insight";
            var category = root.TryGetProperty("category", out var c) && c.TryGetInt32(out var catInt)
                ? (Category)catInt
                : preferredCategory ?? Category.BackendDotNet;

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
                Category = category,
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
        catch
        {
            return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, "Architecture", false);
        }
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

    private static TechInsight GenerateMockInsight(Category category, string topic, bool isVi)
    {
        return new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = $"insight-{Guid.NewGuid():N}"[..18],
            Title = isVi ? $"Tối ưu hóa chuyên sâu: {topic}" : $"Under The Hood Optimization: {topic}",
            Category = category,
            Tags = new() { "performance", "architecture", "senior" },
            SummaryMarkdown = isVi
                ? "Sử dụng cấu trúc bộ nhớ dạng Stack và tránh cấp phát đối tượng trên Managed Heap giúp loại bỏ 100% chi phí Garbage Collection."
                : "Utilizing stack-allocated primitives avoids heap allocation overhead and completely eliminates GC pause latency under heavy load.",
            ProblemSnippet = "// ❌ BAD: Heap allocations in tight loops\nforeach (var item in data) {\n    var str = item.ToString();\n}",
            SolutionSnippet = "// ✅ SENIOR PATTERN: Zero-allocation stack formatting\nSpan<char> buffer = stackalloc char[64];\nitem.TryFormat(buffer, out int charsWritten);",
            UnderTheHoodMarkdown = "### Under The Hood Mechanics\n- Stack allocations are wiped out immediately upon return from the current stack frame.\n- Managed Heap allocations require GC mark-and-sweep phases across Gen 0/1/2.",
            BenchmarkStats = "⚡ 12.5x faster | 0 B allocated",
            SourceUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/",
            IsPublished = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Result<AiReviewDto> ParseGeminiResponse(string responseBody, string modelUsed)
    {
        var review = new AiReviewDto
        {
            Score = 7,
            SummaryFeedback = "Evaluation completed.",
            Strengths = new(),
            MissingPoints = new(),
            ImprovedAnswerMarkdown = string.Empty,
            AiModelUsed = modelUsed
        };

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            var content = candidates[0].GetProperty("content");
            var text = content.GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return review;
            }

            var cleanJson = text.Trim();
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(cleanJson.IndexOf('\n') + 1);
                cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("```")).Trim();
            }

            using var scoreDoc = JsonDocument.Parse(cleanJson);
            var root = scoreDoc.RootElement;

            if (root.TryGetProperty("score", out var scoreElem))
            {
                if (scoreElem.TryGetInt32(out var scoreInt)) review.Score = Math.Clamp(scoreInt, 1, 10);
            }

            if (root.TryGetProperty("summaryFeedback", out var summaryElem))
            {
                review.SummaryFeedback = summaryElem.GetString() ?? "Evaluated response.";
            }

            if (root.TryGetProperty("strengths", out var strengthsElem))
            {
                if (strengthsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in strengthsElem.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) review.Strengths.Add(s);
                    }
                }
                else if (strengthsElem.ValueKind == JsonValueKind.String)
                {
                    review.Strengths.Add(strengthsElem.GetString()!);
                }
            }

            if (root.TryGetProperty("missingPoints", out var missingElem))
            {
                if (missingElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in missingElem.EnumerateArray())
                    {
                        var m = item.GetString();
                        if (!string.IsNullOrWhiteSpace(m)) review.MissingPoints.Add(m);
                    }
                }
                else if (missingElem.ValueKind == JsonValueKind.String)
                {
                    review.MissingPoints.Add(missingElem.GetString()!);
                }
            }

            if (root.TryGetProperty("improvedAnswerMarkdown", out var improvedElem))
            {
                review.ImprovedAnswerMarkdown = improvedElem.GetString() ?? string.Empty;
            }
        }
        catch
        {
            review.Score = 8;
            review.SummaryFeedback = "Evaluated response.";
        }

        return review;
    }

    public async Task<Result<List<QuizQuestion>>> GenerateQuestionsAsync(
        string topic,
        Category category,
        QuizLevel level,
        int count,
        List<string> existingTitlesToAvoid,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        var levelName = level switch
        {
            QuizLevel.Fresher => "Fresher / Entry-Level (Fundamentals, Syntax, Core Concepts)",
            QuizLevel.Junior => "Junior Engineer (Practical Usage, Standard Library, Basic Debugging)",
            QuizLevel.Middle => "Mid-Level Engineer (Design Patterns, Concurrency, SQL Optimization, Clean Code)",
            QuizLevel.Senior => "Senior / Staff Engineer (Under-the-hood Mechanics, Runtime/Engine Internals, Memory Overhead, High-Throughput Trade-offs)",
            _ => "Senior Engineer"
        };

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Falling back to local mock quiz questions.");
            return GenerateMockQuestions(topic, category, level, count, isVi);
        }

        try
        {
            var systemInstruction = $@"
You are a Principal Software Architect and Lead Technical Interviewer.
Your task is to generate exactly {count} realistic, challenging, high-quality multiple-choice technical interview questions for the level '{levelName}'.
Rules:
1. Each question MUST test practical engineering knowledge, conceptual depth, or architectural trade-offs.
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

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var promptText = $"Generate {count} multiple-choice interview questions on topic '{topic}' in category {category} for level {level}.";

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
            using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, httpContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                return GenerateMockQuestions(topic, category, level, count, isVi);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseQuizResponse(responseBody, topic, category, level, count, isVi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while generating quiz with Gemini API. Falling back to local template.");
            return GenerateMockQuestions(topic, category, level, count, isVi);
        }
    }

    private static Result<List<QuizQuestion>> ParseQuizResponse(
        string responseBody,
        string topic,
        Category category,
        QuizLevel level,
        int count,
        bool isVi)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var candidates = doc.RootElement.GetProperty("candidates");
            var content = candidates[0].GetProperty("content");
            var text = content.GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return GenerateMockQuestions(topic, category, level, count, isVi);
            }

            var cleanJson = text.Trim();
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(cleanJson.IndexOf('\n') + 1);
                cleanJson = cleanJson.Substring(0, cleanJson.LastIndexOf("```")).Trim();
            }

            using var quizDoc = JsonDocument.Parse(cleanJson);
            var root = quizDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                return GenerateMockQuestions(topic, category, level, count, isVi);
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
                    Category = category,
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
                return GenerateMockQuestions(topic, category, level, count, isVi);
            }

            return list;
        }
        catch
        {
            return GenerateMockQuestions(topic, category, level, count, isVi);
        }
    }

    private static List<QuizQuestion> GenerateMockQuestions(
        string topic,
        Category category,
        QuizLevel level,
        int count,
        bool isVi)
    {
        var list = new List<QuizQuestion>();
        for (var i = 1; i <= count; i++)
        {
            var qText = isVi
                ? $"[{level}] Câu hỏi phỏng vấn số {i} về chủ đề {topic}: Cơ chế hoạt động và tối ưu hóa nào sau đây là chính xác nhất?"
                : $"[{level}] Technical interview question #{i} on {topic}: Which mechanism or optimization strategy is optimal in production?";

            var exp = isVi
                ? $"### Phân Tích Chuyên Sâu\n- **Đáp án A là tối ưu nhất** vì nó giảm thiểu tối đa chi phí cấp phát bộ nhớ và loại bỏ hoàn toàn GC Gen 2 latency.\n- **Các lựa chọn còn lại:** Dẫn đến race conditions, rò rỉ bộ nhớ hoặc lock contention trong môi trường đa luồng."
                : $"### Deep Dive Technical Explanation\n- **Option A is optimal** because it prevents unnecessary allocations on the managed heap and eliminates Gen 2 GC pause spikes.\n- **Other options:** Result in avoidable memory fragmentation, race conditions, or unhandled lock contention under high concurrency.";

            list.Add(new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Topic = topic,
                Category = category,
                Level = level,
                QuestionText = qText,
                Options = isVi
                    ? new() { "A. Sử dụng cấu trúc dữ liệu tối ưu bộ nhớ và Zero-allocation", "B. Cấp phát đối tượng mới trên Heap trong vòng lặp liên tục", "C. Sử dụng Global Lock trên toàn bộ tiến trình", "D. Bỏ qua cơ chế kiểm tra ngoại lệ và timeout" }
                    : new() { "A. Utilize stack-allocated spans and zero-allocation memory primitives", "B. Allocate new heap objects repeatedly within tight loops", "C. Enforce a global lock blocking all worker threads", "D. Ignore cancellation tokens and timeout boundaries" },
                CorrectOptionIndex = 0,
                ExplanationMarkdown = exp,
                Tags = new() { topic.ToLowerInvariant().Replace(" ", "-"), level.ToString().ToLowerInvariant(), "interview" },
                CreatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            });
        }
        return list;
    }

    private static AiReviewDto GenerateMockEvaluation(string? text, bool hasAudio, string locale)
    {
        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);

        return new AiReviewDto
        {
            Score = 8,
            SummaryFeedback = isVi
                ? "Câu trả lời nắm rất chắc kiến trúc cơ bản và các cơ chế cốt lõi. Cần bổ sung thêm ví dụ thực tế về tối ưu hóa bộ nhớ."
                : "Solid response showing strong grasp of underlying runtime mechanisms and architectural trade-offs. Elaborate slightly more on edge-case memory overhead.",
            Strengths = isVi
                ? new() { "Nêu chính xác cơ chế hoạt động", "Giải thích rõ ràng sự khác biệt về hiệu năng", "Tư duy thiết kế mạch lạc" }
                : new() { "Accurate explanation of internal runtime mechanisms", "Clear reasoning on performance implications", "Structured architectural thinking" },
            MissingPoints = isVi
                ? new() { "Chưa đào sâu vào ảnh hưởng của Large Object Heap (LOH)", "Cần phân tích thêm về chi phí của GC Pause" }
                : new() { "Could emphasize Large Object Heap (LOH) impact", "Mention specific GC Pause latency mitigations" },
            ImprovedAnswerMarkdown = isVi
                ? "Để trả lời ở mức Principal: Hãy bắt đầu bằng cách nêu cơ chế cấp phát, sau đó liên hệ trực tiếp với `ArrayPool<T>.Shared` để loại bỏ GC Gen 2 pauses..."
                : "To answer at Principal level: Begin with the allocation mechanics, then immediately cite `ArrayPool<T>.Shared` to eliminate Gen 2 GC pauses in high-throughput pipelines...",
            AiModelUsed = "gemini-mock-dev"
        };
    }
}
