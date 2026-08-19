using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Accessible à tout utilisateur connecté (peu importe son rôle)
public class ProfilController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ProfilController(MarionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonProfil()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized("Utilisateur non identifié.");

        var user = await _context.Utilisateurs
            .Where(u => u.Id == userId && !u.EstSupprime)
            .Select(u => new
            {
                u.Id,
                u.Nom,
                u.Prenom,
                u.Email,
                u.Telephone,
                u.Role,
                u.Statut
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound("Utilisateur introuvable.");

        return Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> ModifierMonProfil([FromBody] ModifierMonProfilRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized("Utilisateur non identifié.");

        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userId && !u.EstSupprime);
        if (user == null) return NotFound("Utilisateur introuvable.");

        // Mise à jour des informations de base (Nom et Prénom uniquement)
        user.Nom = request.Nom;
        user.Prenom = request.Prenom;

        // Gestion du changement de mot de passe si demandé
        if (!string.IsNullOrWhiteSpace(request.NouveauMotDePasse))
        {
            if (string.IsNullOrWhiteSpace(request.AncienMotDePasse))
            {
                return BadRequest(new { message = "Veuillez fournir votre ancien mot de passe." });
            }

            // Vérification de l'ancien mot de passe haché avec BCrypt
            bool passwordValid = BCrypt.Net.BCrypt.Verify(request.AncienMotDePasse, user.MotDePasseHash);
            if (!passwordValid)
            {
                return BadRequest(new { message = "L'ancien mot de passe est incorrect." });
            }

            user.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.NouveauMotDePasse);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profil mis à jour avec succès." });
    }
}

public record ModifierMonProfilRequest(
    string Nom,
    string Prenom,
    string? AncienMotDePasse,
    string? NouveauMotDePasse
);