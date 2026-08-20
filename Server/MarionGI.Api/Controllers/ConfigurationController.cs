using Microsoft.AspNetCore.Mvc;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfiguration _config;
    public ConfigurationController(IConfiguration config) => _config = config;

    [HttpGet("session-timeout")]
    public IActionResult GetSessionTimeout()
    {
        // Retourne la valeur en minutes depuis appsettings.json
        var expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes");
        return Ok(new { timeoutMinutes = expiryMinutes });
    }
}
