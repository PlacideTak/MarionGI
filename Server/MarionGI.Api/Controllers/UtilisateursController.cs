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
[Authorize(Roles = "Administrateur,Gestionnaire")]
public class UtilisateursController : ControllerBase
{
    private readonly MarionDbContext _context;

    public UtilisateursController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/Utilisateurs
    //
    // Administrateur :
    //     -> tous les utilisateurs de toutes les sociétés
    //
    // Gestionnaire :
    //     -> uniquement les utilisateurs de sa société
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetUtilisateurs()
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized("Utilisateur connecté introuvable.");

        var isAdmin = User.IsInRole("Administrateur");
        var isGestionnaire = User.IsInRole("Gestionnaire");

        var query = _context.Utilisateurs
            .AsNoTracking()
            .Where(u => !u.EstSupprime);

        // --------------------------------------------------------
        // Gestionnaire :
        // uniquement sa société
        // --------------------------------------------------------
        if (isGestionnaire && !isAdmin)
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            query = query.Where(u => u.SocieteId == societeId.Value);
        }

        var users = await query
            .OrderBy(u => u.Societe.Nom)
            .ThenBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .Select(u => new
            {
                u.Id,
                u.Nom,
                u.Prenom,
                u.Telephone,
                u.Email,
                u.Role,
                u.Statut,
                u.DerniereConnexion,
                u.TelephoneVerifie,

                u.SocieteId,

                SocieteNom = u.Societe.Nom
            })
            .ToListAsync();

        return Ok(users);
    }


    // ============================================================
    // GET : api/Utilisateurs/{id}
    //
    // Administrateur :
    //     -> peut consulter n'importe quel utilisateur
    //
    // Gestionnaire :
    //     -> uniquement un utilisateur de sa société
    // ============================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return Unauthorized("Utilisateur connecté introuvable.");

        var query = _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == id &&
                !u.EstSupprime);

        // Gestionnaire : restriction à sa société
        if (User.IsInRole("Gestionnaire") &&
            !User.IsInRole("Administrateur"))
        {
            var societeId = await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            query = query.Where(u => u.SocieteId == societeId.Value);
        }

        var user = await query
            .Select(u => new
            {
                u.Id,
                u.Nom,
                u.Prenom,
                u.Telephone,
                u.Email,
                u.Role,
                u.Statut,
                u.DerniereConnexion,
                u.TelephoneVerifie,

                u.SocieteId,

                SocieteNom = u.Societe.Nom
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(user);
    }


    // ============================================================
    // POST : api/Utilisateurs
    //
    // Administrateur :
    //     -> doit fournir SocieteId
    //
    // Gestionnaire :
    //     -> SocieteId imposé par le backend
    //     -> ne peut pas créer un Administrateur
    // ============================================================
    [HttpPost]
    public async Task<IActionResult> CreerUtilisateur(
        [FromBody] CreerUtilisateurRequest request)
    {
        // --------------------------------------------------------
        // Validation générale
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Prenom))
            return BadRequest("Le prénom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("L'adresse courriel est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.MotDePasseInitial))
            return BadRequest("Le mot de passe initial est obligatoire.");

        if (request.MotDePasseInitial.Length < 8)
        {
            return BadRequest(
                "Le mot de passe doit contenir au moins 8 caractères.");
        }

        var isAdmin = User.IsInRole("Administrateur");
        var isGestionnaire = User.IsInRole("Gestionnaire");

        // --------------------------------------------------------
        // Déterminer la société
        // --------------------------------------------------------

        Guid societeId;

        if (isAdmin)
        {
            // L'administrateur global choisit la société.
            if (!request.SocieteId.HasValue ||
                request.SocieteId.Value == Guid.Empty)
            {
                return BadRequest(
                    "La société de l'utilisateur est obligatoire.");
            }

            societeId = request.SocieteId.Value;
        }
        else if (isGestionnaire)
        {
            // Le gestionnaire est obligatoirement rattaché
            // à sa propre société.
            var currentSocieteId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (currentSocieteId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            societeId = currentSocieteId.Value;

            // Le Gestionnaire ne peut pas créer un Administrateur.
            if (request.Role == RoleUtilisateur.Administrateur)
            {
                return Forbid();
            }
        }
        else
        {
            return Forbid();
        }

        // --------------------------------------------------------
        // Vérifier que la société existe
        // --------------------------------------------------------

        var societe = await _context.Societes
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Id == societeId &&
                !s.EstSupprime);

        if (societe == null)
        {
            return BadRequest(
                "La société sélectionnée est introuvable.");
        }

        // --------------------------------------------------------
        // Une société inactive ne doit pas recevoir de nouveaux
        // utilisateurs.
        // --------------------------------------------------------

        if (!societe.Actif)
        {
            return BadRequest(
                "Impossible de créer un utilisateur dans une société inactive.");
        }

        // --------------------------------------------------------
        // Vérifier l'unicité de l'email dans la société
        // --------------------------------------------------------

        var email = request.Email.Trim().ToLowerInvariant();

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(u =>
                u.SocieteId == societeId &&
                u.Email.ToLower() == email &&
                !u.EstSupprime);

        if (emailExiste)
        {
            return Conflict(
                "Un utilisateur avec cette adresse courriel existe déjà dans cette société.");
        }

        // --------------------------------------------------------
        // Création
        // --------------------------------------------------------

        var user = new Utilisateur
        {
            Id = Guid.NewGuid(),

            Nom = request.Nom.Trim(),
            Prenom = request.Prenom.Trim(),

            Telephone = request.Telephone?.Trim() ?? string.Empty,

            Email = email,

            MotDePasseHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.MotDePasseInitial),

            Role = request.Role,

            Statut = true,

            SocieteId = societeId,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.Utilisateurs.Add(user);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            new
            {
                user.Id,
                user.Nom,
                user.Prenom,
                user.Telephone,
                user.Email,
                user.Role,
                user.Statut,
                user.SocieteId,
                SocieteNom = societe.Nom
            });
    }


    // ============================================================
    // PUT : api/Utilisateurs/{id}
    //
    // Administrateur :
    //     -> peut modifier n'importe quel utilisateur
    //     -> peut changer sa société
    //
    // Gestionnaire :
    //     -> uniquement utilisateur de sa société
    //     -> ne peut pas changer la société
    //     -> ne peut pas attribuer le rôle Administrateur
    // ============================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> ModifierUtilisateur(
        Guid id,
        [FromBody] ModifierUtilisateurRequest request)
    {
        // --------------------------------------------------------
        // Recherche de l'utilisateur
        // --------------------------------------------------------

        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        var isAdmin = User.IsInRole("Administrateur");
        var isGestionnaire = User.IsInRole("Gestionnaire");

        // --------------------------------------------------------
        // Gestionnaire :
        // il ne peut modifier qu'un utilisateur de sa société
        // --------------------------------------------------------

        if (isGestionnaire && !isAdmin)
        {
            var currentSocieteId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (currentSocieteId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (user.SocieteId != currentSocieteId.Value)
            {
                return Forbid();
            }

            // Le Gestionnaire ne peut pas transformer un utilisateur
            // en Administrateur.
            if (request.Role == RoleUtilisateur.Administrateur)
            {
                return Forbid();
            }
        }

        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Prenom))
            return BadRequest("Le prénom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("L'adresse courriel est obligatoire.");

        var email = request.Email.Trim().ToLowerInvariant();

        // --------------------------------------------------------
        // Société cible
        // --------------------------------------------------------

        Guid societeIdCible;

        if (isAdmin)
        {
            // Administrateur :
            // le SocieteId peut être modifié.

            if (!request.SocieteId.HasValue ||
                request.SocieteId.Value == Guid.Empty)
            {
                return BadRequest(
                    "La société de l'utilisateur est obligatoire.");
            }

            societeIdCible = request.SocieteId.Value;
        }
        else
        {
            // Gestionnaire :
            // la société ne peut jamais être modifiée.
            societeIdCible = user.SocieteId;
        }

        // --------------------------------------------------------
        // Vérifier que la société cible existe
        // --------------------------------------------------------

        var societe = await _context.Societes
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Id == societeIdCible &&
                !s.EstSupprime);

        if (societe == null)
        {
            return BadRequest(
                "La société sélectionnée est introuvable.");
        }

        // --------------------------------------------------------
        // Vérifier que la société cible est active
        // --------------------------------------------------------

        if (!societe.Actif)
        {
            return BadRequest(
                "Impossible de rattacher un utilisateur à une société inactive.");
        }

        // --------------------------------------------------------
        // Vérifier l'unicité de l'email dans la société cible
        // --------------------------------------------------------

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(u =>
                u.Id != id &&
                u.SocieteId == societeIdCible &&
                u.Email.ToLower() == email &&
                !u.EstSupprime);

        if (emailExiste)
        {
            return Conflict(
                "Un autre utilisateur utilise déjà cette adresse courriel dans cette société.");
        }

        // --------------------------------------------------------
        // Modification
        // --------------------------------------------------------

        user.Nom = request.Nom.Trim();
        user.Prenom = request.Prenom.Trim();
        user.Telephone = request.Telephone?.Trim() ?? string.Empty;
        user.Email = email;
        user.Role = request.Role;
        user.Statut = request.Statut;

        // Changement de société uniquement pour Administrateur
        if (isAdmin)
        {
            user.SocieteId = societeIdCible;
        }

        // --------------------------------------------------------
        // Mot de passe
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.NouveauMotDePasse))
        {
            if (request.NouveauMotDePasse.Length < 8)
            {
                return BadRequest(
                    "Le nouveau mot de passe doit contenir au moins 8 caractères.");
            }

            user.MotDePasseHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.NouveauMotDePasse);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // ============================================================
    // PATCH : api/Utilisateurs/{id}/statut
    //
    // Administrateur :
    //     -> peut modifier n'importe quel utilisateur
    //
    // Gestionnaire :
    //     -> uniquement dans sa société
    // ============================================================
    [HttpPatch("{id:guid}/statut")]
    public async Task<IActionResult> ModifierStatutUtilisateur(
        Guid id,
        [FromBody] ModifierStatutUtilisateurRequest request)
    {
        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        // --------------------------------------------------------
        // Gestionnaire : uniquement sa société
        // --------------------------------------------------------

        if (User.IsInRole("Gestionnaire") &&
            !User.IsInRole("Administrateur"))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (user.SocieteId != societeId.Value)
                return Forbid();
        }

        // --------------------------------------------------------
        // Empêcher de désactiver son propre compte
        // --------------------------------------------------------

        var currentUserId = GetCurrentUserId();

        if (currentUserId.HasValue &&
            currentUserId.Value == user.Id &&
            !request.Statut)
        {
            return BadRequest(
                "Vous ne pouvez pas désactiver votre propre compte.");
        }

        user.Statut = request.Statut;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = request.Statut
                ? "Utilisateur activé avec succès."
                : "Utilisateur désactivé avec succès.",

            user.Id,
            user.Statut
        });
    }


    // ============================================================
    // DELETE : api/Utilisateurs/{id}
    //
    // Administrateur :
    //     -> peut supprimer n'importe quel utilisateur
    //
    // Gestionnaire :
    //     -> peut supprimer uniquement un utilisateur de sa société
    // ============================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SupprimerUtilisateur(Guid id)
    {
        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (user == null)
            return NotFound("Utilisateur introuvable.");

        // --------------------------------------------------------
        // Gestionnaire : uniquement sa société
        // --------------------------------------------------------

        if (User.IsInRole("Gestionnaire") &&
            !User.IsInRole("Administrateur"))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (societeId == null)
            {
                return Unauthorized(
                    "Impossible de déterminer la société de l'utilisateur connecté.");
            }

            if (user.SocieteId != societeId.Value)
                return Forbid();
        }

        // --------------------------------------------------------
        // Empêcher de supprimer son propre compte
        // --------------------------------------------------------

        var currentUserId = GetCurrentUserId();

        if (currentUserId.HasValue &&
            currentUserId.Value == user.Id)
        {
            return BadRequest(
                "Vous ne pouvez pas supprimer votre propre compte.");
        }

        // --------------------------------------------------------
        // Suppression logique
        // --------------------------------------------------------

        user.EstSupprime = true;
        user.Statut = false;
        user.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Compte utilisateur supprimé avec succès."
        });
    }


    // ============================================================
    // Récupérer le SocieteId de l'utilisateur connecté
    // ============================================================
    private async Task<Guid?> GetSocieteIdUtilisateurConnecteAsync()
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return null;

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
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userId, out var id))
            return id;

        return null;
    }
}


// ================================================================
// DTO - Création
// ================================================================

public record CreerUtilisateurRequest(
    string Nom,
    string Prenom,
    string? Telephone,
    string Email,
    RoleUtilisateur Role,
    string MotDePasseInitial,
    Guid? SocieteId
);


// ================================================================
// DTO - Modification
// ================================================================

public record ModifierUtilisateurRequest(
    string Nom,
    string Prenom,
    string? Telephone,
    string Email,
    RoleUtilisateur Role,
    bool Statut,
    string? NouveauMotDePasse,
    Guid? SocieteId
);


// ================================================================
// DTO - Modification du statut
// ================================================================

public record ModifierStatutUtilisateurRequest(
    bool Statut
);