
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrestatairesController : ControllerBase
{
    private readonly MarionDbContext _context;

    public PrestatairesController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // Récupération de l'utilisateur connecté
    // ============================================================

    private async Task<Utilisateur?> GetUtilisateurConnecteAsync()
    {
        var utilisateurIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("Id");

        if (!Guid.TryParse(utilisateurIdClaim, out var utilisateurId))
            return null;

        return await _context.Utilisateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == utilisateurId &&
                !u.EstSupprime &&
                u.Statut);
    }

    // ============================================================
    // GET : api/Prestataires
    // Liste des prestataires de la société
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "Prestataires.Read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? recherche = null,
        [FromQuery] TypePrestataire? typePrestataire = null,
        [FromQuery] bool? actif = null)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var query = _context.Prestataires
            .AsNoTracking()
            .Where(p =>
                p.SocieteId == utilisateur.SocieteId &&
                !p.EstSupprime);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = recherche.Trim();

            query = query.Where(p =>
                p.Nom.Contains(terme) ||
                p.Prenom.Contains(terme) ||
                (p.NomEntreprise != null &&
                 p.NomEntreprise.Contains(terme)) ||
                p.Telephone.Contains(terme) ||
                p.Specialite.Contains(terme));
        }

        if (typePrestataire.HasValue)
        {
            if (!Enum.IsDefined(
                    typeof(TypePrestataire),
                    typePrestataire.Value))
            {
                return BadRequest(new
                {
                    message = "Le type de prestataire est invalide."
                });
            }

            query = query.Where(p =>
                p.TypePrestataire == typePrestataire.Value);
        }

        if (actif.HasValue)
        {
            query = query.Where(p => p.Actif == actif.Value);
        }

        var prestataires = await query
            .OrderBy(p => p.NomEntreprise ?? p.Nom)
            .ThenBy(p => p.Prenom)
            .Select(p => new PrestataireDto
            {
                Id = p.Id,
                TypePrestataire = p.TypePrestataire,
                Nom = p.Nom,
                Prenom = p.Prenom,
                NomEntreprise = p.NomEntreprise,
                Specialite = p.Specialite,
                Telephone = p.Telephone,
                Courriel = p.Courriel,
                Adresse = p.Adresse,
                Ville = p.Ville,
                NumeroContribuable = p.NumeroContribuable,
                Observations = p.Observations,
                Actif = p.Actif,
                DateCreation = p.DateCreation
            })
            .ToListAsync();

        return Ok(prestataires);
    }

    // ============================================================
    // GET : api/Prestataires/{id}
    // Détail d'un prestataire
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Prestataires.Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var prestataire = await _context.Prestataires
            .AsNoTracking()
            .Where(p =>
                p.Id == id &&
                p.SocieteId == utilisateur.SocieteId &&
                !p.EstSupprime)
            .Select(p => new PrestataireDto
            {
                Id = p.Id,
                TypePrestataire = p.TypePrestataire,
                Nom = p.Nom,
                Prenom = p.Prenom,
                NomEntreprise = p.NomEntreprise,
                Specialite = p.Specialite,
                Telephone = p.Telephone,
                Courriel = p.Courriel,
                Adresse = p.Adresse,
                Ville = p.Ville,
                NumeroContribuable = p.NumeroContribuable,
                Observations = p.Observations,
                Actif = p.Actif,
                DateCreation = p.DateCreation
            })
            .FirstOrDefaultAsync();

        if (prestataire is null)
        {
            return NotFound(new
            {
                message = "Prestataire introuvable."
            });
        }

        return Ok(prestataire);
    }

    // ============================================================
    // POST : api/Prestataires
    // Création d'un prestataire
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "Prestataires.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreerPrestataireDto dto)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var erreur = ValiderPrestataire(
            dto.TypePrestataire,
            dto.Nom,
            dto.Prenom,
            dto.NomEntreprise,
            dto.Specialite,
            dto.Telephone,
            dto.Courriel);

        if (erreur is not null)
            return BadRequest(new { message = erreur });

        var prestataire = new Prestataire
        {
            TypePrestataire = dto.TypePrestataire,
            Nom = dto.Nom.Trim(),
            Prenom = dto.Prenom?.Trim() ?? string.Empty,
            NomEntreprise = Nettoyer(dto.NomEntreprise),
            Specialite = dto.Specialite.Trim(),
            Telephone = dto.Telephone.Trim(),
            Courriel = Nettoyer(dto.Courriel),
            Adresse = Nettoyer(dto.Adresse),
            Ville = Nettoyer(dto.Ville),
            NumeroContribuable = Nettoyer(dto.NumeroContribuable),
            Observations = Nettoyer(dto.Observations),
            Actif = true,
            SocieteId = utilisateur.SocieteId
        };

        _context.Prestataires.Add(prestataire);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = prestataire.Id },
            new
            {
                message = "Prestataire créé avec succès.",
                id = prestataire.Id
            });
    }

    // ============================================================
    // PUT : api/Prestataires/{id}
    // Modification d'un prestataire
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Prestataires.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ModifierPrestataireDto dto)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var prestataire = await _context.Prestataires
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.SocieteId == utilisateur.SocieteId &&
                !p.EstSupprime);

        if (prestataire is null)
        {
            return NotFound(new
            {
                message = "Prestataire introuvable."
            });
        }

        var erreur = ValiderPrestataire(
            dto.TypePrestataire,
            dto.Nom,
            dto.Prenom,
            dto.NomEntreprise,
            dto.Specialite,
            dto.Telephone,
            dto.Courriel);

        if (erreur is not null)
            return BadRequest(new { message = erreur });

        prestataire.TypePrestataire = dto.TypePrestataire;
        prestataire.Nom = dto.Nom.Trim();
        prestataire.Prenom = dto.Prenom?.Trim() ?? string.Empty;
        prestataire.NomEntreprise = Nettoyer(dto.NomEntreprise);
        prestataire.Specialite = dto.Specialite.Trim();
        prestataire.Telephone = dto.Telephone.Trim();
        prestataire.Courriel = Nettoyer(dto.Courriel);
        prestataire.Adresse = Nettoyer(dto.Adresse);
        prestataire.Ville = Nettoyer(dto.Ville);
        prestataire.NumeroContribuable =
            Nettoyer(dto.NumeroContribuable);
        prestataire.Observations = Nettoyer(dto.Observations);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Prestataire modifié avec succès."
        });
    }

    // ============================================================
    // PATCH : api/Prestataires/{id}/statut
    // Activation ou désactivation
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    [Authorize(Policy = "Prestataires.Update")]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutPrestataireDto dto)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var prestataire = await _context.Prestataires
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.SocieteId == utilisateur.SocieteId &&
                !p.EstSupprime);

        if (prestataire is null)
        {
            return NotFound(new
            {
                message = "Prestataire introuvable."
            });
        }

        prestataire.Actif = dto.Actif;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = dto.Actif
                ? "Prestataire activé avec succès."
                : "Prestataire désactivé avec succès.",
            prestataire.Actif
        });
    }

    // ============================================================
    // DELETE : api/Prestataires/{id}
    // Suppression logique
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Prestataires.Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var prestataire = await _context.Prestataires
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.SocieteId == utilisateur.SocieteId &&
                !p.EstSupprime);

        if (prestataire is null)
        {
            return NotFound(new
            {
                message = "Prestataire introuvable."
            });
        }

        // Préserver l'historique des interventions.
        var possedeInterventions = await _context.TicketsIntervenants
            .AnyAsync(i =>
                i.PrestataireId == prestataire.Id &&
                !i.EstSupprime);

        if (possedeInterventions)
        {
            return Conflict(new
            {
                message =
                    "Ce prestataire possède un historique d'interventions. " +
                    "Désactivez-le plutôt que de le supprimer."
            });
        }

        prestataire.EstSupprime = true;
        prestataire.DateSuppression = DateTime.UtcNow;
        prestataire.Actif = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Prestataire supprimé avec succès."
        });
    }

    // ============================================================
    // Validation et utilitaires
    // ============================================================

    private static string? ValiderPrestataire(
        TypePrestataire typePrestataire,
        string? nom,
        string? prenom,
        string? nomEntreprise,
        string? specialite,
        string? telephone,
        string? courriel)
    {
        if (!Enum.IsDefined(typeof(TypePrestataire), typePrestataire))
            return "Le type de prestataire est invalide.";

        if (string.IsNullOrWhiteSpace(nom))
            return "Le nom est obligatoire.";

        if (string.IsNullOrWhiteSpace(specialite))
            return "La spécialité est obligatoire.";

        if (string.IsNullOrWhiteSpace(telephone))
            return "Le téléphone est obligatoire.";

        if (typePrestataire == TypePrestataire.PersonnePhysique &&
            string.IsNullOrWhiteSpace(prenom))
        {
            return "Le prénom est obligatoire pour une personne physique.";
        }

        if (typePrestataire != TypePrestataire.PersonnePhysique &&
            string.IsNullOrWhiteSpace(nomEntreprise))
        {
            return "Le nom de l'entreprise est obligatoire.";
        }

        if (!string.IsNullOrWhiteSpace(courriel) &&
            !new EmailAddressAttribute().IsValid(courriel))
        {
            return "L'adresse courriel est invalide.";
        }

        return null;
    }

    private static string? Nettoyer(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }
}

// ============================================================
// DTO : réponse de l'API
// ============================================================

public class PrestataireDto
{
    public Guid Id { get; set; }
    public TypePrestataire TypePrestataire { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? NomEntreprise { get; set; }
    public string Specialite { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Courriel { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? NumeroContribuable { get; set; }
    public string? Observations { get; set; }
    public bool Actif { get; set; }
    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO : création
// ============================================================

public class CreerPrestataireDto
{
    public TypePrestataire TypePrestataire { get; set; }
        = TypePrestataire.PersonnePhysique;

    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? NomEntreprise { get; set; }
    public string Specialite { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Courriel { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? NumeroContribuable { get; set; }
    public string? Observations { get; set; }
}

// ============================================================
// DTO : modification
// ============================================================

public class ModifierPrestataireDto : CreerPrestataireDto
{
}

// ============================================================
// DTO : activation / désactivation
// ============================================================

public class ModifierStatutPrestataireDto
{
    public bool Actif { get; set; }
}
