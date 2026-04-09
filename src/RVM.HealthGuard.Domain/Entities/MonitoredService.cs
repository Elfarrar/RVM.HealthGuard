namespace RVM.HealthGuard.Domain.Entities;

public class MonitoredService
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int CheckIntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
    public int ExpectedStatusCode { get; set; } = 200;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<HealthCheckResult> HealthCheckResults { get; set; } = [];
    public ICollection<ServiceIncident> Incidents { get; set; } = [];
}
