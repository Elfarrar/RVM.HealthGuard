using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;
using RVM.HealthGuard.Infrastructure.Data;

namespace RVM.HealthGuard.Infrastructure.Repositories;

public class ServiceIncidentRepository(HealthGuardDbContext db) : IServiceIncidentRepository
{
    public Task<ServiceIncident?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ServiceIncidents
            .Include(i => i.MonitoredService)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<ServiceIncident>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
        => db.ServiceIncidents
            .Where(i => i.MonitoredServiceId == serviceId)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync(ct);

    public Task<List<ServiceIncident>> GetActiveAsync(CancellationToken ct = default)
        => db.ServiceIncidents
            .Include(i => i.MonitoredService)
            .Where(i => i.ResolvedAt == null)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync(ct);

    public Task<ServiceIncident?> GetActiveByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
        => db.ServiceIncidents
            .Where(i => i.MonitoredServiceId == serviceId && i.ResolvedAt == null)
            .FirstOrDefaultAsync(ct);

    public Task<List<ServiceIncident>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = db.ServiceIncidents
            .Include(i => i.MonitoredService)
            .AsQueryable();

        if (from.HasValue) query = query.Where(i => i.StartedAt >= from.Value);
        if (to.HasValue) query = query.Where(i => i.StartedAt <= to.Value);

        return query.OrderByDescending(i => i.StartedAt).ToListAsync(ct);
    }

    public async Task AddAsync(ServiceIncident incident, CancellationToken ct = default)
    {
        db.ServiceIncidents.Add(incident);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ServiceIncident incident, CancellationToken ct = default)
    {
        db.ServiceIncidents.Update(incident);
        await db.SaveChangesAsync(ct);
    }
}
