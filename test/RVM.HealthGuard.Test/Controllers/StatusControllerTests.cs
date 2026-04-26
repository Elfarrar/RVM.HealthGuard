using Microsoft.AspNetCore.Mvc;
using Moq;
using RVM.HealthGuard.API.Controllers;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.API.Services;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.Test.Controllers;

public class StatusControllerTests
{
    private readonly Mock<IMonitoredServiceRepository> _serviceRepoMock = new();
    private readonly Mock<IHealthCheckResultRepository> _resultRepoMock = new();
    private readonly Mock<IHealthCheckResultRepository> _uptimeResultRepoMock = new();

    // UptimeCalculatorService depende de IHealthCheckResultRepository (real, nao mock)
    private UptimeCalculatorService CreateUptimeCalculator(Mock<IHealthCheckResultRepository>? resultRepo = null) =>
        new((resultRepo ?? _resultRepoMock).Object);

    private StatusController CreateController(UptimeCalculatorService? calc = null) =>
        new(_serviceRepoMock.Object, _resultRepoMock.Object, calc ?? CreateUptimeCalculator());

    private static MonitoredService CreateService(string name = "API", bool enabled = true) => new()
    {
        Name = name,
        Url = $"https://{name.ToLower()}.example.com/health",
        IsEnabled = enabled,
    };

    private static HealthCheckResult CreateResult(
        Guid serviceId,
        ServiceHealthStatus status = ServiceHealthStatus.Healthy,
        int responseMs = 100) => new()
        {
            MonitoredServiceId = serviceId,
            Status = status,
            ResponseTimeMs = responseMs,
            StatusCode = 200,
            CheckedAt = DateTime.UtcNow,
        };

    // -------------------------------------------------------------------------
    // GetAll
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithNoServices_ReturnsEmptyList()
    {
        _serviceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAll_WithServices_ReturnsStatusForEach()
    {
        var svc1 = CreateService("Api1");
        var svc2 = CreateService("Api2");

        _serviceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([svc1, svc2]);

        _resultRepoMock.Setup(r => r.GetLatestByServiceIdAsync(svc1.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResult(svc1.Id, ServiceHealthStatus.Healthy, 50));
        _resultRepoMock.Setup(r => r.GetLatestByServiceIdAsync(svc2.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResult(svc2.Id, ServiceHealthStatus.Unhealthy, 5000));

        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetAll_NoLatestResult_ShowsUnknownStatus()
    {
        var svc = CreateService();
        _serviceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([svc]);

        _resultRepoMock.Setup(r => r.GetLatestByServiceIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthCheckResult?)null);
        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);

        Assert.Equal("Unknown", result.Value![0].CurrentStatus);
    }

    // -------------------------------------------------------------------------
    // GetServiceStatus
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetServiceStatus_NotFound_ReturnsNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoredService?)null);

        var controller = CreateController();
        var result = await controller.GetServiceStatus(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetServiceStatus_Found_ReturnsStatusResponse()
    {
        var svc = CreateService("MyService");
        _serviceRepoMock.Setup(r => r.GetByIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(svc);

        _resultRepoMock.Setup(r => r.GetLatestByServiceIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResult(svc.Id, ServiceHealthStatus.Healthy, 80));
        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(svc.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateResult(svc.Id)]);

        var controller = CreateController();
        var result = await controller.GetServiceStatus(svc.Id, CancellationToken.None);

        var response = Assert.IsType<ServiceStatusResponse>(result.Value);
        Assert.Equal("MyService", response.ServiceName);
        Assert.Equal("Healthy", response.CurrentStatus);
    }

    [Fact]
    public async Task GetServiceStatus_AllHealthy_Uptime100()
    {
        var svc = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(svc);

        var results = new List<HealthCheckResult>
        {
            CreateResult(svc.Id, ServiceHealthStatus.Healthy, 100),
            CreateResult(svc.Id, ServiceHealthStatus.Healthy, 200),
        };
        _resultRepoMock.Setup(r => r.GetLatestByServiceIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results[^1]);
        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(svc.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var controller = CreateController();
        var result = await controller.GetServiceStatus(svc.Id, CancellationToken.None);

        Assert.Equal(100.0, result.Value!.UptimePercent24h);
    }

    // -------------------------------------------------------------------------
    // GetHistory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHistory_ServiceNotFound_ReturnsNotFound()
    {
        _serviceRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoredService?)null);

        var controller = CreateController();
        var result = await controller.GetHistory(Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetHistory_Found_ReturnsCheckResults()
    {
        var svc = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(svc);

        var results = new List<HealthCheckResult>
        {
            CreateResult(svc.Id, ServiceHealthStatus.Healthy, 50),
            CreateResult(svc.Id, ServiceHealthStatus.Unhealthy, 3000),
        };
        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(svc.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var controller = CreateController();
        var result = await controller.GetHistory(svc.Id, null, null, CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetHistory_WithDateRange_PassesCorrectRange()
    {
        var svc = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(svc);

        var from = DateTime.UtcNow.AddHours(-12);
        var to = DateTime.UtcNow;

        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(svc.Id, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        await controller.GetHistory(svc.Id, from, to, CancellationToken.None);

        _resultRepoMock.Verify(r => r.GetByServiceIdAsync(svc.Id, from, to, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistory_MapsFieldsCorrectly()
    {
        var svc = CreateService();
        _serviceRepoMock.Setup(r => r.GetByIdAsync(svc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(svc);

        var checkResult = new HealthCheckResult
        {
            MonitoredServiceId = svc.Id,
            Status = ServiceHealthStatus.Degraded,
            ResponseTimeMs = 1500,
            StatusCode = 503,
            ErrorMessage = "Slow response",
            CheckedAt = DateTime.UtcNow,
        };
        _resultRepoMock.Setup(r => r.GetByServiceIdAsync(svc.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([checkResult]);

        var controller = CreateController();
        var result = await controller.GetHistory(svc.Id, null, null, CancellationToken.None);

        var item = Assert.Single(result.Value!);
        Assert.Equal("Degraded", item.Status);
        Assert.Equal(1500, item.ResponseTimeMs);
        Assert.Equal(503, item.StatusCode);
        Assert.Equal("Slow response", item.ErrorMessage);
    }
}
