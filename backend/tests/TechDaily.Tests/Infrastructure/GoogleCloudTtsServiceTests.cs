using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TechDaily.Application.Common;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class GoogleCloudTtsServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public List<string> CapturedRequestBodies { get; } = new();
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);
        public Func<HttpRequestMessage, HttpResponseMessage>? CustomResponder { get; set; }
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            CapturedRequest = request;
            if (request.Content != null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                CapturedRequestBody = body;
                CapturedRequestBodies.Add(body);
            }

            if (CustomResponder != null)
            {
                return CustomResponder(request);
            }

            return ResponseToReturn;
        }
    }

    [Fact]
    public async Task SynthesizeAsync_WhenSuccessful_ReturnsAudioBytesAndCharacterCount()
    {
        // Arrange
        var mockAudioBytes = Encoding.UTF8.GetBytes("fake-mp3-audio-bytes");
        var base64Audio = Convert.ToBase64String(mockAudioBytes);

        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { audioContent = base64Audio }),
                    Encoding.UTF8,
                    "application/json")
            }
        };

        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-api-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);
        const string text = "Xin chào, đây là bài đọc mẫu.";
        const string voiceId = "vi-VN-Neural2-A";

        // Act
        var result = await service.SynthesizeAsync(text, voiceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AudioBytes.Should().Equal(mockAudioBytes);
        result.Value.CharacterCount.Should().Be(text.Length);
        result.Value.DurationSeconds.Should().BeGreaterThan(0);

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.RequestUri!.Query.Should().Contain("key=test-api-key");
        handler.CapturedRequestBody.Should().Contain("vi-VN-Neural2-A");
        handler.CapturedRequestBody.Should().Contain("vi-VN");
    }

    [Fact]
    public async Task SynthesizeAsync_WhenApiKeyMissing_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = ""
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Act
        var result = await service.SynthesizeAsync("Hello world", "en-US-Neural2-F");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(Error.GoogleTtsNotConfigured.Code);
    }

    [Fact]
    public async Task SynthesizeAsync_WhenEmptyText_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Act
        var result = await service.SynthesizeAsync("   ", "en-US-Neural2-F");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GOOGLE_TTS_EMPTY_TEXT");
    }

    [Fact]
    public async Task SynthesizeAsync_WhenApiReturns429TooManyRequests_ReturnsAudioQuotaExhausted()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Quota exceeded", Encoding.UTF8, "text/plain")
            }
        };

        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Act
        var result = await service.SynthesizeAsync("Sample text", "en-US-Neural2-D");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(Error.AudioQuotaExhausted.Code);
    }

    [Fact]
    public async Task SynthesizeAsync_WhenApiReturns500InternalServerError_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal server error", Encoding.UTF8, "text/plain")
            }
        };

        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Act
        var result = await service.SynthesizeAsync("Sample text", "en-US-Neural2-D");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GOOGLE_TTS_FAILED");
    }

    [Fact]
    public async Task SynthesizeAsync_WhenResponseContainsNoAudioContent_ReturnsFailure()
    {
        // Arrange
        var handler = new MockHttpMessageHandler
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }
        };

        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Act
        var result = await service.SynthesizeAsync("Sample text", "en-US-Neural2-D");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GOOGLE_TTS_EMPTY_AUDIO");
    }
    [Fact]
    public void PartitionTextIntoSubBatches_WhenShortText_ReturnsSingleBatch()
    {
        var text = "Short sentence under byte limit.";
        var batches = GoogleCloudTtsService.PartitionTextIntoSubBatches(text, 4000);

        batches.Should().HaveCount(1);
        batches[0].Should().Be(text);
    }

    [Fact]
    public void PartitionTextIntoSubBatches_WhenLongText_ReturnsSubBatchesUnderMaxBytes()
    {
        var sentence = "Đây là một câu văn mẫu tiếng Việt với nhiều ký tự có dấu nhằm kiểm tra dung lượng byte UTF-8. ";
        var sb = new StringBuilder();
        for (var i = 0; i < 100; i++)
        {
            sb.Append(sentence);
        }
        var longText = sb.ToString();

        var batches = GoogleCloudTtsService.PartitionTextIntoSubBatches(longText, 1000);

        batches.Count.Should().BeGreaterThan(1);
        foreach (var batch in batches)
        {
            Encoding.UTF8.GetByteCount(batch).Should().BeLessOrEqualTo(1000);
        }
    }

    [Fact]
    public async Task SynthesizeAsync_WhenTextExceedsByteLimit_PartitionsAndConcatenatesAudio()
    {
        // Arrange
        var chunk1Audio = new byte[] { 1, 2, 3 };
        var chunk2Audio = new byte[] { 4, 5, 6 };
        var callIndex = 0;

        var handler = new MockHttpMessageHandler
        {
            CustomResponder = req =>
            {
                var audioBytes = callIndex == 0 ? chunk1Audio : chunk2Audio;
                callIndex++;
                var responseJson = JsonSerializer.Serialize(new
                {
                    audioContent = Convert.ToBase64String(audioBytes)
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            }
        };

        var client = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:TtsApiKey"] = "test-key"
            })
            .Build();

        var service = new GoogleCloudTtsService(client, config, NullLogger<GoogleCloudTtsService>.Instance);

        // Create text > 4000 bytes
        var sentence = "Đây là câu văn tiếng Việt dài để kiểm tra việc phân tách đoạn âm thanh. ";
        var sb = new StringBuilder();
        while (Encoding.UTF8.GetByteCount(sb.ToString()) < 4500)
        {
            sb.Append(sentence);
        }
        var longText = sb.ToString();

        // Act
        var result = await service.SynthesizeAsync(longText, "vi-VN-Neural2-A", "vi");

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.CallCount.Should().BeGreaterThan(1);
        result.Value.AudioBytes.Should().ContainInOrder(1, 2, 3, 4, 5, 6);
        result.Value.CharacterCount.Should().Be(longText.Length);
    }
}
