using TechDaily.Application.Common;
using TechDaily.Domain.Entities;

namespace TechDaily.Application.Interfaces;

public interface IRefreshTokenService
{
    Task<(string RawToken, RefreshToken TokenEntity)> IssueTokenAsync(Guid userId, Guid? familyId = null, CancellationToken ct = default);
    Task<Result<(string NewRawToken, RefreshToken NewToken, User User)>> RotateTokenAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeFamilyAsync(string rawRefreshToken, CancellationToken ct = default);
}
