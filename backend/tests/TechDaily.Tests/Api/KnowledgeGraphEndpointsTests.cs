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
using TechDaily.Api.Endpoints;
using TechDaily.Application;
using TechDaily.Application.Features.KnowledgeGraph.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence;
using Xunit;

namespace TechDaily.Tests.Api;

public class KnowledgeGraphEndpointsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TechDailyDbContext> _dbOptions;
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private readonly Guid _userId = Guid.NewGuid();

    public KnowledgeGraphEndpointsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<TechDailyDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var initDb = new TechDailyDbContext(_dbOptions);
        initDb.Database.EnsureCreated();
    }

    public async Task InitializeAsync()
    {
        TestAuthHandler.UserId = _userId;
        TestAuthHandler.IsAuthenticated = true;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<ITechDailyDbContext>(_ => new TechDailyDbContext(_dbOptions));
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

        using (var db = new TechDailyDbContext(_dbOptions))
        {
            var user = new User { Id = _userId, Email = "graph@techdaily.local", Name = "Graph User" };
            var topic = new Topic
            {
                Id = Guid.NewGuid(),
                Title = "CLR Generational GC",
                Slug = "clr-gc",
                Category = Category.BackendDotNet,
                Difficulty = Difficulty.Senior,
                DayOrder = 4,
                Summary = "Generational garbage collection"
            };
            var book = new DocumentBook
            {
                Id = Guid.NewGuid(),
                Title = "Under the Hood of .NET Memory Management",
                Slug = "dotnet-mem",
                Category = Category.BackendDotNet,
                IsPublished = true
            };

            await db.Users.AddAsync(user);
            await db.Topics.AddAsync(topic);
            await db.DocumentBooks.AddAsync(book);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/graph");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<KnowledgeGraphResponse>();
        payload.Should().NotBeNull();
        payload!.Nodes.Should().HaveCount(2);
        payload.Edges.Should().Contain(e => e.RelationType == GraphRelationType.BookToTopic);
        payload.Stats.TotalNodes.Should().Be(2);
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
