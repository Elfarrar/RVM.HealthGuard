using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;

namespace RVM.HealthGuard.Test.Domain;

public class EntityTests
{
    [Fact]
    public void MonitoredService_HasCorrectDefaults()
    {
        var service = new MonitoredService();

        Assert.NotEqual(Guid.Empty, service.Id);
        Assert.True(service.IsEnabled);
        Assert.Equal(30, service.CheckIntervalSeconds);
        Assert.Equal(10, service.TimeoutSeconds);
        Assert.Equal(200, service.ExpectedStatusCode);
        Assert.Empty(service.HealthCheckResults);
        Assert.Empty(service.Incidents);
    }

    [Fact]
    public void MonitoredService_CreatedAt_IsCloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var service = new MonitoredService();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(service.CreatedAt, before, after);
    }

    [Fact]
    public void HealthCheckResult_HasCorrectDefaults()
    {
        var result = new HealthCheckResult();

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ServiceHealthStatus.Healthy, result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void HealthCheckResult_CheckedAt_IsCloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = new HealthCheckResult();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(result.CheckedAt, before, after);
    }

    [Fact]
    public void ServiceIncident_HasCorrectDefaults()
    {
        var incident = new ServiceIncident();

        Assert.NotEqual(Guid.Empty, incident.Id);
        Assert.Null(incident.ResolvedAt);
        Assert.Null(incident.Duration);
    }

    [Fact]
    public void ServiceIncident_StartedAt_IsCloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var incident = new ServiceIncident();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(incident.StartedAt, before, after);
    }
}
