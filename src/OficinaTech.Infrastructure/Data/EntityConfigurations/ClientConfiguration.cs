using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20);

        builder.Property(c => c.TaxId)
            .HasConversion(
                taxId => taxId.Value,
                value => new TaxId(value))
            .HasColumnName("tax_id")
            .HasMaxLength(14)
            .IsRequired();

        builder.HasIndex(c => c.TaxId).IsUnique();

        // AggregateRoot._domainEvents — never persisted (RESEARCH.md Pitfall 1)
        builder.Ignore(c => c.DomainEvents);
    }
}
