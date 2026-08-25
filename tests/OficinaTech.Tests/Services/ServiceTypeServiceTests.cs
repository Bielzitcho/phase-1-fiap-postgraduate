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

public class ServiceTypeServiceTests
{
    private readonly IServiceTypeRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ServiceTypeService _service;

    public ServiceTypeServiceTests()
    {
        MappingConfig.Register();

        _repo = Substitute.For<IServiceTypeRepository>();
        _uow = Substitute.For<IUnitOfWork>();
        _service = new ServiceTypeService(_repo, _uow);
    }

    private static ServiceType MakeServiceType()
        => new ServiceType("Oil Change", 100m, "Full synthetic oil change");

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccessWithServiceTypeResponse()
    {
        var req = new CreateServiceTypeRequest("Oil Change", 100m, "Full synthetic oil change");
        var result = await _service.CreateAsync(req);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Oil Change", result.Value!.Name);
        Assert.Equal(100m, result.Value.BasePrice);
        Assert.Equal("Full synthetic oil change", result.Value.Description);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithoutDescription_ReturnsSuccessWithNullDescription()
    {
        var req = new CreateServiceTypeRequest("Tire Rotation", 50m);
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
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ServiceType?)null);

        var result = await _service.GetByIdAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"ServiceType {id} not found.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsSuccess()
    {
        var st = MakeServiceType();
        _repo.GetByIdAsync(st.Id, Arg.Any<CancellationToken>()).Returns(st);

        var result = await _service.GetByIdAsync(st.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(st.Id, result.Value!.Id);
    }

    // -----------------------------------------------------------------------
    // GetAllAsync — pageSize clamped
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenPageSizeOver100_ClampsTo100()
    {
        var serviceTypes = new List<ServiceType>().AsReadOnly();
        _repo.GetAllAsync(1, 100, Arg.Any<CancellationToken>())
             .Returns(Task.FromResult(((IReadOnlyList<ServiceType>)serviceTypes, 0)));

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
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ServiceType?)null);

        var req = new UpdateServiceTypeRequest("New Name", 200m);
        var result = await _service.UpdateAsync(id, req);

        Assert.False(result.IsSuccess);
        Assert.Equal($"ServiceType {id} not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndCallsCommitOnce()
    {
        var st = MakeServiceType();
        _repo.GetByIdAsync(st.Id, Arg.Any<CancellationToken>()).Returns(st);

        var req = new UpdateServiceTypeRequest("Tire Rotation", 75m, "New description");
        var result = await _service.UpdateAsync(st.Id, req);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tire Rotation", result.Value!.Name);
        Assert.Equal(75m, result.Value.BasePrice);
        Assert.Equal("New description", result.Value.Description);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // PUT semantics: omitting Description from the request body clears the existing value.
    // This is intentional full-replacement behavior — callers must supply Description
    // to preserve it. Use PATCH for partial updates if needed in the future.
    [Fact]
    public async Task UpdateAsync_WithNullDescription_ClearsExistingDescription()
    {
        var st = MakeServiceType(); // created with "Full synthetic oil change"
        _repo.GetByIdAsync(st.Id, Arg.Any<CancellationToken>()).Returns(st);

        // Omitting Description (null) — PUT full-replacement semantics clear the field
        var req = new UpdateServiceTypeRequest("Oil Change", 100m, null);
        var result = await _service.UpdateAsync(st.Id, req);

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
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ServiceType?)null);

        var result = await _service.DeleteAsync(id);

        Assert.False(result.IsSuccess);
        Assert.Equal($"ServiceType {id} not found.", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ReturnsSuccessAndCallsCommitOnce()
    {
        var st = MakeServiceType();
        _repo.GetByIdAsync(st.Id, Arg.Any<CancellationToken>()).Returns(st);

        var result = await _service.DeleteAsync(st.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
