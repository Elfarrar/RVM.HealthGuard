using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Infrastructure.Data;

public class HealthGuardDbContext(DbContextOptions<HealthGuardDbContext> options) : DbContext(options)
{
    public DbSet<MonitoredService> MonitoredServices => Set<MonitoredService>();
    public DbSet<HealthCheckResult> HealthCheckResults => Set<HealthCheckResult>();
    public DbSet<ServiceIncident> ServiceIncidents => Set<ServiceIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthGuardDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<MonitoredService>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
