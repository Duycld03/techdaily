using DeepPace.Application.Features.AdminDashboard.DTOs;

namespace DeepPace.Application.Interfaces;

public interface IAiHealthProbe
{
    Task<AdminAiProbeDto> ProbeAsync(CancellationToken cancellationToken = default);
}
