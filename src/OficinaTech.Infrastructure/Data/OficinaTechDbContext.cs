using Microsoft.EntityFrameworkCore;
using OficinaTech.Domain.Aggregates;

namespace OficinaTech.Infrastructure.Data;

public class OficinaTechDbContext : DbContext
{
    public OficinaTechDbContext(DbContextOptions<OficinaTechDbContext> options)
        : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaTechDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
