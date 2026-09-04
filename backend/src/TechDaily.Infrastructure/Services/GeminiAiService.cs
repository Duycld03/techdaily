using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Infrastructure.Services;

public class GeminiAiService : ITechInsightGenerator, IQuizGeneratorService
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

    public async Task<Result<TechInsight>> GenerateInsightAsync(
        Category? preferredCategory,
        string? preferredTopic,
        List<string>? existingTitlesToAvoid = null,
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
            var cleanTopic = topicPrompt.Trim();
            var broadKeywords = new[] { "asp.net", "c#", ".net", "dotnet", "postgres", "postgresql", "sql", "database", "react", "vue", "frontend", "system design", "architecture", "docker", "kafka", "redis", "go", "golang", "rust", "python" };
            var isBroad = string.IsNullOrWhiteSpace(preferredTopic) ||
                          cleanTopic.Length <= 25 ||
                          broadKeywords.Any(k => cleanTopic.Equals(k, StringComparison.OrdinalIgnoreCase) || cleanTopic.Equals($"về {k}", StringComparison.OrdinalIgnoreCase));

            var exploratoryLenses = new[]
            {
                "Memory Allocations, GC Generations & Zero-Copy Buffers (ArrayPool, Span/Memory, Sockets)",
                "High-Throughput Concurrency, Lock-Free Internals & ThreadPool Scheduling",
                "Pipeline Architecture, Middleware Ordering, Short-Circuiting & Interceptors",
                "Under-the-Hood Compiler Optimizations, JIT Tiering & Runtime Mechanics",
                "Database Query Compilation, Index Execution Plans & Low-Allocation Data Access",
                "High-Volume Streaming I/O, Channels & Backpressure Management",
                "Distributed Caching, Tag Eviction & Cache Stampede Mitigations",
                "Resilience, Circuit Breakers, Partitioned Rate Limiting & Resource Throttling"
            };
            var randomLens = exploratoryLenses[Random.Shared.Next(exploratoryLenses.Length)];

            var antiDuplicationClause = existingTitlesToAvoid != null && existingTitlesToAvoid.Any()
                ? $@"
CRITICAL ANTI-DUPLICATION RULE:
Do NOT generate insights covering the exact same title, code pattern, or redundant explanation as any of these existing insights:
- {string.Join("\n- ", existingTitlesToAvoid.Take(25))}
If the user's topic relates to an existing insight, you MUST explore a fresh sub-system, complementary edge-case, architectural pitfall, or advanced runtime detail."
                : string.Empty;

            var topicInstruction = isBroad
                ? $"The user provided a broad topic ('{topicPrompt}'). You MUST explore it through the lens of '{randomLens}' or pick an unexpected deep under-the-hood sub-system."
                : $"The user provided a specific topic ('{topicPrompt}'). Focus directly, deeply, and strictly on this requested topic and its under-the-hood implementation mechanics.";

            var systemInstruction = $@"
You are a Principal Software Architect and Staff Engineer.
Generate an authoritative, bite-sized Senior Technical Insight on the requested topic or language.
IMPORTANT: If the user's prompt mentions or implies a specific language or technology (such as Rust, Go, Python, C#, TypeScript, Vue, React, PostgreSQL, Docker, Kafka, etc.), you MUST write the code snippets (`problemSnippet`, `solutionSnippet`) strictly in that requested language! Never default to C# unless C# or .NET was requested.
{topicInstruction}
Focus on under-the-hood runtime mechanisms, memory allocation savings, zero-cost abstractions, or latency optimizations.
Provide realistic, concrete code snippets (bad/naive pattern vs senior optimal pattern) and benchmark statistics.
{antiDuplicationClause}
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
            var promptText = isBroad
                ? $"User requested topic: '{topicPrompt}'. Preferred category: {categoryName}. Exploratory focus angle: '{randomLens}'. Generate a unique, authoritative, non-repetitive Senior Technical Insight."
                : $"User requested specific topic: '{topicPrompt}'. Preferred category: {categoryName}. Deep-dive into this specific topic with concrete architectural patterns and benchmarks.";

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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiApi", $"AI service is temporarily busy (status {(int)response.StatusCode}). Please try again in a few moments.");
                }
                return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topicPrompt, isVi);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseInsightResponse(responseBody, preferredCategory, topicPrompt, isVi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while generating insight with Gemini API.");
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                return new Error("Error.GeminiException", "An error occurred while communicating with the AI service. Please try again.");
            }
            return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topicPrompt, isVi);
        }
    }

    private Result<TechInsight> ParseInsightResponse(
        string responseBody,
        Category? preferredCategory,
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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiEmptyCandidates", "AI model returned no candidates. Please try again.");
                }
                return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topic, isVi);
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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiEmptyText", "AI model returned empty text. Please try again.");
                }
                return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topic, isVi);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while parsing Gemini insight response for topic '{Topic}'. Raw snippet: {Snippet}", topic, rawText ?? responseBody.Substring(0, Math.Min(responseBody.Length, 300)));
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                return new Error("Error.GeminiParse", "Failed to parse insight response from AI model. Please try again.");
            }
            return GenerateMockInsight(preferredCategory ?? Category.BackendDotNet, topic, isVi);
        }
    }

    private async Task<HttpResponseMessage> PostGeminiWithRetryAsync(
        string requestUri,
        string jsonPayload,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(requestUri, httpContent, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            // Retry once if transient 503 (ServiceUnavailable) or 429 (TooManyRequests)
            if ((response.StatusCode == HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429) && attempt == 1)
            {
                _logger.LogWarning("Gemini API transient error ({StatusCode}) on attempt {Attempt}. Retrying after 1500ms delay...", response.StatusCode, attempt);
                response.Dispose();
                await Task.Delay(1500, cancellationToken);
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

    private static TechInsight GenerateMockInsight(Category category, string topic, bool isVi)
    {
        var lowerTopic = topic.ToLowerInvariant();

        (string title, string[] tags, string summary, string prob, string sol, string uth, string bench, string url)[] pool;

        if (lowerTopic.Contains("asp") || lowerTopic.Contains("kestrel") || lowerTopic.Contains("middleware") || lowerTopic.Contains("web api"))
        {
            pool = isVi ? new[]
            {
                (
                    "Tối ưu hóa Memory Allocation trong ASP.NET Core với ArrayPool<T>.Shared",
                    new[] { "aspnetcore", "memory-allocation", "arraypool", "gc-tuning" },
                    "Trong các pipeline xử lý request tần suất cao, việc khởi tạo `byte[]` liên tục gây áp lực phân mảnh lên Large Object Heap (LOH). Sử dụng `ArrayPool<T>.Shared.Rent()` giúp tái sử dụng buffer và loại bỏ 100% chi phí GC Gen 2.",
                    "// ❌ BAD: Cấp phát mảng mới mỗi HTTP request\npublic byte[] ReadBody(Stream stream, int size) {\n    var buffer = new byte[size];\n    stream.Read(buffer, 0, size);\n    return buffer;\n}",
                    "// ✅ SENIOR PATTERN: Mượn và hoàn trả buffer từ ArrayPool\npublic void ReadBody(Stream stream, int size) {\n    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);\n    try {\n        stream.Read(buffer, 0, size);\n        ProcessBuffer(buffer, size);\n    } finally {\n        ArrayPool<byte>.Shared.Return(buffer);\n    }\n}",
                    "### Under The Hood Mechanics\n- `ArrayPool<T>` quản lý các bucket mảng theo lũy thừa của 2, giữ các buffer đã cấp phát trong bộ nhớ mà không giải phóng cho GC.\n- Giảm thiểu hoàn toàn GC pauses (Stop-the-world) trên các web API xử lý hơn 50,000 RPS.",
                    "⚡ Giảm 95% LOH Allocations | 0 B GC Gen 2",
                    "https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1"
                ),
                (
                    "Kiến trúc Non-blocking Request Processing với System.Threading.Channels trong ASP.NET Core",
                    new[] { "aspnetcore", "channels", "concurrency", "background-worker" },
                    "Thay vì xử lý các tác vụ nền nặng bên trong HTTP Request thread, hãy đẩy payload vào `Channel<T>` có giới hạn kích thước (Bounded Channel) với chiến lược Backpressure chống tràn bộ nhớ.",
                    "// ❌ BAD: Dùng Task.Run không kiểm soát số lượng luồng\n[HttpPost(\"telemetry\")]\npublic IActionResult Ingest([FromBody] MetricData data) {\n    Task.Run(() => _heavyService.Process(data));\n    return Accepted();\n}",
                    "// ✅ SENIOR PATTERN: Bounded Channel làm producer-consumer pipeline\n[HttpPost(\"telemetry\")]\npublic async ValueTask<IActionResult> Ingest([FromBody] MetricData data, [FromServices] ChannelWriter<MetricData> writer) {\n    await writer.WriteAsync(data);\n    return Accepted();\n}",
                    "### Under The Hood Mechanics\n- `Channel<T>` sử dụng cấu trúc ring-buffer không khóa (lock-free) kết hợp ValueTask awaiters để loại bỏ hoàn toàn chi phí context switch và thread starvation.\n- Bảo vệ HTTP worker pool của Kestrel không bị sụp đổ khi lượng traffic tăng đột biến.",
                    "⚡ 10x Throughput | Giữ vững 99.9th percentile SLA",
                    "https://learn.microsoft.com/en-us/dotnet/core/extensions/channels"
                ),
                (
                    "Tối ưu hóa Latency với OutputCache và Tag-Based Eviction trong ASP.NET Core",
                    new[] { "aspnetcore", "caching", "outputcache", "redis" },
                    "Sử dụng middleware `OutputCache` (tích hợp từ ASP.NET Core 7+) kết hợp cache tagging cho phép short-circuit pipeline ngay tại mức HTTP socket, bỏ qua hoàn toàn Routing, Model Binding và Controller Action.",
                    "// ❌ BAD: Tự kiểm tra IMemoryCache thủ công trong từng Action\n[HttpGet(\"catalog\")]\npublic async Task<IActionResult> Get() {\n    if (!_cache.TryGetValue(\"catalog\", out var data)) {\n        data = await _repo.GetAllAsync();\n        _cache.Set(\"catalog\", data);\n    }\n    return Ok(data);\n}",
                    "// ✅ SENIOR PATTERN: Declarative OutputCache với Cache Tags\napp.MapGet(\"/catalog\", async (ICatalogRepo repo) => await repo.GetAllAsync())\n   .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(10)).Tag(\"catalog-tag\"));",
                    "### Under The Hood Mechanics\n- `OutputCache` ghi thẳng response bytes đã được nén (Gzip/Brotli) vào Network Stream mà không qua chu trình serialization JSON lặp lại.\n- Khi dữ liệu thay đổi, lệnh `EvictByTagAsync(\"catalog-tag\")` dọn cache lập tức trên toàn cụm server.",
                    "⚡ 0 ms C# Execution | 120,000 RPS trên single node",
                    "https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output"
                ),
                (
                    "Phòng chống ThreadPool Starvation bằng cách loại bỏ Sync-over-Async trong ASP.NET Core",
                    new[] { "aspnetcore", "async-await", "threadpool-starvation", "kestrel" },
                    "Gọi `.Result` hoặc `.GetAwaiter().GetResult()` trên bất kỳ `Task` nào trong luồng xử lý của Kestrel sẽ khóa chặt worker thread và có thể gây tê liệt (Deadlock/Starvation) toàn bộ hệ thống chỉ với vài chục request đồng thời.",
                    "// ❌ BAD: Sync-over-Async khóa ThreadPool worker\n[HttpGet(\"config\")]\npublic IActionResult GetConfig() {\n    var config = _configService.GetRemoteConfigAsync().Result;\n    return Ok(config);\n}",
                    "// ✅ SENIOR PATTERN: Full async chain tới tận Socket IO\n[HttpGet(\"config\")]\npublic async Task<IActionResult> GetConfig(CancellationToken ct) {\n    var config = await _configService.GetRemoteConfigAsync(ct);\n    return Ok(config);\n}",
                    "### Under The Hood Mechanics\n- Kestrel sử dụng cơ chế Socket IO Completion Ports (IOCP) không tiêu tốn thread khi chờ I/O. Lệnh `.Result` ép ThreadPool phải mượn một thread mới để chạy tiếp, dẫn đến hiện tượng 'ThreadPool ramp-up delay' (500ms/thread).",
                    "⚡ Loại bỏ 100% ThreadPool Queuing Delay | CPU ổn định",
                    "https://learn.microsoft.com/en-us/dotnet/standard/async-in-depth"
                )
            } : new[]
            {
                (
                    "High-Throughput Buffer Pooling with ArrayPool<T>.Shared in ASP.NET Core",
                    new[] { "aspnetcore", "memory-allocation", "arraypool", "gc-tuning" },
                    "Allocating raw byte arrays per incoming request triggers severe Large Object Heap (LOH) fragmentation. Leveraging `ArrayPool<T>.Shared.Rent()` enables zero-allocation buffer reuse and eliminates Gen 2 GC pauses.",
                    "// ❌ BAD: New heap allocation per HTTP request\npublic byte[] ReadBody(Stream stream, int size) {\n    var buffer = new byte[size];\n    stream.Read(buffer, 0, size);\n    return buffer;\n}",
                    "// ✅ SENIOR PATTERN: Rent and return pooled buffers\npublic void ReadBody(Stream stream, int size) {\n    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);\n    try {\n        stream.Read(buffer, 0, size);\n        ProcessBuffer(buffer, size);\n    } finally {\n        ArrayPool<byte>.Shared.Return(buffer);\n    }\n}",
                    "### Under The Hood Mechanics\n- `ArrayPool<T>` partitions pre-allocated buffers across power-of-two size buckets, shielding the GC from rapid allocation cycles.\n- Guarantees sub-millisecond response latency under 50,000+ concurrent requests.",
                    "⚡ 95% Reduced LOH Allocations | 0 B Gen 2 GC Overhead",
                    "https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1"
                ),
                (
                    "Architecting Non-Blocking Request Ingestion with System.Threading.Channels",
                    new[] { "aspnetcore", "channels", "concurrency", "background-worker" },
                    "Offload heavy telemetry and event persistence out of the Kestrel HTTP pipeline into a bounded lock-free `Channel<T>` with backpressure semantics.",
                    "// ❌ BAD: Unbounded Task.Run depleting worker threads\n[HttpPost(\"telemetry\")]\npublic IActionResult Ingest([FromBody] MetricData data) {\n    Task.Run(() => _heavyService.Process(data));\n    return Accepted();\n}",
                    "// ✅ SENIOR PATTERN: Bounded Channel producer-consumer pipeline\n[HttpPost(\"telemetry\")]\npublic async ValueTask<IActionResult> Ingest([FromBody] MetricData data, [FromServices] ChannelWriter<MetricData> writer) {\n    await writer.WriteAsync(data);\n    return Accepted();\n}",
                    "### Under The Hood Mechanics\n- Channels utilize lock-free ring buffers and ValueTask-based awaiters to eliminate thread synchronization context switches and thread pool starvation under spikes.",
                    "⚡ 10x Throughput Surge Capacity | 99.9th percentile SLA guaranteed",
                    "https://learn.microsoft.com/en-us/dotnet/core/extensions/channels"
                ),
                (
                    "Sub-Millisecond Pipeline Bypass with OutputCache & Tag Eviction",
                    new[] { "aspnetcore", "caching", "outputcache", "redis" },
                    "ASP.NET Core `OutputCache` short-circuits execution at the socket layer, bypassing Routing, Model Binding, and Controller instantiation completely.",
                    "// ❌ BAD: Manual IMemoryCache lookups per controller action\n[HttpGet(\"catalog\")]\npublic async Task<IActionResult> Get() {\n    if (!_cache.TryGetValue(\"catalog\", out var data)) {\n        data = await _repo.GetAllAsync();\n        _cache.Set(\"catalog\", data);\n    }\n    return Ok(data);\n}",
                    "// ✅ SENIOR PATTERN: Declarative OutputCache with cache tags\napp.MapGet(\"/catalog\", async (ICatalogRepo repo) => await repo.GetAllAsync())\n   .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(10)).Tag(\"catalog-tag\"));",
                    "### Under The Hood Mechanics\n- `OutputCache` writes pre-compressed bytes (Brotli/Gzip) straight to the TCP output buffer without CPU-intensive JSON re-serialization.\n- `EvictByTagAsync` broadcasts instant invalidations across distributed clusters.",
                    "⚡ 0 ms Managed Execution | 120,000 RPS on single node",
                    "https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output"
                )
            };
        }
        else if (lowerTopic.Contains("postgres") || lowerTopic.Contains("sql") || lowerTopic.Contains("database") || lowerTopic.Contains("db"))
        {
            pool = isVi ? new[]
            {
                (
                    "Tối ưu hóa B-Tree Index với Covering Index (INCLUDE Clause) trong PostgreSQL",
                    new[] { "postgresql", "indexing", "performance", "database-tuning" },
                    "Khi một truy vấn SELECT cần thêm một vài cột không dùng để lọc, thay vì thêm cột vào Composite Index gây phình to B-Tree, hãy sử dụng mệnh đề `INCLUDE` để đạt được Index-Only Scan.",
                    "// ❌ BAD: Composite Index quá khổ gây tốn bộ nhớ đệm shared_buffers\nCREATE INDEX idx_orders_user_created_total ON orders (user_id, created_at, total_amount, status);",
                    "// ✅ SENIOR PATTERN: B-Tree Index lọc chính xác kèm payload INCLUDE\nCREATE INDEX idx_orders_user_created ON orders (user_id, created_at) INCLUDE (total_amount, status);",
                    "### Under The Hood Mechanics\n- Các cột trong `INCLUDE` được lưu trữ trực tiếp tại Leaf Nodes của B-Tree mà không tham gia vào cấu trúc cây tìm kiếm (Root/Branch Nodes).\n- Công cụ tối ưu hóa truy vấn PostgreSQL thực hiện Index-Only Scan và không cần truy cập Heap Table (Zero Table I/O).",
                    "⚡ Giảm 4x I/O Reads | Tăng tốc độ truy vấn 8x",
                    "https://www.postgresql.org/docs/current/indexes-index-only-scans.html"
                ),
                (
                    "Cơ chế Heap-Only Tuples (HOT) & FILLFACTOR: Loại bỏ Table Bloat trong PostgreSQL",
                    new[] { "postgresql", "mvcc", "table-bloat", "hot-updates" },
                    "Trong PostgreSQL, mỗi câu lệnh UPDATE mặc định sẽ tạo tuple mới trên Heap và ghi nhận vào tất cả các Index. Giảm `FILLFACTOR` xuống 85-90% giúp kích hoạt cơ chế HOT, cập nhật in-place mà không làm bẩn Index.",
                    "// ❌ BAD: Bảng nhận nhiều UPDATE liên tục với FILLFACTOR 100 mặc định\nCREATE TABLE user_telemetry (\n    id uuid PRIMARY KEY,\n    last_seen timestamptz,\n    payload jsonb\n);",
                    "// ✅ SENIOR PATTERN: Dự trữ không gian trống trên mỗi 8KB Page để kích hoạt HOT\nCREATE TABLE user_telemetry (\n    id uuid PRIMARY KEY,\n    last_seen timestamptz,\n    payload jsonb\n) WITH (fillfactor = 85);",
                    "### Under The Hood Mechanics\n- Khi có chỗ trống trên cùng Page, PostgreSQL tạo tuple mới và liên kết chuỗi con trỏ (HOT chain) ngay tại Page đó.\n- Tránh được hoàn toàn chi phí cập nhật hàng loạt Index Trees và giảm thiểu tần suất cần chạy VACUUM.",
                    "⚡ 0 Index Write Amplification | Giảm 70% Table Bloat",
                    "https://www.postgresql.org/docs/current/storage-hot.html"
                )
            } : new[]
            {
                (
                    "Covering Indexes via INCLUDE Clause for Zero-Heap-I/O in PostgreSQL",
                    new[] { "postgresql", "indexing", "performance", "database-tuning" },
                    "Over-indexing query payload columns in composite keys balloons B-Tree size. The `INCLUDE` clause appends payload columns strictly to leaf nodes for pure Index-Only Scans.",
                    "// ❌ BAD: Fat composite index bloating tree branches\nCREATE INDEX idx_orders ON orders (user_id, created_at, total_amount, status);",
                    "// ✅ SENIOR PATTERN: Lean B-Tree traversal key with leaf payload\nCREATE INDEX idx_orders ON orders (user_id, created_at) INCLUDE (total_amount, status);",
                    "### Under The Hood Mechanics\n- Included attributes reside solely in the leaf pages without contributing to tree traversal depth.\n- The planner executes pure Index-Only Scans without reading the physical heap.",
                    "⚡ 4x Lower I/O | 8x Query Speedup",
                    "https://www.postgresql.org/docs/current/indexes-index-only-scans.html"
                )
            };
        }
        else if (lowerTopic.Contains("vue") || lowerTopic.Contains("react") || lowerTopic.Contains("frontend") || lowerTopic.Contains("js") || lowerTopic.Contains("ts"))
        {
            pool = isVi ? new[]
            {
                (
                    "Tối ưu hóa Reactive Memory Footprint với shallowRef() trong Vue 3",
                    new[] { "vue3", "reactivity", "performance", "shallowref" },
                    "Khi làm việc với danh sách lớn (hàng chục nghìn đối tượng telemetry hoặc bảng dữ liệu), `reactive()` hoặc `ref()` sẽ đệ quy bọc Proxy lên từng thuộc tính con, gây tốn hàng chục MB RAM và lag trình duyệt.",
                    "// ❌ BAD: Deep proxy đệ quy trên 50,000 items\nconst dataset = ref<DataPoint[]>([]);\n// Mỗi object bên trong đều bị bọc bởi Proxy",
                    "// ✅ SENIOR PATTERN: shallowRef chỉ theo dõi thay đổi tham chiếu mảng\nconst dataset = shallowRef<DataPoint[]>([]);\n// Khi cập nhật dữ liệu mới: dataset.value = [...newData];",
                    "### Under The Hood Mechanics\n- `shallowRef()` bỏ qua hoàn toàn cơ chế phản ứng sâu (deep reactive conversion). Khi cập nhật cả mảng, Vue chỉ kích hoạt trigger effect một lần duy nhất thay vì duyệt qua toàn bộ cây đối tượng.",
                    "⚡ Giảm 80% RAM Consumption | 60 FPS mượt mà",
                    "https://vuejs.org/api/reactivity-advanced.html#shallowref"
                )
            } : new[]
            {
                (
                    "Mastering shallowRef() vs reactive() for 10,000+ Items in Vue 3",
                    new[] { "vue3", "reactivity", "performance", "shallowref" },
                    "Deep reactivity proxies thousands of sub-properties recursively, causing severe heap pressure. `shallowRef()` restricts reactive tracking to root reference assignments.",
                    "// ❌ BAD: Deep reactive tree over huge telemetry stream\nconst dataset = ref<DataPoint[]>([]);",
                    "// ✅ SENIOR PATTERN: Shallow reactive tracking\nconst dataset = shallowRef<DataPoint[]>([]);\ndataset.value = [...newData];",
                    "### Under The Hood Mechanics\n- `shallowRef()` skips recursive getter/setter interception entirely, preventing garbage collector spikes and frame drops during batch streaming updates.",
                    "⚡ 80% Less Heap Overhead | Locked 60 FPS",
                    "https://vuejs.org/api/reactivity-advanced.html#shallowref"
                )
            };
        }
        else
        {
            pool = isVi ? new[]
            {
                (
                    $"Tối ưu hóa cấp phát bộ nhớ Stack & Zero-Allocation: {topic}",
                    new[] { "performance", "architecture", "zero-allocation", "senior" },
                    "Sử dụng cấu trúc bộ nhớ dạng Stack và tránh cấp phát đối tượng trên Managed Heap giúp loại bỏ 100% chi phí Garbage Collection.",
                    "// ❌ BAD: Cấp phát chuỗi mới liên tục trên Heap trong vòng lặp\nforeach (var item in data) {\n    var str = item.ToString();\n}",
                    "// ✅ SENIOR PATTERN: Zero-allocation stack formatting\nSpan<char> buffer = stackalloc char[64];\nitem.TryFormat(buffer, out int charsWritten);",
                    "### Under The Hood Mechanics\n- Bộ nhớ Stack được tự động thu hồi khi thoát khỏi stack frame mà không cần qua các giai đoạn Mark & Sweep của Garbage Collector.\n- Giúp duy trì độ trễ P99 ổn định ngay cả khi hệ thống chịu tải đột biến.",
                    "⚡ 12.5x faster | 0 B allocated",
                    "https://learn.microsoft.com/en-us/dotnet/csharp/"
                ),
                (
                    $"Kiến trúc Transactional Outbox Pattern & CDC: {topic}",
                    new[] { "system-design", "microservices", "outbox-pattern", "event-driven" },
                    "Ghi đồng thời vào Database và Message Broker (Dual-write) luôn tiềm ẩn rủi ro mất mát dữ liệu hoặc phân mảnh trạng thái khi có lỗi mạng. Transactional Outbox đảm bảo tính nhất quán cuối cùng (Eventual Consistency).",
                    "// ❌ BAD: Dual-write trực tiếp vào DB và Kafka\nawait _db.Orders.AddAsync(order);\nawait _kafka.ProduceAsync(\"order-created\", order); // Nguy cơ crash tại đây!",
                    "// ✅ SENIOR PATTERN: Lưu Order & Outbox Message trong cùng một Database Transaction\nusing var tx = await _db.Database.BeginTransactionAsync();\nawait _db.Orders.AddAsync(order);\nawait _db.OutboxMessages.AddAsync(new OutboxMessage(order));\nawait tx.CommitAsync();",
                    "### Under The Hood Mechanics\n- Transaction đảm bảo tính ACID (cả hai cùng thành công hoặc cùng thất bại).\n- Tiến trình nền (Debezium hoặc polling worker) đọc Outbox table và phát event sang Kafka với ngữ nghĩa At-Least-Once Delivery.",
                    "⚡ Loại bỏ 100% rủi ro Dual-Write Inconsistency",
                    "https://microservices.io/patterns/data/transactional-outbox.html"
                )
            } : new[]
            {
                (
                    $"Stack Allocation & Zero-Allocation Primitives: {topic}",
                    new[] { "performance", "architecture", "zero-allocation", "senior" },
                    "Utilizing stack-allocated primitives avoids heap allocation overhead and completely eliminates GC pause latency under heavy load.",
                    "// ❌ BAD: Heap allocations in tight loops\nforeach (var item in data) {\n    var str = item.ToString();\n}",
                    "// ✅ SENIOR PATTERN: Zero-allocation stack formatting\nSpan<char> buffer = stackalloc char[64];\nitem.TryFormat(buffer, out int charsWritten);",
                    "### Under The Hood Mechanics\n- Stack frames are automatically unwound with zero GC tracking or collection pause cycles.",
                    "⚡ 12.5x faster | 0 B allocated",
                    "https://learn.microsoft.com/en-us/dotnet/csharp/"
                )
            };
        }

        var chosen = pool[Random.Shared.Next(pool.Length)];

        return new TechInsight
        {
            Id = Guid.NewGuid(),
            Slug = GenerateSlug(chosen.title),
            Title = chosen.title,
            Category = category,
            Tags = chosen.tags.ToList(),
            SummaryMarkdown = chosen.summary,
            ProblemSnippet = chosen.prob,
            SolutionSnippet = chosen.sol,
            UnderTheHoodMarkdown = chosen.uth,
            BenchmarkStats = chosen.bench,
            SourceUrl = chosen.url,
            IsPublished = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
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
            var response = await PostGeminiWithRetryAsync(requestUri, jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, errorBody);
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiApi", $"AI question generation is temporarily busy (status {(int)response.StatusCode}). Please try again in a few moments.");
                }
                return GenerateMockQuestions(topic, category, level, count, isVi);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseQuizResponse(responseBody, topic, category, level, count, isVi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while generating quiz with Gemini API.");
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                return new Error("Error.GeminiException", "An error occurred while communicating with the AI service. Please try again.");
            }
            return GenerateMockQuestions(topic, category, level, count, isVi);
        }
    }

    private Result<List<QuizQuestion>> ParseQuizResponse(
        string responseBody,
        string topic,
        Category category,
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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiEmptyCandidates", "AI model returned no candidates. Please try again.");
                }
                return GenerateMockQuestions(topic, category, level, count, isVi);
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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiEmptyText", "AI model returned empty text. Please try again.");
                }
                return GenerateMockQuestions(topic, category, level, count, isVi);
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
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiNotArray", "AI model response was not in expected array format. Please try again.");
                }
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
                _logger.LogWarning("No valid questions parsed from Gemini response for topic '{Topic}'.", topic);
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    return new Error("Error.GeminiEmptyParsed", "No valid questions could be extracted from AI response. Please try again.");
                }
                return GenerateMockQuestions(topic, category, level, count, isVi);
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while parsing Gemini quiz response for topic '{Topic}'. Raw response snippet: {Snippet}", topic, rawText ?? responseBody.Substring(0, Math.Min(responseBody.Length, 300)));
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                return new Error("Error.GeminiParse", "Failed to parse quiz response from AI model. Please try again.");
            }
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
        var lowerTopic = topic.ToLowerInvariant();

        (string, string, string, string, string)[] aspects;

        if (lowerTopic.Contains("asp") || lowerTopic.Contains("web api") || lowerTopic.Contains("controller") || lowerTopic.Contains("endpoint"))
        {
            // ASP.NET Core specific aspects
            aspects = isVi ? new[]
            {
                ("Vòng đời Service DI (Scoped vs Transient vs Singleton)", "Sử dụng AddScoped cho DbContext và Repository theo từng HTTP Request", "Đăng ký DbContext là Singleton để chia sẻ trên toàn ứng dụng", "Dùng AddTransient cho DbContext trong concurrent operations", "Không cần đăng ký Service Provider"),
                ("Thứ tự thực thi Middleware trong HTTP Request Pipeline", "UseRouting -> UseAuthentication -> UseAuthorization -> UseEndpoints", "UseAuthorization đặt trước UseAuthentication", "UseExceptionHandler đặt ở cuối pipeline", "UseStaticFiles đặt sau UseEndpoints"),
                ("Tối ưu hóa Response Caching và Output Caching trong ASP.NET Core", "Áp dụng OutputCache với Cache Key theo Header/Query Param và ETag", "Tắt toàn bộ bộ nhớ đệm HTTP để đảm bảo dữ liệu luôn mới nhất", "Lưu toàn bộ HTML response vào HttpContext.Items", "Sử dụng In-Memory Session cho static assets"),
                ("Cấu hình Rate Limiting và chống DDoS cho Web API", "Triển khai Token Bucket hoặc Sliding Window Partitioned Rate Limiter tích hợp sẵn", "Tạo Thread.Sleep chặn toàn bộ worker thread khi quá tải", "Bỏ qua Rate Limiting để tăng throughput tối đa", "Dùng Session State lưu trữ số lượng request của client"),
                ("Xử lý ngoại lệ toàn cục (Global Exception Handling)", "Sử dụng IExceptionHandler (ASP.NET Core 8+) kết hợp RFC 7807 Problem Details", "Bọc try-catch thủ công trong từng controller action và trả về 200 OK rỗng", "Để ứng dụng crash và Nginx tự trả về 502", "Dùng Middleware ném lại exception ra Console mà không response")
            } : new[]
            {
                ("Service Lifetimes in Dependency Injection (Scoped vs Transient vs Singleton)", "Register DbContext and repositories as Scoped per HTTP request scope", "Register DbContext as a Singleton shared across all application threads", "Use Transient for DbContext during concurrent multi-threaded requests", "Bypass DI container and use raw static instances"),
                ("Middleware Pipeline Execution Order", "Order: UseRouting -> UseAuthentication -> UseAuthorization -> MapEndpoints", "Place UseAuthorization before UseAuthentication", "Place UseExceptionHandler at the very end of the pipeline", "Execute static file middleware after endpoint mapping"),
                ("Response and Output Caching in ASP.NET Core", "Leverage OutputCache policy with ETag and query parameter vary rules", "Disable all HTTP caching headers to guarantee strict freshness", "Store entire dynamic HTML responses into HttpContext.Items", "Use in-memory session dictionaries for static assets"),
                ("API Rate Limiting Architecture", "Employ built-in sliding window or token bucket partitioned rate limiters", "Block worker threads with Thread.Sleep when request quotas exceed", "Disable rate limiting entirely to optimize synthetic throughput", "Store client request counters inside distributed user sessions"),
                ("Global Exception Handling Strategy", "Implement IExceptionHandler with standardized RFC 7807 Problem Details", "Wrap every controller action with manual empty try-catch blocks returning 200", "Allow uncaught exceptions to crash the Kestrel worker process", "Log errors to console only without returning structured JSON")
            };
        }
        else if (lowerTopic.Contains("c#") || lowerTopic.Contains(".net") || lowerTopic.Contains("dotnet"))
        {
            // C# / .NET specific aspects
            aspects = isVi ? new[]
            {
                ("Tối ưu hóa cấp phát bộ nhớ với Span<T> và Memory<T>", "Sử dụng ReadOnlySpan<char> để cắt chuỗi (slicing) không tạo thêm object trên Heap", "Dùng string.Substring() liên tục trong vòng lặp lớn", "Chuyển toàn bộ string sang char[] bằng ToCharArray()", "Ép kiểu string sang StringBuilder trong mọi hàm"),
                ("Cơ chế Garbage Collection và Large Object Heap (LOH)", "Tái sử dụng mảng lớn thông qua ArrayPool<T>.Shared để tránh phân mảnh LOH", "Liên tục `new byte[100_000]` trong các luồng xử lý I/O", "Gọi `GC.Collect()` thủ công sau mỗi HTTP request", "Chuyển toàn bộ dữ liệu LOH sang POH (Pinned Object Heap) vô thời hạn"),
                ("Quản lý luồng bất đồng bộ với async/await và ValueTask<T>", "Sử dụng ValueTask<T> cho các phương thức thường hoàn thành đồng bộ (cached)", "Dùng Task.Run() bọc các hàm I/O bất đồng bộ có sẵn", "Gọi `.Result` hoặc `.Wait()` gây nguy cơ ThreadPool starvation và Deadlock", "Dùng `async void` trên các service method nghiệp vụ"),
                ("Sự khác biệt giữa Record, Class và Struct trong C#", "Record struct cho dữ liệu nhỏ bất biến (value semantics, zero-allocation)", "Dùng Class cho mọi đối tượng DTO 2 trường để tránh copy", "Dùng mutable struct lớn trên 64 bytes truyền qua nhiều layer", "Lạm dụng struct chứa nhiều reference types gây áp lực GC"),
                ("Thực thi truy vấn LINQ: Deferred Execution vs Immediate Execution", "Dùng IQueryable<T> để database thực thi lọc dữ liệu trước khi nạp vào memory", "Gọi `.ToList()` trước các lệnh `.Where()` khi truy vấn hàng triệu bản ghi", "Lặp `foreach` trên IEnumerable gọi lại DbContext nhiều lần", "Dùng `.AsEnumerable()` thay vì `.AsNoTracking()` trong read-only queries")
            } : new[]
            {
                ("Memory Allocation Optimization with Span<T> and Memory<T>", "Use ReadOnlySpan<char> for zero-allocation string slicing without heap overhead", "Call string.Substring() repeatedly inside high-throughput loops", "Convert strings to char[] arrays using ToCharArray() for parsing", "Instantiate StringBuilder instances per character inspection"),
                ("Garbage Collection and Large Object Heap (LOH) Fragmentation", "Rent large buffers from ArrayPool<T>.Shared to prevent LOH fragmentation", "Allocate new byte[100_000] buffers on the heap for every I/O stream", "Force manual GC.Collect() invocations after every HTTP transaction", "Pin all large allocations permanently on the Pinned Object Heap"),
                ("Asynchronous Threading with async/await and ValueTask<T>", "Return ValueTask<T> for high-frequency operations that complete synchronously", "Wrap native async I/O calls inside Task.Run() indiscriminately", "Block asynchronous calls synchronously using .Result or .Wait()", "Declare business logic service methods as async void"),
                ("Memory Layout & Semantics: Record vs Class vs Struct", "Utilize readonly record structs for small immutable values with value semantics", "Default to reference classes for tiny 2-field data transfer payloads", "Pass large mutable structs exceeding 64 bytes across deep call stacks", "Embed multiple reference types inside structs causing pointer tracking overhead"),
                ("LINQ Query Execution: Deferred vs Immediate Evaluation", "Leverage IQueryable<T> expressions to compose SQL queries at database tier", "Materialize entire tables with .ToList() before applying .Where() filters", "Iterate un-buffered IEnumerable queries triggering N+1 database roundtrips", "Use .AsEnumerable() instead of .AsNoTracking() on read-only projection pipelines")
            };
        }
        else if (lowerTopic.Contains("postgres") || lowerTopic.Contains("sql") || lowerTopic.Contains("database") || lowerTopic.Contains("db"))
        {
            // Database & PostgreSQL aspects
            aspects = isVi ? new[]
            {
                ("Chiến lược thiết kế Index (B-Tree, GIN, BRIN)", "Dùng GIN Index cho JSONB/Full-text search và BRIN Index cho bảng time-series lớn", "Tạo B-Tree Index trên toàn bộ các cột mà không phân tích tần suất truy vấn", "Tắt hoàn toàn Index để tăng tốc độ ghi", "Sử dụng Hash Index cho các truy vấn tìm kiếm theo khoảng (Range Query)"),
                ("Cơ chế MVCC và Vacuuming trong PostgreSQL", "Cấu hình Autovacuum tích cực để dọn dẹp Dead Tuples và ngăn chặn Table Bloat", "Chạy VACUUM FULL thủ công khóa toàn bộ bảng trong giờ cao điểm", "Tắt tiến trình autovacuum để giải phóng CPU", "Dùng lệnh DELETE thay cho TRUNCATE khi dọn sạch bảng tạm"),
                ("Quản lý Transaction Isolation Level", "Dùng Read Committed kết hợp Optimistic Locking để đạt throughput cao và an toàn", "Dùng Serializable cho mọi transaction bất kể latency", "Bỏ qua Transaction khi cập nhật số dư tài khoản người dùng", "Dùng Read Uncommitted để chấp nhận đọc dữ liệu bẩn (Dirty Read)"),
                ("Tối ưu hóa N+1 Query và Eager Loading", "Sử dụng JOIN / Include có chọn lọc hoặc batch query thay vì truy vấn từng row", "Lặp foreach nạp từng child entity bằng query độc lập", "Luôn dùng Lazy Loading trên tất cả quan hệ dữ liệu", "Bỏ qua Foreign Key để tăng tốc độ query"),
                ("Kiểm soát Connection Pool", "Sử dụng PgBouncer / Connection Pooler để giới hạn số kết nối trực tiếp vào Postgres", "Mở một kết nối mới trực tiếp cho mỗi HTTP request và không đóng", "Tăng max_connections lên 10,000 trên server 4GB RAM", "Giữ kết nối transaction mở trong khi chờ external API response")
            } : new[]
            {
                ("Database Indexing Architecture (B-Tree, GIN, BRIN)", "Employ GIN indexes for JSONB/array filters and BRIN for sequential time-series tables", "Create B-Tree indexes on every column without query pattern profiling", "Drop all indexes permanently to accelerate bulk write ingestion", "Use Hash indexes for range queries and sorting clauses"),
                ("PostgreSQL MVCC Mechanics and Autovacuum Tuning", "Tune autovacuum thresholds aggressively to reclaim dead tuples and prevent bloat", "Execute manual VACUUM FULL during peak traffic locking tables completely", "Disable the autovacuum daemon entirely to save CPU cycles", "Rely on raw DELETE queries instead of TRUNCATE for ephemeral staging data"),
                ("Transaction Isolation Level Management", "Apply Read Committed with optimistic concurrency tokens for high throughput", "Enforce Serializable isolation across all read-only query workflows", "Bypass database transactions when mutating financial account balances", "Configure Read Uncommitted to permit dirty reads in audit logs"),
                ("Mitigating N+1 Query Antipatterns", "Use explicit projections and batch includes instead of repetitive row queries", "Iterate child entities sequentially issuing individual SELECT queries", "Enable unrestricted transparent lazy loading across API response mappers", "Eliminate relational foreign keys to reduce constraint evaluation overhead"),
                ("Database Connection Pooling Architecture", "Deploy PgBouncer in transaction mode to multiplex connections efficiently", "Instantiate a dedicated raw database connection per incoming web request", "Scale max_connections to 10,000 on low-memory database instances", "Retain active open transactions while awaiting slow third-party webhooks")
            };
        }
        else
        {
            // General / System Design fallback
            aspects = isVi ? new[]
            {
                ("Cơ chế tối ưu hóa cấp phát bộ nhớ và quản lý vòng đời đối tượng", "Sử dụng bộ nhớ ngăn xếp (Stack) và Zero-allocation primitives", "Cấp phát liên tục trên Heap trong vòng lặp", "Tắt hoàn toàn trình thu gom rác GC", "Sử dụng Finalizer trên toàn bộ đối tượng"),
                ("Chiến lược kiểm soát concurrency và giảm thiểu lock contention", "Áp dụng cấu trúc dữ liệu Lock-free hoặc ReaderWriterLockSlim", "Dùng exclusive lock toàn cục chặn mọi luồng", "Chạy Thread.Sleep trong vòng lặp chờ khóa", "Bỏ qua đồng bộ hóa trạng thái dùng chung"),
                ("Xử lý lỗi ngoại lệ và đảm bảo tính kiên cường (Resilience) trong hệ thống phân tán", "Tích hợp Circuit Breaker và Retry có Exponential Backoff & Jitter", "Bắt tất cả Exception và nuốt âm thầm", "Thử lại vô hạn ngay lập tức khi xảy ra lỗi mạng", "Đóng băng tiến trình khi gặp timeout"),
                ("Kiến trúc truy xuất dữ liệu và tối ưu hóa I/O throughput", "Sử dụng luồng bất đồng bộ Non-blocking I/O và batching", "Đọc toàn bộ bảng dữ liệu vào bộ nhớ RAM mỗi request", "Dùng Blocking I/O trên ThreadPool worker", "Mở kết nối cơ sở dữ liệu mới cho mỗi bản ghi"),
                ("Thiết kế API và quản lý trạng thái tải cao", "Triển khai Rate Limiting theo token bucket và Caching phân tán", "Gửi toàn bộ dữ liệu thô không nén qua HTTP/1.0", "Lưu toàn bộ phiên làm việc người dùng trong bộ nhớ cục bộ đơn lẻ", "Bỏ qua xác thực JWT và kiểm tra quyền hạn")
            } : new[]
            {
                ("Memory allocation optimization and object lifecycle management", "Leverage stack-allocated primitives and zero-allocation spans", "Allocate short-lived objects continuously on the managed heap", "Disable the Garbage Collector entirely during high traffic", "Implement expensive finalizers on all domain classes"),
                ("Concurrency control and lock contention mitigation strategies", "Employ lock-free data structures or fine-grained read-write locks", "Wrap all critical sections in a single global exclusive lock", "Spin-wait with Thread.Sleep inside tight acquisition loops", "Ignore synchronization primitives across worker threads"),
                ("Fault tolerance and resilience in distributed topologies", "Implement Circuit Breaker with exponential backoff and jitter", "Catch generic exceptions and swallow them without logging", "Retry network requests indefinitely with zero delay", "Block calling threads until deadlocked dependencies respond"),
                ("High-throughput non-blocking I/O and query architecture", "Utilize asynchronous non-blocking pipelines and batch processing", "Load entire unindexed dataset partitions into application memory", "Synchronously block ThreadPool workers waiting on socket I/O", "Instantiate a new persistent database connection per row"),
                ("High-scale API design and state management", "Enforce Token Bucket rate limiting and distributed caching tiers", "Stream uncompressed raw payloads over unversioned endpoints", "Store stateful session data in isolated single-instance memory", "Bypass token verification and claim inspection under load")
            };
        }

        // Vary base offset by hash of topic and level to prevent identical sequencing
        var baseOffset = Math.Abs((lowerTopic + (int)level).GetHashCode()) % aspects.Length;

        var list = new List<QuizQuestion>();
        for (var i = 0; i < count; i++)
        {
            var aspectIndex = (baseOffset + i) % aspects.Length;
            var correctOptionIdx = (aspectIndex + i) % 4;

            var aspect = aspects[aspectIndex];
            string qText = isVi
                ? $"[{level}] Câu hỏi #{i + 1} về {topic}: Khi giải quyết vấn đề \"{aspect.Item1}\", phương án kiến trúc nào sau đây là tối ưu nhất?"
                : $"[{level}] Question #{i + 1} on {topic}: When addressing \"{aspect.Item1}\", which architectural strategy is optimal?";

            var correctOpt = aspect.Item2;
            var distractor1 = aspect.Item3;
            var distractor2 = aspect.Item4;
            var distractor3 = aspect.Item5;

            string exp = isVi
                ? $"### Phân Tích Kỹ Thuật Chuyên Sâu\n- **Phương án đúng:** \"{correctOpt}\" là giải pháp chuẩn công nghiệp giúp tối đa hóa throughput và độ ổn định của hệ thống.\n- **Nhận định phương án sai:** Các phương án còn lại dẫn đến race conditions, memory leak hoặc nghẽn cổ chai I/O nghiêm trọng."
                : $"### Technical Deep Dive\n- **Optimal Solution:** \"{correctOpt}\" maximizes throughput while preventing resource exhaustion under production workloads.\n- **Flawed Distractors:** The alternative choices introduce severe lock contention, memory fragmentation, or unhandled failures.";

            var allOptions = new List<string> { correctOpt, distractor1, distractor2, distractor3 };
            if (correctOptionIdx > 0)
            {
                (allOptions[0], allOptions[correctOptionIdx]) = (allOptions[correctOptionIdx], allOptions[0]);
            }

            list.Add(new QuizQuestion
            {
                Id = Guid.NewGuid(),
                Topic = topic,
                Category = category,
                Level = level,
                QuestionText = qText,
                Options = allOptions,
                CorrectOptionIndex = correctOptionIdx,
                ExplanationMarkdown = exp,
                Tags = new() { topic.ToLowerInvariant().Replace(" ", "-"), level.ToString().ToLowerInvariant(), "interview" },
                CreatedAt = DateTimeOffset.UtcNow,
                IsDeleted = false
            });
        }
        return list;
    }
}
