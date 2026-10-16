using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;
using DeepPace.Infrastructure.Services;
using Xunit;

namespace DeepPace.Tests.Api;

public class JwtAdmissionAuthenticationTests : IAsyncLifetime
{
    private const string Secret = "super-secret-key-that-has-at-least-32-characters-entropy!";
    private const string Issuer = "DeepPaceTest";
    private const string Audience = "DeepPaceUsersTest";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private DeepPaceDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<DeepPaceDbContext>().UseSqlite(_connection).Options;
        _db = new DeepPaceDbContext(dbOptions);
        await _db.Database.EnsureCreatedAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddRouting();
        builder.Services.AddScoped(_ => new DeepPaceDbContext(dbOptions));
        builder.Services.AddScoped<IDeepPaceDbContext>(sp => sp.GetRequiredService<DeepPaceDbContext>());
        builder.Services.AddScoped<IAccountSessionAuthority, AccountSessionAuthority>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!Guid.TryParse(idClaim, out var userId))
                        {
                            context.Fail("Missing or invalid user identifier claim.");
                            return;
                        }

                        var sessionVersionClaim = context.Principal?.FindFirst("session_version")?.Value;
                        if (string.IsNullOrWhiteSpace(sessionVersionClaim) || !int.TryParse(sessionVersionClaim, out var sessionVersion) || sessionVersion <= 0)
                        {
                            context.Fail("Missing or invalid session_version claim.");
                            return;
                        }

                        var authority = context.HttpContext.RequestServices.GetRequiredService<IAccountSessionAuthority>();
                        var validation = await authority.ValidateSessionAsync(userId, sessionVersion, context.HttpContext.RequestAborted);

                        if (validation.Status == AccountAuthorityStatus.Valid)
                        {
                            return;
                        }

                        if (validation.Status == AccountAuthorityStatus.Unavailable)
                        {
                            context.Fail("Account authority verification is temporarily unavailable.");
                            context.HttpContext.Items["SessionAuthorityUnavailable"] = true;
                            return;
                        }

                        context.Fail("Session has been revoked or account is inactive.");
                    }
                };
            });

        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.Use(async (context, next) =>
        {
            if (context.Items.ContainsKey("SessionAuthorityUnavailable"))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers.CacheControl = "no-store";
                await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Service Unavailable",
                    Detail = "Account authority verification is temporarily unavailable."
                });
                return;
            }
            await next(context);
        });
        _app.UseAuthorization();

        _app.MapGet("/protected", () => Results.Ok("authorized"))
            .RequireAuthorization();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _db.Dispose();
        await _connection.DisposeAsync();
    }

    private static string GenerateJwt(Guid userId, int? sessionVersion = null, DateTime? expires = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(Secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "User")
        };

        if (sessionVersion.HasValue)
        {
            claims.Add(new Claim("session_version", sessionVersion.Value.ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(60),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task ValidTokenAndSessionVersion_ShouldAuthorizeRequest()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "valid@example.com",
            Name = "Valid User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.Id, 1);
        var req = new HttpRequestMessage(HttpMethod.Get, "/protected");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidToken_WithFreshSessionVersionGreaterThanOne_ShouldAuthorizeRequest()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "v2@example.com",
            Name = "V2 User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 3
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.Id, 3);
        var req = new HttpRequestMessage(HttpMethod.Get, "/protected");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LegacyTokenWithoutSessionVersion_ShouldBeRejectedWith401()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "legacy@example.com",
            Name = "Legacy User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // Legacy token missing session_version claim
        var legacyToken = GenerateJwt(user.Id, sessionVersion: null);
        var req = new HttpRequestMessage(HttpMethod.Get, "/protected");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", legacyToken);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnexpiredToken_AfterForcedSignOut_ShouldBeRejectedWith401()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "revoked@example.com",
            Name = "Revoked User",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // Token issued at version 1
        var tokenV1 = GenerateJwt(user.Id, 1);

        // Forced sign-out increments version to 2
        user.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "/protected");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenV1);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnexpiredToken_AfterSuspension_ShouldBeRejectedWith401()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspended-bearer@example.com",
            Name = "Suspended Bearer",
            Status = AccountStatus.Active,
            AuthenticationVersion = 1
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user.Id, 1);

        // Suspension updates status to Suspended and advances version
        user.Status = AccountStatus.Suspended;
        user.AuthenticationVersion = 2;
        await _db.SaveChangesAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "/protected");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
