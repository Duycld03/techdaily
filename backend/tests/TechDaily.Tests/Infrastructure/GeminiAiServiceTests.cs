using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class GeminiAiServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content != null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return ResponseToReturn;
        }
    }

    [Fact]
    public async Task FormatSliceAsync_WhenEngineeringCraft_ShouldIncludeVerbatimMandateAndStructuralRestorationInPrompt()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "mock-api-key",
            ["Gemini:Model"] = "gemini-3.5-flash-lite"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);

        const string rawText = "Vào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt. Sức mạnh đáng kinh ngạc của những thói quen nhỏ bé. Dave Brailsford và triết lý tích lũy lợi ích cận biên 1% mỗi ngày.";
        const string chapterTitle = "Sức Mạnh Của Những Thay Đổi Nhỏ";

        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    formattedMarkdown = $"# {chapterTitle}\n\n> [!NOTE]\n> Executive context on cognitive habits.\n\n### Sức mạnh đáng kinh ngạc của những thói quen nhỏ bé\n\nVào đúng ngày cuối cùng của năm thứ hai cao trung, tôi bị một cây gậy bóng chày nện trúng mặt.\n\nDave Brailsford và triết lý tích lũy lợi ích cận biên 1% mỗi ngày.\n\n### Key Takeaways\n- Focus on marginal gains\n- Systems over goals\n- Identity-based habits",
                                    summaryMarkdown = "Executive summary of the small changes principle.",
                                    keyTakeaways = new[]
                                    {
                                        "Focus on marginal gains",
                                        "Systems over goals",
                                        "Identity-based habits"
                                    },
                                    estimatedReadMinutes = 4,
                                    scenarioDrill = new
                                    {
                                        questionText = "How should a tech lead foster systematic improvement?",
                                        options = new[] { "Option A", "Option B", "Option C", "Option D" },
                                        correctOptionIndex = 1,
                                        explanationMarkdown = "Option B encourages sustainable 1% daily increments.",
                                        expectedKeyPoints = new[] { "Compounding habit loop", "Cognitive load reduction" }
                                    }
                                })
                            }
                        }
                    }
                }
            }
        });

        handler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(geminiResponseJson, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await service.FormatSliceAsync(
            rawText: rawText,
            chapterTitle: chapterTitle,
            language: "vi",
            category: Category.EngineeringCraft);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.CapturedRequestBody.Should().NotBeNull();

        using var requestDoc = JsonDocument.Parse(handler.CapturedRequestBody!);
        var root = requestDoc.RootElement;

        // Verify generation config maxOutputTokens is 8192
        var generationConfig = root.GetProperty("generationConfig");
        generationConfig.GetProperty("maxOutputTokens").GetInt32().Should().Be(8192);

        // Verify systemInstruction includes strict verbatim mandate and structural restoration rules
        var systemInstructionText = root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString();
        systemInstructionText.Should().NotBeNullOrWhiteSpace();

        systemInstructionText.Should().Contain("100% Verbatim Text Retention & Structural Restoration");
        systemInstructionText.Should().Contain("ZERO SUMMARIZATION");
        systemInstructionText.Should().Contain("Detect Run-in Headings");
        systemInstructionText.Should().Contain("### {SectionTitle}");
        systemInstructionText.Should().Contain("Eliminate Duplicated Heading Echo");
        systemInstructionText.Should().Contain(@"\n\n");
        systemInstructionText.Should().Contain("Preserve Spoken Dialogue & Quotes");

        // Verify parsed result contains clean restored markdown with promoted heading
        result.Value.FormattedMarkdown.Should().Contain("### Sức mạnh đáng kinh ngạc của những thói quen nhỏ bé");
        result.Value.FormattedMarkdown.Should().Contain("> [!NOTE]");
        result.Value.FormattedMarkdown.Should().Contain("Vào đúng ngày cuối cùng của năm thứ hai cao trung");
        result.Value.KeyTakeaways.Should().HaveCount(3);
        result.Value.ScenarioDrill.Should().NotBeNull();
        result.Value.ScenarioDrill!.CorrectOptionIndex.Should().Be(1);
    }
}
