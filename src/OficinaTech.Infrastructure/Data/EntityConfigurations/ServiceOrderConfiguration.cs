using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Entities;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("service_orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ClientId).IsRequired();
        builder.Property(o => o.VehicleId).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.FinalizationDate);

        // Status with private setter — EF Core handles private setters automatically
        // Stored as string for readability; max 30 chars covers all enum value names
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Private backing field _orderedServices — field name matches ServiceOrder.cs line 15
        // HasForeignKey is explicit to ensure shadow property name is 'ServiceOrderId' (RESEARCH.md Pitfall 4)
        builder.HasMany<OrderedService>("_orderedServices")
            .WithOne()
            .HasForeignKey("ServiceOrderId")
            .IsRequired();

        // UsePropertyAccessMode on the navigation, NOT the entity type builder (RESEARCH.md Pitfall 9)
        builder.Navigation("_orderedServices")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Private backing field _orderedParts — field name matches ServiceOrder.cs line 16
        builder.HasMany<OrderedPart>("_orderedParts")
            .WithOne()
            .HasForeignKey("ServiceOrderId")
            .IsRequired();

        builder.Navigation("_orderedParts")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // TotalAmount is computed from child collections — never persisted (RESEARCH.md Pitfall 2)
        builder.Ignore(o => o.TotalAmount);

        // AggregateRoot._domainEvents — never persisted
        builder.Ignore(o => o.DomainEvents);
    }
}
