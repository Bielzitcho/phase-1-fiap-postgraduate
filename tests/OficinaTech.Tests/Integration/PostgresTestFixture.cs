using Microsoft.EntityFrameworkCore;
using OficinaTech.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace OficinaTech.Tests.Integration;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public OficinaTechDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OficinaTechDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new OficinaTechDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _container.DisposeAsync().AsTask();
}
