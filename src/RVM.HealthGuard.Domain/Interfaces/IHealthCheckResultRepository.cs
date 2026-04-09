using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Domain.Interfaces;

public interface IHealthCheckResultRepository
{
    Task<HealthCheckResult?> GetLatestByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task<List<HealthCheckResult>> GetByServiceIdAsync(Guid serviceId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(HealthCheckResult result, CancellationToken ct = default);
}
