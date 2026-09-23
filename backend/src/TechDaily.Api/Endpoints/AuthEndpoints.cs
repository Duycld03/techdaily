using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Infrastructure.Persistence;
using TechDaily.Infrastructure.Security;

namespace TechDaily.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 characters (256-bit entropy).");
        }
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "TechDaily";
        var jwtAudience = configuration["Jwt:Audience"] ?? "TechDailyUsers";

        // Standard Email & Password Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            HttpContext context,
            TechDailyDbContext db,
            IRefreshTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { code = Error.EmailPasswordRequired.Code, error = Error.EmailPasswordRequired.Message });
            }

            if (request.Password.Length < 8)
            {
                return Results.BadRequest(new { code = "AUTH_PASSWORD_TOO_SHORT", error = "Password must be at least 8 characters long." });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser != null)
            {
                return Results.BadRequest(new { code = Error.EmailExists.Code, error = Error.EmailExists.Message });
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

            var (rawRefreshToken, _) = await tokenService.IssueTokenAsync(user.Id);
            SetRefreshTokenCookie(context, rawRefreshToken);

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
            HttpContext context,
            TechDailyDbContext db,
            IRefreshTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { code = Error.EmailPasswordRequired.Code, error = Error.EmailPasswordRequired.Message });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Results.BadRequest(new { code = Error.InvalidCredentials.Code, error = Error.InvalidCredentials.Message });
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.BadRequest(new { code = Error.InvalidCredentials.Code, error = Error.InvalidCredentials.Message });
            }

            // Automatic password rehash migration if iterations were upgraded
            if (PasswordHasher.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.HashPassword(request.Password);
                user.MarkUpdated();
                await db.SaveChangesAsync();
            }

            var (rawRefreshToken, _) = await tokenService.IssueTokenAsync(user.Id);
            SetRefreshTokenCookie(context, rawRefreshToken);

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
            HttpContext context,
            TechDailyDbContext db,
            IRefreshTokenService tokenService,
            IConfiguration config) =>
        {
            var clientId = (!string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"]) ? config["Authentication:Google:ClientId"] : null)
                ?? (!string.IsNullOrWhiteSpace(config["Authentication__Google__ClientId"]) ? config["Authentication__Google__ClientId"] : null)
                ?? (!string.IsNullOrWhiteSpace(config["GOOGLE_CLIENT_ID"]) ? config["GOOGLE_CLIENT_ID"] : null);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.BadRequest(new { code = Error.GoogleNotConfigured.Code, error = Error.GoogleNotConfigured.Message });
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId.Trim() },
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleAuth Error] Token validation failed: {ex.Message}");
                return Results.BadRequest(new { code = Error.GoogleTokenInvalid.Code, error = "Invalid Google token: " + ex.Message });
            }

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

            var (rawRefreshToken, _) = await tokenService.IssueTokenAsync(user.Id);
            SetRefreshTokenCookie(context, rawRefreshToken);

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
        })
        .WithName("GoogleLogin")
        .WithSummary("Authenticates with Google ID token and returns app JWT.");

        // Refresh Token Rotation
        group.MapPost("/refresh", async (
            HttpContext context,
            IRefreshTokenService tokenService) =>
        {
            var rawToken = context.Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return Results.Json(new { code = "AUTH_INVALID_CREDENTIALS", error = "Refresh token is missing." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var rotateResult = await tokenService.RotateTokenAsync(rawToken);
            if (!rotateResult.IsSuccess)
            {
                ClearRefreshTokenCookie(context);
                return Results.Json(new { code = rotateResult.Error.Code, error = rotateResult.Error.Message }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var (newRawToken, _, user) = rotateResult.Value;
            SetRefreshTokenCookie(context, newRawToken);

            var newAccessToken = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience);
            return Results.Ok(new
            {
                Token = newAccessToken,
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
        .WithName("RefreshToken")
        .WithSummary("Rotates refresh token and issues a new access token.");

        // Revoke Token / Logout
        group.MapPost("/revoke", async (
            HttpContext context,
            IRefreshTokenService tokenService) =>
        {
            var rawToken = context.Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(rawToken))
            {
                await tokenService.RevokeFamilyAsync(rawToken);
            }
            ClearRefreshTokenCookie(context);
            return Results.NoContent();
        })
        .WithName("RevokeToken")
        .WithSummary("Revokes refresh token family and clears the cookie.");

        return group;
    }

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        context.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth"
        });
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
            Expires = DateTime.UtcNow.AddMinutes(60),
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
