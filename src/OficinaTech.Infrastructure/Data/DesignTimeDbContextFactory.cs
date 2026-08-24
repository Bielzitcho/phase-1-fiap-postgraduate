using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OficinaTech.Infrastructure.Data;

/// <summary>
/// Provides a DbContext instance for dotnet ef design-time tooling (migrations add, migrations list).
/// This removes the dependency on the Presentation host for EF CLI DbContext resolution (RESEARCH.md A4).
/// The connection string here is a design-time literal — no live database is contacted during
/// 'dotnet ef migrations add'. A real connection is only needed for 'dotnet ef database update'.
/// At runtime the Presentation project's appsettings.json / env var overrides this factory entirely.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OficinaTechDbContext>
{
    public OficinaTechDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OficinaTechDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=oficina_tech;Username=oficina;Password=oficina_secret");

        return new OficinaTechDbContext(optionsBuilder.Options);
    }
}
