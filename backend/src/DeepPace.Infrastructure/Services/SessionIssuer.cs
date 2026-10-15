using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;
using DeepPace.Domain.Enums;
using DeepPace.Infrastructure.Persistence;

namespace DeepPace.Infrastructure.Services;

public class SessionIssuer : ISessionIssuer
{
    private readonly DeepPaceDbContext _dbContext;
    private readonly IRefreshTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public SessionIssuer(
        DeepPaceDbContext dbContext,
        IRefreshTokenService tokenService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public string GenerateJwtToken(User user, int sessionVersion)
    {
        var secret = _configuration["Jwt:Secret"]!;
        var issuer = _configuration["Jwt:Issuer"] ?? "DeepPace";
        var audience = _configuration["Jwt:Audience"] ?? "DeepPaceUsers";
        var expiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("session_version", sessionVersion.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<Result<IssuedSessionDto>> IssueSessionAsync(Guid userId, bool isPersistent = true, CancellationToken ct = default)
    {
        User? user;
        var isNpgsql = _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (isNpgsql && _dbContext.Database.CurrentTransaction != null)
        {
            // Acquire row lock in PostgreSQL
            user = await _dbContext.Users
                .FromSqlRaw("SELECT * FROM \"Users\" WHERE \"Id\" = {0} AND \"IsDeleted\" = false FOR UPDATE", userId)
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
        }

        if (user == null)
        {
            return Error.Custom("AUTH_INVALID_CREDENTIALS", "User not found.");
        }

        if (user.Status == AccountStatus.Suspended)
        {
            return Error.Custom("AUTH_ACCOUNT_SUSPENDED", "Account is suspended.");
        }

        var (rawRefreshToken, tokenEntity) = await _tokenService.IssueTokenAsync(user.Id, isPersistent: isPersistent, ct: ct);
        var accessToken = GenerateJwtToken(user, user.AuthenticationVersion);

        return new IssuedSessionDto(accessToken, rawRefreshToken, tokenEntity, user, user.AuthenticationVersion);
    }

    public async Task<Result<IssuedSessionDto>> RotateSessionAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var rotateResult = await _tokenService.RotateTokenAsync(rawRefreshToken, ct);
        if (!rotateResult.IsSuccess)
        {
            return rotateResult.Error;
        }

        var (newRawToken, newToken, user) = rotateResult.Value;
        var accessToken = GenerateJwtToken(user, newToken.AuthenticationVersion);

        return new IssuedSessionDto(accessToken, newRawToken, newToken, user, newToken.AuthenticationVersion);
    }
}
