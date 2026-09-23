using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;

namespace TechDaily.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ITechDailyDbContext _dbContext;

    public RefreshTokenService(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public async Task<(string RawToken, RefreshToken TokenEntity)> IssueTokenAsync(
        Guid userId,
        Guid? familyId = null,
        CancellationToken ct = default)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(token, ct);
        await _dbContext.SaveChangesAsync(ct);

        return (rawToken, token);
    }

    public async Task<Result<(string NewRawToken, RefreshToken NewToken, User User)>> RotateTokenAsync(
        string rawRefreshToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Error.Custom("AUTH_INVALID_CREDENTIALS", "Refresh token is required.");
        }

        var hash = HashToken(rawRefreshToken);
        var now = DateTimeOffset.UtcNow;

        var token = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (token == null)
        {
            return Error.Custom("AUTH_INVALID_CREDENTIALS", "Invalid refresh token.");
        }

        if (token.RevokedAt != null)
        {
            return Error.Custom("AUTH_INVALID_CREDENTIALS", "Refresh token has been revoked.");
        }

        if (token.ExpiresAt <= now)
        {
            return Error.Custom("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token has expired.");
        }

        // Check if token was already used
        if (token.UsedAt != null)
        {
            // 10-second multi-tab grace window
            if (now - token.UsedAt.Value <= TimeSpan.FromSeconds(10) && token.ReplacedByTokenId.HasValue)
            {
                var successor = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(r => r.Id == token.ReplacedByTokenId.Value, ct);

                if (successor != null && successor.RevokedAt == null && successor.ExpiresAt > now)
                {
                    // Return valid successor context for concurrent tab
                    return (rawRefreshToken, successor, token.User!);
                }
            }

            // Outside grace window: Reuse detected -> revoke entire family!
            var familyTokens = await _dbContext.RefreshTokens
                .Where(r => r.FamilyId == token.FamilyId && r.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var t in familyTokens)
            {
                t.RevokedAt = now;
                t.MarkUpdated();
            }
            await _dbContext.SaveChangesAsync(ct);

            return Error.Custom("AUTH_TOKEN_REUSE_DETECTED", "Refresh token reuse detected. Session terminated.");
        }

        // Active unused token: Issue successor and rotate
        var (newRawToken, newToken) = await IssueTokenAsync(token.UserId, token.FamilyId, ct);

        token.UsedAt = now;
        token.ReplacedByTokenId = newToken.Id;
        token.MarkUpdated();

        await _dbContext.SaveChangesAsync(ct);

        return (newRawToken, newToken, token.User!);
    }

    public async Task RevokeFamilyAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var hash = HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (token == null) return;

        var familyTokens = await _dbContext.RefreshTokens
            .Where(r => r.FamilyId == token.FamilyId && r.RevokedAt == null)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var t in familyTokens)
        {
            t.RevokedAt = now;
            t.MarkUpdated();
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
