using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace DeepPace.Tests.Api;

// Program reads environment configuration before building its service provider.
[CollectionDefinition(ActualProgramCollection.Name, DisableParallelization = true)]
public sealed class ActualProgramCollection
{
    public const string Name = "Actual production Program with isolated PostgreSQL";
}

[Collection(ActualProgramCollection.Name)]
public sealed class HostStartupAdmissionAndPipelineTests : IAsyncLifetime
{
    private static readonly string[] AdministrativeRoutes =
    [
        "/api/v1/admin/dashboard/overview",
        "/api/v1/admin/roles",
        "/api/v1/admin/users/",
        "/api/v1/admin/audit/",
        "/api/v1/admin/documents/",
        "/api/v1/admin/categories/",
        "/api/v1/admin/catalog/documents"
    ];

    private const string Issuer = "ActualProgramIntegrationTests";
    private const string Audience = "ActualProgramIntegrationClients";
    private readonly string _secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private readonly string _databaseName = $"host_admission_{Guid.NewGuid():N}";
    private readonly Dictionary<string, string?> _previousEnvironment = new();
    private string? _previousDirectory;
    private string _contentRoot = null!;
    private string _maintenanceConnection = null!;
    private string _databaseConnection = null!;
    private bool _databaseCreated;
    private ActualApiFactory? _factory;

    [Fact]
    public async Task ExplicitInitialization_ChangesRealAdmissionFromUnavailableToCurrentRolePermissions()
    {
        using var client = CreateClient();
        using var healthy = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        var (learner, candidate) = await PersistLearnersAsync();
        var learnerToken = SignToken(learner.Id, learner.AuthenticationVersion);
        var candidateToken = SignToken(candidate.Id, candidate.AuthenticationVersion);
        var staleToken = SignToken(learner.Id, learner.AuthenticationVersion - 1);

        foreach (var route in AdministrativeRoutes.Concat(["/api/v1/admin/capabilities", "/api/v1/catalog/capabilities"]))
        {
            using var authenticated = await GetAsync(client, route, learnerToken);
            await AssertSafeUnavailableAsync(authenticated);
            using var guest = await GetAsync(client, route);
            AssertNoStoreStatus(guest, HttpStatusCode.Unauthorized);
            using var stale = await GetAsync(client, route, staleToken);
            AssertNoStoreStatus(stale, HttpStatusCode.Unauthorized);
        }

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            Assert.False(await db.AuthorizationInitializations.AnyAsync());
            Assert.Equal(RoleDefaults.LearnerRoleId, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == candidate.Id)).RoleId);
            var initialized = await scope.ServiceProvider.GetRequiredService<IAuthorizationInitializationService>()
                .InitializeAsync([candidate.Id]);
            Assert.True(initialized.IsSuccess);
        }

        using var verification = _factory!.Services.CreateScope();
        var currentAdmin = await verification.ServiceProvider.GetRequiredService<DeepPaceDbContext>()
            .Users.AsNoTracking().SingleAsync(u => u.Id == candidate.Id);
        Assert.Equal(RoleDefaults.AdminRoleId, currentAdmin.RoleId);
        Assert.True(currentAdmin.AuthenticationVersion > candidate.AuthenticationVersion);
        var adminToken = SignToken(currentAdmin.Id, currentAdmin.AuthenticationVersion);
        foreach (var route in AdministrativeRoutes)
        {
            using var learnerResponse = await GetAsync(client, route, learnerToken);
            AssertNoStoreStatus(learnerResponse, HttpStatusCode.Forbidden);
            using var revoked = await GetAsync(client, route, candidateToken);
            AssertNoStoreStatus(revoked, HttpStatusCode.Unauthorized);
            using var admitted = await GetAsync(client, route, adminToken);
            AssertNoStoreStatus(admitted, HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ReceiptQueryFailure_AfterHealthyStartup_ReturnsSafeAdministrativeAndPublicHealthErrors()
    {
        using var client = CreateClient();
        using var healthy = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
        var (learner, candidate) = await PersistLearnersAsync();
        using (var scope = _factory!.Services.CreateScope())
        {
            var initialized = await scope.ServiceProvider.GetRequiredService<IAuthorizationInitializationService>()
                .InitializeAsync([candidate.Id]);
            Assert.True(initialized.IsSuccess);
            var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
            Assert.True((await scope.ServiceProvider.GetRequiredService<IAuthorizationReadinessService>().GetReadinessAsync()).IsInitialized);
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"AuthorizationInitializations\" RENAME COLUMN \"Scope\" TO \"UnavailableScope\"");
            var unavailable = await scope.ServiceProvider.GetRequiredService<IAuthorizationReadinessService>().GetReadinessAsync();
            Assert.False(unavailable.IsReady);
            Assert.True(unavailable.IsDatabaseConnected);
            Assert.False(string.IsNullOrWhiteSpace(unavailable.FailureReason));
        }

        foreach (var route in AdministrativeRoutes.Concat(["/api/v1/admin/capabilities", "/api/v1/catalog/capabilities"]))
        {
            using var unavailable = await GetAsync(client, route, SignToken(learner.Id, learner.AuthenticationVersion));
            await AssertSafeUnavailableAsync(unavailable);
        }

        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        var healthBody = await health.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(healthBody);
        Assert.Equal("unhealthy", payload.RootElement.GetProperty("status").GetString());
        AssertNoProviderOrSchemaDiagnostics(healthBody);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidRequiredGrants_RefuseActualHostCreationWithoutRepairOrInitialization(bool grantLearnerPermission)
    {
        using (var healthyClient = CreateClient())
        using (var health = await healthyClient.GetAsync("/health"))
        {
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }
        var (_, candidate) = await PersistLearnersAsync();
        await _factory!.DisposeAsync();
        await using var db = CreateDatabaseContext();
        if (grantLearnerPermission)
        {
            db.RolePermissions.Add(RolePermission.Create(RoleDefaults.LearnerRoleId, PermissionKeys.ManageRoles));
            await db.SaveChangesAsync();
        }
        else
        {
            Assert.Equal(1, await db.RolePermissions
                .Where(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageRoles)
                .ExecuteDeleteAsync());
        }

        await using var refusedHost = new ActualApiFactory(_contentRoot);
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var unexpectedClient = refusedHost.CreateClient();
        });

        Assert.Equal(grantLearnerPermission, await db.RolePermissions.AsNoTracking()
            .AnyAsync(p => p.RoleId == RoleDefaults.LearnerRoleId && p.PermissionKey == PermissionKeys.ManageRoles));
        Assert.Equal(grantLearnerPermission, await db.RolePermissions.AsNoTracking()
            .AnyAsync(p => p.RoleId == RoleDefaults.AdminRoleId && p.PermissionKey == PermissionKeys.ManageRoles));
        Assert.False(await db.AuthorizationInitializations.AnyAsync());
        Assert.Equal(RoleDefaults.LearnerRoleId, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == candidate.Id)).RoleId);
    }

    public async Task InitializeAsync()
    {
        var suppliedConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(suppliedConnection))
        {
            throw new InvalidOperationException(
                "TEST_POSTGRES_CONNECTION is required for actual Program integration tests; its PostgreSQL role must create/drop private databases and install pgvector.");
        }

        var connection = new NpgsqlConnectionStringBuilder(suppliedConnection) { Database = "postgres", Pooling = false };
        _maintenanceConnection = connection.ConnectionString;
        _databaseConnection = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Database = _databaseName }.ConnectionString;
        try
        {
            await using var maintenance = new NpgsqlConnection(_maintenanceConnection);
            await maintenance.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", maintenance);
            await create.ExecuteNonQueryAsync();
            _databaseCreated = true;
            _contentRoot = FindApiContentRoot();
            _previousDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_contentRoot);
            SetEnvironment("ConnectionStrings__DefaultConnection", _databaseConnection);
            SetEnvironment("Jwt__Secret", _secret);
            SetEnvironment("Jwt__Issuer", Issuer);
            SetEnvironment("Jwt__Audience", Audience);
            SetEnvironment("DOTNET_ENVIRONMENT", "Development");
            SetEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
            _factory = new ActualApiFactory(_contentRoot);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_factory is not null) await _factory.DisposeAsync();
        }
        finally
        {
            foreach (var (name, value) in _previousEnvironment) Environment.SetEnvironmentVariable(name, value);
            if (_previousDirectory is not null) Directory.SetCurrentDirectory(_previousDirectory);
            if (_databaseCreated)
            {
                await using var maintenance = new NpgsqlConnection(_maintenanceConnection);
                await maintenance.OpenAsync();
                await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", maintenance);
                await drop.ExecuteNonQueryAsync();
                _databaseCreated = false;
            }
        }
    }

    private HttpClient CreateClient() => _factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    private async Task<(User Learner, User Candidate)> PersistLearnersAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeepPaceDbContext>();
        var learner = new User { Id = Guid.NewGuid(), Email = "learner@host-test.invalid", Name = "Learner", AuthenticationVersion = 2, RoleId = RoleDefaults.LearnerRoleId, Status = AccountStatus.Active };
        var candidate = new User { Id = Guid.NewGuid(), Email = "candidate@host-test.invalid", Name = "Administrator candidate", RoleId = RoleDefaults.LearnerRoleId, Status = AccountStatus.Active };
        db.Users.AddRange(learner, candidate);
        await db.SaveChangesAsync();
        return (learner, candidate);
    }

    private DeepPaceDbContext CreateDatabaseContext() => new(new DbContextOptionsBuilder<DeepPaceDbContext>()
        .UseNpgsql(_databaseConnection, postgres => postgres.UseVector()).Options);

    private string SignToken(Guid userId, int version)
    {
        var token = new JwtSecurityToken(Issuer, Audience,
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim("session_version", version.ToString(CultureInfo.InvariantCulture))],
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string route, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static void AssertNoStoreStatus(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore == true);
    }

    private static async Task AssertSafeUnavailableAsync(HttpResponseMessage response)
    {
        AssertNoStoreStatus(response, HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(body);
        Assert.Equal(503, payload.RootElement.GetProperty("status").GetInt32());
        AssertNoProviderOrSchemaDiagnostics(body);
    }

    private static void AssertNoProviderOrSchemaDiagnostics(string body)
    {
        foreach (var diagnostic in new[] { "AuthorizationInitializations", "Scope", "42703", "Npgsql", "PostgresException", "column", "POSITION:", "receipt schema" })
        {
            Assert.DoesNotContain(diagnostic, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SetEnvironment(string name, string value)
    {
        _previousEnvironment.Add(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }

    private static string FindApiContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "DeepPace.Api");
            if (File.Exists(Path.Combine(candidate, "DeepPace.Api.csproj"))) return candidate;
        }
        throw new InvalidOperationException("Cannot locate the DeepPace.Api source content root from the test output directory.");
    }

    private sealed class ActualApiFactory(string contentRoot) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
            .UseContentRoot(contentRoot)
            .UseEnvironment("Development");
    }
}
