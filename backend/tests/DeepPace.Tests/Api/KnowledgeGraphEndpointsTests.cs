using System.Net;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeepPace.Api.Endpoints;
using DeepPace.Application;
using DeepPace.Application.Features.KnowledgeGraph.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Xunit;

namespace DeepPace.Tests.Api;

public class KnowledgeGraphEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<DeepPaceDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _userId = Guid.NewGuid();

    public KnowledgeGraphEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initDb = new DeepPaceDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        TestAuthHandler.UserId = _userId;
        TestAuthHandler.IsAuthenticated = true;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<IDeepPaceDbContext>(_ => new DeepPaceDbContext(_dbOptions));
        builder.Services.AddApplicationServices();

        builder.Services.AddAuthentication(defaultScheme: "Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();

        _app.MapGroup("/api/v1/graph")
            .WithTags("Knowledge Graph")
            .RequireAuthorization()
            .MapKnowledgeGraphEndpoints();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetGraph_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = false;

        // Act
        var response = await _client.GetAsync("/api/v1/graph");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetGraph_WhenAuthenticated_ShouldReturn200OkWithGraphResponse()
    {
        // Arrange
        TestAuthHandler.IsAuthenticated = true;
        TestAuthHandler.UserId = _userId;

        using (var db = new DeepPaceDbContext(_dbOptions))
        {
            var user = new User { Id = _userId, Email = "graph@techdaily.local", Name = "Graph User" };
            var book = new DocumentBook
            {
                Id = Guid.NewGuid(),
                Title = "Under the Hood of CLR Generational GC",
                Slug = "clr-gc-book",
                Category = Category.BackendRuntime,
                IsPublished = true,
                CreatedByUserId = _userId
            };
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentBookId = book.Id,
                ChunkOrder = 1,
                ChapterTitle = "Generational GC Mechanics"
            };
            var card = SpacedRepetitionCard.CreateFromDrillMistake(
                _userId,
                chunk.Id,
                "What trigger GC generation 2?",
                "Allocation budget exceeded in generation 1.",
                DateOnly.FromDateTime(DateTime.UtcNow));

            await db.Users.AddAsync(user);
            await db.DocumentBooks.AddAsync(book);
            await db.DocumentChunks.AddAsync(chunk);
            await db.SpacedRepetitionCards.AddAsync(card);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/graph");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<KnowledgeGraphResponse>();
        payload.Should().NotBeNull();
        payload!.Nodes.Should().HaveCount(4);
        payload.Nodes.Should().Contain(n => n.Type == GraphNodeType.Pillar);
        payload.Nodes.Should().Contain(n => n.Type == GraphNodeType.Book);
        payload.Nodes.Should().Contain(n => n.Type == GraphNodeType.Chunk);
        payload.Nodes.Should().Contain(n => n.Type == GraphNodeType.Card);
        payload.Nodes.Should().NotContain(n => n.Type == "topic");
        payload.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToPillar);
        payload.Edges.Should().Contain(e => e.RelationType == GraphRelationType.ChunkToBook);
        payload.Edges.Should().Contain(e => e.RelationType == GraphRelationType.CardToChunk);
        payload.Stats.TotalNodes.Should().Be(4);
        payload.Stats.NodeTypeCounts[GraphNodeType.Pillar].Should().Be(1);
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static Guid UserId { get; set; } = Guid.NewGuid();
        public static bool IsAuthenticated { get; set; } = true;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Email, "test@techdaily.local")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
