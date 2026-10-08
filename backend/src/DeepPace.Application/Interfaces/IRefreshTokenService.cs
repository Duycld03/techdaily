using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<(string RawToken, RefreshToken TokenEntity)> IssueTokenAsync(Guid userId, Guid? familyId = null, bool isPersistent = true, CancellationToken ct = default);
    Task<Result<(string NewRawToken, RefreshToken NewToken, User User)>> RotateTokenAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeFamilyAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}
