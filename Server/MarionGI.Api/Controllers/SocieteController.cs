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
    // Toutes les sociétés non supprimées.
    //
    // GESTIONNAIRE :
    // Uniquement sa société.
    //
    // AGENT / LOCATAIRE :
    // Aucun accès.
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
            var societes =
                await _context.Societes
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

                        NombreUtilisateurs =
                            s.Utilisateurs
                                .Count(u => !u.EstSupprime),

                        NombreBiens =
                            s.BiensImmobiliers
                                .Count(b => !b.EstSupprime)
                    })
                    .ToListAsync();

            return Ok(societes);
        }

        // --------------------------------------------------------
        // GESTIONNAIRE
        // --------------------------------------------------------

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync();

        if (!societeId.HasValue ||
            societeId.Value == Guid.Empty)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var societe =
            await _context.Societes
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

                    NombreUtilisateurs =
                        s.Utilisateurs
                            .Count(u => !u.EstSupprime),

                    NombreBiens =
                        s.BiensImmobiliers
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
            return BadRequest(
                "L'identifiant de la société est invalide.");
        }

        // --------------------------------------------------------
        // GESTIONNAIRE
        // --------------------------------------------------------

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
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
        // accès à toutes les sociétés non supprimées
        // --------------------------------------------------------

        var societe =
            await _context.Societes
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

                    NombreUtilisateurs =
                        s.Utilisateurs
                            .Count(u => !u.EstSupprime),

                    NombreBiens =
                        s.BiensImmobiliers
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
    // ============================================================

    [HttpPost]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> CreerSociete(
        [FromBody] CreerSocieteRequest request)
    {
        if (request == null)
        {
            return BadRequest(
                "Les données de la société sont obligatoires.");
        }

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            return BadRequest(
                "Le nom de la société est obligatoire.");
        }

        var nom = request.Nom.Trim();

        // --------------------------------------------------------
        // Vérification unicité du nom
        // --------------------------------------------------------

        var nomExiste =
            await _context.Societes
                .AsNoTracking()
                .AnyAsync(s =>
                    !s.EstSupprime &&
                    s.Nom.ToLower() == nom.ToLower());

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

            NumeroEntreprise =
                request.NumeroEntreprise?.Trim(),

            Adresse =
                request.Adresse?.Trim(),

            Ville =
                request.Ville?.Trim(),

            CodePostal =
                request.CodePostal?.Trim(),

            Telephone =
                request.Telephone?.Trim(),

            Email =
                request.Email?
                    .Trim()
                    .ToLowerInvariant(),

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
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de la société est invalide.");
        }

        if (request == null)
        {
            return BadRequest(
                "Les données de modification sont obligatoires.");
        }

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            return BadRequest(
                "Le nom de la société est obligatoire.");
        }

        // --------------------------------------------------------
        // GESTIONNAIRE :
        // uniquement sa société
        // --------------------------------------------------------

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
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
        // Recherche société
        // --------------------------------------------------------

        var societe =
            await _context.Societes
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    !s.EstSupprime);

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        var nom = request.Nom.Trim();

        // --------------------------------------------------------
        // Unicité du nom
        // --------------------------------------------------------

        var nomExiste =
            await _context.Societes
                .AsNoTracking()
                .AnyAsync(s =>
                    s.Id != id &&
                    !s.EstSupprime &&
                    s.Nom.ToLower() == nom.ToLower());

        if (nomExiste)
        {
            return Conflict(
                "Une autre société utilise déjà ce nom.");
        }

        // --------------------------------------------------------
        // Mise à jour
        // --------------------------------------------------------

        societe.Nom = nom;

        societe.NumeroEntreprise =
            request.NumeroEntreprise?.Trim();

        societe.Adresse =
            request.Adresse?.Trim();

        societe.Ville =
            request.Ville?.Trim();

        societe.CodePostal =
            request.CodePostal?.Trim();

        societe.Telephone =
            request.Telephone?.Trim();

        societe.Email =
            request.Email?
                .Trim()
                .ToLowerInvariant();

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
    // Peut modifier le statut de sa société.
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutSocieteRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de la société est invalide.");
        }

        if (request == null)
        {
            return BadRequest(
                "Les données de statut sont obligatoires.");
        }

        // --------------------------------------------------------
        // GESTIONNAIRE :
        // uniquement sa société
        // --------------------------------------------------------

        var utilisateurId =
            GetCurrentUserId();

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
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
        // Recherche société
        // --------------------------------------------------------

        var societe =
            await _context.Societes
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
            // Un gestionnaire ne doit pas être bloqué par
            // son propre compte actif.
            //
            // On vérifie donc les AUTRES utilisateurs actifs.
            var utilisateursActifs =
                await _context.Utilisateurs
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.SocieteId == id &&
                        u.Statut &&
                        !u.EstSupprime &&
                        (!utilisateurId.HasValue ||
                         u.Id != utilisateurId.Value));

            if (utilisateursActifs)
            {
                return Conflict(
                    "La société ne peut pas être désactivée tant qu'elle possède d'autres utilisateurs actifs.");
            }
        }

        // --------------------------------------------------------
        // Changement de statut
        // --------------------------------------------------------

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
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> SupprimerSociete(
        Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de la société est invalide.");
        }

        var societe =
            await _context.Societes
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    !s.EstSupprime);

        if (societe == null)
        {
            return NotFound("Société introuvable.");
        }

        // --------------------------------------------------------
        // Utilisateurs actifs
        // --------------------------------------------------------

        var utilisateursActifs =
            await _context.Utilisateurs
                .AsNoTracking()
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
        // Biens immobiliers actifs
        // --------------------------------------------------------

        var biensActifs =
            await _context.BiensImmobiliers
                .AsNoTracking()
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
    // RÉCUPÉRER LA SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private async Task<Guid?>
        GetSocieteIdUtilisateurConnecteAsync()
    {
        var currentUserId =
            GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == currentUserId.Value &&
                !u.EstSupprime &&
                u.Statut &&
                u.SocieteId != Guid.Empty)
            .Select(u => (Guid?)u.SocieteId)
            .FirstOrDefaultAsync();
    }

    // ============================================================
    // RÉCUPÉRER L'ID DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        // --------------------------------------------------------
        // Claim standard ASP.NET Core
        // --------------------------------------------------------

        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (Guid.TryParse(
                userId,
                out var id))
        {
            return id;
        }

        // --------------------------------------------------------
        // JWT : sub
        // --------------------------------------------------------

        userId =
            User.FindFirstValue("sub");

        if (Guid.TryParse(
                userId,
                out id))
        {
            return id;
        }

        // --------------------------------------------------------
        // Fallback éventuel : Id
        // --------------------------------------------------------

        userId =
            User.FindFirstValue("Id");

        if (Guid.TryParse(
                userId,
                out id))
        {
            return id;
        }

        return null;
    }
}


// ==================================================================
// DTO : CRÉATION
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
// DTO : MODIFICATION
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
// DTO : STATUT
// ==================================================================

public record ModifierStatutSocieteRequest(
    bool Actif
);