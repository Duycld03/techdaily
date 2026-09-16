using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class TermExplanationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public TermExplanationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TechDailyDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ExplainTermAsync_WhenGeminiApiFails_ShouldReturnFailureAndNotPersistToCache()
    {
        // Arrange
        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "fake-key",
                ["Gemini:Model"] = "gemini-3.1-flash-lite"
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "{\"error\": \"Service unavailable\"}");
        var httpClient = new HttpClient(handler);

        var service = new TermExplanationService(
            _db,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // Act
        var result = await service.ExplainTermAsync("Goroutine", "Go Concurrency", "Context about goroutines", "en");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AiService.Unavailable");

        // Must NOT add any record to TermExplanationCaches
        var cacheCount = await _db.TermExplanationCaches.CountAsync();
        cacheCount.Should().Be(0);
    }

    [Fact]
    public async Task ExplainTermAsync_WhenNoApiKey_ShouldReturnFailureAndNotPersistToCache()
    {
        // Arrange
        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = ""
            })
            .Build();

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);

        var service = new TermExplanationService(
            _db,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // Act
        var result = await service.ExplainTermAsync("Goroutine", "Go Concurrency", "Context about goroutines", "vi");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AiService.Unavailable");

        // Must NOT add any record to TermExplanationCaches
        var cacheCount = await _db.TermExplanationCaches.CountAsync();
        cacheCount.Should().Be(0);
    }

    [Fact]
    public async Task ExplainTermAsync_WhenGeminiApiSucceeds_ShouldPersistToCacheWithEmbedding()
    {
        // Arrange
        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "fake-valid-key",
                ["Gemini:Model"] = "gemini-3.1-flash-lite"
            })
            .Build();

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
                            new { text = "A lightweight thread managed by the Go runtime scheduler." }
                        }
                    }
                }
            }
        });

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, geminiResponseJson);
        var httpClient = new HttpClient(handler);

        var service = new TermExplanationService(
            _db,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // Act
        var result = await service.ExplainTermAsync("Goroutine", "Go Concurrency", "Context about goroutines", "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Explanation.Should().Be("A lightweight thread managed by the Go runtime scheduler.");
        result.Value.IsFromCache.Should().BeFalse();

        // Must persist to TermExplanationCaches
        var cachedRecords = await _db.TermExplanationCaches.ToListAsync();
        cachedRecords.Should().HaveCount(1);

        var record = cachedRecords[0];
        record.Term.Should().Be("goroutine");
        record.Category.Should().Be("Go Concurrency");
        record.Locale.Should().Be("en");
        record.ExplanationText.Should().Be("A lightweight thread managed by the Go runtime scheduler.");
        record.Embedding.Should().NotBeNull();
        record.HitCount.Should().Be(1);
    }
    [Fact]
    public async Task ExplainTermAsync_WhenTermExceeds200Chars_ShouldClampTo200CharsWithoutException()
    {
        // Arrange
        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "fake-valid-key",
                ["Gemini:Model"] = "gemini-3.1-flash-lite"
            })
            .Build();

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
                            new { text = "An explanation for a very long technical phrase or excerpt." }
                        }
                    }
                }
            }
        });

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, geminiResponseJson);
        var httpClient = new HttpClient(handler);

        var service = new TermExplanationService(
            _db,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // A term of 250 characters with mixed case and trailing spaces
        var rawLongTerm = "   " + new string('A', 150) + new string('B', 100) + "   ";
        var expectedClampedTerm = new string('a', 150) + new string('b', 50); // exactly 200 chars, lowercased

        // Act
        var result = await service.ExplainTermAsync(rawLongTerm, "System Architecture", "Context", "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Explanation.Should().Be("An explanation for a very long technical phrase or excerpt.");
        result.Value.IsFromCache.Should().BeFalse();

        var cachedRecords = await _db.TermExplanationCaches.ToListAsync();
        cachedRecords.Should().HaveCount(1);
        cachedRecords[0].Term.Length.Should().Be(200);
        cachedRecords[0].Term.Should().Be(expectedClampedTerm);

        // Second call with same long term should hit the exact cache using the clamped term
        var cachedResult = await service.ExplainTermAsync(rawLongTerm, "System Architecture", "Context", "en");
        cachedResult.IsSuccess.Should().BeTrue();
        cachedResult.Value.IsFromCache.Should().BeTrue();
        cachedResult.Value.Explanation.Should().Be("An explanation for a very long technical phrase or excerpt.");
    }

    [Fact]
    public async Task ExplainTermAsync_WhenDatabaseFailsDuringCacheSave_ShouldNotThrowAndReturnGeneratedExplanation()
    {
        // Arrange
        using var failingConnection = new SqliteConnection("DataSource=:memory:");
        failingConnection.Open();

        var options = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(failingConnection)
            .Options;

        using var throwingDb = new ThrowingSaveChangesDbContext(options);
        throwingDb.Database.EnsureCreated();

        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "fake-valid-key",
                ["Gemini:Model"] = "gemini-3.1-flash-lite"
            })
            .Build();

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
                            new { text = "Resilient explanation delivered despite cache failure." }
                        }
                    }
                }
            }
        });

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, geminiResponseJson);
        var httpClient = new HttpClient(handler);

        var service = new TermExplanationService(
            throwingDb,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // Act
        var result = await service.ExplainTermAsync("Kubernetes Ingress", "Cloud Native", "Context", "en");

        // Assert - The failure during SaveChangesAsync must NOT bubble up as an unhandled 500 error
        result.IsSuccess.Should().BeTrue();
        result.Value.Explanation.Should().Be("Resilient explanation delivered despite cache failure.");
        result.Value.IsFromCache.Should().BeFalse();
    }

    private class ThrowingSaveChangesDbContext : TechDailyDbContext
    {
        public ThrowingSaveChangesDbContext(DbContextOptions<TechDailyDbContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated database failure during cache save.");
        }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class CountingFakeEmbeddingService : IEmbeddingService
    {
        public int CallCount { get; private set; }

        public Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<Result<Vector>>(new Vector(new float[768]));
        }

        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
        {
            CallCount += texts.Count;
            return Task.FromResult<Result<List<Vector>>>(texts.Select(_ => new Vector(new float[768])).ToList());
        }
    }
}
