using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Infrastructure;

public class AdminDashboardHealthTests
{
    [Theory]
    [InlineData(null, "test-key", "AI_MODEL_MISSING")]
    [InlineData("", "test-key", "AI_MODEL_MISSING")]
    [InlineData("gemini-3.6-flash", null, "AI_API_KEY_MISSING")]
    public async Task MissingConfiguration_FailsImmediatelyWithoutCallingProviders(string? model, string? key, string code)
    {
        var http = new ProbeHttpHandler();
        var embedding = new ProbeEmbeddingService();
        var result = await CreateProbe(http, embedding, model, key).ProbeAsync();
        result.IsHealthy.Should().BeFalse();
        result.TextGeneration.ErrorCode.Should().Be(code);
        result.Embeddings.ErrorCode.Should().Be(code);
        result.TextGeneration.LatencyMs.Should().BeNull();
        http.Calls.Should().Be(0);
        embedding.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProviderFailures_ReturnSafeCodesWithoutRawBodiesOrExceptionMessages()
    {
        var http = new ProbeHttpHandler { ResponseStatus = HttpStatusCode.InternalServerError };
        var embedding = new ProbeEmbeddingService { Failure = true };
        var result = await CreateProbe(http, embedding).ProbeAsync();
        result.TextGeneration.Status.Should().Be("unhealthy");
        result.TextGeneration.ErrorCode.Should().Be("AI_PROVIDER_ERROR");
        result.Embeddings.ErrorCode.Should().Be("AI_PROVIDER_ERROR");
        JsonSerializer.Serialize(result).Should().NotContain("private-provider-body").And.NotContain("test-key");
    }

    [Fact]
    public async Task Probe_MeasuresBothServicesAndRequiresExpectedEmbeddingDimension()
    {
        var http = new ProbeHttpHandler();
        var embedding = new ProbeEmbeddingService { Dimension = 3072 };
        var result = await CreateProbe(http, embedding).ProbeAsync();
        result.TextGeneration.Status.Should().Be("healthy");
        result.TextGeneration.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        result.Embeddings.Status.Should().Be("unhealthy");
        result.Embeddings.Dimension.Should().Be(3072);
        result.Embeddings.ErrorCode.Should().Be("EMBEDDING_DIMENSION_MISMATCH");
        http.ReceivedCancellation.Should().BeTrue();
        embedding.ReceivedCancellation.Should().BeTrue();
    }

    [Fact(Timeout = 5000)]
    public async Task Probe_UsesAnOverallDeadlineAndReportsTimeout()
    {
        var http = new ProbeHttpHandler { WaitForCancellation = true };
        var embedding = new ProbeEmbeddingService { WaitForCancellation = true };
        var result = await CreateProbe(http, embedding, clock: new FastDeadlineClock()).ProbeAsync();
        result.TextGeneration.ErrorCode.Should().Be("AI_TIMEOUT");
        result.Embeddings.ErrorCode.Should().Be("AI_TIMEOUT");
        result.TextGeneration.Status.Should().Be("unavailable");
    }

    [Fact]
    public async Task Probe_PropagatesRequestCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var action = () => CreateProbe(new ProbeHttpHandler(), new ProbeEmbeddingService()).ProbeAsync(cancellation.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DatabaseObservation_IsMeasuredAndFailureIsSanitized()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new DeepPaceDbContext(new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(connection).Options);
        var service = new AdminSystemStatusService(db, TimeProvider.System);
        var available = await service.ObserveAsync();
        available.Database.Status.Should().Be("healthy");
        available.Database.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        await db.DisposeAsync();
        var unavailable = await service.ObserveAsync();
        unavailable.Database.Status.Should().Be("unavailable");
        unavailable.Database.ErrorCode.Should().Be("DATABASE_UNAVAILABLE");
        JsonSerializer.Serialize(unavailable).Should().NotContain("DataSource").And.NotContain("ObjectDisposed");
    }

    private static AiHealthProbe CreateProbe(ProbeHttpHandler http, ProbeEmbeddingService embedding,
        string? model = "gemini-3.6-flash", string? key = "test-key", TimeProvider? clock = null)
        => new(new ProbeHttpFactory(http), new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Gemini:Model"] = model, ["Gemini:ApiKey"] = key }).Build(),
            embedding, clock ?? TimeProvider.System);

    private sealed class FastDeadlineClock : TimeProvider
    {
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            => base.CreateTimer(callback, state, TimeSpan.FromMilliseconds(50), period);
    }

    private sealed class ProbeHttpFactory(ProbeHttpHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ProbeHttpHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpStatusCode ResponseStatus { get; init; } = HttpStatusCode.OK;
        public bool WaitForCancellation { get; init; }
        public bool ReceivedCancellation { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            ReceivedCancellation = ct.CanBeCanceled;
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(ResponseStatus) { Content = new StringContent("private-provider-body") };
        }
    }

    private sealed class ProbeEmbeddingService : IEmbeddingService
    {
        public int Calls { get; private set; }
        public bool Failure { get; init; }
        public bool WaitForCancellation { get; init; }
        public int Dimension { get; init; } = 768;
        public bool ReceivedCancellation { get; private set; }
        public async Task<Result<Vector>> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
        {
            Calls++;
            ReceivedCancellation = ct.CanBeCanceled;
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, ct);
            return Failure ? Error.Custom("Provider.Error", "private-provider-body") : new Vector(new float[Dimension]);
        }
        public Task<Result<List<Vector>>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken ct = default)
            => throw new NotSupportedException();
    }
}
