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

public class EmbeddingAndSemanticCacheTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TechDailyDbContext _db;

    public EmbeddingAndSemanticCacheTests()
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
    public async Task GeminiEmbeddingService_WithoutApiKey_ShouldReturnNormalized768DimMockVector()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var service = new GeminiEmbeddingService(httpClient, config, NullLogger<GeminiEmbeddingService>.Instance);

        // Act
        var result = await service.GenerateEmbeddingAsync("PostgreSQL write ahead log");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var vec = result.Value;
        vec.ToArray().Length.Should().Be(768);

        // Vector should be normalized to unit length ~ 1.0
        var norm = Math.Sqrt(vec.ToArray().Sum(x => x * x));
        norm.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public async Task GeminiEmbeddingService_BatchEmbeddings_WithoutApiKey_ShouldReturnMatchingCount()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();
        var service = new GeminiEmbeddingService(httpClient, config, NullLogger<GeminiEmbeddingService>.Instance);

        var texts = new List<string> { "Text A", "Text B", "Text C" };

        // Act
        var result = await service.GenerateBatchEmbeddingsAsync(texts);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        foreach (var v in result.Value)
        {
            v.ToArray().Length.Should().Be(768);
        }
    }

    [Fact]
    public async Task TermExplanationService_WhenExactCacheHit_ShouldReturnWithoutCallingAI()
    {
        // Arrange
        var cached = new TermExplanationCache
        {
            Term = "optimistic locking",
            Category = "Database",
            Locale = "en",
            ExplanationText = "A concurrency control method where records are checked before committing.",
            HitCount = 1
        };
        await _db.TermExplanationCaches.AddAsync(cached);
        await _db.SaveChangesAsync();

        var fakeEmbedding = new CountingFakeEmbeddingService();
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();

        var service = new TermExplanationService(
            _db,
            fakeEmbedding,
            httpClient,
            config,
            NullLogger<TermExplanationService>.Instance);

        // Act
        var result = await service.ExplainTermAsync("Optimistic Locking", "Database", "Some context", "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Explanation.Should().Be(cached.ExplanationText);
        result.Value.IsFromCache.Should().BeTrue();

        // HitCount should have incremented
        var updated = await _db.TermExplanationCaches.FirstAsync(t => t.Id == cached.Id);
        updated.HitCount.Should().Be(2);

        // Did not invoke embedding service because exact match hit first
        fakeEmbedding.CallCount.Should().Be(0);
    }

    public class CountingFakeEmbeddingService : IEmbeddingService
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
