using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RVM.HealthGuard.Domain.Entities;

namespace RVM.HealthGuard.Infrastructure.Data.Configurations;

public class HealthCheckResultConfiguration : IEntityTypeConfiguration<HealthCheckResult>
{
    public void Configure(EntityTypeBuilder<HealthCheckResult> builder)
    {
        builder.ToTable("health_check_results");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(e => new { e.MonitoredServiceId, e.CheckedAt })
            .IsDescending(false, true);

        builder.HasIndex(e => e.CheckedAt);
    }
}
