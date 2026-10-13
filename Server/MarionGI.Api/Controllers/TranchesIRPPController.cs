
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
    public async Task<ActionResult<IEnumerable<TrancheIRPPDto>>> ObtenirToutes(
        [FromQuery] int? anneeFiscale)
    {
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
    public async Task<ActionResult<TrancheIRPPDto>> ObtenirParId(
        Guid id)
    {
        var tranche = await _context.TranchesIRPP
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == id && !t.EstSupprime);

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
            request.OrdreTranche);

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
            Description = Nettoyer(request.Description)
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
            .FirstOrDefaultAsync(
                t => t.Id == id && !t.EstSupprime);

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
            .FirstOrDefaultAsync(
                t => t.Id == id && !t.EstSupprime);

        if (tranche == null)
        {
            return NotFound(new
            {
                message = "La tranche IRPP demandée est introuvable."
            });
        }

        tranche.EstSupprime = true;
        tranche.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========================================================
    // VALIDATION
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

        if (request.SeuilMax <= request.SeuilMin)
        {
            return "Le seuil maximum doit être supérieur au seuil minimum.";
        }

        if (request.Taux < 0 || request.Taux > 100)
        {
            return "Le taux doit être compris entre 0 et 100.";
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

        return null;
    }

    // ========================================================
    // DÉTECTION DES DOUBLONS ET DES CHEVAUCHEMENTS
    // ========================================================

    private async Task<string?> VerifierConflits(
        int anneeFiscale,
        decimal seuilMin,
        decimal seuilMax,
        int ordreTranche,
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

        var ordreExiste = await query.AnyAsync(
            t => t.OrdreTranche == ordreTranche);

        if (ordreExiste)
        {
            return "Une tranche utilise déjà cet ordre pour cette année fiscale.";
        }

        // Les bornes sont considérées comme inclusives.
        // Deux intervalles qui se touchent sont donc en conflit.
        var seuilsChevauchent = await query.AnyAsync(t =>
            t.SeuilMin <= seuilMax &&
            t.SeuilMax >= seuilMin);

        if (seuilsChevauchent)
        {
            return "Les seuils chevauchent une autre tranche de cette année fiscale.";
        }

        return null;
    }

    // ========================================================
    // MAPPING
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
            Description = tranche.Description,
            DateCreation = tranche.DateCreation
        };
    }

    private static string Nettoyer(string valeur)
    {
        return valeur.Trim();
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
    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================

public class CreerTrancheIRPPRequest
{
    public int AnneeFiscale { get; set; }
    public decimal SeuilMin { get; set; }
    public decimal SeuilMax { get; set; }
    public decimal Taux { get; set; }
    public int OrdreTranche { get; set; }
    public string Description { get; set; } = string.Empty;
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierTrancheIRPPRequest : CreerTrancheIRPPRequest
{
}
