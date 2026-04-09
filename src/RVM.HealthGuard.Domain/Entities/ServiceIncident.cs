using RVM.HealthGuard.Domain.Enums;

namespace RVM.HealthGuard.Domain.Entities;

public class ServiceIncident
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid MonitoredServiceId { get; set; }
    public IncidentType Type { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public TimeSpan? Duration { get; set; }

    public MonitoredService MonitoredService { get; set; } = null!;
}
