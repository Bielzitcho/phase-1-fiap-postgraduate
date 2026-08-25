using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaTech.Application.DTOs;
using OficinaTech.Application.Interfaces;

namespace OficinaTech.Presentation.Controllers;

[ApiController]
[Route("api/service-orders")]
[Authorize(Roles = "admin")]
public class ServiceOrdersController : ControllerBase
{
    private readonly IServiceOrderService _service;

    public ServiceOrdersController(IServiceOrderService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceOrderRequest req, CancellationToken ct)
    {
        var result = await _service.CreateAsync(req, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);

        // D-02/OS-02: client or vehicle resolution failure → 404
        var statusCode = result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
            ? 404
            : 400;
        return Problem(detail: result.Error, statusCode: statusCode);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error, statusCode: 404);
    }
}
