using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class GeminiAiServiceTests
{
    [Theory]
    [InlineData("insight")]
    [InlineData("questions")]
    [InlineData("formatter")]
    public async Task Generation_PropagatesProviderCancellation(string operation)
    {
        var handler = new MockHttpMessageHandler { ThrowCancellation = true };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key", ["Gemini:Model"] = "test-model"
        }).Build();
        var service = new GeminiAiService(new HttpClient(handler), config, NullLogger<GeminiAiService>.Instance);
        Func<Task> action = operation switch
        {
            "insight" => async () => { await service.GenerateInsightAsync(Guid.NewGuid(), "Pottery"); },
            "questions" => async () => { await service.GenerateQuestionsAsync("Pottery", Guid.NewGuid(), QuizLevel.Foundation, 1, []); },
            _ => async () => { await service.FormatSliceAsync("Source words", "Chapter"); }
        };

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("\"invented-category\"")]
    public async Task GenerateInsightAsync_PreservesRequestedUuidRegardlessOfAiCategory(string aiCategory)
    {
        var categoryId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[] { new { content = new { parts = new[] {
                        new { text = $"{{\"title\":\"Source topic\",\"category\":{aiCategory}}}" }
                    } } } }
                }))
            }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key",
            ["Gemini:Model"] = "test-model"
        }).Build();
        var service = new GeminiAiService(new HttpClient(handler), config, NullLogger<GeminiAiService>.Instance);

        var result = await service.GenerateInsightAsync(categoryId, "Pottery glazing");

        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        handler.CapturedRequestBody.Should().Contain("Pottery glazing");
        handler.CapturedRequestBody.Should().NotContain(categoryId.ToString());
    }

    [Fact]
    public async Task GenerateQuestionsAsync_AssignsRequestedUuidAndPreservesQuestionText()
    {
        var categoryId = Guid.NewGuid();
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[] { new { content = new { parts = new[] {
                        new { text = "[{\"questionText\":\"Which glaze?\",\"category\":0,\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctOptionIndex\":1,\"explanationMarkdown\":\"B works.\"}]" }
                    } } } }
                }))
            }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key", ["Gemini:Model"] = "test-model"
        }).Build();
        var service = new GeminiAiService(new HttpClient(handler), config, NullLogger<GeminiAiService>.Instance);

        var result = await service.GenerateQuestionsAsync("Pottery", categoryId, QuizLevel.Foundation, 1, []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.CategoryId.Should().Be(categoryId);
        result.Value[0].QuestionText.Should().Be("Which glaze?");
        handler.CapturedRequestBody.Should().NotContain(categoryId.ToString());
        result.Value[0].Topic.Should().Be("Pottery");
    }
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);
        public bool ThrowCancellation { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            CapturedRequest = request;
            if (request.Content != null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return ResponseToReturn;
        }
    }

    [Fact]
    public async Task FormatSliceAsync_WhenProviderReturnsRestoredMarkdown_ShouldPreserveParsedContentAndDrill()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "mock-api-key",
            ["Gemini:Model"] = "gemini-3.6-flash"
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
            language: "vi");

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.CapturedRequestBody.Should().NotBeNull();

        // Verify parsed result contains clean restored markdown with promoted heading
        result.Value.FormattedMarkdown.Should().Contain("### Sức mạnh đáng kinh ngạc của những thói quen nhỏ bé");
        result.Value.FormattedMarkdown.Should().Contain("> [!NOTE]");
        result.Value.FormattedMarkdown.Should().Contain("Vào đúng ngày cuối cùng của năm thứ hai cao trung");
        result.Value.KeyTakeaways.Should().HaveCount(3);
        result.Value.ScenarioDrill.Should().NotBeNull();
        result.Value.ScenarioDrill!.CorrectOptionIndex.Should().Be(1);
    }

    [Fact]
    public async Task FormatSliceAsync_WhenProviderReturnsNoTakeaways_ShouldPreserveContentWithoutInventingAdvice()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-gemini-key",
            ["Gemini:Model"] = "gemini-3.6-flash"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);

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
                                    formattedMarkdown = "# Title\n\n> [!NOTE]\n> Context\n\nBody",
                                    summaryMarkdown = "Summary",
                                    keyTakeaways = Array.Empty<string>(),
                                    estimatedReadMinutes = 3
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
        var result = await service.FormatSliceAsync("Content", "Title", "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.KeyTakeaways.Should().BeEmpty("missing source-grounded takeaways must not be replaced by canned engineering advice");
        result.Value.FormattedMarkdown.Should().Be("# Title\n\n> [!NOTE]\n> Context\n\nBody");
        handler.CapturedRequestBody.Should().NotBeNull();

    }

    [Fact]
    public async Task GeminiAiService_WhenModelMissing_ShouldReturnFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "valid-api-key",
            ["Gemini:Model"] = ""
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var service = new GeminiAiService(client, config, NullLogger<GeminiAiService>.Instance);

        // Act
        var result = await service.GenerateInsightAsync(Guid.NewGuid(), "asp.net", locale: "en");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AiService.Unavailable");
        result.Error.Message.Should().Contain("API key or model is not configured");
    }
}
