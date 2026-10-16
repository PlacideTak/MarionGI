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
public class ValeursFiscalesBiensController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ValeursFiscalesBiensController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // Récupération de l'utilisateur connecté
    // ============================================================

    private async Task<Utilisateur?> GetUtilisateurConnecteAsync()
    {
        var idClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("Id");

        if (!Guid.TryParse(idClaim, out var utilisateurId))
            return null;

        return await _context.Utilisateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == utilisateurId &&
                !u.EstSupprime &&
                u.Statut);
    }

    // ============================================================
    // Vérifier que le bien appartient à la société autorisée
    // ============================================================

    private async Task<bool> BienAccessibleAsync(
        Guid bienImmobilierId,
        Guid societeId)
    {
        return await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                b.Id == bienImmobilierId &&
                b.SocieteId == societeId &&
                !b.EstSupprime);
    }

    // ============================================================
    // GET : api/ValeursFiscalesBiens
    // Liste des valeurs fiscales des biens de la société
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "ValeursFiscalesBiens.Read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? bienImmobilierId = null,
        [FromQuery] int? anneeFiscale = null,
        [FromQuery] TypeValeurFiscale? typeValeur = null)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        if (anneeFiscale.HasValue &&
            (anneeFiscale.Value < 1900 ||
             anneeFiscale.Value > DateTime.UtcNow.Year + 1))
        {
            return BadRequest(new
            {
                message = "L'année fiscale indiquée est invalide."
            });
        }

        if (typeValeur.HasValue &&
            !Enum.IsDefined(typeof(TypeValeurFiscale), typeValeur.Value))
        {
            return BadRequest(new
            {
                message = "Le type de valeur fiscale est invalide."
            });
        }

        var query = _context.ValeursFiscalesBiens
            .AsNoTracking()
            .Where(v =>
                !v.EstSupprime &&
                v.BienImmobilier.SocieteId == utilisateur.SocieteId &&
                !v.BienImmobilier.EstSupprime);

        if (bienImmobilierId.HasValue)
        {
            query = query.Where(v =>
                v.BienImmobilierId == bienImmobilierId.Value);
        }

        if (anneeFiscale.HasValue)
        {
            query = query.Where(v =>
                v.AnneeFiscale == anneeFiscale.Value);
        }

        if (typeValeur.HasValue)
        {
            query = query.Where(v =>
                v.TypeValeur == typeValeur.Value);
        }

        var resultats = await query
            .OrderByDescending(v => v.AnneeFiscale)
            .ThenByDescending(v => v.DateEvaluation)
            .Select(v => new ValeurFiscaleBienDto
            {
                Id = v.Id,
                BienImmobilierId = v.BienImmobilierId,
                ReferenceBien = v.BienImmobilier.Reference,
                NomBien = v.BienImmobilier.Nom,
                AnneeFiscale = v.AnneeFiscale,
                Montant = v.Montant,
                TypeValeur = v.TypeValeur,
                DateEvaluation = v.DateEvaluation,
                DateValidationOfficielle = v.DateValidationOfficielle,
                ReferenceJustificative = v.ReferenceJustificative,
                CheminJustificatif = v.CheminJustificatif,
                Observations = v.Observations,
                DateCreation = v.DateCreation
            })
            .ToListAsync();

        return Ok(resultats);
    }

    // ============================================================
    // GET : api/ValeursFiscalesBiens/{id}
    // Détail d'une valeur fiscale
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ValeursFiscalesBiens.Read")]
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

        var valeur = await _context.ValeursFiscalesBiens
            .AsNoTracking()
            .Where(v =>
                v.Id == id &&
                !v.EstSupprime &&
                !v.BienImmobilier.EstSupprime &&
                v.BienImmobilier.SocieteId == utilisateur.SocieteId)
            .Select(v => new ValeurFiscaleBienDto
            {
                Id = v.Id,
                BienImmobilierId = v.BienImmobilierId,
                ReferenceBien = v.BienImmobilier.Reference,
                NomBien = v.BienImmobilier.Nom,
                AnneeFiscale = v.AnneeFiscale,
                Montant = v.Montant,
                TypeValeur = v.TypeValeur,
                DateEvaluation = v.DateEvaluation,
                DateValidationOfficielle = v.DateValidationOfficielle,
                ReferenceJustificative = v.ReferenceJustificative,
                CheminJustificatif = v.CheminJustificatif,
                Observations = v.Observations,
                DateCreation = v.DateCreation
            })
            .FirstOrDefaultAsync();

        if (valeur is null)
        {
            return NotFound(new
            {
                message = "Valeur fiscale introuvable."
            });
        }

        return Ok(valeur);
    }

    // ============================================================
    // POST : api/ValeursFiscalesBiens
    // Création d'une valeur fiscale
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "ValeursFiscalesBiens.Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreerValeurFiscaleBienDto dto)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var erreur = Valider(dto);

        if (erreur is not null)
            return BadRequest(new { message = erreur });

        var bienAccessible = await BienAccessibleAsync(
            dto.BienImmobilierId,
            utilisateur.SocieteId);

        if (!bienAccessible)
        {
            return NotFound(new
            {
                message = "Bien immobilier introuvable dans votre société."
            });
        }

        var valeur = new ValeurFiscaleBien
        {
            BienImmobilierId = dto.BienImmobilierId,
            AnneeFiscale = dto.AnneeFiscale,
            Montant = dto.Montant,
            TypeValeur = dto.TypeValeur,
            DateEvaluation = dto.DateEvaluation,
            DateValidationOfficielle = dto.DateValidationOfficielle,
            ReferenceJustificative = Nettoyer(dto.ReferenceJustificative),
            CheminJustificatif = Nettoyer(dto.CheminJustificatif),
            Observations = Nettoyer(dto.Observations)
        };

        _context.ValeursFiscalesBiens.Add(valeur);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = valeur.Id },
            new
            {
                message = "Valeur fiscale créée avec succès.",
                id = valeur.Id
            });
    }

    // ============================================================
    // PUT : api/ValeursFiscalesBiens/{id}
    // Modification d'une valeur fiscale
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ValeursFiscalesBiens.Update")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ModifierValeurFiscaleBienDto dto)
    {
        var utilisateur = await GetUtilisateurConnecteAsync();

        if (utilisateur is null)
        {
            return Unauthorized(new
            {
                message = "Utilisateur introuvable, supprimé ou inactif."
            });
        }

        var valeur = await _context.ValeursFiscalesBiens
            .FirstOrDefaultAsync(v =>
                v.Id == id &&
                !v.EstSupprime &&
                v.BienImmobilier.SocieteId == utilisateur.SocieteId &&
                !v.BienImmobilier.EstSupprime);

        if (valeur is null)
        {
            return NotFound(new
            {
                message = "Valeur fiscale introuvable."
            });
        }

        var erreur = Valider(dto);

        if (erreur is not null)
            return BadRequest(new { message = erreur });

        var bienAccessible = await BienAccessibleAsync(
            dto.BienImmobilierId,
            utilisateur.SocieteId);

        if (!bienAccessible)
        {
            return NotFound(new
            {
                message = "Bien immobilier introuvable dans votre société."
            });
        }

        valeur.BienImmobilierId = dto.BienImmobilierId;
        valeur.AnneeFiscale = dto.AnneeFiscale;
        valeur.Montant = dto.Montant;
        valeur.TypeValeur = dto.TypeValeur;
        valeur.DateEvaluation = dto.DateEvaluation;
        valeur.DateValidationOfficielle = dto.DateValidationOfficielle;
        valeur.ReferenceJustificative = Nettoyer(dto.ReferenceJustificative);
        valeur.CheminJustificatif = Nettoyer(dto.CheminJustificatif);
        valeur.Observations = Nettoyer(dto.Observations);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Valeur fiscale modifiée avec succès."
        });
    }

    // ============================================================
    // DELETE : api/ValeursFiscalesBiens/{id}
    // Suppression logique
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ValeursFiscalesBiens.Delete")]
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

        var valeur = await _context.ValeursFiscalesBiens
            .FirstOrDefaultAsync(v =>
                v.Id == id &&
                !v.EstSupprime &&
                !v.BienImmobilier.EstSupprime &&
                v.BienImmobilier.SocieteId == utilisateur.SocieteId);

        if (valeur is null)
        {
            return NotFound(new
            {
                message = "Valeur fiscale introuvable."
            });
        }

        valeur.EstSupprime = true;
        valeur.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // Validation
    // ============================================================

    private static string? Valider(
        CreerValeurFiscaleBienDto dto)
    {
        if (dto.BienImmobilierId == Guid.Empty)
            return "Le bien immobilier est obligatoire.";

        if (dto.AnneeFiscale < 1900 ||
            dto.AnneeFiscale > DateTime.UtcNow.Year + 1)
        {
            return "L'année fiscale est invalide.";
        }

        if (dto.Montant <= 0)
            return "Le montant doit être supérieur à zéro.";

        if (!Enum.IsDefined(typeof(TypeValeurFiscale), dto.TypeValeur))
            return "Le type de valeur fiscale est invalide.";

        if (dto.DateEvaluation == default)
            return "La date d'évaluation est obligatoire.";

        if (dto.DateValidationOfficielle.HasValue &&
            dto.DateValidationOfficielle.Value.Date <
            dto.DateEvaluation.Date)
        {
            return "La date de validation officielle ne peut pas précéder la date d'évaluation.";
        }

        if (dto.DateEvaluation.Date >
            DateTime.UtcNow.Date.AddDays(1))
        {
            return "La date d'évaluation ne peut pas être dans le futur.";
        }

        if (dto.DateValidationOfficielle.HasValue &&
            dto.DateValidationOfficielle.Value.Date >
            DateTime.UtcNow.Date.AddDays(1))
        {
            return "La date de validation officielle ne peut pas être dans le futur.";
        }

        return null;
    }

    private static string? Valider(
        ModifierValeurFiscaleBienDto dto)
    {
        return Valider((CreerValeurFiscaleBienDto)dto);
    }

    private static string? Nettoyer(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }
}

// ================================================================
// DTO de réponse
// ================================================================

public class ValeurFiscaleBienDto
{
    public Guid Id { get; set; }
    public Guid BienImmobilierId { get; set; }
    public string ReferenceBien { get; set; } = string.Empty;
    public string NomBien { get; set; } = string.Empty;
    public int AnneeFiscale { get; set; }
    public decimal Montant { get; set; }
    public TypeValeurFiscale TypeValeur { get; set; }
    public DateTime DateEvaluation { get; set; }
    public DateTime? DateValidationOfficielle { get; set; }
    public string? ReferenceJustificative { get; set; }
    public string? CheminJustificatif { get; set; }
    public string? Observations { get; set; }
    public DateTime DateCreation { get; set; }
}

// ================================================================
// DTO de création
// ================================================================

public class CreerValeurFiscaleBienDto
{
    public Guid BienImmobilierId { get; set; }
    public int AnneeFiscale { get; set; }
    public decimal Montant { get; set; }
    public TypeValeurFiscale TypeValeur { get; set; }
    public DateTime DateEvaluation { get; set; }
    public DateTime? DateValidationOfficielle { get; set; }
    public string? ReferenceJustificative { get; set; }
    public string? CheminJustificatif { get; set; }
    public string? Observations { get; set; }
}

// ================================================================
// DTO de modification
// ================================================================

public class ModifierValeurFiscaleBienDto : CreerValeurFiscaleBienDto
{
}
