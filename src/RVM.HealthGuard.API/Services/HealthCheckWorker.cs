using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using RVM.HealthGuard.API.Dtos;
using RVM.HealthGuard.API.Hubs;
using RVM.HealthGuard.Domain.Entities;
using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.API.Services;

public class HealthCheckWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IHubContext<HealthStatusHub> hubContext,
    NotifyAlertService notifyAlertService,
    ILogger<HealthCheckWorker> logger) : BackgroundService
{
    private readonly Dictionary<Guid, DateTime> _nextCheckAt = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HealthCheckWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var serviceRepo = scope.ServiceProvider.GetRequiredService<IMonitoredServiceRepository>();
                var resultRepo = scope.ServiceProvider.GetRequiredService<IHealthCheckResultRepository>();
                var incidentRepo = scope.ServiceProvider.GetRequiredService<IServiceIncidentRepository>();

                var services = await serviceRepo.GetEnabledAsync(stoppingToken);

                foreach (var service in services)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    if (_nextCheckAt.TryGetValue(service.Id, out var nextCheck) && DateTime.UtcNow < nextCheck)
                        continue;

                    await CheckServiceAsync(service, resultRepo, incidentRepo, stoppingToken);
                    _nextCheckAt[service.Id] = DateTime.UtcNow.AddSeconds(service.CheckIntervalSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in HealthCheckWorker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("HealthCheckWorker stopped");
    }

    private async Task CheckServiceAsync(
        MonitoredService service,
        IHealthCheckResultRepository resultRepo,
        IServiceIncidentRepository incidentRepo,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        ServiceHealthStatus status;
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(service.TimeoutSeconds);

            var response = await client.GetAsync(service.Url, ct);
            sw.Stop();
            statusCode = (int)response.StatusCode;

            status = statusCode == service.ExpectedStatusCode
                ? ServiceHealthStatus.Healthy
                : ServiceHealthStatus.Degraded;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            status = ServiceHealthStatus.Unhealthy;
            errorMessage = "Request timed out";
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            status = ServiceHealthStatus.Unhealthy;
            errorMessage = ex.Message;
        }

        var result = new HealthCheckResult
        {
            MonitoredServiceId = service.Id,
            Status = status,
            ResponseTimeMs = (int)sw.ElapsedMilliseconds,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
        };

        await resultRepo.AddAsync(result, ct);

        // Incident management
        var activeIncident = await incidentRepo.GetActiveByServiceIdAsync(service.Id, ct);

        if (status != ServiceHealthStatus.Healthy && activeIncident is null)
        {
            var incidentType = status == ServiceHealthStatus.Degraded ? IncidentType.Degraded :
                errorMessage?.Contains("timed out") == true ? IncidentType.Timeout : IncidentType.Down;

            var incident = new ServiceIncident
            {
                MonitoredServiceId = service.Id,
                Type = incidentType,
            };
            await incidentRepo.AddAsync(incident, ct);

            logger.LogWarning("Incident started for {Service}: {Type}", service.Name, incidentType);

            await notifyAlertService.SendIncidentAlertAsync(service.Name, incidentType.ToString(), errorMessage, ct);

            await hubContext.Clients.All.SendAsync("IncidentStarted", new
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                Type = incidentType.ToString(),
                StartedAt = incident.StartedAt,
            }, ct);
        }
        else if (status == ServiceHealthStatus.Healthy && activeIncident is not null)
        {
            activeIncident.ResolvedAt = DateTime.UtcNow;
            activeIncident.Duration = activeIncident.ResolvedAt - activeIncident.StartedAt;
            await incidentRepo.UpdateAsync(activeIncident, ct);

            logger.LogInformation("Incident resolved for {Service}, duration: {Duration}", service.Name, activeIncident.Duration);

            await notifyAlertService.SendResolutionAlertAsync(service.Name, activeIncident.Duration, ct);

            await hubContext.Clients.All.SendAsync("IncidentResolved", new
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                ResolvedAt = activeIncident.ResolvedAt,
                Duration = activeIncident.Duration?.ToString(),
            }, ct);
        }

        // Push status update
        await hubContext.Clients.All.SendAsync("ServiceStatusChanged", new ServiceStatusUpdate(
            service.Id, service.Name, status.ToString(),
            result.ResponseTimeMs, statusCode, errorMessage, result.CheckedAt), ct);
    }
}
