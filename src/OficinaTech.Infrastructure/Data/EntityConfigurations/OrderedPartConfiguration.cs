using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Entities;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class OrderedPartConfiguration : IEntityTypeConfiguration<OrderedPart>
{
    public void Configure(EntityTypeBuilder<OrderedPart> builder)
    {
        builder.ToTable("ordered_parts");
        builder.HasKey(op => op.Id);

        builder.Property(op => op.PartId).IsRequired();
        builder.Property(op => op.PartName).HasMaxLength(200).IsRequired();
        builder.Property(op => op.UnitPrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(op => op.Quantity).IsRequired();

        // ServiceOrderId FK is a shadow property configured via ServiceOrderConfiguration
        // OrderedPart extends Entity<Guid> (not AggregateRoot) — no DomainEvents to ignore
    }
}
