using Mapster;
using NSubstitute;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;
using OficinaTech.Application.Mapping;
using OficinaTech.Application.Services;
using OficinaTech.Domain.Aggregates;
using OficinaTech.Domain.Repositories;
using Xunit;

namespace OficinaTech.Tests.Services;

public class PartServiceTests
{
    private readonly IPartRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly PartService _service;

    public PartServiceTests()
    {
        MappingConfig.Register();

        _repo = Substitute.For<IPartRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _service = new PartService(_repo, _uow);
    }

    private static Part MakePart()
        => new Part("Spark Plug", 15.99m, 50, "NGK iridium");

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccessWithPartResponse()
    {
        var req = new CreatePartRequest("Spark Plug", 15.99m, 50, "NGK iridium");
        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Spark Plug", result.Value!.Name);
        Assert.Equal(15.99m, result.Value.UnitPrice);
        Assert.Equal(50, result.Value.StockQuantity);
        Assert.Equal("NGK iridium", result.Value.Description);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutDescription_ReturnsSuccessWithNullDescription()
    {
        var req = new CreatePartRequest("Oil Filter", 25m, 20);
        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Description);
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Part?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Part {id} not found.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var part = MakePart();
        _repo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);

        var result = await _service.GetByIdAsync(part.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(part.Id, result.Value!.Id);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — pageSize clamped
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenPageSizeOver100_ClampsTo100()
    {
        var parts = new List<Part>().AsReadOnly();
        _repo.GetAllAsync(1, 100, Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(((IReadOnlyList<Part>)parts, 0)));

        var result = await _service.GetAllAsync(1, 500);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.PageSize);
        await _repo.Received(1).GetAllAsync(1, 100, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Part?)null);

        var req = new UpdatePartRequest("Oil Filter", 25m, 20);
        var result = await _service.UpdateAsync(id, req);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Part {id} not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndCallsCommitOnce()
    {
        var part = MakePart();
        _repo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);

        var req = new UpdatePartRequest("Oil Filter", 25m, 100, "Heavy-duty");
        var result = await _service.UpdateAsync(part.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Equal("Oil Filter", result.Value!.Name);
        Assert.Equal(25m, result.Value.UnitPrice);
        Assert.Equal(100, result.Value.StockQuantity);
        Assert.Equal("Heavy-duty", result.Value.Description);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // PUT semantics: omitting Description from the request body clears the existing value.
    // This is intentional full-replacement behavior — callers must supply Description
    // to preserve it. Use PATCH for partial updates if needed in the future.
    [Fact]
    public async Task UpdateAsync_WithNullDescription_ClearsExistingDescription()
    {
        var part = MakePart(); // created with "NGK iridium"
        _repo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);

        // Omitting Description (null) — PUT full-replacement semantics clear the field
        var req = new UpdatePartRequest("Spark Plug", 15.99m, 50, null);
        var result = await _service.UpdateAsync(part.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Description);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Part?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"Part {id} not found.", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ReturnsSuccessAndCallsCommitOnce()
    {
        var part = MakePart();
        _repo.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);

        var result = await _service.DeleteAsync(part.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
