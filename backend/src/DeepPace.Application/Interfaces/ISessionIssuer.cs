using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public record IssuedSessionDto(
    string AccessToken,
    string RawRefreshToken,
    RefreshToken RefreshToken,
    User User,
    int SessionVersion);

public interface ISessionIssuer
{
    Task<Result<IssuedSessionDto>> IssueSessionAsync(Guid userId, bool isPersistent = true, CancellationToken ct = default);
    Task<Result<IssuedSessionDto>> RotateSessionAsync(string rawRefreshToken, CancellationToken ct = default);
}
