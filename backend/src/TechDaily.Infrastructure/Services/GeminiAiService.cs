using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Common;
using TechDaily.Application.DTOs;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class GeminiAiService : IAiReviewService
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
Locale preference: {locale} (Provide explanations in {locale}).
";

            var promptText = $@"
Question: {questionText}
Key points required for Senior level: {string.Join(", ", expectedKeyPoints)}
Benchmark Principal Model Answer: {modelAnswer}

Candidate's Answer:
{(string.IsNullOrWhiteSpace(userAnswerText) ? "[Audio recording provided below]" : userAnswerText)}
";

            var parts = new List<object>
            {
                new { text = promptText }
            };

            if (audioBytes != null && audioBytes.Length > 0)
            {
                var base64Audio = Convert.ToBase64String(audioBytes);
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = audioMimeType ?? "audio/webm",
                        data = base64Audio
                    }
                });
            }

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new { parts = parts.ToArray() }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.2
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-goog-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API request failed: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return Error.Custom("Gemini.ApiError", $"Gemini API returned status {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);

            var candidateText = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(candidateText))
            {
                return Error.Custom("Gemini.EmptyResponse", "Gemini returned an empty response.");
            }

            var review = ParseAiReviewJson(candidateText);
            review.AiModelUsed = _model;
            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Gemini AI evaluation");
            return Error.Custom("Gemini.Exception", ex.Message);
        }
    }

    private static AiReviewDto ParseAiReviewJson(string jsonString)
    {
        var review = new AiReviewDto();
        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // 1. Score parsing (int, float, string percentage)
            if (root.TryGetProperty("score", out var scoreElem))
            {
                if (scoreElem.ValueKind == JsonValueKind.Number)
                {
                    var raw = scoreElem.GetDouble();
                    review.Score = raw > 10 ? (int)Math.Round(raw / 10.0) : (int)Math.Round(raw);
                }
                else if (scoreElem.ValueKind == JsonValueKind.String)
                {
                    var str = scoreElem.GetString()?.Trim().TrimEnd('%', '/').Split('/')[0] ?? "8";
                    if (double.TryParse(str, out var parsed))
                    {
                        review.Score = parsed > 10 ? (int)Math.Round(parsed / 10.0) : (int)Math.Round(parsed);
                    }
                    else
                    {
                        review.Score = 8;
                    }
                }
            }
            review.Score = Math.Clamp(review.Score, 1, 10);

            // 2. Summary Feedback
            if (root.TryGetProperty("summaryFeedback", out var feedbackElem))
            {
                review.SummaryFeedback = feedbackElem.GetString() ?? string.Empty;
            }

            // 3. Strengths (Array or String)
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

            // 4. Missing Points (Array or String)
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

            // 5. Improved Answer Markdown
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
