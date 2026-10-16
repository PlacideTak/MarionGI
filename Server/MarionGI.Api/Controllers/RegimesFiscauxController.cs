using MarionGI.Domain.Entities;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegimesFiscauxController : ControllerBase
{
    private readonly MarionDbContext _context;

    public RegimesFiscauxController(MarionDbContext context)
    {
        _context = context;
    }

    // ========================================================
    // GET : Liste des régimes fiscaux
    //
    // GET /api/RegimesFiscaux
    // GET /api/RegimesFiscaux?estActif=true
    // GET /api/RegimesFiscaux?dateReference=2026-01-01
    // ========================================================

    [HttpGet]
    [Authorize(Policy = "RegimesFiscaux.Read")]
    public async Task<ActionResult<IEnumerable<RegimeFiscalDto>>> ObtenirTous(
        [FromQuery] bool? estActif,
        [FromQuery] DateTime? dateReference)
    {
        var query = _context.RegimesFiscaux
            .AsNoTracking()
            .Where(r => !r.EstSupprime);

        if (estActif.HasValue)
        {
            query = query.Where(
                r => r.EstActif == estActif.Value);
        }

        if (dateReference.HasValue)
        {
            var date = dateReference.Value.Date;

            query = query.Where(r =>
                (!r.DateDebutValidite.HasValue ||
                 r.DateDebutValidite.Value <= date) &&
                (!r.DateFinValidite.HasValue ||
                 r.DateFinValidite.Value >= date));
        }

        var regimes = await query
            .OrderBy(r => r.Libelle)
            .Select(r => VersDto(r))
            .ToListAsync();

        return Ok(regimes);
    }

    // ========================================================
    // GET : Détail d'un régime fiscal
    // ========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RegimesFiscaux.Read")]
    public async Task<ActionResult<RegimeFiscalDto>> ObtenirParId(
        Guid id)
    {
        var regime = await _context.RegimesFiscaux
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == id && !r.EstSupprime);

        if (regime == null)
        {
            return NotFound(new
            {
                message = "Le régime fiscal demandé est introuvable."
            });
        }

        return Ok(VersDto(regime));
    }

    // ========================================================
    // POST : Créer un régime fiscal
    // ========================================================

    [HttpPost]
    [Authorize(Policy = "RegimesFiscaux.Create")]
    public async Task<ActionResult<RegimeFiscalDto>> Creer(
        [FromBody] CreerRegimeFiscalRequest request)
    {
        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var code = request.Code.Trim();

        var codeExiste = await CodeExisteAsync(code);

        if (codeExiste)
        {
            return Conflict(new
            {
                message = "Un régime fiscal utilise déjà ce code."
            });
        }

        var regime = new RegimeFiscal
        {
            Code = code,
            Libelle = request.Libelle.Trim(),
            Description = request.Description.Trim(),
            DateDebutValidite = request.DateDebutValidite,
            DateFinValidite = request.DateFinValidite,
            ReferenceLegale = NormaliserTexte(request.ReferenceLegale),
            DateDerniereVerification = request.DateDerniereVerification,
            EstActif = request.EstActif
        };

        _context.RegimesFiscaux.Add(regime);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenirParId),
            new { id = regime.Id },
            VersDto(regime));
    }

    // ========================================================
    // PUT : Modifier un régime fiscal
    // ========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RegimesFiscaux.Update")]
    public async Task<ActionResult<RegimeFiscalDto>> Modifier(
        Guid id,
        [FromBody] ModifierRegimeFiscalRequest request)
    {
        var regime = await _context.RegimesFiscaux
            .FirstOrDefaultAsync(
                r => r.Id == id && !r.EstSupprime);

        if (regime == null)
        {
            return NotFound(new
            {
                message = "Le régime fiscal demandé est introuvable."
            });
        }

        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var code = request.Code.Trim();

        var codeExiste = await CodeExisteAsync(code, id);

        if (codeExiste)
        {
            return Conflict(new
            {
                message = "Un autre régime fiscal utilise déjà ce code."
            });
        }

        regime.Code = code;
        regime.Libelle = request.Libelle.Trim();
        regime.Description = request.Description.Trim();
        regime.DateDebutValidite = request.DateDebutValidite;
        regime.DateFinValidite = request.DateFinValidite;
        regime.ReferenceLegale = NormaliserTexte(request.ReferenceLegale);
        regime.DateDerniereVerification = request.DateDerniereVerification;
        regime.EstActif = request.EstActif;

        await _context.SaveChangesAsync();

        return Ok(VersDto(regime));
    }

    // ========================================================
    // DELETE : Suppression logique
    // ========================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RegimesFiscaux.Delete")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var regime = await _context.RegimesFiscaux
            .FirstOrDefaultAsync(
                r => r.Id == id && !r.EstSupprime);

        if (regime == null)
        {
            return NotFound(new
            {
                message = "Le régime fiscal demandé est introuvable."
            });
        }

        // Un régime utilisé par des paramètres fiscaux ne doit pas
        // être supprimé logiquement, afin de préserver la cohérence
        // et l'historique des règles qui lui sont associées.

        var possedeParametres = await _context.ParametresFiscaux
            .AnyAsync(p =>
                !p.EstSupprime &&
                p.RegimeFiscalId == id);

        if (possedeParametres)
        {
            return Conflict(new
            {
                message =
                    "Ce régime fiscal est associé à des paramètres fiscaux. Désactivez-le plutôt que de le supprimer."
            });
        }

        regime.EstSupprime = true;
        regime.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========================================================
    // VALIDATION DES DONNÉES
    // ========================================================

    private static string? Valider(
        CreerRegimeFiscalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return "Le code du régime fiscal est obligatoire.";
        }

        if (request.Code.Trim().Length > 50)
        {
            return "Le code ne peut pas dépasser 50 caractères.";
        }

        if (string.IsNullOrWhiteSpace(request.Libelle))
        {
            return "Le libellé du régime fiscal est obligatoire.";
        }

        if (request.Libelle.Trim().Length > 150)
        {
            return "Le libellé ne peut pas dépasser 150 caractères.";
        }

        if (request.Description == null)
        {
            return "La description ne peut pas être nulle.";
        }

        if (request.Description.Trim().Length > 1000)
        {
            return "La description ne peut pas dépasser 1000 caractères.";
        }

        if (request.DateDebutValidite.HasValue &&
            request.DateDebutValidite.Value == default)
        {
            return "La date de début de validité est invalide.";
        }

        if (request.DateFinValidite.HasValue &&
            request.DateFinValidite.Value == default)
        {
            return "La date de fin de validité est invalide.";
        }

        if (request.DateDebutValidite.HasValue &&
            request.DateFinValidite.HasValue &&
            request.DateFinValidite.Value.Date <
            request.DateDebutValidite.Value.Date)
        {
            return "La date de fin doit être supérieure ou égale à la date de début.";
        }

        if (request.DateDerniereVerification.HasValue &&
            request.DateDerniereVerification.Value == default)
        {
            return "La date de dernière vérification est invalide.";
        }

        if (!string.IsNullOrWhiteSpace(request.ReferenceLegale) &&
            request.ReferenceLegale.Trim().Length > 1000)
        {
            return "La référence légale ne peut pas dépasser 1000 caractères.";
        }

        return null;
    }

    // ========================================================
    // VÉRIFICATION DE L'UNICITÉ DU CODE
    // ========================================================

    private async Task<bool> CodeExisteAsync(
        string code,
        Guid? idExclu = null)
    {
        var codeNormalise = code.Trim().ToUpper();

        var query = _context.RegimesFiscaux
            .Where(r =>
                !r.EstSupprime &&
                r.Code.ToUpper() == codeNormalise);

        if (idExclu.HasValue)
        {
            query = query.Where(r => r.Id != idExclu.Value);
        }

        return await query.AnyAsync();
    }

    // ========================================================
    // OUTILS
    // ========================================================

    private static string? NormaliserTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }

    // ========================================================
    // MAPPING DTO
    // ========================================================

    private static RegimeFiscalDto VersDto(RegimeFiscal regime)
    {
        return new RegimeFiscalDto
        {
            Id = regime.Id,
            Code = regime.Code,
            Libelle = regime.Libelle,
            Description = regime.Description,
            DateDebutValidite = regime.DateDebutValidite,
            DateFinValidite = regime.DateFinValidite,
            ReferenceLegale = regime.ReferenceLegale,
            DateDerniereVerification = regime.DateDerniereVerification,
            EstActif = regime.EstActif,
            DateCreation = regime.DateCreation
        };
    }
}

// ============================================================
// DTO DE RÉPONSE
// ============================================================

public class RegimeFiscalDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Libelle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public string? ReferenceLegale { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    public bool EstActif { get; set; }

    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================

public class CreerRegimeFiscalRequest
{
    public string Code { get; set; } = string.Empty;

    public string Libelle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public string? ReferenceLegale { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    public bool EstActif { get; set; } = true;
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierRegimeFiscalRequest
    : CreerRegimeFiscalRequest
{
}
