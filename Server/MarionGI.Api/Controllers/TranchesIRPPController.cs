using MarionGI.Domain.Entities;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TranchesIRPPController : ControllerBase
{
    private readonly MarionDbContext _context;

    public TranchesIRPPController(MarionDbContext context)
    {
        _context = context;
    }

    // ========================================================
    // GET : Liste des tranches IRPP
    // GET /api/TranchesIRPP?anneeFiscale=2026
    // Administrateur et Gestionnaire
    // ========================================================

    [HttpGet]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<ActionResult<IEnumerable<TrancheIRPPDto>>>
        ObtenirToutes([FromQuery] int? anneeFiscale)
    {
        if (anneeFiscale.HasValue &&
            (anneeFiscale.Value < 2000 ||
             anneeFiscale.Value > 2100))
        {
            return BadRequest(new
            {
                message = "L'année fiscale doit être comprise entre 2000 et 2100."
            });
        }

        var query = _context.TranchesIRPP
            .AsNoTracking()
            .Where(t => !t.EstSupprime);

        if (anneeFiscale.HasValue)
        {
            query = query.Where(
                t => t.AnneeFiscale == anneeFiscale.Value);
        }

        var tranches = await query
            .OrderBy(t => t.AnneeFiscale)
            .ThenBy(t => t.DateDebutValidite)
            .ThenBy(t => t.OrdreTranche)
            .Select(t => VersDto(t))
            .ToListAsync();

        return Ok(tranches);
    }

    // ========================================================
    // GET : Une tranche par ID
    // GET /api/TranchesIRPP/{id}
    // Administrateur et Gestionnaire
    // ========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    public async Task<ActionResult<TrancheIRPPDto>>
        ObtenirParId(Guid id)
    {
        var tranche = await _context.TranchesIRPP
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime);

        if (tranche == null)
        {
            return NotFound(new
            {
                message = "La tranche IRPP demandée est introuvable."
            });
        }

        return Ok(VersDto(tranche));
    }

    // ========================================================
    // POST : Créer une tranche
    // POST /api/TranchesIRPP
    // Administrateur seulement
    // ========================================================

    [HttpPost]
    [Authorize(Roles = "Administrateur")]
    public async Task<ActionResult<TrancheIRPPDto>> Creer(
        [FromBody] CreerTrancheIRPPRequest request)
    {
        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var conflit = await VerifierConflits(
            request.AnneeFiscale,
            request.SeuilMin,
            request.SeuilMax,
            request.OrdreTranche,
            request.DateDebutValidite,
            request.DateFinValidite);

        if (conflit != null)
        {
            return Conflict(new { message = conflit });
        }

        var tranche = new TrancheIRPP
        {
            AnneeFiscale = request.AnneeFiscale,
            SeuilMin = request.SeuilMin,
            SeuilMax = request.SeuilMax,
            Taux = request.Taux,
            OrdreTranche = request.OrdreTranche,
            Description = Nettoyer(request.Description),
            ReferenceLegale = Nettoyer(request.ReferenceLegale),
            DateDebutValidite = request.DateDebutValidite,
            DateFinValidite = request.DateFinValidite
        };

        _context.TranchesIRPP.Add(tranche);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenirParId),
            new { id = tranche.Id },
            VersDto(tranche));
    }

    // ========================================================
    // PUT : Modifier une tranche
    // PUT /api/TranchesIRPP/{id}
    // Administrateur seulement
    // ========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrateur")]
    public async Task<ActionResult<TrancheIRPPDto>> Modifier(
        Guid id,
        [FromBody] ModifierTrancheIRPPRequest request)
    {
        var tranche = await _context.TranchesIRPP
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime);

        if (tranche == null)
        {
            return NotFound(new
            {
                message = "La tranche IRPP demandée est introuvable."
            });
        }

        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var conflit = await VerifierConflits(
            request.AnneeFiscale,
            request.SeuilMin,
            request.SeuilMax,
            request.OrdreTranche,
            request.DateDebutValidite,
            request.DateFinValidite,
            id);

        if (conflit != null)
        {
            return Conflict(new { message = conflit });
        }

        tranche.AnneeFiscale = request.AnneeFiscale;
        tranche.SeuilMin = request.SeuilMin;
        tranche.SeuilMax = request.SeuilMax;
        tranche.Taux = request.Taux;
        tranche.OrdreTranche = request.OrdreTranche;
        tranche.Description = Nettoyer(request.Description);
        tranche.ReferenceLegale = Nettoyer(request.ReferenceLegale);
        tranche.DateDebutValidite = request.DateDebutValidite;
        tranche.DateFinValidite = request.DateFinValidite;

        await _context.SaveChangesAsync();

        return Ok(VersDto(tranche));
    }

    // ========================================================
    // DELETE : Suppression logique
    // DELETE /api/TranchesIRPP/{id}
    // Administrateur seulement
    // ========================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrateur")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var tranche = await _context.TranchesIRPP
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime);

        if (tranche == null)
        {
            return NotFound(new
            {
                message = "La tranche IRPP demandée est introuvable."
            });
        }

        // La suppression logique conserve l'enregistrement
        // pour la traçabilité. Vérifier séparément les éventuelles
        // dépendances vers les calculs fiscaux avant suppression.

        tranche.EstSupprime = true;
        tranche.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========================================================
    // VALIDATION DES DONNÉES
    // ========================================================

    private static string? Valider(
        CreerTrancheIRPPRequest request)
    {
        if (request.AnneeFiscale < 2000 ||
            request.AnneeFiscale > 2100)
        {
            return "L'année fiscale doit être comprise entre 2000 et 2100.";
        }

        if (request.SeuilMin < 0)
        {
            return "Le seuil minimum ne peut pas être négatif.";
        }

        if (request.SeuilMax.HasValue &&
            request.SeuilMax.Value <= request.SeuilMin)
        {
            return "Le seuil maximum doit être strictement supérieur au seuil minimum.";
        }

        if (request.Taux < 0 || request.Taux > 1)
        {
            return "Le taux doit être compris entre 0 et 1 (0 % à 100 %).";
        }

        if (request.OrdreTranche <= 0)
        {
            return "L'ordre de la tranche doit être supérieur à zéro.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "La description de la tranche est obligatoire.";
        }

        if (request.Description.Trim().Length > 500)
        {
            return "La description ne peut pas dépasser 500 caractères.";
        }

        if (request.ReferenceLegale?.Length > 500)
        {
            return "La référence légale ne peut pas dépasser 500 caractères.";
        }

        if (request.DateDebutValidite == default)
        {
            return "La date de début de validité est obligatoire.";
        }

        if (request.DateDebutValidite.Year < 2000 ||
            request.DateDebutValidite.Year > 2100)
        {
            return "La date de début de validité est invalide.";
        }

        if (request.DateFinValidite.HasValue)
        {
            if (request.DateFinValidite.Value == default)
            {
                return "La date de fin de validité est invalide.";
            }

            if (request.DateFinValidite.Value <
                request.DateDebutValidite)
            {
                return "La date de fin doit être postérieure ou égale à la date de début.";
            }
        }

        return null;
    }

    // ========================================================
    // DÉTECTION DES CONFLITS
    // ========================================================

    private async Task<string?> VerifierConflits(
        int anneeFiscale,
        decimal seuilMin,
        decimal? seuilMax,
        int ordreTranche,
        DateTime dateDebutValidite,
        DateTime? dateFinValidite,
        Guid? idExclu = null)
    {
        var query = _context.TranchesIRPP
            .Where(t =>
                !t.EstSupprime &&
                t.AnneeFiscale == anneeFiscale);

        if (idExclu.HasValue)
        {
            query = query.Where(t => t.Id != idExclu.Value);
        }

        // Charger les règles existantes pour vérifier les conflits
        // en tenant compte des dates et des seuils.
        var existantes = await query.ToListAsync();

        foreach (var existante in existantes)
        {
            // Deux périodes de validité se chevauchent si chacune
            // commence avant la fin de l'autre période.
            var periodesSeChevauchent =
                (!dateFinValidite.HasValue ||
                 existante.DateDebutValidite <= dateFinValidite.Value) &&
                (!existante.DateFinValidite.HasValue ||
                 existante.DateFinValidite.Value >= dateDebutValidite);

            if (!periodesSeChevauchent)
            {
                continue;
            }

            if (existante.OrdreTranche == ordreTranche)
            {
                return "Une tranche utilise déjà cet ordre pour une période de validité qui se chevauche.";
            }

            // Deux intervalles de revenus se chevauchent si leurs
            // bornes se recoupent. Null signifie sans plafond.
            var seuilsSeChevauchent =
                (!seuilMax.HasValue ||
                 existante.SeuilMin < seuilMax.Value) &&
                (!existante.SeuilMax.HasValue ||
                 existante.SeuilMax.Value > seuilMin);

            if (seuilsSeChevauchent)
            {
                return "Les seuils chevauchent une autre tranche pendant une période de validité commune.";
            }
        }

        return null;
    }

    // ========================================================
    // MAPPING ENTITÉ VERS DTO
    // ========================================================

    private static TrancheIRPPDto VersDto(TrancheIRPP tranche)
    {
        return new TrancheIRPPDto
        {
            Id = tranche.Id,
            AnneeFiscale = tranche.AnneeFiscale,
            SeuilMin = tranche.SeuilMin,
            SeuilMax = tranche.SeuilMax,
            Taux = tranche.Taux,
            OrdreTranche = tranche.OrdreTranche,
            Description = tranche.Description ?? string.Empty,
            ReferenceLegale = tranche.ReferenceLegale,
            DateDebutValidite = tranche.DateDebutValidite,
            DateFinValidite = tranche.DateFinValidite,
            DateDerniereVerification = tranche.DateDerniereVerification,
            DateCreation = tranche.DateCreation
        };
    }

    // ========================================================
    // UTILITAIRE
    // ========================================================

    private static string? Nettoyer(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }
}

// ============================================================
// DTO DE RÉPONSE
// ============================================================

public class TrancheIRPPDto
{
    public Guid Id { get; set; }

    public int AnneeFiscale { get; set; }

    public decimal SeuilMin { get; set; }

    public decimal? SeuilMax { get; set; }

    public decimal Taux { get; set; }

    public int OrdreTranche { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ReferenceLegale { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================

public class CreerTrancheIRPPRequest
{
    public int AnneeFiscale { get; set; }

    public decimal SeuilMin { get; set; }

    public decimal? SeuilMax { get; set; }

    // 0.10 représente un taux de 10 %.
    public decimal Taux { get; set; }

    public int OrdreTranche { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ReferenceLegale { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierTrancheIRPPRequest : CreerTrancheIRPPRequest
{
}