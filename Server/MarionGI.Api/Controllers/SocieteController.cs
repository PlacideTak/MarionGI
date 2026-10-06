using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SocietesController : ControllerBase
{
    private readonly MarionDbContext _context;

    public SocietesController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/Societes
    //
    // ADMINISTRATEUR :
    // Retourne toutes les sociétés actives/non supprimées.
    //
    // GESTIONNAIRE :
    // Retourne uniquement sa société.
    // ============================================================
    [HttpGet]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> GetSocietes()
    {
        // --------------------------------------------------------
        // ADMINISTRATEUR
        // --------------------------------------------------------
        if (User.IsInRole(nameof(RoleUtilisateur.Administrateur)))
        {
            var societes = await _context.Societes
                .AsNoTracking()
                .Where(s => !s.EstSupprime)
                .OrderBy(s => s.Nom)
                .Select(s => new
                {
                    s.Id,
                    s.Nom,
                    s.NumeroEntreprise,
                    s.Adresse,
                    s.Ville,
                    s.CodePostal,
                    s.Telephone,
                    s.Email,
                    s.Actif,

                    NombreUtilisateurs = s.Utilisateurs
                        .Count(u => !u.EstSupprime),

                    NombreBiens = s.BiensImmobiliers
                        .Count(b => !b.EstSupprime)
                })
                .ToListAsync();

            return Ok(societes);
        }

        // --------------------------------------------------------
        // GESTIONNAIRE
        // --------------------------------------------------------

        var societeId = await GetSocieteIdUtilisateurConnecteAsync();

        if (societeId == null || societeId == Guid.Empty)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var societe = await _context.Societes
            .AsNoTracking()
            .Where(s =>
                s.Id == societeId.Value &&
                !s.EstSupprime)
            .Select(s => new
            {
                s.Id,
                s.Nom,
                s.NumeroEntreprise,
                s.Adresse,
                s.Ville,
                s.CodePostal,
                s.Telephone,
                s.Email,
                s.Actif,

                NombreUtilisateurs = s.Utilisateurs
                    .Count(u => !u.EstSupprime),

                NombreBiens = s.BiensImmobiliers
                    .Count(b => !b.EstSupprime)
            })
            .FirstOrDefaultAsync();

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        return Ok(societe);
    }


    // ============================================================
    // GET : api/Societes/{id}
    //
    // ADMINISTRATEUR :
    // Peut consulter n'importe quelle société.
    //
    // GESTIONNAIRE :
    // Peut consulter uniquement sa société.
    // ============================================================
    [HttpGet("{id:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("L'identifiant de la société est invalide.");
        }

        // --------------------------------------------------------
        // GESTIONNAIRE
        // --------------------------------------------------------
        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }
        }

        // --------------------------------------------------------
        // ADMINISTRATEUR :
        // aucune restriction sur la société demandée
        // --------------------------------------------------------

        var societe = await _context.Societes
            .AsNoTracking()
            .Where(s =>
                s.Id == id &&
                !s.EstSupprime)
            .Select(s => new
            {
                s.Id,
                s.Nom,
                s.NumeroEntreprise,
                s.Adresse,
                s.Ville,
                s.CodePostal,
                s.Telephone,
                s.Email,
                s.Actif,

                NombreUtilisateurs = s.Utilisateurs
                    .Count(u => !u.EstSupprime),

                NombreBiens = s.BiensImmobiliers
                    .Count(b => !b.EstSupprime)
            })
            .FirstOrDefaultAsync();

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        return Ok(societe);
    }


    // ============================================================
    // POST : api/Societes
    //
    // ADMINISTRATEUR UNIQUEMENT
    //
    // L'administrateur peut créer autant de sociétés que nécessaire.
    //
    // IMPORTANT :
    // On ne rattache PAS l'administrateur à la société créée.
    // ============================================================
    [HttpPost]
    [Authorize(Roles = nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> CreerSociete(
        [FromBody] CreerSocieteRequest request)
    {
        if (request == null)
        {
            return BadRequest("Les données de la société sont obligatoires.");
        }

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            return BadRequest("Le nom de la société est obligatoire.");
        }

        var nom = request.Nom.Trim();

        // --------------------------------------------------------
        // Vérifier l'unicité du nom
        // --------------------------------------------------------

        var nomExiste = await _context.Societes
            .AnyAsync(s =>
                s.Nom.ToLower() == nom.ToLower() &&
                !s.EstSupprime);

        if (nomExiste)
        {
            return Conflict(
                "Une société portant ce nom existe déjà.");
        }

        // --------------------------------------------------------
        // Création
        // --------------------------------------------------------

        var societe = new Societe
        {
            Id = Guid.NewGuid(),

            Nom = nom,
            NumeroEntreprise = request.NumeroEntreprise?.Trim(),
            Adresse = request.Adresse?.Trim(),
            Ville = request.Ville?.Trim(),
            CodePostal = request.CodePostal?.Trim(),
            Telephone = request.Telephone?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),

            Actif = true,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.Societes.Add(societe);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = societe.Id },
            new
            {
                societe.Id,
                societe.Nom,
                societe.NumeroEntreprise,
                societe.Adresse,
                societe.Ville,
                societe.CodePostal,
                societe.Telephone,
                societe.Email,
                societe.Actif,

                NombreUtilisateurs = 0,
                NombreBiens = 0
            });
    }


    // ============================================================
    // PUT : api/Societes/{id}
    //
    // ADMINISTRATEUR :
    // Peut modifier n'importe quelle société.
    //
    // GESTIONNAIRE :
    // Peut modifier uniquement sa société.
    // ============================================================
    [HttpPut("{id:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierSociete(
        Guid id,
        [FromBody] ModifierSocieteRequest request)
    {
        if (request == null)
        {
            return BadRequest("Les données de modification sont obligatoires.");
        }

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            return BadRequest("Le nom de la société est obligatoire.");
        }

        // --------------------------------------------------------
        // GESTIONNAIRE :
        // uniquement sa société
        // --------------------------------------------------------

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }
        }

        // --------------------------------------------------------
        // ADMINISTRATEUR :
        // peut modifier n'importe quelle société
        // --------------------------------------------------------

        var societe = await _context.Societes
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                !s.EstSupprime);

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        var nom = request.Nom.Trim();

        var nomExiste = await _context.Societes
            .AnyAsync(s =>
                s.Id != id &&
                s.Nom.ToLower() == nom.ToLower() &&
                !s.EstSupprime);

        if (nomExiste)
        {
            return Conflict(
                "Une autre société utilise déjà ce nom.");
        }

        societe.Nom = nom;
        societe.NumeroEntreprise = request.NumeroEntreprise?.Trim();
        societe.Adresse = request.Adresse?.Trim();
        societe.Ville = request.Ville?.Trim();
        societe.CodePostal = request.CodePostal?.Trim();
        societe.Telephone = request.Telephone?.Trim();
        societe.Email = request.Email?.Trim().ToLowerInvariant();

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // ============================================================
    // PATCH : api/Societes/{id}/statut
    //
    // ADMINISTRATEUR :
    // Peut activer/désactiver n'importe quelle société.
    //
    // GESTIONNAIRE :
    // Peut uniquement modifier le statut de sa société.
    // ============================================================
    [HttpPatch("{id:guid}/statut")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutSocieteRequest request)
    {
        // --------------------------------------------------------
        // GESTIONNAIRE :
        // uniquement sa société
        // --------------------------------------------------------

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }
        }

        var societe = await _context.Societes
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                !s.EstSupprime);

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        // --------------------------------------------------------
        // Désactivation
        // --------------------------------------------------------

        if (!request.Actif)
        {
            var utilisateursActifs = await _context.Utilisateurs
                .AnyAsync(u =>
                    u.SocieteId == id &&
                    u.Statut &&
                    !u.EstSupprime);

            if (utilisateursActifs)
            {
                return Conflict(
                    "La société ne peut pas être désactivée tant qu'elle possède des utilisateurs actifs.");
            }
        }

        societe.Actif = request.Actif;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = request.Actif
                ? "Société activée avec succès."
                : "Société désactivée avec succès.",

            societe.Id,
            societe.Actif
        });
    }


    // ============================================================
    // DELETE : api/Societes/{id}
    //
    // ADMINISTRATEUR UNIQUEMENT
    //
    // Suppression logique.
    // ============================================================
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> SupprimerSociete(Guid id)
    {
        var societe = await _context.Societes
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                !s.EstSupprime);

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        // --------------------------------------------------------
        // Vérifier les utilisateurs actifs
        // --------------------------------------------------------

        var utilisateursActifs = await _context.Utilisateurs
            .AnyAsync(u =>
                u.SocieteId == id &&
                !u.EstSupprime &&
                u.Statut);

        if (utilisateursActifs)
        {
            return Conflict(
                "Cette société ne peut pas être supprimée car elle possède encore des utilisateurs actifs.");
        }

        // --------------------------------------------------------
        // Vérifier les biens actifs
        // --------------------------------------------------------

        var biensActifs = await _context.BiensImmobiliers
            .AnyAsync(b =>
                b.SocieteId == id &&
                !b.EstSupprime);

        if (biensActifs)
        {
            return Conflict(
                "Cette société ne peut pas être supprimée car elle possède encore des biens immobiliers.");
        }

        // --------------------------------------------------------
        // Suppression logique
        // --------------------------------------------------------

        societe.EstSupprime = true;
        societe.Actif = false;
        societe.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Société supprimée avec succès."
        });
    }


    // ============================================================
    // Récupérer la société de l'utilisateur connecté
    // ============================================================
    private async Task<Guid?> GetSocieteIdUtilisateurConnecteAsync()
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == currentUserId.Value &&
                !u.EstSupprime &&
                u.Statut)
            .Select(u => (Guid?)u.SocieteId)
            .FirstOrDefaultAsync();
    }


    // ============================================================
    // Récupérer l'ID de l'utilisateur connecté
    // ============================================================
    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userId, out var id))
        {
            return id;
        }

        return null;
    }
}


// ==================================================================
// DTO : création
// ==================================================================

public record CreerSocieteRequest(
    string Nom,
    string? NumeroEntreprise,
    string? Adresse,
    string? Ville,
    string? CodePostal,
    string? Telephone,
    string? Email
);


// ==================================================================
// DTO : modification
// ==================================================================

public record ModifierSocieteRequest(
    string Nom,
    string? NumeroEntreprise,
    string? Adresse,
    string? Ville,
    string? CodePostal,
    string? Telephone,
    string? Email
);


// ==================================================================
// DTO : statut
// ==================================================================

public record ModifierStatutSocieteRequest(
    bool Actif
);