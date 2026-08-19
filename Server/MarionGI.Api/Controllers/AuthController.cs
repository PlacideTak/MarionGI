using MarionGI.Application.Dtos;
using MarionGI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("connexion")]
    public async Task<IActionResult> Connexion([FromBody] ConnexionRequestDto dto)
    {
        var result = await _authService.ConnexionAsync(dto);
        return Ok(result);
    }

    [HttpPost("valider-otp")]
    public async Task<IActionResult> ValiderOtp([FromBody] ValiderOtpRequestDto dto)
    {
        var result = await _authService.ValiderOtpAsync(dto);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);
        return Ok(result);
    }
}