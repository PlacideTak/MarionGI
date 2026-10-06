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
public class ContratsController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ContratsController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET: api/Contrats
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<IActionResult> GetContrats(
        [FromQuery] StatutContrat? statut)
    {
        var societeId = GetSocieteId();

        if (!societeId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Société introuvable dans le jeton."
            });
        }

        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        // ========================================================
        // Filtrage selon le rôle
        // ========================================================

        query = ApplyOwnershipFilter(query);

        // ========================================================
        // Filtre statut
        // ========================================================

        if (statut.HasValue)
        {
            query = query.Where(c => c.Statut == statut.Value);
        }

        // ========================================================
        // Projection DTO
        // ========================================================

        var result = await query
            .OrderByDescending(c => c.DateCreation)
            .Select(c => new
            {
                c.Id,
                c.Reference,

                c.UniteLocativeId,
                c.LocataireId,

                c.DateDebut,
                c.DateFin,

                c.MontantLoyer,
                c.MontantCaution,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.Statut,

                // ==================================================
                // Unité locative
                // ==================================================

                UniteLocative = c.UniteLocative == null
                    ? null
                    : new
                    {
                        c.UniteLocative.Id,
                        c.UniteLocative.Reference,
                        c.UniteLocative.Type,
                        c.UniteLocative.Superficie,
                        c.UniteLocative.Loyer,
                        c.UniteLocative.Statut,

                        c.UniteLocative.BienImmobilierId,

                        // Informations du bien parent
                        BienReference =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Reference
                                : null,

                        BienAdresse =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Adresse
                                : null,

                        BienVille =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Ville
                                : null,

                        SocieteId =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.SocieteId
                                : Guid.Empty
                    },

                // ==================================================
                // Locataire
                // ==================================================

                Locataire = c.Locataire == null
                    ? null
                    : new
                    {
                        c.Locataire.Id,
                        c.Locataire.Nom,
                        c.Locataire.Prenom,

                        NomComplet =
                            c.Locataire.Prenom
                            + " "
                            + c.Locataire.Nom,

                        c.Locataire.Email,
                        c.Locataire.Telephone
                    }
            })
            .ToListAsync();

        return Ok(result);
    }

    // ============================================================
    // GET: api/Contrats/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var societeId = GetSocieteId();

        if (!societeId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Société introuvable dans le jeton."
            });
        }

        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                c.Id == id &&
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&
                c.UniteLocative.BienImmobilier.SocieteId
                    == societeId.Value);

        // ========================================================
        // Sécurité propriétaire / locataire
        // ========================================================

        query = ApplyOwnershipFilter(query);

        var contrat = await query
            .Select(c => new
            {
                c.Id,
                c.Reference,

                c.UniteLocativeId,
                c.LocataireId,

                c.DateDebut,
                c.DateFin,

                c.MontantLoyer,
                c.MontantCaution,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.Statut,

                // ==================================================
                // Unité locative
                // ==================================================

                UniteLocative = c.UniteLocative == null
                    ? null
                    : new
                    {
                        c.UniteLocative.Id,
                        c.UniteLocative.Reference,
                        c.UniteLocative.Type,
                        c.UniteLocative.Superficie,
                        c.UniteLocative.Loyer,
                        c.UniteLocative.Statut,

                        c.UniteLocative.BienImmobilierId,

                        // Informations du bien
                        BienReference =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Reference
                                : null,

                        BienAdresse =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Adresse
                                : null,

                        BienVille =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.Ville
                                : null,

                        SocieteId =
                            c.UniteLocative.BienImmobilier != null
                                ? c.UniteLocative.BienImmobilier.SocieteId
                                : Guid.Empty
                    },

                // ==================================================
                // Locataire
                // ==================================================

                Locataire = c.Locataire == null
                    ? null
                    : new
                    {
                        c.Locataire.Id,
                        c.Locataire.Nom,
                        c.Locataire.Prenom,

                        NomComplet =
                            c.Locataire.Prenom
                            + " "
                            + c.Locataire.Nom,

                        c.Locataire.Email,
                        c.Locataire.Telephone
                    },

                // ==================================================
                // Paiements
                // ==================================================

                Paiements = c.Paiements
                    .Where(p => !p.EstSupprime)
                    .OrderByDescending(p => p.DatePaiement)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.DatePaiement,
                        p.ModePaiement,
                        p.StatutTransaction,
                        p.ReferenceTransactionOperateur,
                        p.NumeroQuittance,
                        p.EnregistreParUtilisateurId
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (contrat == null)
        {
            return NotFound(new
            {
                message =
                    "Contrat introuvable ou accès non autorisé."
            });
        }

        return Ok(contrat);
    }

    // ============================================================
    // POST: api/Contrats
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "Contrats.Create")]
    public async Task<IActionResult> Creer(
        [FromBody] CreerContratRequest request)
    {
        var societeId = GetSocieteId();

        if (!societeId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Société introuvable dans le jeton."
            });
        }

        // ========================================================
        // Validation de la référence
        // ========================================================

        var reference = request.Reference?.Trim();

        if (string.IsNullOrWhiteSpace(reference))
        {
            return BadRequest(new
            {
                message =
                    "La référence du contrat est obligatoire."
            });
        }

        if (reference.Length > 50)
        {
            return BadRequest(new
            {
                message =
                    "La référence du contrat ne peut pas dépasser 50 caractères."
            });
        }

        // ========================================================
        // Vérifier l'unicité de la référence
        // ========================================================

        var referenceExiste = await _context.Contrats
            .AnyAsync(c =>
                c.Reference == reference);

        if (referenceExiste)
        {
            return BadRequest(new
            {
                message =
                    $"La référence de contrat « {reference} » est déjà utilisée."
            });
        }

        // ========================================================
        // Validation des dates
        // ========================================================

        if (request.DateDebut >= request.DateFin)
        {
            return BadRequest(new
            {
                message =
                    "La date de début doit être antérieure à la date de fin."
            });
        }

        // ========================================================
        // Validation du loyer
        // ========================================================

        if (request.MontantLoyer <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Le montant du loyer doit être supérieur à zéro."
            });
        }

        // ========================================================
        // Validation de la caution
        // ========================================================

        if (request.MontantCaution < 0)
        {
            return BadRequest(new
            {
                message =
                    "Le montant de la caution ne peut pas être négatif."
            });
        }

        // ========================================================
        // Validation du délai de tolérance
        // ========================================================

        if (request.DelaiJoursTolerance < 0)
        {
            return BadRequest(new
            {
                message =
                    "Le délai de tolérance ne peut pas être négatif."
            });
        }

        // ========================================================
        // Vérifier l'unité locative
        // ========================================================

        var uniteLocative = await _context.UnitesLocatives
            .Include(u => u.BienImmobilier)
            .FirstOrDefaultAsync(u =>
                u.Id == request.UniteLocativeId &&
                u.BienImmobilier != null &&
                u.BienImmobilier.SocieteId == societeId.Value);

        if (uniteLocative == null)
        {
            return BadRequest(new
            {
                message =
                    "Unité locative introuvable ou appartenant à une autre société."
            });
        }

        // ========================================================
        // Vérifier le locataire
        // ========================================================

        var locataire = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == request.LocataireId &&
                u.SocieteId == societeId.Value);

        if (locataire == null)
        {
            return BadRequest(new
            {
                message =
                    "Locataire introuvable ou appartenant à une autre société."
            });
        }

        // ========================================================
        // Vérifier qu'il n'existe pas déjà un contrat actif
        // ========================================================

        var contratActifExiste = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.UniteLocativeId == request.UniteLocativeId &&
                c.Statut == StatutContrat.Actif);

        if (contratActifExiste)
        {
            return BadRequest(new
            {
                message =
                    "Cette unité locative possède déjà un contrat actif."
            });
        }

        // ========================================================
        // Création
        // ========================================================

        var contrat = new Contrat
        {
            Id = Guid.NewGuid(),

            Reference = reference,

            UniteLocativeId =
                request.UniteLocativeId,

            LocataireId =
                request.LocataireId,

            DateDebut =
                request.DateDebut,

            DateFin =
                request.DateFin,

            MontantLoyer =
                request.MontantLoyer,

            MontantCaution =
                request.MontantCaution,

            FrequencePaiement =
                request.FrequencePaiement,

            DelaiJoursTolerance =
                request.DelaiJoursTolerance,

            Statut =
                StatutContrat.Actif,

            DateCreation =
                DateTime.UtcNow,

            EstSupprime = false
        };

        // ========================================================
        // Synchroniser l'unité
        // ========================================================

        uniteLocative.Statut =
            StatutDisponibilite.Loue;

        _context.Contrats.Add(contrat);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = contrat.Id },
            new
            {
                contrat.Id,
                contrat.Reference,
                contrat.UniteLocativeId,
                contrat.LocataireId,
                contrat.DateDebut,
                contrat.DateFin,
                contrat.MontantLoyer,
                contrat.MontantCaution,
                contrat.FrequencePaiement,
                contrat.DelaiJoursTolerance,
                contrat.Statut
            });
    }

    // ============================================================
    // PUT: api/Contrats/{id}
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Contrats.Update")]
    public async Task<IActionResult> Modifier(
        Guid id,
        [FromBody] ModifierContratRequest request)
    {
        var societeId = GetSocieteId();

        if (!societeId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Société introuvable dans le jeton."
            });
        }

        // ========================================================
        // Validation
        // ========================================================

        if (request.DateDebut >= request.DateFin)
        {
            return BadRequest(new
            {
                message =
                    "La date de début doit être antérieure à la date de fin."
            });
        }

        if (request.MontantLoyer <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Le montant du loyer doit être supérieur à zéro."
            });
        }

        if (request.MontantCaution < 0)
        {
            return BadRequest(new
            {
                message =
                    "Le montant de la caution ne peut pas être négatif."
            });
        }

        if (request.DelaiJoursTolerance < 0)
        {
            return BadRequest(new
            {
                message =
                    "Le délai de tolérance ne peut pas être négatif."
            });
        }

        // ========================================================
        // Récupérer le contrat
        // ========================================================

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                !c.EstSupprime &&
                c.Id == id &&
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&
                c.UniteLocative.BienImmobilier.SocieteId
                    == societeId.Value);

        if (contrat == null)
        {
            return NotFound(new
            {
                message =
                    "Contrat introuvable ou accès non autorisé."
            });
        }

        // ========================================================
        // Vérifier le passage vers Actif
        // ========================================================

        if (request.Statut == StatutContrat.Actif)
        {
            var autreContratActif =
                await _context.Contrats
                    .AnyAsync(c =>
                        !c.EstSupprime &&
                        c.Id != contrat.Id &&
                        c.UniteLocativeId ==
                            contrat.UniteLocativeId &&
                        c.Statut ==
                            StatutContrat.Actif);

            if (autreContratActif)
            {
                return BadRequest(new
                {
                    message =
                        "Cette unité possède déjà un autre contrat actif."
                });
            }
        }

        // ========================================================
        // Mise à jour
        // ========================================================

        // IMPORTANT :
        // La référence n'est pas modifiée.
        // Elle reste définitive pour ce contrat.

        contrat.DateDebut =
            request.DateDebut;

        contrat.DateFin =
            request.DateFin;

        contrat.MontantLoyer =
            request.MontantLoyer;

        contrat.MontantCaution =
            request.MontantCaution;

        contrat.FrequencePaiement =
            request.FrequencePaiement;

        contrat.DelaiJoursTolerance =
            request.DelaiJoursTolerance;

        contrat.Statut =
            request.Statut;

        // ========================================================
        // Synchroniser le statut de l'unité
        // ========================================================

        if (request.Statut == StatutContrat.Actif)
        {
            contrat.UniteLocative!.Statut =
                StatutDisponibilite.Loue;
        }
        else if (
            request.Statut == StatutContrat.Resilie ||
            request.Statut == StatutContrat.Expire)
        {
            var autreContratActif =
                await _context.Contrats.AnyAsync(c =>
                    !c.EstSupprime &&
                    c.Id != contrat.Id &&
                    c.UniteLocativeId ==
                        contrat.UniteLocativeId &&
                    c.Statut ==
                        StatutContrat.Actif);

            if (!autreContratActif)
            {
                contrat.UniteLocative!.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // DELETE: api/Contrats/{id}
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Contrats.Delete")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var societeId = GetSocieteId();

        if (!societeId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Société introuvable dans le jeton."
            });
        }

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                !c.EstSupprime &&
                c.Id == id &&
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&
                c.UniteLocative.BienImmobilier.SocieteId
                    == societeId.Value);

        if (contrat == null)
        {
            return NotFound(new
            {
                message =
                    "Contrat introuvable ou accès non autorisé."
            });
        }

        // ========================================================
        // Soft delete
        // ========================================================

        contrat.EstSupprime = true;
        contrat.DateSuppression = DateTime.UtcNow;

        // ========================================================
        // Vérifier s'il reste un autre contrat actif
        // ========================================================

        var autreContratActif =
            await _context.Contrats.AnyAsync(c =>
                !c.EstSupprime &&
                c.Id != contrat.Id &&
                c.UniteLocativeId ==
                    contrat.UniteLocativeId &&
                c.Statut ==
                    StatutContrat.Actif);

        if (!autreContratActif &&
            contrat.UniteLocative != null)
        {
            contrat.UniteLocative.Statut =
                StatutDisponibilite.Disponible;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Contrat archivé avec succès."
        });
    }

    // ============================================================
    // RÉCUPÉRER LA SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private Guid? GetSocieteId()
    {
        var claim =
            User.FindFirst("SocieteId")?.Value;

        if (Guid.TryParse(
                claim,
                out var societeId))
        {
            return societeId;
        }

        return null;
    }

    // ============================================================
    // FILTRE ABAC
    // ============================================================

    private IQueryable<Contrat> ApplyOwnershipFilter(
        IQueryable<Contrat> query)
    {
        var roles = User.Claims
            .Where(c =>
                c.Type == ClaimTypes.Role ||
                c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        // ========================================================
        // Administrateur / Admin / Gestionnaire
        // ========================================================

        if (roles.Any(r =>
            r.Equals(
                "Administrateur",
                StringComparison.OrdinalIgnoreCase)
            ||
            r.Equals(
                "Gestionnaire",
                StringComparison.OrdinalIgnoreCase)
            ||
            r.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase)))
        {
            return query;
        }

        // ========================================================
        // Locataire
        // ========================================================

        if (roles.Any(r =>
            r.Equals(
                "Locataire",
                StringComparison.OrdinalIgnoreCase)))
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ??
                User.FindFirstValue("sub");

            if (!Guid.TryParse(
                    userIdClaim,
                    out var userId))
            {
                return query.Where(_ => false);
            }

            return query.Where(
                c => c.LocataireId == userId);
        }

        // ========================================================
        // Agent / autres rôles
        // ========================================================

        return query.Where(_ => false);
    }

    // ============================================================
    // DTO DE CRÉATION
    // ============================================================

    public class CreerContratRequest
    {
        public string Reference { get; set; } = string.Empty;

        public Guid UniteLocativeId { get; set; }

        public Guid LocataireId { get; set; }

        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        public decimal MontantLoyer { get; set; }

        public decimal MontantCaution { get; set; }

        public FrequencePaiement FrequencePaiement { get; set; }
            = FrequencePaiement.Mensuel;

        public int DelaiJoursTolerance { get; set; } = 5;
    }

    // ============================================================
    // DTO DE MODIFICATION
    // ============================================================

    public class ModifierContratRequest
    {
        public DateTime DateDebut { get; set; }

        public DateTime DateFin { get; set; }

        public decimal MontantLoyer { get; set; }

        public decimal MontantCaution { get; set; }

        public FrequencePaiement FrequencePaiement { get; set; }

        public int DelaiJoursTolerance { get; set; }

        public StatutContrat Statut { get; set; }
    }
}