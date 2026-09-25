using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechDaily.Api.Contracts;
using TechDaily.Api.Http;
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
        var jwtExpiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;
        // Standard Email & Password Registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            TechDailyDbContext db,
            IOtpService otpService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Error.EmailPasswordRequired.ToProblem(StatusCodes.Status400BadRequest);
            }

            if (request.Password.Length < 8)
            {
                return new Error("AUTH_PASSWORD_TOO_SHORT", "Password must be at least 8 characters long.").ToProblem(StatusCodes.Status400BadRequest);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);
            if (exists)
            {
                return Error.EmailExists.ToProblem(StatusCodes.Status409Conflict);
            }

            var name = string.IsNullOrWhiteSpace(request.Name) ? normalizedEmail.Split('@')[0] : request.Name.Trim();
            var locale = request.Locale ?? "en";
            var pending = new PendingRegistration(name, PasswordHasher.HashPassword(request.Password), locale);

            var result = await otpService.RequestAsync(normalizedEmail, OtpPurpose.EmailVerification, pending, locale, ct);
            if (result.IsFailure)
            {
                return result.Error.ToProblem();
            }

            // Step 1 of OTP-first registration: no session is issued until the code is verified.
            return Results.Ok(new OtpChallengeResponse(normalizedEmail));
        })
        .WithName("Register")
        .WithSummary("Register User (request email verification code)")
        .WithDescription("Validates the input, stores a pending registration, and emails a verification code. No account or session is created until the code is verified.")
        .Produces<OtpChallengeResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireRateLimiting("OtpEndpointsPolicy");

        // Standard Email & Password Login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            HttpContext context,
            TechDailyDbContext db,
            IRefreshTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Error.EmailPasswordRequired.ToProblem(StatusCodes.Status400BadRequest);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Error.InvalidCredentials.ToProblem(StatusCodes.Status400BadRequest);
            }

            if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Error.InvalidCredentials.ToProblem(StatusCodes.Status400BadRequest);
            }

            // Automatic password rehash migration if iterations were upgraded
            if (PasswordHasher.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.HashPassword(request.Password);
                user.MarkUpdated();
                await db.SaveChangesAsync();
            }

            var (rawRefreshToken, _) = await tokenService.IssueTokenAsync(user.Id, isPersistent: request.RememberMe);
            SetRefreshTokenCookie(context, rawRefreshToken, request.RememberMe);

            var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes);
            return Results.Ok(new AuthSessionResponse(
                Token: token,
                User: new AuthUserDto(
                    Id: user.Id,
                    Email: user.Email,
                    Name: user.Name,
                    PreferredLocale: user.PreferredLocale,
                    TargetRole: user.TargetRole,
                    DailyGoalMinutes: user.DailyGoalMinutes,
                    AvatarUrl: user.AvatarUrl)));
        })
        .WithName("Login")
        .WithSummary("Login User")
        .WithDescription("Authenticates user with standard email and password.")
        .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Google OAuth Login
        group.MapPost("/google", async (
            [FromBody] GoogleAuthRequest request,
            HttpContext context,
            TechDailyDbContext db,
            IRefreshTokenService tokenService,
            IConfiguration config,
            IStarterHandbookService starterHandbookService,
            CancellationToken ct) =>
        {
            var clientId = (!string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"]) ? config["Authentication:Google:ClientId"] : null)
                ?? (!string.IsNullOrWhiteSpace(config["Authentication__Google__ClientId"]) ? config["Authentication__Google__ClientId"] : null)
                ?? (!string.IsNullOrWhiteSpace(config["GOOGLE_CLIENT_ID"]) ? config["GOOGLE_CLIENT_ID"] : null);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Error.GoogleNotConfigured.ToProblem(StatusCodes.Status400BadRequest);
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
                return new Error(Error.GoogleTokenInvalid.Code, "Invalid Google token: " + ex.Message).ToProblem(StatusCodes.Status400BadRequest);
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

                await db.SaveChangesAsync(ct);

                await starterHandbookService.ProvisionForUserAsync(user.Id, ct);
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

            var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes);
            return Results.Ok(new AuthSessionResponse(
                Token: token,
                User: new AuthUserDto(
                    Id: user.Id,
                    Email: user.Email,
                    Name: user.Name,
                    PreferredLocale: user.PreferredLocale,
                    TargetRole: user.TargetRole,
                    DailyGoalMinutes: user.DailyGoalMinutes,
                    AvatarUrl: user.AvatarUrl)));
        })
        .WithName("GoogleLogin")
        .WithSummary("Google Login")
        .WithDescription("Authenticates with Google ID token and returns app JWT.")
        .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Refresh Token Rotation
        group.MapPost("/refresh", async (
            HttpContext context,
            IRefreshTokenService tokenService) =>
        {
            var rawToken = context.Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return new Error("AUTH_INVALID_CREDENTIALS", "Refresh token is missing.").ToProblem(StatusCodes.Status401Unauthorized);
            }

            var rotateResult = await tokenService.RotateTokenAsync(rawToken);
            if (!rotateResult.IsSuccess)
            {
                ClearRefreshTokenCookie(context);
                return rotateResult.Error.ToProblem(StatusCodes.Status401Unauthorized);
            }

            var (newRawToken, newToken, user) = rotateResult.Value;
            SetRefreshTokenCookie(context, newRawToken, newToken.IsPersistent);

            var newAccessToken = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes);
            return Results.Ok(new AuthSessionResponse(
                Token: newAccessToken,
                User: new AuthUserDto(
                    Id: user.Id,
                    Email: user.Email,
                    Name: user.Name,
                    PreferredLocale: user.PreferredLocale,
                    TargetRole: user.TargetRole,
                    DailyGoalMinutes: user.DailyGoalMinutes,
                    AvatarUrl: user.AvatarUrl)));
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh Access Token")
        .WithDescription("Rotates refresh token and issues a new access token.")
        .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
        .WithSummary("Revoke Token")
        .WithDescription("Revokes refresh token family and clears the cookie.")
        .Produces(StatusCodes.Status204NoContent);

        // Register Step 2: verify the emailed code, create the account, and sign in
        group.MapPost("/register/verify", async (
            [FromBody] VerifyRegistrationRequest request,
            HttpContext context,
            TechDailyDbContext db,
            IOtpService otpService,
            IRefreshTokenService tokenService,
            IStarterHandbookService starterHandbookService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            {
                return Error.OtpInvalid.ToProblem();
            }

            var verify = await otpService.VerifyAsync(request.Email, OtpPurpose.EmailVerification, request.Code, ct);
            if (verify.IsFailure)
            {
                return verify.Error.ToProblem();
            }

            var otp = verify.Value;
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct))
            {
                return Error.EmailExists.ToProblem(StatusCodes.Status409Conflict);
            }

            if (string.IsNullOrWhiteSpace(otp.PendingPasswordHash))
            {
                return Error.OtpInvalid.ToProblem();
            }

            var user = new User
            {
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(otp.PendingName) ? normalizedEmail.Split('@')[0] : otp.PendingName!,
                PasswordHash = otp.PendingPasswordHash!,
                PreferredLocale = otp.PendingLocale ?? "en",
                TargetRole = "Senior Engineer",
                DailyGoalMinutes = 10
            };

            await db.Users.AddAsync(user, ct);
            await db.StreakRecords.AddAsync(StreakRecord.Create(user.Id), ct);
            await db.SaveChangesAsync(ct);

            await starterHandbookService.ProvisionForUserAsync(user.Id, ct);

            var (rawRefreshToken, _) = await tokenService.IssueTokenAsync(user.Id, isPersistent: request.RememberMe);
            SetRefreshTokenCookie(context, rawRefreshToken, request.RememberMe);

            var token = GenerateJwtToken(user, jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes);
            return Results.Ok(new AuthSessionResponse(
                Token: token,
                User: new AuthUserDto(
                    Id: user.Id,
                    Email: user.Email,
                    Name: user.Name,
                    PreferredLocale: user.PreferredLocale,
                    TargetRole: user.TargetRole,
                    DailyGoalMinutes: user.DailyGoalMinutes,
                    AvatarUrl: user.AvatarUrl)));
        })
        .WithName("RegisterVerify")
        .WithSummary("Verify Registration Code")
        .WithDescription("Verifies the email OTP, creates the user, provisions starter content, and returns an authenticated session.")
        .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireRateLimiting("OtpEndpointsPolicy");

        // Forgot Password: always 200 (anti-enumeration); emails a reset code if the account exists
        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            TechDailyDbContext db,
            IOtpService otpService,
            CancellationToken ct) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);
                if (user != null)
                {
                    // Ignore cooldown result to avoid leaking existence; transport failures still surface as 500.
                    await otpService.RequestAsync(normalizedEmail, OtpPurpose.PasswordReset, null, user.PreferredLocale, ct);
                }
            }

            return Results.Ok(new OtpMessageResponse("If the email is registered, a reset code has been sent."));
        })
        .WithName("ForgotPassword")
        .WithSummary("Request Password Reset Code")
        .WithDescription("Sends a password reset OTP when the email is registered. Always returns 200 to prevent account enumeration.")
        .Produces<OtpMessageResponse>(StatusCodes.Status200OK)
        .RequireRateLimiting("OtpEndpointsPolicy");

        // Reset Password: verify code, set new hash, revoke all sessions
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            HttpContext context,
            TechDailyDbContext db,
            IOtpService otpService,
            IRefreshTokenService tokenService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            {
                return Error.OtpInvalid.ToProblem();
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            {
                return new Error("AUTH_PASSWORD_TOO_SHORT", "Password must be at least 8 characters long.").ToProblem(StatusCodes.Status400BadRequest);
            }

            var verify = await otpService.VerifyAsync(request.Email, OtpPurpose.PasswordReset, request.Code, ct);
            if (verify.IsFailure)
            {
                return verify.Error.ToProblem();
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);
            if (user == null)
            {
                return Error.InvalidCredentials.ToProblem(StatusCodes.Status400BadRequest);
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.MarkUpdated();
            await db.SaveChangesAsync(ct);

            // A credential change signs the account out of every device.
            await tokenService.RevokeAllForUserAsync(user.Id, ct);
            ClearRefreshTokenCookie(context);

            return Results.Ok(new OtpMessageResponse("Password has been reset. Please sign in again."));
        })
        .WithName("ResetPassword")
        .WithSummary("Reset Password")
        .WithDescription("Verifies the reset OTP, updates the password, and revokes all refresh token families for the user.")
        .Produces<OtpMessageResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("OtpEndpointsPolicy");

        // Resend an OTP (subject to the 60s cooldown)
        group.MapPost("/otp/resend", async (
            [FromBody] ResendOtpRequest request,
            TechDailyDbContext db,
            IOtpService otpService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Purpose)
                || !Enum.TryParse<OtpPurpose>(request.Purpose, ignoreCase: true, out var purpose))
            {
                return Error.OtpInvalid.ToProblem();
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (purpose == OtpPurpose.PasswordReset)
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);
                if (user == null)
                {
                    return Results.Ok(new OtpMessageResponse("If the email is registered, a reset code has been sent."));
                }

                var reset = await otpService.RequestAsync(normalizedEmail, OtpPurpose.PasswordReset, null, user.PreferredLocale, ct);
                return reset.IsFailure ? reset.Error.ToProblem() : Results.Ok(new OtpChallengeResponse(normalizedEmail));
            }

            var resend = await otpService.RequestAsync(normalizedEmail, OtpPurpose.EmailVerification, null, null, ct);
            return resend.IsFailure ? resend.Error.ToProblem() : Results.Ok(new OtpChallengeResponse(normalizedEmail));
        })
        .WithName("ResendOtp")
        .WithSummary("Resend OTP")
        .WithDescription("Re-issues a verification or password reset code, subject to the resend cooldown.")
        .Produces<OtpChallengeResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .RequireRateLimiting("OtpEndpointsPolicy");

        return group;
    }

    private static bool IsHttpsRequest(HttpContext context)
    {
        return context.Request.IsHttps ||
               string.Equals(context.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetRefreshTokenCookie(HttpContext context, string refreshToken, bool persistent = true)
    {
        var isHttps = IsHttpsRequest(context);
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth"
        };
        if (persistent)
        {
            options.MaxAge = TimeSpan.FromDays(30);
        }
        context.Response.Cookies.Append("refreshToken", refreshToken, options);
    }

    private static void ClearRefreshTokenCookie(HttpContext context)
    {
        var isHttps = IsHttpsRequest(context);
        context.Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth"
        });
    }

    private static string GenerateJwtToken(User user, string secret, string issuer, string audience, int expiryMinutes = 60)
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
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public record RegisterRequest(string Email, string Password, string? Name = null, string? Locale = "en", bool RememberMe = true);
public record LoginRequest(string Email, string Password, bool RememberMe = true);
public record GoogleAuthRequest(string IdToken);
public record VerifyRegistrationRequest(string Email, string Code, bool RememberMe = true);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
public record ResendOtpRequest(string Email, string Purpose);
public record OtpChallengeResponse(string Email);
public record OtpMessageResponse(string Message);
