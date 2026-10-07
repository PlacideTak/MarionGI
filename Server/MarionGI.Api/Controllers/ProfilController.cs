using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfilController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ProfilController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : Mon profil
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetMonProfil(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized("Utilisateur non identifié.");

        var user = await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == userId.Value &&
                !u.EstSupprime)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(user);
    }

    // ============================================================
    // PUT : Modifier mon profil
    // ============================================================
    [HttpPut]
    public async Task<IActionResult> ModifierMonProfil(
        [FromBody] ModifierMonProfilRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest("Les données du profil sont obligatoires.");

        var userId = GetCurrentUserId();

        if (!userId.HasValue)
            return Unauthorized("Utilisateur non identifié.");

        // --------------------------------------------------------
        // Validation des informations de base
        // --------------------------------------------------------
        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Prenom))
            return BadRequest("Le prénom est obligatoire.");

        var nom = request.Nom.Trim();
        var prenom = request.Prenom.Trim();

        if (nom.Length > 100)
            return BadRequest("Le nom ne peut pas dépasser 100 caractères.");

        if (prenom.Length > 100)
            return BadRequest("Le prénom ne peut pas dépasser 100 caractères.");

        // --------------------------------------------------------
        // Récupération de l'utilisateur connecté
        // --------------------------------------------------------
        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(
                u =>
                    u.Id == userId.Value &&
                    !u.EstSupprime,
                cancellationToken);

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        // Un utilisateur désactivé ne devrait normalement plus
        // pouvoir appeler cette API puisque le JWT devrait être
        // refusé. Cette vérification ajoute néanmoins une défense
        // supplémentaire côté serveur.
        if (!user.Statut)
            return Forbid();

        // --------------------------------------------------------
        // Mise à jour des informations personnelles
        // --------------------------------------------------------
        user.Nom = nom;
        user.Prenom = prenom;

        // --------------------------------------------------------
        // Changement du mot de passe
        // --------------------------------------------------------
        bool changementMotDePasse =
            !string.IsNullOrWhiteSpace(request.NouveauMotDePasse);

        if (changementMotDePasse)
        {
            if (string.IsNullOrWhiteSpace(request.AncienMotDePasse))
            {
                return BadRequest(new
                {
                    message = "Veuillez fournir votre ancien mot de passe."
                });
            }

            if (string.IsNullOrWhiteSpace(user.MotDePasseHash))
            {
                return BadRequest(new
                {
                    message = "Impossible de vérifier votre ancien mot de passe."
                });
            }

            // Vérification de l'ancien mot de passe
            bool passwordValid;

            try
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(
                    request.AncienMotDePasse,
                    user.MotDePasseHash);
            }
            catch
            {
                passwordValid = false;
            }

            if (!passwordValid)
            {
                return BadRequest(new
                {
                    message = "L'ancien mot de passe est incorrect."
                });
            }

            var nouveauMotDePasse = request.NouveauMotDePasse!.Trim();

            if (nouveauMotDePasse.Length < 8)
            {
                return BadRequest(new
                {
                    message = "Le nouveau mot de passe doit contenir au moins 8 caractères."
                });
            }

            if (nouveauMotDePasse.Length > 100)
            {
                return BadRequest(new
                {
                    message = "Le nouveau mot de passe ne peut pas dépasser 100 caractères."
                });
            }

            // Éviter de réutiliser exactement le même mot de passe
            if (BCrypt.Net.BCrypt.Verify(
                    nouveauMotDePasse,
                    user.MotDePasseHash))
            {
                return BadRequest(new
                {
                    message = "Le nouveau mot de passe doit être différent de l'ancien."
                });
            }

            user.MotDePasseHash =
                BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);
        }

        // --------------------------------------------------------
        // Sauvegarde
        // --------------------------------------------------------
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = changementMotDePasse
                ? "Profil et mot de passe mis à jour avec succès."
                : "Profil mis à jour avec succès."
        });
    }

    // ============================================================
    // Récupération sécurisée de l'utilisateur connecté
    // ============================================================
    private Guid? GetCurrentUserId()
    {
        var claims = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("sub"),
            User.FindFirstValue("Id")
        };

        foreach (var claim in claims)
        {
            if (Guid.TryParse(claim, out var userId) &&
                userId != Guid.Empty)
            {
                return userId;
            }
        }

        return null;
    }
}

// ================================================================
// DTO
// ================================================================
public record ModifierMonProfilRequest(
    string Nom,
    string Prenom,
    string? AncienMotDePasse,
    string? NouveauMotDePasse
);