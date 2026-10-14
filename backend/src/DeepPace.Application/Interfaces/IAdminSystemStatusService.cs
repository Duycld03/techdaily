using DeepPace.Application.Features.AdminDashboard.DTOs;

namespace DeepPace.Application.Interfaces;

public interface IAdminSystemStatusService
{
    Task<AdminDashboardStatusDto> ObserveAsync(CancellationToken cancellationToken = default);
}
