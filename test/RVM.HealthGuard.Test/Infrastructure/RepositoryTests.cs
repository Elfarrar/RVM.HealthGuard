using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Infrastructure.Data;
using RVM.HealthGuard.Infrastructure.Repositories;

namespace RVM.HealthGuard.Test.Infrastructure;

public class RepositoryTests : IDisposable
{
    private readonly HealthGuardDbContext _db;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<HealthGuardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HealthGuardDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private MonitoredService CreateService(string name = "TestService", bool enabled = true) => new()
    {
        Name = name,
        Url = $"https://{name.ToLower()}.example.com/health",
        IsEnabled = enabled,
    };

    // MonitoredServiceRepository

    [Fact]
    public async Task MonitoredService_AddAndGetById()
    {
        var repo = new MonitoredServiceRepository(_db);
        var service = CreateService();

        await repo.AddAsync(service);
        var found = await repo.GetByIdAsync(service.Id);

        Assert.NotNull(found);
        Assert.Equal("TestService", found.Name);
    }

    [Fact]
    public async Task MonitoredService_GetById_ReturnsNull_WhenNotFound()
    {
        var repo = new MonitoredServiceRepository(_db);
        var found = await repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(found);
    }

    [Fact]
    public async Task MonitoredService_GetAll()
    {
        var repo = new MonitoredServiceRepository(_db);
        await repo.AddAsync(CreateService("A"));
        await repo.AddAsync(CreateService("B"));

        var all = await repo.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task MonitoredService_GetEnabled_FiltersDisabled()
    {
        var repo = new MonitoredServiceRepository(_db);
        await repo.AddAsync(CreateService("Enabled", true));
        await repo.AddAsync(CreateService("Disabled", false));

        var enabled = await repo.GetEnabledAsync();
        Assert.Single(enabled);
        Assert.Equal("Enabled", enabled[0].Name);
    }

    [Fact]
    public async Task MonitoredService_Update()
    {
        var repo = new MonitoredServiceRepository(_db);
        var service = CreateService();
        await repo.AddAsync(service);

        service.Name = "Updated";
        await repo.UpdateAsync(service);

        var found = await repo.GetByIdAsync(service.Id);
        Assert.Equal("Updated", found!.Name);
    }

    [Fact]
    public async Task MonitoredService_Delete()
    {
        var repo = new MonitoredServiceRepository(_db);
        var service = CreateService();
        await repo.AddAsync(service);

        await repo.DeleteAsync(service.Id);
        var found = await repo.GetByIdAsync(service.Id);
        Assert.Null(found);
    }

    // HealthCheckResultRepository

    [Fact]
    public async Task HealthCheckResult_AddAndGetLatest()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var resultRepo = new HealthCheckResultRepository(_db);

        var service = CreateService();
        await serviceRepo.AddAsync(service);

        var older = new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 50,
            StatusCode = 200,
            CheckedAt = DateTime.UtcNow.AddMinutes(-10),
        };
        var newer = new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Unhealthy,
            ResponseTimeMs = 5000,
            ErrorMessage = "Timeout",
            CheckedAt = DateTime.UtcNow,
        };

        await resultRepo.AddAsync(older);
        await resultRepo.AddAsync(newer);

        var latest = await resultRepo.GetLatestByServiceIdAsync(service.Id);
        Assert.NotNull(latest);
        Assert.Equal(ServiceHealthStatus.Unhealthy, latest.Status);
    }

    [Fact]
    public async Task HealthCheckResult_GetByServiceId_FiltersByDateRange()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var resultRepo = new HealthCheckResultRepository(_db);

        var service = CreateService();
        await serviceRepo.AddAsync(service);

        var now = DateTime.UtcNow;
        await resultRepo.AddAsync(new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 50,
            CheckedAt = now.AddHours(-2),
        });
        await resultRepo.AddAsync(new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 60,
            CheckedAt = now.AddHours(-1),
        });
        await resultRepo.AddAsync(new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 70,
            CheckedAt = now.AddDays(-2),
        });

        var results = await resultRepo.GetByServiceIdAsync(service.Id, now.AddHours(-3), now);
        Assert.Equal(2, results.Count);
    }

    // ServiceIncidentRepository

    [Fact]
    public async Task ServiceIncident_AddAndGetActive()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var incidentRepo = new ServiceIncidentRepository(_db);

        var service = CreateService();
        await serviceRepo.AddAsync(service);

        var incident = new ServiceIncident
        {
            MonitoredServiceId = service.Id,
            Type = IncidentType.Down,
        };
        await incidentRepo.AddAsync(incident);

        var active = await incidentRepo.GetActiveAsync();
        Assert.Single(active);
        Assert.Equal(IncidentType.Down, active[0].Type);
    }

    [Fact]
    public async Task ServiceIncident_GetActiveByServiceId()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var incidentRepo = new ServiceIncidentRepository(_db);

        var service = CreateService();
        await serviceRepo.AddAsync(service);

        await incidentRepo.AddAsync(new ServiceIncident
        {
            MonitoredServiceId = service.Id,
            Type = IncidentType.Down,
        });

        var active = await incidentRepo.GetActiveByServiceIdAsync(service.Id);
        Assert.NotNull(active);

        // Resolve it
        active.ResolvedAt = DateTime.UtcNow;
        active.Duration = active.ResolvedAt - active.StartedAt;
        await incidentRepo.UpdateAsync(active);

        var stillActive = await incidentRepo.GetActiveByServiceIdAsync(service.Id);
        Assert.Null(stillActive);
    }

    [Fact]
    public async Task ServiceIncident_GetByServiceId()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var incidentRepo = new ServiceIncidentRepository(_db);

        var service = CreateService();
        await serviceRepo.AddAsync(service);

        await incidentRepo.AddAsync(new ServiceIncident
        {
            MonitoredServiceId = service.Id,
            Type = IncidentType.Down,
            ResolvedAt = DateTime.UtcNow,
        });
        await incidentRepo.AddAsync(new ServiceIncident
        {
            MonitoredServiceId = service.Id,
            Type = IncidentType.Timeout,
        });

        var incidents = await incidentRepo.GetByServiceIdAsync(service.Id);
        Assert.Equal(2, incidents.Count);
    }
}
