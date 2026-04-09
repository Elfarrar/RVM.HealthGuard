using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Infrastructure.Data.Configurations;

public class MonitoredServiceConfiguration : IEntityTypeConfiguration<MonitoredService>
{
    public void Configure(EntityTypeBuilder<MonitoredService> builder)
    {
        builder.ToTable("monitored_services");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Url).IsRequired().HasMaxLength(500);

        builder.HasIndex(e => e.IsEnabled);

        builder.HasMany(e => e.HealthCheckResults)
            .WithOne(r => r.MonitoredService)
            .HasForeignKey(r => r.MonitoredServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Incidents)
            .WithOne(i => i.MonitoredService)
            .HasForeignKey(i => i.MonitoredServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
