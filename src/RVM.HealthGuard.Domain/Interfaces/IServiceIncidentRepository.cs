using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Domain.Interfaces;

public interface IServiceIncidentRepository
{
    Task<ServiceIncident?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ServiceIncident>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<List<ServiceIncident>> GetActiveAsync(CancellationToken ct = default);
    Task<ServiceIncident?> GetActiveByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<List<ServiceIncident>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task AddAsync(ServiceIncident incident, CancellationToken ct = default);
    Task UpdateAsync(ServiceIncident incident, CancellationToken ct = default);
}
