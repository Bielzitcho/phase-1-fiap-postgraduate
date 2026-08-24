using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Entities;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class OrderedServiceConfiguration : IEntityTypeConfiguration<OrderedService>
{
    public void Configure(EntityTypeBuilder<OrderedService> builder)
    {
        builder.ToTable("ordered_services");
        builder.HasKey(os => os.Id);

        builder.Property(os => os.ServiceTypeId).IsRequired();
        builder.Property(os => os.ServiceTypeName).HasMaxLength(200).IsRequired();
        builder.Property(os => os.UnitPrice).HasColumnType("numeric(10,2)").IsRequired();

        // ServiceOrderId FK is a shadow property configured via ServiceOrderConfiguration
        // OrderedService extends Entity<Guid> (not AggregateRoot) — no DomainEvents to ignore
    }
}
