using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Security;

namespace TechDaily.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? "techdaily_development_secret_key_32_characters_minimum_12345";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TechDaily";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TechDailyUsers";

        // Standard Email & Password Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            TechDailyDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and password are required." });
            }

            if (request.Password.Length < 6)
            {
                return Results.BadRequest(new { error = "Password must be at least 6 characters." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser != null)
            {
                return Results.BadRequest(new { error = "An account with this email already exists." });
            }

            var user = new User
            {
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(request.Name) ? normalizedEmail.Split('@')[0] : request.Name.Trim(),
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                PreferredLocale = request.Locale ?? "en",
                TargetRole = "Senior Engineer",
                DailyGoalMinutes = 10
            };

            await db.Users.AddAsync(user);

            var streak = StreakRecord.Create(user.Id);
            await db.StreakRecords.AddAsync(streak);

            await db.SaveChangesAsync();

            var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience);
            return Results.Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.Name,
                    user.PreferredLocale,
                    user.TargetRole,
                    user.DailyGoalMinutes
                }
            });
        })
        .WithName("Register")
        .WithSummary("Registers a new user with standard email and password.");

        // Standard Email & Password Login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            TechDailyDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Email and password are required." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Results.BadRequest(new { error = "Invalid email or password." });
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.BadRequest(new { error = "Invalid email or password." });
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
                    user.PreferredLocale,
                    user.TargetRole,
                    user.DailyGoalMinutes,
                    user.AvatarUrl
                }
            });
        })
        .WithName("Login")
        .WithSummary("Authenticates with standard email and password.");

        // Google OAuth Login
        group.MapPost("/google", async (
            [FromBody] GoogleAuthRequest request,
            TechDailyDbContext db,
            IConfiguration config) =>
        {
            var clientId = config["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.BadRequest(new { error = "Google Client ID is not configured." });
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
                        GoogleSubjectId = payload.Subject,
                        PreferredLocale = "vi",
                        TargetRole = "Senior Engineer",
                        DailyGoalMinutes = 10
                    };
                    await db.Users.AddAsync(user);

                    var streak = StreakRecord.Create(user.Id);
                    await db.StreakRecords.AddAsync(streak);

                    await db.SaveChangesAsync();
                }
                else
                {
                    bool updated = false;
                    if (string.IsNullOrWhiteSpace(user.GoogleSubjectId) && !string.IsNullOrWhiteSpace(payload.Subject))
                    {
                        user.GoogleSubjectId = payload.Subject;
                        updated = true;
                    }
                    if (!string.IsNullOrWhiteSpace(payload.Picture) && user.AvatarUrl != payload.Picture)
                    {
                        user.AvatarUrl = payload.Picture;
                        updated = true;
                    }
                    if (string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(payload.Name))
                    {
                        user.Name = payload.Name;
                        updated = true;
                    }
                    if (string.IsNullOrWhiteSpace(user.TargetRole))
                    {
                        user.TargetRole = "Senior Engineer";
                        updated = true;
                    }
                    if (user.DailyGoalMinutes <= 0)
                    {
                        user.DailyGoalMinutes = 10;
                        updated = true;
                    }

                    if (updated)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
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
                        user.PreferredLocale,
                        user.AvatarUrl,
                        user.TargetRole,
                        user.DailyGoalMinutes
                    }
                });
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

public record RegisterRequest(string Email, string Password, string? Name = null, string? Locale = "en");
public record LoginRequest(string Email, string Password);
public record GoogleAuthRequest(string IdToken);
