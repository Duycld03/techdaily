using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using TechDaily.Api.Endpoints;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using Xunit;

namespace TechDaily.Tests.Api;

public class SystemEndpointsTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler);
        }
    }

    private class FakeEmbeddingService : IEmbeddingService
    {
        private readonly bool _shouldSucceed;
        private readonly int _dimensions;

        public FakeEmbeddingService(bool shouldSucceed = true, int dimensions = 768)
        {
            _shouldSucceed = shouldSucceed;
            _dimensions = dimensions;
        }

        public Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!_shouldSucceed)
            {
                return Task.FromResult(Result<Vector>.Failure(Error.Custom("Embedding.Error", "Embedding generation failed.")));
            }

            var floats = new float[_dimensions];
            return Task.FromResult(Result<Vector>.Success(new Vector(floats)));
        }

        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            if (!_shouldSucceed)
            {
                return Task.FromResult(Result<List<Vector>>.Failure(Error.Custom("Embedding.Error", "Batch generation failed.")));
            }

            var list = texts.Select(_ => new Vector(new float[_dimensions])).ToList();
            return Task.FromResult(Result<List<Vector>>.Success(list));
        }
    }

    private async Task<(WebApplication app, HttpClient client)> CreateTestAppAsync(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IEmbeddingService embeddingService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(configuration);
        builder.Services.AddSingleton(httpClientFactory);
        builder.Services.AddSingleton(embeddingService);

        var app = builder.Build();
        app.MapGroup("/api/v1/system").MapSystemEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();
        return (app, client);
    }

    [Fact]
    public async Task GetAiHealth_WhenBothModelsSucceed_ShouldReturn200OkWith768Dimensions()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "valid-api-key",
                ["Gemini:Model"] = "gemini-3.5-flash-lite",
                ["Gemini:EmbeddingModel"] = "gemini-embedding-001"
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"pong\"}]}}]}");
        var httpFactory = new StubHttpClientFactory(handler);
        var embeddingService = new FakeEmbeddingService(shouldSucceed: true, dimensions: 768);

        var (app, client) = await CreateTestAppAsync(config, httpFactory, embeddingService);
        try
        {
            // Act
            var response = await client.GetAsync("/api/v1/system/ai-health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("status").GetString().Should().Be("healthy");
            root.GetProperty("textModel").GetString().Should().Be("gemini-3.5-flash-lite");
            root.GetProperty("embeddingModel").GetString().Should().Be("gemini-embedding-001");
            root.GetProperty("dimension").GetInt32().Should().Be(768);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAiHealth_WhenTextModelFails_ShouldReturn503ServiceUnavailable()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "valid-api-key",
                ["Gemini:Model"] = "gemini-3.5-flash-lite",
                ["Gemini:EmbeddingModel"] = "gemini-embedding-001"
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "{\"error\":\"Model overloaded\"}");
        var httpFactory = new StubHttpClientFactory(handler);
        var embeddingService = new FakeEmbeddingService(shouldSucceed: true, dimensions: 768);

        var (app, client) = await CreateTestAppAsync(config, httpFactory, embeddingService);
        try
        {
            // Act
            var response = await client.GetAsync("/api/v1/system/ai-health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("status").GetString().Should().Be("unhealthy");
            root.GetProperty("error").GetString().Should().Contain("Text model");
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAiHealth_WhenEmbeddingDimensionMismatch_ShouldReturn503ServiceUnavailable()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "valid-api-key",
                ["Gemini:Model"] = "gemini-3.5-flash-lite",
                ["Gemini:EmbeddingModel"] = "gemini-embedding-001"
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"pong\"}]}}]}");
        var httpFactory = new StubHttpClientFactory(handler);
        var embeddingService = new FakeEmbeddingService(shouldSucceed: true, dimensions: 3072); // Wrong dimension!

        var (app, client) = await CreateTestAppAsync(config, httpFactory, embeddingService);
        try
        {
            // Act
            var response = await client.GetAsync("/api/v1/system/ai-health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("status").GetString().Should().Be("unhealthy");
            root.GetProperty("error").GetString().Should().Contain("Expected 768 dimensions but received 3072");
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAiHealth_WhenApiKeyMissing_ShouldReturn503ServiceUnavailable()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "",
                ["Gemini:Model"] = "gemini-3.5-flash-lite",
                ["Gemini:EmbeddingModel"] = "gemini-embedding-001"
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpFactory = new StubHttpClientFactory(handler);
        var embeddingService = new FakeEmbeddingService(shouldSucceed: true, dimensions: 768);

        var (app, client) = await CreateTestAppAsync(config, httpFactory, embeddingService);
        try
        {
            // Act
            var response = await client.GetAsync("/api/v1/system/ai-health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("status").GetString().Should().Be("unhealthy");
            root.GetProperty("error").GetString().Should().Contain("Gemini API key is not configured.");
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
