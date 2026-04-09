using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.API.Services;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatusController(
    IMonitoredServiceRepository serviceRepo,
    IHealthCheckResultRepository resultRepo,
    UptimeCalculatorService uptimeCalculator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ServiceStatusResponse>>> GetAll(CancellationToken ct)
    {
        var services = await serviceRepo.GetAllAsync(ct);
        var now = DateTime.UtcNow;
        var yesterday = now.AddHours(-24);

        var statuses = new List<ServiceStatusResponse>();
        foreach (var service in services)
        {
            var latest = await resultRepo.GetLatestByServiceIdAsync(service.Id, ct);
            var uptime = await uptimeCalculator.CalculateUptimePercentAsync(service.Id, yesterday, now, ct);
            var avgResponse = await uptimeCalculator.CalculateAverageResponseTimeAsync(service.Id, yesterday, now, ct);

            statuses.Add(new ServiceStatusResponse(
                service.Id, service.Name,
                latest?.Status.ToString() ?? "Unknown",
                latest?.ResponseTimeMs,
                latest?.CheckedAt,
                uptime, avgResponse));
        }

        return statuses;
    }

    [HttpGet("{serviceId:guid}")]
    public async Task<ActionResult<ServiceStatusResponse>> GetServiceStatus(Guid serviceId, CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(serviceId, ct);
        if (service is null) return NotFound();

        var now = DateTime.UtcNow;
        var yesterday = now.AddHours(-24);

        var latest = await resultRepo.GetLatestByServiceIdAsync(serviceId, ct);
        var uptime = await uptimeCalculator.CalculateUptimePercentAsync(serviceId, yesterday, now, ct);
        var avgResponse = await uptimeCalculator.CalculateAverageResponseTimeAsync(serviceId, yesterday, now, ct);

        return new ServiceStatusResponse(
            service.Id, service.Name,
            latest?.Status.ToString() ?? "Unknown",
            latest?.ResponseTimeMs,
            latest?.CheckedAt,
            uptime, avgResponse);
    }

    [HttpGet("{serviceId:guid}/history")]
    public async Task<ActionResult<List<HealthCheckResultResponse>>> GetHistory(
        Guid serviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var service = await serviceRepo.GetByIdAsync(serviceId, ct);
        if (service is null) return NotFound();

        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddHours(-24);

        var results = await resultRepo.GetByServiceIdAsync(serviceId, fromDate, toDate, ct);

        return results.Select(r => new HealthCheckResultResponse(
            r.Id, r.Status.ToString(), r.ResponseTimeMs,
            r.StatusCode, r.ErrorMessage, r.CheckedAt)).ToList();
    }
}
