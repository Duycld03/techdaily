using DeepPace.Application.Common;
using DeepPace.Application.Features.AdminDashboard.DTOs;

namespace DeepPace.Application.Interfaces;

public interface IAdminDashboardReader
{
    Task<Result<AdminDashboardOverviewDto>> ReadAsync(CancellationToken cancellationToken = default);
}
