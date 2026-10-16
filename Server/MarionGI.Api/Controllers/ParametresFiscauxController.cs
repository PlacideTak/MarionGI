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
public class ParametresFiscauxController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ParametresFiscauxController(MarionDbContext context)
    {
        _context = context;
    }

    // ========================================================
    // GET : Liste des paramètres fiscaux
    // ========================================================

    [HttpGet]
    [Authorize(Policy = "ParametresFiscaux.Read")]
    public async Task<ActionResult<IEnumerable<ParametreFiscalDto>>> ObtenirTous(
        [FromQuery] int? anneeFiscale,
        [FromQuery] string? codeRegle,
        [FromQuery] DateTime? dateReference,
        [FromQuery] TypeImpotFiscal? typeImpot,
        [FromQuery] TypeParametreFiscal? typeParametre,
        [FromQuery] Guid? regimeFiscalId)
    {
        var query = _context.ParametresFiscaux
            .AsNoTracking()
            .Where(p => !p.EstSupprime);

        if (anneeFiscale.HasValue)
        {
            query = query.Where(
                p => p.AnneeFiscale == anneeFiscale.Value);
        }

        if (!string.IsNullOrWhiteSpace(codeRegle))
        {
            var code = codeRegle.Trim();

            query = query.Where(
                p => p.CodeRegle == code);
        }

        if (dateReference.HasValue)
        {
            var date = dateReference.Value;

            query = query.Where(p =>
                p.DateDebutValidite <= date &&
                (!p.DateFinValidite.HasValue ||
                 p.DateFinValidite.Value >= date));
        }

        if (typeImpot.HasValue)
        {
            query = query.Where(
                p => p.TypeImpot == typeImpot.Value);
        }

        if (typeParametre.HasValue)
        {
            query = query.Where(
                p => p.TypeParametre == typeParametre.Value);
        }

        if (regimeFiscalId.HasValue)
        {
            query = query.Where(
                p => p.RegimeFiscalId == regimeFiscalId.Value);
        }

        var resultats = await query
            .OrderBy(p => p.AnneeFiscale)
            .ThenBy(p => p.TypeImpot)
            .ThenBy(p => p.CodeRegle)
            .ThenBy(p => p.DateDebutValidite)
            .Select(p => VersDto(p))
            .ToListAsync();

        return Ok(resultats);
    }

    // ========================================================
    // GET : Détail d'un paramètre fiscal
    // ========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ParametresFiscaux.Read")]
    public async Task<ActionResult<ParametreFiscalDto>> ObtenirParId(
        Guid id)
    {
        var parametre = await _context.ParametresFiscaux
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == id && !p.EstSupprime);

        if (parametre == null)
        {
            return NotFound(new
            {
                message = "Le paramètre fiscal est introuvable."
            });
        }

        return Ok(VersDto(parametre));
    }

    // ========================================================
    // POST : Créer un paramètre fiscal
    // ========================================================

    [HttpPost]
    [Authorize(Policy = "ParametresFiscaux.Create")]
    public async Task<ActionResult<ParametreFiscalDto>> Creer(
        [FromBody] CreerParametreFiscalRequest request)
    {
        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var erreurRegime = await ValiderRegimeFiscal(
            request.RegimeFiscalId);

        if (erreurRegime != null)
        {
            return BadRequest(new { message = erreurRegime });
        }

        var codeRegle = request.CodeRegle.Trim();

        var conflit = await VerifierChevauchement(
            request.AnneeFiscale,
            codeRegle,
            request.TypeImpot,
            request.RegimeFiscalId,
            request.DateDebutValidite,
            request.DateFinValidite);

        if (conflit)
        {
            return Conflict(new
            {
                message =
                    "Une règle fiscale portant ce code possède déjà une période de validité qui chevauche celle demandée pour cette année fiscale, ce type d'impôt et ce régime fiscal."
            });
        }

        var parametre = new ParametreFiscal
        {
            AnneeFiscale = request.AnneeFiscale,
            CodeRegle = codeRegle,
            TypeImpot = request.TypeImpot,
            Description = request.Description.Trim(),
            Valeur = request.Valeur,
            TypeParametre = request.TypeParametre,
            DateDebutValidite = request.DateDebutValidite,
            DateFinValidite = request.DateFinValidite,
            ReferenceLegale = NormaliserTexte(request.ReferenceLegale),
            DateDerniereVerification =
                request.DateDerniereVerification,
            ConditionsApplication =
                NormaliserTexte(request.ConditionsApplication),
            RegimeFiscalId = request.RegimeFiscalId
        };

        _context.ParametresFiscaux.Add(parametre);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenirParId),
            new { id = parametre.Id },
            VersDto(parametre));
    }

    // ========================================================
    // PUT : Modifier un paramètre fiscal
    // ========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ParametresFiscaux.Update")]
    public async Task<ActionResult<ParametreFiscalDto>> Modifier(
        Guid id,
        [FromBody] ModifierParametreFiscalRequest request)
    {
        var parametre = await _context.ParametresFiscaux
            .FirstOrDefaultAsync(
                p => p.Id == id && !p.EstSupprime);

        if (parametre == null)
        {
            return NotFound(new
            {
                message = "Le paramètre fiscal est introuvable."
            });
        }

        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var erreurRegime = await ValiderRegimeFiscal(
            request.RegimeFiscalId);

        if (erreurRegime != null)
        {
            return BadRequest(new { message = erreurRegime });
        }

        var codeRegle = request.CodeRegle.Trim();

        var conflit = await VerifierChevauchement(
            request.AnneeFiscale,
            codeRegle,
            request.TypeImpot,
            request.RegimeFiscalId,
            request.DateDebutValidite,
            request.DateFinValidite,
            id);

        if (conflit)
        {
            return Conflict(new
            {
                message =
                    "Une autre règle fiscale portant ce code possède déjà une période de validité qui chevauche celle demandée pour cette année fiscale, ce type d'impôt et ce régime fiscal."
            });
        }

        parametre.AnneeFiscale = request.AnneeFiscale;
        parametre.CodeRegle = codeRegle;
        parametre.TypeImpot = request.TypeImpot;
        parametre.Description = request.Description.Trim();
        parametre.Valeur = request.Valeur;
        parametre.TypeParametre = request.TypeParametre;
        parametre.DateDebutValidite = request.DateDebutValidite;
        parametre.DateFinValidite = request.DateFinValidite;
        parametre.ReferenceLegale =
            NormaliserTexte(request.ReferenceLegale);
        parametre.DateDerniereVerification =
            request.DateDerniereVerification;
        parametre.ConditionsApplication =
            NormaliserTexte(request.ConditionsApplication);
        parametre.RegimeFiscalId = request.RegimeFiscalId;

        await _context.SaveChangesAsync();

        return Ok(VersDto(parametre));
    }

    // ========================================================
    // DELETE : Suppression logique
    // ========================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ParametresFiscaux.Delete")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var parametre = await _context.ParametresFiscaux
            .FirstOrDefaultAsync(
                p => p.Id == id && !p.EstSupprime);

        if (parametre == null)
        {
            return NotFound(new
            {
                message = "Le paramètre fiscal est introuvable."
            });
        }

        parametre.EstSupprime = true;
        parametre.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========================================================
    // VALIDATION DES DONNÉES
    // ========================================================

    private static string? Valider(
        CreerParametreFiscalRequest request)
    {
        if (request.AnneeFiscale < 2000 ||
            request.AnneeFiscale > 2100)
        {
            return "L'année fiscale doit être comprise entre 2000 et 2100.";
        }

        if (string.IsNullOrWhiteSpace(request.CodeRegle))
        {
            return "Le code de règle est obligatoire.";
        }

        if (request.CodeRegle.Trim().Length > 100)
        {
            return "Le code de règle ne peut pas dépasser 100 caractères.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "La description est obligatoire.";
        }

        if (request.Description.Trim().Length > 1000)
        {
            return "La description ne peut pas dépasser 1000 caractères.";
        }

        if (request.Valeur < 0)
        {
            return "La valeur du paramètre ne peut pas être négative.";
        }

        if (!Enum.IsDefined(
                typeof(TypeImpotFiscal),
                request.TypeImpot))
        {
            return "Le type d'impôt est invalide.";
        }

        if (!Enum.IsDefined(
                typeof(TypeParametreFiscal),
                request.TypeParametre))
        {
            return "Le type de paramètre fiscal est invalide.";
        }

        if (request.DateDebutValidite == default)
        {
            return "La date de début de validité est obligatoire.";
        }

        if (request.DateFinValidite.HasValue &&
            request.DateFinValidite.Value <
            request.DateDebutValidite)
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

        if (!string.IsNullOrWhiteSpace(request.ConditionsApplication) &&
            request.ConditionsApplication.Trim().Length > 2000)
        {
            return "Les conditions d'application ne peuvent pas dépasser 2000 caractères.";
        }

        return null;
    }

    // ========================================================
    // VALIDATION DU RÉGIME FISCAL
    // ========================================================

    private async Task<string?> ValiderRegimeFiscal(
        Guid? regimeFiscalId)
    {
        if (!regimeFiscalId.HasValue)
        {
            return null;
        }

        var regimeExiste = await _context.RegimesFiscaux
            .AnyAsync(r =>
                r.Id == regimeFiscalId.Value &&
                !r.EstSupprime);

        if (!regimeExiste)
        {
            return "Le régime fiscal sélectionné est introuvable ou supprimé.";
        }

        return null;
    }

    // ========================================================
    // VALIDATION DES PÉRIODES DE VALIDITÉ
    // ========================================================

    private async Task<bool> VerifierChevauchement(
        int anneeFiscale,
        string codeRegle,
        TypeImpotFiscal typeImpot,
        Guid? regimeFiscalId,
        DateTime dateDebut,
        DateTime? dateFin,
        Guid? idExclu = null)
    {
        var query = _context.ParametresFiscaux
            .Where(p =>
                !p.EstSupprime &&
                p.AnneeFiscale == anneeFiscale &&
                p.CodeRegle == codeRegle &&
                p.TypeImpot == typeImpot &&
                p.RegimeFiscalId == regimeFiscalId);

        if (idExclu.HasValue)
        {
            query = query.Where(p => p.Id != idExclu.Value);
        }

        // Les périodes sont considérées comme inclusives :
        // une date de fin égale à une date de début constitue
        // donc un chevauchement.
        //
        // Une date de fin absente représente une validité
        // sans date de fin.

        return await query.AnyAsync(p =>
            (!p.DateFinValidite.HasValue ||
             p.DateFinValidite.Value >= dateDebut) &&
            (!dateFin.HasValue ||
             p.DateDebutValidite <= dateFin.Value));
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

    private static ParametreFiscalDto VersDto(
        ParametreFiscal parametre)
    {
        return new ParametreFiscalDto
        {
            Id = parametre.Id,
            AnneeFiscale = parametre.AnneeFiscale,
            CodeRegle = parametre.CodeRegle,
            TypeImpot = parametre.TypeImpot,
            Description = parametre.Description,
            Valeur = parametre.Valeur,
            TypeParametre = parametre.TypeParametre,
            DateDebutValidite = parametre.DateDebutValidite,
            DateFinValidite = parametre.DateFinValidite,
            ReferenceLegale = parametre.ReferenceLegale,
            DateDerniereVerification =
                parametre.DateDerniereVerification,
            ConditionsApplication = parametre.ConditionsApplication,
            RegimeFiscalId = parametre.RegimeFiscalId,
            DateCreation = parametre.DateCreation
        };
    }
}

// ============================================================
// DTO DE RÉPONSE
// ============================================================

public class ParametreFiscalDto
{
    public Guid Id { get; set; }

    public int AnneeFiscale { get; set; }

    public string CodeRegle { get; set; } = string.Empty;

    public TypeImpotFiscal TypeImpot { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Valeur { get; set; }

    public TypeParametreFiscal TypeParametre { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public string? ReferenceLegale { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    public string? ConditionsApplication { get; set; }

    public Guid? RegimeFiscalId { get; set; }

    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================

public class CreerParametreFiscalRequest
{
    public int AnneeFiscale { get; set; }

    public string CodeRegle { get; set; } = string.Empty;

    public TypeImpotFiscal TypeImpot { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Valeur { get; set; }

    public TypeParametreFiscal TypeParametre { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public string? ReferenceLegale { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    public string? ConditionsApplication { get; set; }

    public Guid? RegimeFiscalId { get; set; }
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierParametreFiscalRequest
    : CreerParametreFiscalRequest
{
}
