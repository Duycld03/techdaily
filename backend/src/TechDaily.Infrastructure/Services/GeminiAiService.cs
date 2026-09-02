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
        var viAspects = new[]
        {
            ("Cơ chế tối ưu hóa cấp phát bộ nhớ và quản lý vòng đời đối tượng", "Sử dụng bộ nhớ ngăn xếp (Stack) và Zero-allocation primitives", "Cấp phát liên tục trên Heap trong vòng lặp", "Tắt hoàn toàn trình thu gom rác GC", "Sử dụng Finalizer trên toàn bộ đối tượng"),
            ("Chiến lược kiểm soát concurrency và giảm thiểu lock contention", "Áp dụng cấu trúc dữ liệu Lock-free hoặc ReaderWriterLockSlim", "Dùng exclusive lock toàn cục chặn mọi luồng", "Chạy Thread.Sleep trong vòng lặp chờ khóa", "Bỏ qua đồng bộ hóa trạng thái dùng chung"),
            ("Xử lý lỗi ngoại lệ và đảm bảo tính kiên cường (Resilience) trong hệ thống phân tán", "Tích hợp Circuit Breaker và Retry có Exponential Backoff & Jitter", "Bắt tất cả Exception và nuốt âm thầm", "Thử lại vô hạn ngay lập tức khi xảy ra lỗi mạng", "Đóng băng tiến trình khi gặp timeout"),
            ("Kiến trúc truy xuất dữ liệu và tối ưu hóa I/O throughput", "Sử dụng luồng bất đồng bộ Non-blocking I/O và batching", "Đọc toàn bộ bảng dữ liệu vào bộ nhớ RAM mỗi request", "Dùng Blocking I/O trên ThreadPool worker", "Mở kết nối cơ sở dữ liệu mới cho mỗi bản ghi"),
            ("Thiết kế API và quản lý trạng thái tải cao", "Triển khai Rate Limiting theo token bucket và Caching phân tán", "Gửi toàn bộ dữ liệu thô không nén qua HTTP/1.0", "Lưu toàn bộ phiên làm việc người dùng trong bộ nhớ cục bộ đơn lẻ", "Bỏ qua xác thực JWT và kiểm tra quyền hạn"),
            ("Tối ưu hóa Index và mô hình truy vấn cơ sở dữ liệu", "Thiết kế Covering Index hoặc Partial Index phù hợp mẫu truy vấn", "Tạo Index trên toàn bộ cột không phân biệt tần suất", "Quét toàn bộ bảng (Full Table Scan) thay vì dùng Index", "Xóa toàn bộ Foreign Key để tăng tốc độ ghi mà không có ràng buộc"),
            ("Bảo vệ ứng dụng trước race conditions và deadlock", "Tuân thủ thứ tự khóa tài nguyên nghiêm ngặt và dùng Timeout khi chờ khóa", "Khóa các tài nguyên theo thứ tự ngẫu nhiên giữa các luồng", "Dùng nested lock không giới hạn độ sâu", "Tắt cờ an toàn đa luồng của trình biên dịch"),
            ("Tối ưu hóa hiệu năng CPU và Cache Locality (L1/L2)", "Bố trí dữ liệu liền kề (Data-oriented Design / Struct of Arrays)", "Truy cập bộ nhớ ngẫu nhiên qua nhiều con trỏ phân tán", "Tạo hàng nghìn object nhỏ rải rác trên heap", "Liên tục boxing/unboxing giá trị nguyên thủy"),
            ("Quản lý kết nối mạng và socket trong môi trường microservices", "Tái sử dụng Connection Pool (HttpClientFactory) và kiểm soát DNS TTL", "Khởi tạo HttpClient mới cho mỗi HTTP request đơn lẻ", "Để socket mở vô thời hạn không thiết lập keep-alive", "Tắt cơ chế TLS để giảm tải mã hóa trên môi trường public"),
            ("Chiến lược CI/CD và kiến trúc zero-downtime deployment", "Triển khai Blue-Green hoặc Rolling Update với Health Checks tự động", "Ghi đè trực tiếp file nhị phân đang chạy trên server production", "Tắt toàn bộ hệ thống trong 2 giờ để cập nhật phiên bản", "Bỏ qua bước chạy Automated Integration Test trước khi release")
        };

        var enAspects = new[]
        {
            ("Memory allocation optimization and object lifecycle management", "Leverage stack-allocated primitives and zero-allocation spans", "Allocate short-lived objects continuously on the managed heap", "Disable the Garbage Collector entirely during high traffic", "Implement expensive finalizers on all domain classes"),
            ("Concurrency control and lock contention mitigation strategies", "Employ lock-free data structures or fine-grained read-write locks", "Wrap all critical sections in a single global exclusive lock", "Spin-wait with Thread.Sleep inside tight acquisition loops", "Ignore synchronization primitives across worker threads"),
            ("Fault tolerance and resilience in distributed topologies", "Implement Circuit Breaker with exponential backoff and jitter", "Catch generic exceptions and swallow them without logging", "Retry network requests indefinitely with zero delay", "Block calling threads until deadlocked dependencies respond"),
            ("High-throughput non-blocking I/O and query architecture", "Utilize asynchronous non-blocking pipelines and batch processing", "Load entire unindexed dataset partitions into application memory", "Synchronously block ThreadPool workers waiting on socket I/O", "Instantiate a new persistent database connection per row"),
            ("High-scale API design and state management", "Enforce Token Bucket rate limiting and distributed caching tiers", "Stream uncompressed raw payloads over unversioned endpoints", "Store stateful session data in isolated single-instance memory", "Bypass token verification and claim inspection under load"),
            ("Database indexing strategies and query execution plans", "Construct covering indexes tailored to query filter criteria", "Create unclustered indexes on every column indiscriminately", "Force full table scans to avoid index maintenance overhead", "Drop all foreign key constraints without referential checks"),
            ("Deadlock prevention and multi-threaded synchronization", "Enforce strict lock acquisition hierarchy with explicit timeouts", "Acquire multiple resource locks in arbitrary non-deterministic order", "Nest synchronization monitors indefinitely without timeouts", "Rely on thread priority manipulation instead of synchronization"),
            ("CPU cache locality (L1/L2) and hardware efficiency", "Align data structures sequentially in memory (Data-Oriented Design)", "Traverse scattered linked-node pointer chains randomly", "Distribute millions of micro-objects across fragmented memory", "Perform repetitive boxing/unboxing conversions in hot paths"),
            ("Microservice network socket management and connection pooling", "Reuse managed connection pools with HttpClientFactory and DNS TTL", "Instantiate a disposable HttpClient per outgoing HTTP request", "Leave idle TCP connections open indefinitely without heartbeats", "Disable TLS transport security to reduce cryptographic overhead"),
            ("Zero-downtime deployment and modern release engineering", "Execute Blue-Green or Rolling deployments with automated probes", "Directly overwrite running production binaries on the host VM", "Schedule mandatory 2-hour offline windows for minor patches", "Skip continuous integration test validation before releasing")
        };

        var list = new List<QuizQuestion>();
        for (var i = 0; i < count; i++)
        {
            var aspectIndex = i % 10;
            var correctOptionIdx = i % 4;

            string qText;
            string correctOpt;
            string distractor1;
            string distractor2;
            string distractor3;
            string exp;

            if (isVi)
            {
                var aspect = viAspects[aspectIndex];
                qText = $"[{level}] Câu hỏi #{i + 1} về {topic}: Khi giải quyết vấn đề \"{aspect.Item1}\", phương án kiến trúc nào sau đây là tối ưu nhất?";
                correctOpt = aspect.Item2;
                distractor1 = aspect.Item3;
                distractor2 = aspect.Item4;
                distractor3 = aspect.Item5;
                exp = $"### Phân Tích Kỹ Thuật Chuyên Sâu\n- **Phương án đúng:** \"{correctOpt}\" là giải pháp chuẩn công nghiệp giúp tối đa hóa throughput và độ ổn định của hệ thống.\n- **Nhận định phương án sai:** Các phương án còn lại dẫn đến race conditions, memory leak hoặc nghẽn cổ chai I/O nghiêm trọng.";
            }
            else
            {
                var aspect = enAspects[aspectIndex];
                qText = $"[{level}] Question #{i + 1} on {topic}: When addressing \"{aspect.Item1}\", which architectural strategy is optimal?";
                correctOpt = aspect.Item2;
                distractor1 = aspect.Item3;
                distractor2 = aspect.Item4;
                distractor3 = aspect.Item5;
                exp = $"### Technical Deep Dive\n- **Optimal Solution:** \"{correctOpt}\" maximizes throughput while preventing resource exhaustion under production workloads.\n- **Flawed Distractors:** The alternative choices introduce severe lock contention, memory fragmentation, or unhandled failures.";
            }

            var allOptions = new List<string> { correctOpt, distractor1, distractor2, distractor3 };
            if (correctOptionIdx > 0)
            {
                // Swap correct answer to target index
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
