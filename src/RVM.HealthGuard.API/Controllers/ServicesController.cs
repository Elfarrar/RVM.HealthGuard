using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController(
    IMonitoredServiceRepository serviceRepo,
    ILogger<ServicesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ServiceResponse>>> GetAll(CancellationToken ct)
    {
        var services = await serviceRepo.GetAllAsync(ct);
        return services.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(id, ct);
        if (service is null) return NotFound();
        return MapToResponse(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> Create(CreateServiceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { error = "Url is required." });

        var service = new MonitoredService
        {
            Name = request.Name,
            Url = request.Url,
            CheckIntervalSeconds = request.CheckIntervalSeconds,
            TimeoutSeconds = request.TimeoutSeconds,
            ExpectedStatusCode = request.ExpectedStatusCode,
            IsEnabled = request.IsEnabled,
        };

        await serviceRepo.AddAsync(service, ct);
        logger.LogInformation("Created monitored service {Name} ({Id})", service.Name, service.Id);

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, MapToResponse(service));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResponse>> Update(Guid id, UpdateServiceRequest request, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        if (request.Name is not null) service.Name = request.Name;
        if (request.Url is not null) service.Url = request.Url;
        if (request.CheckIntervalSeconds.HasValue) service.CheckIntervalSeconds = request.CheckIntervalSeconds.Value;
        if (request.TimeoutSeconds.HasValue) service.TimeoutSeconds = request.TimeoutSeconds.Value;
        if (request.ExpectedStatusCode.HasValue) service.ExpectedStatusCode = request.ExpectedStatusCode.Value;
        if (request.IsEnabled.HasValue) service.IsEnabled = request.IsEnabled.Value;

        await serviceRepo.UpdateAsync(service, ct);
        return MapToResponse(service);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(id, ct);
        if (service is null) return NotFound();

        await serviceRepo.DeleteAsync(id, ct);
        logger.LogInformation("Deleted monitored service {Name} ({Id})", service.Name, id);
        return NoContent();
    }

    private static ServiceResponse MapToResponse(MonitoredService s) => new(
        s.Id, s.Name, s.Url, s.CheckIntervalSeconds, s.TimeoutSeconds,
        s.ExpectedStatusCode, s.IsEnabled, s.CreatedAt, s.UpdatedAt);
}
