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
    // GET /api/RegimesFiscaux
    // GET /api/RegimesFiscaux?estActif=true
    // ========================================================

    [HttpGet]
    [Authorize(Policy = "RegimesFiscaux.Read")]
    public async Task<ActionResult<IEnumerable<RegimeFiscalDto>>> ObtenirTous(
        [FromQuery] bool? estActif)
    {
        var query = _context.RegimesFiscaux
            .AsNoTracking()
            .Where(r => !r.EstSupprime);

        if (estActif.HasValue)
        {
            query = query.Where(
                r => r.EstActif == estActif.Value);
        }

        var regimes = await query
            .OrderBy(r => r.Libelle)
            .Select(r => VersDto(r))
            .ToListAsync();

        return Ok(regimes);
    }

    // ========================================================
    // GET : Détail d'un régime fiscal
    // GET /api/RegimesFiscaux/{id}
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

        var codeExiste = await _context.RegimesFiscaux
            .AnyAsync(r =>
                !r.EstSupprime &&
                r.Code.ToUpper() == code.ToUpper());

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

        var codeExiste = await _context.RegimesFiscaux
            .AnyAsync(r =>
                !r.EstSupprime &&
                r.Id != id &&
                r.Code.ToUpper() == code.ToUpper());

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

        regime.EstSupprime = true;
        regime.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========================================================
    // VALIDATION
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

        return null;
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
    public bool EstActif { get; set; } = true;
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierRegimeFiscalRequest
    : CreerRegimeFiscalRequest
{
}
