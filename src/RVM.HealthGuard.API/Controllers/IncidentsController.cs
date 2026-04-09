using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentsController(IServiceIncidentRepository incidentRepo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<IncidentResponse>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var incidents = await incidentRepo.GetAllAsync(from, to, ct);
        return incidents.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> GetById(Guid id, CancellationToken ct)
    {
        var incident = await incidentRepo.GetByIdAsync(id, ct);
        if (incident is null) return NotFound();
        return MapToResponse(incident);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<IncidentResponse>>> GetActive(CancellationToken ct)
    {
        var incidents = await incidentRepo.GetActiveAsync(ct);
        return incidents.Select(MapToResponse).ToList();
    }

    [HttpGet("service/{serviceId:guid}")]
    public async Task<ActionResult<List<IncidentResponse>>> GetByService(Guid serviceId, CancellationToken ct)
    {
        var incidents = await incidentRepo.GetByServiceIdAsync(serviceId, ct);
        return incidents.Select(MapToResponse).ToList();
    }

    private static IncidentResponse MapToResponse(ServiceIncident i) => new(
        i.Id, i.MonitoredServiceId,
        i.MonitoredService?.Name ?? "",
        i.Type.ToString(),
        i.StartedAt, i.ResolvedAt,
        i.Duration?.ToString());
}
