using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RVM.HealthGuard.Domain.Interfaces;
using RVM.HealthGuard.Infrastructure.Data;
using RVM.HealthGuard.Infrastructure.Repositories;

namespace RVM.HealthGuard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<HealthGuardDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMonitoredServiceRepository, MonitoredServiceRepository>();
        services.AddScoped<IHealthCheckResultRepository, HealthCheckResultRepository>();
        services.AddScoped<IServiceIncidentRepository, ServiceIncidentRepository>();

        return services;
    }
}
