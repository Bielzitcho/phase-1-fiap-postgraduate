using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OficinaTech.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "admin")]
    public IActionResult Get() => Ok(new { status = "pong" });
}
