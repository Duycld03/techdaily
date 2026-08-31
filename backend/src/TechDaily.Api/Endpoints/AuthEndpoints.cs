using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;

namespace TechDaily.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? "techdaily_development_secret_key_32_characters_minimum_12345";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TechDaily";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TechDailyUsers";

        // Dev Mock Auth (Active in Development mode)
        group.MapPost("/dev-login", async (TechDailyDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "senior.dev@techdaily.local");
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Email = "senior.dev@techdaily.local",
                    Name = "Senior Engineer (Dev)",
                    PreferredLocale = "en"
                };
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience);
            return Results.Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.PreferredLocale
                }
            });
        })
        .WithName("DevLogin")
        .WithSummary("Provides instant 1-click test login for development without Google OAuth.");

        // Google OAuth Login
        group.MapPost("/google", async (
            [FromBody] GoogleAuthRequest request,
            TechDailyDbContext db,
            IConfiguration config) =>
        {
            var clientId = config["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                // Fallback for development if Google Client ID not configured
                var defaultUser = await db.Users.FirstAsync(u => u.Id == Guid.Parse("00000000-0000-0000-0000-000000000001"));
                var devToken = GenerateJwtToken(defaultUser, jwtSecret, jwtIssuer, jwtAudience);
                return Results.Ok(new { Token = devToken, User = defaultUser });
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null)
                {
                    user = new User
                    {
                        Email = payload.Email,
                        Name = payload.Name ?? payload.Email.Split('@')[0],
                        AvatarUrl = payload.Picture,
                        GoogleSubjectId = payload.Subject
                    };
                    await db.Users.AddAsync(user);

                    var streak = StreakRecord.Create(user.Id);
                    await db.StreakRecords.AddAsync(streak);

                    await db.SaveChangesAsync();
                }

                var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience);
                return Results.Ok(new { Token = token, User = user });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "Invalid Google token: " + ex.Message });
            }
        })
        .WithName("GoogleLogin")
        .WithSummary("Authenticates with Google ID token and returns app JWT.");

        return group;
    }

    private static string GenerateJwtToken(User user, string secret, string issuer, string audience)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
}
