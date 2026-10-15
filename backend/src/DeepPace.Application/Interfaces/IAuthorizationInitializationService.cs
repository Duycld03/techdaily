using DeepPace.Application.Common;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Interfaces;

public interface IAuthorizationInitializationService
{
    Task<Result<AuthorizationInitialization>> InitializeAsync(
        IEnumerable<Guid> adminUserIds,
        CancellationToken ct = default);

    Task<bool> IsInitializedAsync(CancellationToken ct = default);
}
