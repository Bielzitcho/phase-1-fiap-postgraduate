using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.ValueObjects;

namespace OficinaTech.Infrastructure.Data.EntityConfigurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ClientId).IsRequired();
        builder.Property(v => v.Make).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Year).IsRequired();

        builder.Property(v => v.LicensePlate)
            .HasConversion(
                lp => lp.Value,
                value => new LicensePlate(value))
            .HasColumnName("license_plate")
            .HasMaxLength(7)
            .IsRequired();

        builder.HasIndex(v => v.LicensePlate).IsUnique();

        // AggregateRoot._domainEvents — never persisted
        builder.Ignore(v => v.DomainEvents);
    }
}
