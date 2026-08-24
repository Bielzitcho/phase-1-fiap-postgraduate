using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("service_types");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.BasePrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);

        // Field-only properties: private accumulators with no public getter
        // Field names must exactly match ServiceType.cs lines 10-11
        builder.Property<int>("_executionCount")
            .HasColumnName("execution_count")
            .HasDefaultValue(0);

        builder.Property<double>("_totalExecutionMinutes")
            .HasColumnName("total_execution_minutes")
            .HasDefaultValue(0.0);

        // AverageExecutionTime is computed in domain — not persisted (RESEARCH.md Pitfall 3)
        builder.Ignore(s => s.AverageExecutionTime);

        // AggregateRoot._domainEvents — never persisted
        builder.Ignore(s => s.DomainEvents);
    }
}
