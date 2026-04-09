using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RVM.HealthGuard.API.Controllers;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.Test.Controllers;

public class ServicesControllerTests
{
    private readonly Mock<IMonitoredServiceRepository> _repoMock = new();
    private readonly Mock<ILogger<ServicesController>> _loggerMock = new();

    private ServicesController CreateController() =>
        new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        var controller = CreateController();
        var request = new CreateServiceRequest("My API", "https://api.example.com/health");

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ServiceResponse>(created.Value);
        Assert.Equal("My API", response.Name);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<MonitoredService>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateServiceRequest("", "https://api.example.com/health");

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithEmptyUrl_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateServiceRequest("My API", "");

        var result = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WithExistingService_ReturnsOk()
    {
        var service = new MonitoredService { Name = "My API", Url = "https://api.example.com/health" };
        _repoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var controller = CreateController();
        var result = await controller.GetById(service.Id, CancellationToken.None);

        Assert.NotNull(result.Value);
        Assert.Equal("My API", result.Value!.Name);
    }

    [Fact]
    public async Task GetById_WithNonexistentService_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoredService?)null);

        var controller = CreateController();
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_WithExistingService_ReturnsNoContent()
    {
        var service = new MonitoredService { Name = "My API", Url = "https://api.example.com/health" };
        _repoMock.Setup(r => r.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var controller = CreateController();
        var result = await controller.Delete(service.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _repoMock.Verify(r => r.DeleteAsync(service.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonexistentService_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoredService?)null);

        var controller = CreateController();
        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllServices()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MonitoredService { Name = "A", Url = "https://a.com/health" },
                new MonitoredService { Name = "B", Url = "https://b.com/health" },
            ]);

        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);

        Assert.Equal(2, result.Value!.Count);
    }
}
