using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;
using RVM.HealthGuard.Infrastructure.Data;

namespace RVM.HealthGuard.Infrastructure.Repositories;

public class MonitoredServiceRepository(HealthGuardDbContext db) : IMonitoredServiceRepository
{
    public Task<List<MonitoredService>> GetAllAsync(CancellationToken ct = default)
        => db.MonitoredServices.OrderBy(s => s.Name).ToListAsync(ct);

    public Task<MonitoredService?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.MonitoredServices.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<MonitoredService>> GetEnabledAsync(CancellationToken ct = default)
        => db.MonitoredServices.Where(s => s.IsEnabled).OrderBy(s => s.Name).ToListAsync(ct);

    public async Task AddAsync(MonitoredService service, CancellationToken ct = default)
    {
        db.MonitoredServices.Add(service);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MonitoredService service, CancellationToken ct = default)
    {
        db.MonitoredServices.Update(service);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var service = await db.MonitoredServices.FindAsync([id], ct);
        if (service is not null)
        {
            db.MonitoredServices.Remove(service);
            await db.SaveChangesAsync(ct);
        }
    }
}
