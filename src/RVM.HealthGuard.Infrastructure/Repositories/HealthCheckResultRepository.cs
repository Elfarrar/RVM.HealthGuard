using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;
using RVM.HealthGuard.Infrastructure.Data;

namespace RVM.HealthGuard.Infrastructure.Repositories;

public class HealthCheckResultRepository(HealthGuardDbContext db) : IHealthCheckResultRepository
{
    public Task<HealthCheckResult?> GetLatestByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
        => db.HealthCheckResults
            .Where(r => r.MonitoredServiceId == serviceId)
            .OrderByDescending(r => r.CheckedAt)
            .FirstOrDefaultAsync(ct);

    public Task<List<HealthCheckResult>> GetByServiceIdAsync(Guid serviceId, DateTime from, DateTime to, CancellationToken ct = default)
        => db.HealthCheckResults
            .Where(r => r.MonitoredServiceId == serviceId && r.CheckedAt >= from && r.CheckedAt <= to)
            .OrderByDescending(r => r.CheckedAt)
            .ToListAsync(ct);

    public async Task AddAsync(HealthCheckResult result, CancellationToken ct = default)
    {
        db.HealthCheckResults.Add(result);
        await db.SaveChangesAsync(ct);
    }
}
