using RVM.HealthGuard.Domain.Enums;

namespace RVM.HealthGuard.Domain.Entities;

public class HealthCheckResult
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid MonitoredServiceId { get; set; }
    public ServiceHealthStatus Status { get; set; }
    public int ResponseTimeMs { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public MonitoredService MonitoredService { get; set; } = null!;
}
