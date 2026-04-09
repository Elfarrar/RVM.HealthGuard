using RVM.HealthGuard.Domain.Enums;
using RVM.HealthGuard.Domain.Interfaces;

namespace RVM.HealthGuard.API.Services;

public class UptimeCalculatorService(IHealthCheckResultRepository resultRepo)
{
    public async Task<double> CalculateUptimePercentAsync(Guid serviceId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var results = await resultRepo.GetByServiceIdAsync(serviceId, from, to, ct);
        if (results.Count == 0) return 100.0;

        var healthyCount = results.Count(r => r.Status == ServiceHealthStatus.Healthy);
        return Math.Round((double)healthyCount / results.Count * 100, 2);
    }

    public async Task<double> CalculateAverageResponseTimeAsync(Guid serviceId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var results = await resultRepo.GetByServiceIdAsync(serviceId, from, to, ct);
        if (results.Count == 0) return 0;

        return Math.Round(results.Average(r => r.ResponseTimeMs), 2);
    }
}
