using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OficinaTech.Infrastructure.Services;

namespace OficinaTech.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AdminCredentialService _credentials;

    public AuthController(IConfiguration config, AdminCredentialService credentials)
    {
        _config = config;
        _credentials = credentials;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!_credentials.Validate(request.Email, request.Password))
            return Unauthorized();

        var expiryMinutes = int.Parse(_config["Admin:JwtExpiryMinutes"] ?? "60");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Admin:JwtSecret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.Role, "admin") };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public record LoginRequest(string Email, string Password);
