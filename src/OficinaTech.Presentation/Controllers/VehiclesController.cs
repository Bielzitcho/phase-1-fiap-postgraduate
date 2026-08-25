using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;

namespace OficinaTech.Presentation.Controllers;

[ApiController]
[Route("api/vehicles")]
[Authorize(Roles = "admin")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _service;

    public VehiclesController(IVehicleService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest req, CancellationToken ct)
    {
        var result = await _service.CreateAsync(req, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(page, pageSize, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 500);
    }

    [HttpGet("/api/clients/{clientId:guid}/vehicles")]
    public async Task<IActionResult> GetByClient(
        Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _service.GetByClientAsync(clientId, page, pageSize, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateVehicleRequest req, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, req, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error, statusCode: 404);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : Problem(detail: result.Error, statusCode: 404);
    }
}
