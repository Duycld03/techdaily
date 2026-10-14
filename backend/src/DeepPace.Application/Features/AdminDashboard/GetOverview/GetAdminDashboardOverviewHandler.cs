using DeepPace.Application.Common;
using DeepPace.Application.Features.AdminDashboard.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.AdminDashboard.GetOverview;

public record GetAdminDashboardOverviewRequest;

public class GetAdminDashboardOverviewHandler(IAdminDashboardReader reader)
    : IUseCase<GetAdminDashboardOverviewRequest, AdminDashboardOverviewDto>
{
    public Task<Result<AdminDashboardOverviewDto>> ExecuteAsync(
        GetAdminDashboardOverviewRequest request, CancellationToken cancellationToken = default)
        => reader.ReadAsync(cancellationToken);
}
