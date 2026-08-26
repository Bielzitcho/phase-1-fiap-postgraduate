using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Enums;
using OficinaTech.Domain.ValueObjects;
using Xunit;

namespace OficinaTech.Tests.Integration;

[Collection("postgres")]
public class ServiceOrderIntegrationTests : IClassFixture<PostgresTestFixture>
{
    private readonly PostgresTestFixture _fixture;

    // Two distinct valid CPFs — each test seeds its own client to avoid IX_clients_tax_id collision
    // since all tests share the same PostgreSQL container (IClassFixture).
    private const string ValidCpf1 = "529.982.247-25";   // used by CreateAndApproveOS test
    private const string ValidCpf2 = "123.456.789-09";   // used by OS_CreatedInDb test

    public ServiceOrderIntegrationTests(PostgresTestFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task CreateAndApproveOS_FullFlow_DecrementStock()
    {
        await using var ctx = _fixture.CreateContext();

        // Seed: client, vehicle, part — each with unique identifiers per test run
        var client = new Client("Test User", new TaxId(ValidCpf1), "test@test.com", "11900000000", "Rua Teste, 1");
        var vehicle = new Vehicle(client.Id, new LicensePlate("ABC-1234"), "Toyota", "Corolla", 2020);
        var part = new Part("Filtro de oleo", 50m, 10, null);
        ctx.Set<Client>().Add(client);
        ctx.Set<Vehicle>().Add(vehicle);
        ctx.Set<Part>().Add(part);
        await ctx.SaveChangesAsync();

        // Create OS via aggregate methods
        var order = new ServiceOrder(client.Id, vehicle.Id);
        order.AddPart(part.Id, part.Name, part.UnitPrice, 3);
        order.StartDiagnosis();
        order.SendForApproval();
        ctx.Set<ServiceOrder>().Add(order);
        await ctx.SaveChangesAsync();

        // Approve — reload part from DB to exercise real DB round-trip and concurrency token
        var dbPart = await ctx.Set<Part>().FindAsync(part.Id);
        dbPart!.DecrementStock(3);
        order.Approve();
        await ctx.SaveChangesAsync();

        // Assert: reload from DB and verify state persisted correctly
        var dbOrder = await ctx.Set<ServiceOrder>().FindAsync(order.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal(ServiceOrderStatus.EmExecucao, dbOrder!.Status);

        var updatedPart = await ctx.Set<Part>().FindAsync(part.Id);
        Assert.NotNull(updatedPart);
        Assert.Equal(7, updatedPart!.StockQuantity);
    }

    [Fact]
    public async Task OS_CreatedInDb_HasStatusRecebida()
    {
        await using var ctx = _fixture.CreateContext();

        // Seed: client and vehicle with distinct CPF and plate to avoid unique-constraint collisions
        // with the other test (both share the same container/schema via IClassFixture).
        var client = new Client("Test User 2", new TaxId(ValidCpf2), "test2@test.com", "11911111111", "Rua Teste, 2");
        var vehicle = new Vehicle(client.Id, new LicensePlate("XYZ-9999"), "Honda", "Civic", 2021);
        ctx.Set<Client>().Add(client);
        ctx.Set<Vehicle>().Add(vehicle);
        await ctx.SaveChangesAsync();

        // Create OS
        var order = new ServiceOrder(client.Id, vehicle.Id);
        ctx.Set<ServiceOrder>().Add(order);
        await ctx.SaveChangesAsync();

        // Assert: newly created OS must have status Recebida
        var dbOrder = await ctx.Set<ServiceOrder>().FindAsync(order.Id);
        Assert.NotNull(dbOrder);
        Assert.Equal(ServiceOrderStatus.Recebida, dbOrder!.Status);
    }
}
