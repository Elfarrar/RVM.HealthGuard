using Microsoft.AspNetCore.Mvc;
using Moq;
using RVM.HealthGuard.API.Controllers;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.Test.Controllers;

public class IncidentsControllerTests
{
    private readonly Mock<IServiceIncidentRepository> _repoMock = new();

    private IncidentsController CreateController() =>
        new(_repoMock.Object);

    private static ServiceIncident CreateIncident(
        Guid? serviceId = null,
        IncidentType type = IncidentType.Down,
        bool resolved = false) => new()
        {
            MonitoredServiceId = serviceId ?? Guid.NewGuid(),
            Type = type,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            ResolvedAt = resolved ? DateTime.UtcNow : null,
            Duration = resolved ? TimeSpan.FromMinutes(30) : null,
            MonitoredService = new MonitoredService { Name = "TestService", Url = "https://test.com/health" },
        };

    // -------------------------------------------------------------------------
    // GetAll
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsAllIncidents()
    {
        var incidents = new List<ServiceIncident>
        {
            CreateIncident(type: IncidentType.Down),
            CreateIncident(type: IncidentType.Timeout, resolved: true),
        };
        _repoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidents);

        var controller = CreateController();
        var result = await controller.GetAll(null, null, CancellationToken.None);

        var ok = Assert.IsType<ActionResult<List<IncidentResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetAll_WithDateRange_PassesParametersToRepo()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetAllAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        await controller.GetAll(from, to, CancellationToken.None);

        _repoMock.Verify(r => r.GetAllAsync(from, to, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_MapsIncidentTypeToString()
    {
        _repoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateIncident(type: IncidentType.Degraded)]);

        var controller = CreateController();
        var result = await controller.GetAll(null, null, CancellationToken.None);

        Assert.Contains(result.Value!, r => r.Type == "Degraded");
    }

    // -------------------------------------------------------------------------
    // GetById
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingIncident_ReturnsOk()
    {
        var incident = CreateIncident();
        _repoMock.Setup(r => r.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var controller = CreateController();
        var result = await controller.GetById(incident.Id, CancellationToken.None);

        var value = Assert.IsType<IncidentResponse>(result.Value);
        Assert.Equal(incident.Id, value.Id);
        Assert.Equal("TestService", value.ServiceName);
    }

    [Fact]
    public async Task GetById_NonexistentIncident_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceIncident?)null);

        var controller = CreateController();
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ResolvedIncident_HasDurationAndResolvedAt()
    {
        var incident = CreateIncident(resolved: true);
        _repoMock.Setup(r => r.GetByIdAsync(incident.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        var controller = CreateController();
        var result = await controller.GetById(incident.Id, CancellationToken.None);

        var response = Assert.IsType<IncidentResponse>(result.Value);
        Assert.NotNull(response.ResolvedAt);
        Assert.NotNull(response.Duration);
    }

    // -------------------------------------------------------------------------
    // GetActive
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActive_ReturnsOnlyActiveIncidents()
    {
        _repoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateIncident(), CreateIncident(type: IncidentType.Timeout)]);

        var controller = CreateController();
        var result = await controller.GetActive(CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetActive_Empty_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        var result = await controller.GetActive(CancellationToken.None);

        Assert.Empty(result.Value!);
    }

    // -------------------------------------------------------------------------
    // GetByService
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByService_ReturnsIncidentsForService()
    {
        var serviceId = Guid.NewGuid();
        var incidents = new List<ServiceIncident>
        {
            CreateIncident(serviceId, IncidentType.Down),
            CreateIncident(serviceId, IncidentType.Timeout, resolved: true),
        };
        _repoMock.Setup(r => r.GetByServiceIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incidents);

        var controller = CreateController();
        var result = await controller.GetByService(serviceId, CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value!, r => Assert.Equal(serviceId, r.ServiceId));
    }

    [Fact]
    public async Task GetByService_NoIncidents_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByServiceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = CreateController();
        var result = await controller.GetByService(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result.Value!);
    }

    // -------------------------------------------------------------------------
    // MapToResponse — via GetAll
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MapToResponse_NullServiceName_FallsBackToEmptyString()
    {
        var incident = new ServiceIncident
        {
            MonitoredServiceId = Guid.NewGuid(),
            Type = IncidentType.Down,
            MonitoredService = null!, // navegacao nula
        };
        // Configurar MonitoredService como nulo via propriedade
        // O campo MonitoredService e null!, mas o ?? "" deve proteger
        var incidentWithNullNav = new ServiceIncident
        {
            MonitoredServiceId = Guid.NewGuid(),
            Type = IncidentType.Down,
        };
        // MonitoredService nao inicializado — simula null nav
        typeof(ServiceIncident).GetProperty("MonitoredService")!
            .SetValue(incidentWithNullNav, null);

        _repoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([incidentWithNullNav]);

        var controller = CreateController();
        var result = await controller.GetAll(null, null, CancellationToken.None);

        Assert.Equal("", result.Value![0].ServiceName);
    }
}
