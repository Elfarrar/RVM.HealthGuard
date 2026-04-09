using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Infrastructure.Data.Configurations;

public class ServiceIncidentConfiguration : IEntityTypeConfiguration<ServiceIncident>
{
    public void Configure(EntityTypeBuilder<ServiceIncident> builder)
    {
        builder.ToTable("service_incidents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.MonitoredServiceId);
        builder.HasIndex(e => e.StartedAt);
        builder.HasIndex(e => e.ResolvedAt);
    }
}
