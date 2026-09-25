namespace TechDaily.Application.Interfaces;

public interface IStarterHandbookService
{
    Task ProvisionForUserAsync(Guid userId, CancellationToken ct = default);
}
