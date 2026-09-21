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

    // 👇 Nouveaux endpoints pour la récupération du mot de passe

    [HttpPost("mot-de-passe-oublie")]
    public async Task<IActionResult> MotDePasseOublie([FromBody] MotDePasseOublieRequestDto dto)
    {
        try
        {
            await _authService.DemanderRecuperationAsync(dto);
            return Ok(new { message = "Code de réinitialisation envoyé avec succès." });
        }
        catch (Exception ex)
        {
            // Renvoie un code 400 avec le message d'erreur précis au lieu d'un crash 500
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reinitialiser-mot-de-passe")]
    public async Task<IActionResult> ReinitialiserMotDePasse([FromBody] ReinitialiserMotDePasseRequestDto dto)
    {
        await _authService.ReinitialiserMotDePasseAsync(dto);
        return Ok(new { message = "Mot de passe réinitialisé avec succès." });
    }
}