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
public class OptionsDeductionFiscalesController : ControllerBase
{
    private readonly MarionDbContext _context;

    public OptionsDeductionFiscalesController(
        MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/OptionsDeductionFiscales?anneeFiscale=2026
    // Liste des options de la société connectée
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "OptionsDeductionFiscales.Read")]
    public async Task<IActionResult> GetOptions(
        [FromQuery] int? anneeFiscale,
        CancellationToken cancellationToken)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var query = _context.OptionsDeductionFiscales
            .AsNoTracking()
            .Where(o =>
                o.SocieteId == societeId.Value &&
                !o.EstSupprime);

        if (anneeFiscale.HasValue)
        {
            if (anneeFiscale.Value < 1900 ||
                anneeFiscale.Value > 2100)
            {
                return BadRequest(new
                {
                    Message = "L'année fiscale doit être comprise entre 1900 et 2100."
                });
            }

            query = query.Where(
                o => o.AnneeFiscale == anneeFiscale.Value);
        }

        var options = await query
            .OrderByDescending(o => o.AnneeFiscale)
            .Select(o => new
            {
                o.Id,
                o.SocieteId,
                o.AnneeFiscale,
                o.ModeDeduction,
                o.DateConfirmation,
                o.EstVerrouillee,
                o.DateVerrouillage,
                o.Observations,
                o.DateCreation
            })
            .ToListAsync(cancellationToken);

        return Ok(options);
    }

    // ============================================================
    // GET : api/OptionsDeductionFiscales/{id}
    // Détail d'une option
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "OptionsDeductionFiscales.Read")]
    public async Task<IActionResult> GetOption(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'option est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var option = await _context.OptionsDeductionFiscales
            .AsNoTracking()
            .Where(o =>
                o.Id == id &&
                !o.EstSupprime &&
                o.SocieteId == societeId.Value)
            .Select(o => new
            {
                o.Id,
                o.SocieteId,
                o.AnneeFiscale,
                o.ModeDeduction,
                o.DateConfirmation,
                o.EstVerrouillee,
                o.DateVerrouillage,
                o.Observations,
                o.DateCreation
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (option == null)
        {
            return NotFound(new
            {
                Message = "L'option de déduction fiscale est introuvable."
            });
        }

        return Ok(option);
    }

    // ============================================================
    // POST : api/OptionsDeductionFiscales
    // Création d'une option annuelle
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "OptionsDeductionFiscales.Create")]
    public async Task<IActionResult> CreerOption(
        [FromBody] OptionDeductionFiscaleRequest dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de l'option sont obligatoires."
            });
        }

        var erreur = ValiderOption(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var existe = await _context.OptionsDeductionFiscales
            .AnyAsync(
                o =>
                    o.SocieteId == societeId.Value &&
                    o.AnneeFiscale == dto.AnneeFiscale &&
                    !o.EstSupprime,
                cancellationToken);

        if (existe)
        {
            return Conflict(new
            {
                Message = $"Une option de déduction fiscale existe déjà pour l'année {dto.AnneeFiscale}."
            });
        }

        var maintenant = DateTime.UtcNow;

        var option = new OptionDeductionFiscale
        {
            Id = Guid.NewGuid(),
            SocieteId = societeId.Value,
            AnneeFiscale = dto.AnneeFiscale,
            ModeDeduction = dto.ModeDeduction,
            Observations = NormaliserTexte(dto.Observations),

            DateConfirmation = null,
            EstVerrouillee = false,
            DateVerrouillage = null,

            DateCreation = maintenant,
            EstSupprime = false
        };

        _context.OptionsDeductionFiscales.Add(option);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Protection contre deux créations simultanées.
            return Conflict(new
            {
                Message = "Une option existe déjà pour cette société et cette année fiscale."
            });
        }

        return CreatedAtAction(
            nameof(GetOption),
            new { id = option.Id },
            new
            {
                option.Id,
                option.SocieteId,
                option.AnneeFiscale,
                option.ModeDeduction,
                option.DateConfirmation,
                option.EstVerrouillee,
                option.DateVerrouillage,
                option.Observations,
                option.DateCreation
            });
    }

    // ============================================================
    // PUT : api/OptionsDeductionFiscales/{id}
    // Modification d'une option non verrouillée
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OptionsDeductionFiscales.Update")]
    public async Task<IActionResult> ModifierOption(
        Guid id,
        [FromBody] OptionDeductionFiscaleRequest dto,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'option est invalide."
            });
        }

        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de l'option sont obligatoires."
            });
        }

        var erreur = ValiderOption(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var option = await _context.OptionsDeductionFiscales
            .FirstOrDefaultAsync(
                o =>
                    o.Id == id &&
                    !o.EstSupprime &&
                    o.SocieteId == societeId.Value,
                cancellationToken);

        if (option == null)
        {
            return NotFound(new
            {
                Message = "L'option de déduction fiscale est introuvable."
            });
        }

        if (option.EstVerrouillee)
        {
            return BadRequest(new
            {
                Message = "Cette option est verrouillée et ne peut plus être modifiée."
            });
        }

        // Une option déjà utilisée dans une déclaration
        // conserve son choix historique.
        var utilisee = await _context.DeclarationsFiscales
            .IgnoreQueryFilters()
            .AnyAsync(
                d =>
                    d.OptionDeductionFiscaleId == option.Id,
                cancellationToken);

        if (utilisee)
        {
            return BadRequest(new
            {
                Message = "Cette option est déjà associée à une déclaration fiscale. Sa modification n'est plus autorisée."
            });
        }

        var anneeDejaUtilisee = await _context.OptionsDeductionFiscales
            .AnyAsync(
                o =>
                    o.Id != option.Id &&
                    o.SocieteId == societeId.Value &&
                    o.AnneeFiscale == dto.AnneeFiscale &&
                    !o.EstSupprime,
                cancellationToken);

        if (anneeDejaUtilisee)
        {
            return Conflict(new
            {
                Message = $"Une autre option existe déjà pour l'année {dto.AnneeFiscale}."
            });
        }

        option.AnneeFiscale = dto.AnneeFiscale;
        option.ModeDeduction = dto.ModeDeduction;
        option.Observations = NormaliserTexte(dto.Observations);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // POST : api/OptionsDeductionFiscales/{id}/verrouiller
    // Confirmation et verrouillage définitif
    // ============================================================

    [HttpPost("{id:guid}/verrouiller")]
    [Authorize(Policy = "OptionsDeductionFiscales.Update")]
    public async Task<IActionResult> VerrouillerOption(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'option est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var option = await _context.OptionsDeductionFiscales
            .FirstOrDefaultAsync(
                o =>
                    o.Id == id &&
                    !o.EstSupprime &&
                    o.SocieteId == societeId.Value,
                cancellationToken);

        if (option == null)
        {
            return NotFound(new
            {
                Message = "L'option de déduction fiscale est introuvable."
            });
        }

        if (option.EstVerrouillee)
        {
            return BadRequest(new
            {
                Message = "Cette option est déjà verrouillée."
            });
        }

        var maintenant = DateTime.UtcNow;

        option.DateConfirmation = maintenant;
        option.EstVerrouillee = true;
        option.DateVerrouillage = maintenant;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = "L'option de déduction fiscale a été confirmée et verrouillée.",
            option.Id,
            option.AnneeFiscale,
            option.ModeDeduction,
            option.DateConfirmation,
            option.EstVerrouillee,
            option.DateVerrouillage
        });
    }

    // ============================================================
    // DELETE : api/OptionsDeductionFiscales/{id}
    // Suppression logique d'une option non utilisée
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "OptionsDeductionFiscales.Delete")]
    public async Task<IActionResult> SupprimerOption(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'option est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var option = await _context.OptionsDeductionFiscales
            .FirstOrDefaultAsync(
                o =>
                    o.Id == id &&
                    !o.EstSupprime &&
                    o.SocieteId == societeId.Value,
                cancellationToken);

        if (option == null)
        {
            return NotFound(new
            {
                Message = "L'option de déduction fiscale est introuvable."
            });
        }

        var utilisee = await _context.DeclarationsFiscales
            .IgnoreQueryFilters()
            .AnyAsync(
                d => d.OptionDeductionFiscaleId == option.Id,
                cancellationToken);

        if (utilisee)
        {
            return BadRequest(new
            {
                Message = "Cette option est associée à une déclaration fiscale et ne peut pas être supprimée."
            });
        }

        option.EstSupprime = true;
        option.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static string? ValiderOption(
        OptionDeductionFiscaleRequest dto)
    {
        if (dto.AnneeFiscale < 1900 ||
            dto.AnneeFiscale > 2100)
        {
            return "L'année fiscale doit être comprise entre 1900 et 2100.";
        }

        if (!Enum.IsDefined(
                typeof(ModeDeductionCharges),
                dto.ModeDeduction))
        {
            return "Le mode de déduction fiscale sélectionné est invalide.";
        }

        if (dto.Observations?.Trim().Length > 2000)
        {
            return "Les observations ne doivent pas dépasser 2 000 caractères.";
        }

        return null;
    }

    private static string? NormaliserTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }

    // ============================================================
    // IDENTITÉ DE LA SOCIÉTÉ CONNECTÉE
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("Id");

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<Guid?> GetSocieteIdUtilisateurConnecteAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == userId.Value &&
                !u.EstSupprime &&
                u.Statut)
            .Select(u => (Guid?)u.SocieteId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

// ============================================================
// DTO DE CRÉATION ET DE MODIFICATION
// ============================================================

public class OptionDeductionFiscaleRequest
{
    public int AnneeFiscale { get; set; }

    public ModeDeductionCharges ModeDeduction { get; set; }

    public string? Observations { get; set; }
}
