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
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UtilisateurDto>>> GetUtilisateurs(
        CancellationToken cancellationToken)
    {
        var utilisateurConnecte = await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        IQueryable<Utilisateur> query = _context.Utilisateurs
            .AsNoTracking()
            .Where(u => !u.EstSupprime);

        // --------------------------------------------------------
        // Administrateur : accès à toutes les sociétés
        // Gestionnaire : uniquement sa société
        // --------------------------------------------------------

        if (utilisateurConnecte.Role == RoleUtilisateur.Gestionnaire)
        {
            query = query.Where(u =>
                u.SocieteId == utilisateurConnecte.SocieteId);
        }

        var utilisateurs = await query
            .OrderBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .Select(u => new UtilisateurDto
            {
                Id = u.Id,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Telephone = u.Telephone,
                Email = u.Email,
                Role = u.Role,
                Statut = u.Statut,
                SocieteId = u.SocieteId
            })
            .ToListAsync(cancellationToken);

        return Ok(utilisateurs);
    }

    // ============================================================
    // GET : api/Utilisateurs/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UtilisateurDto>> GetUtilisateur(
        Guid id,
        CancellationToken cancellationToken)
    {
        var utilisateurConnecte = await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        var query = _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == id &&
                !u.EstSupprime);

        // --------------------------------------------------------
        // Gestionnaire : uniquement sa société
        // --------------------------------------------------------

        if (utilisateurConnecte.Role == RoleUtilisateur.Gestionnaire)
        {
            query = query.Where(u =>
                u.SocieteId == utilisateurConnecte.SocieteId);
        }

        var utilisateur = await query
            .Select(u => new UtilisateurDto
            {
                Id = u.Id,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Telephone = u.Telephone,
                Email = u.Email,
                Role = u.Role,
                Statut = u.Statut,
                SocieteId = u.SocieteId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (utilisateur is null)
            return NotFound();

        return Ok(utilisateur);
    }

    // ============================================================
    // POST : api/Utilisateurs
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<UtilisateurDto>> CreerUtilisateur(
        [FromBody] CreerUtilisateurRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("La requête est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Prenom))
            return BadRequest("Le prénom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("L'adresse courriel est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.MotDePasseInitial))
            return BadRequest("Le mot de passe initial est obligatoire.");

        if (request.MotDePasseInitial.Length < 8)
            return BadRequest(
                "Le mot de passe doit contenir au moins 8 caractères.");

        if (!Enum.IsDefined(typeof(RoleUtilisateur), request.Role))
            return BadRequest("Le rôle sélectionné est invalide.");

        var utilisateurConnecte =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        Guid societeId;

        // ========================================================
        // ADMINISTRATEUR
        // ========================================================

        if (utilisateurConnecte.Role == RoleUtilisateur.Administrateur)
        {
            if (!request.SocieteId.HasValue ||
                request.SocieteId.Value == Guid.Empty)
            {
                return BadRequest(
                    "La société est obligatoire pour un nouvel utilisateur.");
            }

            societeId = request.SocieteId.Value;
        }
        else
        {
            // ====================================================
            // GESTIONNAIRE
            // ====================================================

            societeId = utilisateurConnecte.SocieteId;

            // Un Gestionnaire ne peut pas créer un Administrateur
            if (request.Role == RoleUtilisateur.Administrateur)
            {
                return Forbid();
            }

            // Le Gestionnaire ne peut pas choisir une autre société
            if (request.SocieteId.HasValue &&
                request.SocieteId.Value != societeId)
            {
                return Forbid();
            }
        }

        // ========================================================
        // Vérification de la société
        // ========================================================

        var societe = await _context.Societes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s =>
                    s.Id == societeId &&
                    !s.EstSupprime,
                cancellationToken);

        if (societe is null)
            return BadRequest("La société sélectionnée n'existe pas.");

        if (!societe.Actif)
            return BadRequest(
                "Impossible de créer un utilisateur dans une société inactive.");

        // ========================================================
        // Email unique dans la société
        // ========================================================

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(
                u =>
                    u.SocieteId == societeId &&
                    !u.EstSupprime &&
                    u.Email.ToLower() == request.Email.Trim().ToLower(),
                cancellationToken);

        if (emailExiste)
        {
            return Conflict(
                "Un utilisateur avec cette adresse courriel existe déjà dans cette société.");
        }

        // ========================================================
        // Création
        // ========================================================

        var utilisateur = new Utilisateur
        {
            Id = Guid.NewGuid(),

            Nom = request.Nom.Trim(),
            Prenom = request.Prenom.Trim(),
            Telephone = string.IsNullOrWhiteSpace(request.Telephone)
                ? null
                : request.Telephone.Trim(),

            Email = request.Email.Trim(),

            // IMPORTANT :
            // Role est un RoleUtilisateur et non une string
            Role = request.Role,

            SocieteId = societeId,

            Statut = true,
            EstSupprime = false,

            MotDePasseHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.MotDePasseInitial)
        };

        _context.Utilisateurs.Add(utilisateur);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new UtilisateurDto
        {
            Id = utilisateur.Id,
            Nom = utilisateur.Nom,
            Prenom = utilisateur.Prenom,
            Telephone = utilisateur.Telephone,
            Email = utilisateur.Email,
            Role = utilisateur.Role,
            Statut = utilisateur.Statut,
            SocieteId = utilisateur.SocieteId
        };

        return CreatedAtAction(
            nameof(GetUtilisateur),
            new { id = utilisateur.Id },
            dto);
    }

    // ============================================================
    // PUT : api/Utilisateurs/{id}
    // ============================================================

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UtilisateurDto>> ModifierUtilisateur(
        Guid id,
        [FromBody] ModifierUtilisateurRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("La requête est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Prenom))
            return BadRequest("Le prénom est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("L'adresse courriel est obligatoire.");

        if (!Enum.IsDefined(typeof(RoleUtilisateur), request.Role))
            return BadRequest("Le rôle sélectionné est invalide.");

        var utilisateurConnecte =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(
                u =>
                    u.Id == id &&
                    !u.EstSupprime,
                cancellationToken);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        // ========================================================
        // Protection de l'Administrateur
        // ========================================================

        if (utilisateurConnecte.Role == RoleUtilisateur.Gestionnaire)
        {
            // Le Gestionnaire ne peut modifier que sa société
            if (utilisateur.SocieteId != utilisateurConnecte.SocieteId)
                return Forbid();

            // Impossible pour un Gestionnaire de modifier un Admin
            if (utilisateur.Role == RoleUtilisateur.Administrateur)
                return Forbid();

            // Impossible d'attribuer le rôle Administrateur
            if (request.Role == RoleUtilisateur.Administrateur)
                return Forbid();
        }

        // ========================================================
        // Protection de son propre compte
        // ========================================================

        if (utilisateur.Id == utilisateurConnecte.Id)
        {
            // On ne permet pas de modifier son rôle
            if (request.Role != utilisateur.Role)
            {
                return BadRequest(
                    "Vous ne pouvez pas modifier votre propre rôle.");
            }

            // On ne permet pas de changer sa société
            if (request.SocieteId.HasValue &&
                request.SocieteId.Value != utilisateur.SocieteId)
            {
                return BadRequest(
                    "Vous ne pouvez pas modifier votre propre société.");
            }

            // On ne permet pas de désactiver son propre compte
            if (!request.Statut)
            {
                return BadRequest(
                    "Vous ne pouvez pas désactiver votre propre compte.");
            }
        }

        // ========================================================
        // Détermination de la société
        // ========================================================

        Guid societeId;

        if (utilisateurConnecte.Role == RoleUtilisateur.Administrateur)
        {
            societeId = request.SocieteId ?? utilisateur.SocieteId;
        }
        else
        {
            societeId = utilisateurConnecte.SocieteId;

            if (request.SocieteId.HasValue &&
                request.SocieteId.Value != societeId)
            {
                return Forbid();
            }
        }

        // ========================================================
        // Société cible
        // ========================================================

        var societe = await _context.Societes
            .FirstOrDefaultAsync(
                s =>
                    s.Id == societeId &&
                    !s.EstSupprime,
                cancellationToken);

        if (societe is null)
            return BadRequest("La société sélectionnée n'existe pas.");

        if (!societe.Actif && request.Statut)
        {
            return BadRequest(
                "Impossible d'activer un utilisateur dans une société inactive.");
        }

        // ========================================================
        // Email unique dans la société
        // ========================================================

        var emailNormalise = request.Email.Trim().ToLower();

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(
                u =>
                    u.Id != id &&
                    u.SocieteId == societeId &&
                    !u.EstSupprime &&
                    u.Email.ToLower() == emailNormalise,
                cancellationToken);

        if (emailExiste)
        {
            return Conflict(
                "Un autre utilisateur avec cette adresse courriel existe déjà dans cette société.");
        }

        // ========================================================
        // Mise à jour
        // ========================================================

        utilisateur.Nom = request.Nom.Trim();
        utilisateur.Prenom = request.Prenom.Trim();

        utilisateur.Telephone =
            string.IsNullOrWhiteSpace(request.Telephone)
                ? null
                : request.Telephone.Trim();

        utilisateur.Email = request.Email.Trim();

        // IMPORTANT :
        // Role est un enum
        utilisateur.Role = request.Role;

        utilisateur.Statut = request.Statut;
        utilisateur.SocieteId = societeId;

        // ========================================================
        // Nouveau mot de passe facultatif
        // ========================================================

        if (!string.IsNullOrWhiteSpace(request.NouveauMotDePasse))
        {
            if (request.NouveauMotDePasse.Length < 8)
            {
                return BadRequest(
                    "Le nouveau mot de passe doit contenir au moins 8 caractères.");
            }

            utilisateur.MotDePasseHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.NouveauMotDePasse);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new UtilisateurDto
        {
            Id = utilisateur.Id,
            Nom = utilisateur.Nom,
            Prenom = utilisateur.Prenom,
            Telephone = utilisateur.Telephone,
            Email = utilisateur.Email,
            Role = utilisateur.Role,
            Statut = utilisateur.Statut,
            SocieteId = utilisateur.SocieteId
        };

        return Ok(dto);
    }

    // ============================================================
    // PATCH : api/Utilisateurs/{id}/statut
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutUtilisateurRequest request,
        CancellationToken cancellationToken)
    {
        var utilisateurConnecte =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(
                u =>
                    u.Id == id &&
                    !u.EstSupprime,
                cancellationToken);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        // ========================================================
        // Impossible de modifier son propre statut
        // ========================================================

        if (utilisateur.Id == utilisateurConnecte.Id)
        {
            return BadRequest(
                "Vous ne pouvez pas modifier votre propre statut.");
        }

        // ========================================================
        // Gestionnaire
        // ========================================================

        if (utilisateurConnecte.Role == RoleUtilisateur.Gestionnaire)
        {
            // Un Gestionnaire ne gère que sa société
            if (utilisateur.SocieteId != utilisateurConnecte.SocieteId)
                return Forbid();

            // Impossible de désactiver/modifier un Administrateur
            if (utilisateur.Role == RoleUtilisateur.Administrateur)
                return Forbid();
        }

        // ========================================================
        // Si activation : vérifier que la société est active
        // ========================================================

        if (request.Statut)
        {
            var societeActive = await _context.Societes
                .AnyAsync(
                    s =>
                        s.Id == utilisateur.SocieteId &&
                        !s.EstSupprime &&
                        s.Actif,
                    cancellationToken);

            if (!societeActive)
            {
                return BadRequest(
                    "Impossible d'activer un utilisateur dans une société inactive.");
            }
        }

        utilisateur.Statut = request.Statut;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // DELETE : api/Utilisateurs/{id}
    // ============================================================

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SupprimerUtilisateur(
        Guid id,
        CancellationToken cancellationToken)
    {
        var utilisateurConnecte =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateurConnecte is null)
            return Unauthorized();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(
                u =>
                    u.Id == id &&
                    !u.EstSupprime,
                cancellationToken);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        // ========================================================
        // Impossible de se supprimer soi-même
        // ========================================================

        if (utilisateur.Id == utilisateurConnecte.Id)
        {
            return BadRequest(
                "Vous ne pouvez pas supprimer votre propre compte.");
        }

        // ========================================================
        // Impossible de supprimer un Administrateur
        // ========================================================

        if (utilisateur.Role == RoleUtilisateur.Administrateur)
        {
            return Forbid();
        }

        // ========================================================
        // Gestionnaire : uniquement sa société
        // ========================================================

        if (utilisateurConnecte.Role == RoleUtilisateur.Gestionnaire)
        {
            if (utilisateur.SocieteId != utilisateurConnecte.SocieteId)
                return Forbid();
        }

        // ========================================================
        // Suppression logique
        // ========================================================

        utilisateur.EstSupprime = true;
        utilisateur.Statut = false;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // UTILITAIRE : utilisateur connecté
    // ============================================================

    private async Task<UtilisateurConnecteInfo?>
        GetUtilisateurConnecteAsync(
            CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
            return null;

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == currentUserId.Value &&
                !u.EstSupprime &&
                u.Statut &&
                u.SocieteId != Guid.Empty)
            .Select(u => new UtilisateurConnecteInfo
            {
                Id = u.Id,
                SocieteId = u.SocieteId,

                // Role est un RoleUtilisateur
                Role = u.Role
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ============================================================
    // UTILITAIRE : récupération de l'ID depuis les claims JWT
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("Id");

        if (Guid.TryParse(value, out var id))
            return id;

        return null;
    }

    // ============================================================
    // DTO UTILISATEUR CONNECTÉ
    // ============================================================

    private sealed class UtilisateurConnecteInfo
    {
        public Guid Id { get; set; }

        public Guid SocieteId { get; set; }

        // IMPORTANT :
        // Le type doit être RoleUtilisateur et non string.
        public RoleUtilisateur Role { get; set; }
    }
}

// =================================================================
// DTOs
// =================================================================

public record UtilisateurDto
{
    public Guid Id { get; init; }

    public string Nom { get; init; } = string.Empty;

    public string Prenom { get; init; } = string.Empty;

    public string? Telephone { get; init; }

    public string Email { get; init; } = string.Empty;

    public RoleUtilisateur Role { get; init; }

    public bool Statut { get; init; }

    public Guid SocieteId { get; init; }
}

public record CreerUtilisateurRequest(
    string Nom,
    string Prenom,
    string? Telephone,
    string Email,
    RoleUtilisateur Role,
    string MotDePasseInitial,
    Guid? SocieteId
);

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

public record ModifierStatutUtilisateurRequest(
    bool Statut
);