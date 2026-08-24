using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UnitPrice).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);

        // AggregateRoot._domainEvents — never persisted
        builder.Ignore(p => p.DomainEvents);
    }
}
