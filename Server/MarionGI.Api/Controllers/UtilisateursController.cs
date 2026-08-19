using MarionGI.Domain.Entities;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrateur")]
public class UtilisateursController : ControllerBase
{
    private readonly MarionDbContext _context;
    
    public UtilisateursController(MarionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUtilisateurs()
    {
        var users = await _context.Utilisateurs
            .Where(u => !u.EstSupprime)
            .Select(u => new
            {
                u.Id,
                u.Nom,
                u.Prenom,
                u.Email,
                u.Telephone,
                u.Role,
                u.Statut,
                u.DerniereConnexion
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _context.Utilisateurs
            .Where(u => u.Id == id && !u.EstSupprime)
            .Select(u => new
            {
                u.Id,
                u.Nom,
                u.Prenom,
                u.Email,
                u.Telephone,
                u.Role,
                u.Statut,
                u.DerniereConnexion
            })
            .FirstOrDefaultAsync();

        if (user == null) return NotFound("Utilisateur introuvable.");
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreerUtilisateur([FromBody] Utilisateur user, [FromQuery] string motDePasseInitial)
    {
        user.Id = Guid.NewGuid();

        // 🔐 Utilisation de BCrypt
        user.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseInitial);

        user.DateCreation = DateTime.UtcNow;
        user.EstSupprime = false;

        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.Email, user.Role });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ModifierUtilisateur(Guid id, [FromBody] ModifierUtilisateurRequest request)
    {
        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id && !u.EstSupprime);
        if (user == null) return NotFound("Utilisateur introuvable.");

        user.Nom = request.Nom;
        user.Prenom = request.Prenom;
        user.Telephone = request.Telephone;
        user.Email = request.Email;
        user.Role = request.Role;
        user.Statut = request.Statut;

        // Réinitialisation optionnelle du mot de passe
        if (!string.IsNullOrWhiteSpace(request.NouveauMotDePasse))
        {
            user.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.NouveauMotDePasse);  //_passwordHasher.HashPassword(user, request.NouveauMotDePasse);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Suppression logique d'un compte utilisateur
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> SupprimerUtilisateur(Guid id)
    {
        var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id && !u.EstSupprime);
        if (user == null) return NotFound("Utilisateur introuvable.");

        user.EstSupprime = true;
        user.Statut = false; // Désactivation immédiate de l'accès
        user.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Compte utilisateur supprimé avec succès." });
    }
}

public record ModifierUtilisateurRequest(
    string Nom,
    string Prenom,
    string Telephone,
    string Email,
    Domain.Enums.RoleUtilisateur Role,
    bool Statut,
    string? NouveauMotDePasse
);