using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Domain.Interfaces;

public interface IMonitoredServiceRepository
{
    Task<MonitoredService?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<MonitoredService>> GetAllAsync(CancellationToken ct = default);
    Task<List<MonitoredService>> GetEnabledAsync(CancellationToken ct = default);
    Task AddAsync(MonitoredService service, CancellationToken ct = default);
    Task UpdateAsync(MonitoredService service, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
