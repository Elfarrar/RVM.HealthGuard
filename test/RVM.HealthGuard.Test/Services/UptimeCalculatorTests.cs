using Microsoft.EntityFrameworkCore;
using RVM.HealthGuard.API.Services;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Infrastructure.Data;
using RVM.HealthGuard.Infrastructure.Repositories;

namespace RVM.HealthGuard.Test.Services;

public class UptimeCalculatorTests : IDisposable
{
    private readonly HealthGuardDbContext _db;

    public UptimeCalculatorTests()
    {
        var options = new DbContextOptionsBuilder<HealthGuardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HealthGuardDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CalculateUptimePercent_WithNoResults_Returns100()
    {
        var resultRepo = new HealthCheckResultRepository(_db);
        var calculator = new UptimeCalculatorService(resultRepo);

        var uptime = await calculator.CalculateUptimePercentAsync(
            Guid.NewGuid(), DateTime.UtcNow.AddHours(-24), DateTime.UtcNow);

        Assert.Equal(100.0, uptime);
    }

    [Fact]
    public async Task CalculateUptimePercent_ReturnsCorrectPercentage()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var resultRepo = new HealthCheckResultRepository(_db);
        var calculator = new UptimeCalculatorService(resultRepo);

        var service = new MonitoredService { Name = "Test", Url = "http://test.com/health" };
        await serviceRepo.AddAsync(service);

        var now = DateTime.UtcNow;
        // 8 healthy, 2 unhealthy = 80%
        for (var i = 0; i < 8; i++)
        {
            await resultRepo.AddAsync(new HealthCheckResult
            {
                MonitoredServiceId = service.Id,
                Status = ServiceHealthStatus.Healthy,
                ResponseTimeMs = 50,
                CheckedAt = now.AddMinutes(-i),
            });
        }
        for (var i = 0; i < 2; i++)
        {
            await resultRepo.AddAsync(new HealthCheckResult
            {
                MonitoredServiceId = service.Id,
                Status = ServiceHealthStatus.Unhealthy,
                ResponseTimeMs = 5000,
                CheckedAt = now.AddMinutes(-8 - i),
            });
        }

        var uptime = await calculator.CalculateUptimePercentAsync(service.Id, now.AddHours(-1), now);

        Assert.Equal(80.0, uptime);
    }

    [Fact]
    public async Task CalculateAverageResponseTime_ReturnsCorrectAverage()
    {
        var serviceRepo = new MonitoredServiceRepository(_db);
        var resultRepo = new HealthCheckResultRepository(_db);
        var calculator = new UptimeCalculatorService(resultRepo);

        var service = new MonitoredService { Name = "Test", Url = "http://test.com/health" };
        await serviceRepo.AddAsync(service);

        var now = DateTime.UtcNow;
        await resultRepo.AddAsync(new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 100,
            CheckedAt = now.AddMinutes(-2),
        });
        await resultRepo.AddAsync(new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = ServiceHealthStatus.Healthy,
            ResponseTimeMs = 200,
            CheckedAt = now.AddMinutes(-1),
        });

        var avg = await calculator.CalculateAverageResponseTimeAsync(service.Id, now.AddHours(-1), now);

        Assert.Equal(150.0, avg);
    }

    [Fact]
    public async Task CalculateAverageResponseTime_WithNoResults_Returns0()
    {
        var resultRepo = new HealthCheckResultRepository(_db);
        var calculator = new UptimeCalculatorService(resultRepo);

        var avg = await calculator.CalculateAverageResponseTimeAsync(
            Guid.NewGuid(), DateTime.UtcNow.AddHours(-24), DateTime.UtcNow);

        Assert.Equal(0, avg);
    }
}
