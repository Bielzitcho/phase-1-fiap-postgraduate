using Microsoft.Extensions.Configuration;

namespace OficinaTech.Infrastructure.Services;

public class AdminCredentialService
{
    private readonly IConfiguration _config;

    public AdminCredentialService(IConfiguration config) => _config = config;

    public bool Validate(string email, string password)
    {
        var storedEmail = _config["Admin:Email"];
        var storedHash = _config["Admin:PasswordHash"];

        if (storedEmail is null || storedHash is null)
            return false;

        if (!string.Equals(email, storedEmail, StringComparison.OrdinalIgnoreCase))
            return false;

        // BCrypt.Verify is constant-time — return generic failure either way (no enumeration leak)
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }
}
