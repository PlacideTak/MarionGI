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
public class ChargesBiensController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ChargesBiensController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/ChargesBiens
    // Filtres facultatifs : bien, unité, type et période
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "Charges.Read")]
    public async Task<IActionResult> GetCharges(
        [FromQuery] Guid? bienImmobilierId,
        [FromQuery] Guid? uniteLocativeId,
        [FromQuery] TypeCharge? typeCharge,
        [FromQuery] DateTime? dateDebut,
        [FromQuery] DateTime? dateFin,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        if (dateDebut.HasValue &&
            dateFin.HasValue &&
            dateDebut.Value.Date > dateFin.Value.Date)
        {
            return BadRequest(new
            {
                message = "La date de début doit précéder la date de fin."
            });
        }

        var query = _context.ChargesBiens
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value &&
                !c.BienImmobilier.EstSupprime);

        if (bienImmobilierId.HasValue)
        {
            query = query.Where(c =>
                c.BienImmobilierId == bienImmobilierId.Value);
        }

        if (uniteLocativeId.HasValue)
        {
            query = query.Where(c =>
                c.UniteLocativeId == uniteLocativeId.Value);
        }

        if (typeCharge.HasValue)
        {
            query = query.Where(c =>
                c.TypeCharge == typeCharge.Value);
        }

        if (dateDebut.HasValue)
        {
            var debut = dateDebut.Value.Date;
            query = query.Where(c => c.DateDepense >= debut);
        }

        if (dateFin.HasValue)
        {
            var finExclusive = dateFin.Value.Date.AddDays(1);
            query = query.Where(c => c.DateDepense < finExclusive);
        }

        var charges = await query
            .OrderByDescending(c => c.DateDepense)
            .Select(c => new ChargeBienListeDto
            {
                Id = c.Id,
                BienImmobilierId = c.BienImmobilierId,
                NomBienImmobilier = c.BienImmobilier.Nom,

                UniteLocativeId = c.UniteLocativeId,
                ReferenceUnite = c.UniteLocative != null
                    ? c.UniteLocative.Reference
                    : null,

                DateDepense = c.DateDepense,
                TypeCharge = c.TypeCharge,
                Description = c.Description,
                MontantTTC = c.MontantTTC,
                MontantHT = c.MontantHT,
                MontantTVA = c.MontantTVA,
                ReferenceFacture = c.ReferenceFacture,
                EstDeductibleFiscalement = c.EstDeductibleFiscalement,
                JustificationNonDeductibilite =
                    c.JustificationNonDeductibilite,
                DateCreation = c.DateCreation
            })
            .ToListAsync(cancellationToken);

        return Ok(charges);
    }

    // ============================================================
    // GET : api/ChargesBiens/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Charges.Read")]
    public async Task<IActionResult> GetCharge(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var charge = await _context.ChargesBiens
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value &&
                !c.BienImmobilier.EstSupprime)
            .Select(c => new ChargeBienListeDto
            {
                Id = c.Id,
                BienImmobilierId = c.BienImmobilierId,
                NomBienImmobilier = c.BienImmobilier.Nom,

                UniteLocativeId = c.UniteLocativeId,
                ReferenceUnite = c.UniteLocative != null
                    ? c.UniteLocative.Reference
                    : null,

                DateDepense = c.DateDepense,
                TypeCharge = c.TypeCharge,
                Description = c.Description,
                MontantTTC = c.MontantTTC,
                MontantHT = c.MontantHT,
                MontantTVA = c.MontantTVA,
                ReferenceFacture = c.ReferenceFacture,
                EstDeductibleFiscalement = c.EstDeductibleFiscalement,
                JustificationNonDeductibilite =
                    c.JustificationNonDeductibilite,
                DateCreation = c.DateCreation
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (charge == null)
        {
            return NotFound(new
            {
                message = "Charge introuvable."
            });
        }

        return Ok(charge);
    }

    // ============================================================
    // POST : api/ChargesBiens
    // Administrateur / Gestionnaire selon la policy
    // ============================================================
    [HttpPost]
    [Authorize(Policy = "Charges.Create")]
    public async Task<IActionResult> CreerCharge(
        [FromBody] CreerChargeBienRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var erreur = ValiderCharge(
            request.DateDepense,
            request.TypeCharge,
            request.Description,
            request.MontantTTC,
            request.MontantHT,
            request.MontantTVA,
            request.ReferenceFacture,
            request.EstDeductibleFiscalement,
            request.JustificationNonDeductibilite);

        if (erreur != null)
            return BadRequest(new { message = erreur });

        // L'immeuble doit appartenir à la société connectée.
        var bienExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                b.Id == request.BienImmobilierId &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime,
                cancellationToken);

        if (!bienExiste)
        {
            return BadRequest(new
            {
                message = "L'immeuble sélectionné est introuvable."
            });
        }

        // Si une unité est fournie, elle doit appartenir à cet immeuble.
        if (request.UniteLocativeId.HasValue)
        {
            var uniteValide = await _context.UnitesLocatives
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == request.UniteLocativeId.Value &&
                    u.BienImmobilierId == request.BienImmobilierId &&
                    !u.EstSupprime,
                    cancellationToken);

            if (!uniteValide)
            {
                return BadRequest(new
                {
                    message =
                        "L'unité locative sélectionnée n'appartient pas à cet immeuble ou n'existe pas."
                });
            }
        }

        var charge = new ChargeBien
        {
            Id = Guid.NewGuid(),
            BienImmobilierId = request.BienImmobilierId,
            UniteLocativeId = request.UniteLocativeId,
            DateDepense = request.DateDepense,
            TypeCharge = request.TypeCharge,
            Description = request.Description.Trim(),
            MontantTTC = request.MontantTTC,
            MontantHT = request.MontantHT,
            MontantTVA = request.MontantTVA,
            ReferenceFacture = NettoyerTexte(request.ReferenceFacture),
            EstDeductibleFiscalement =
                request.EstDeductibleFiscalement,
            JustificationNonDeductibilite =
                NettoyerTexte(request.JustificationNonDeductibilite),
            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.ChargesBiens.Add(charge);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetCharge),
            new { id = charge.Id },
            new
            {
                message = "Charge enregistrée avec succès.",
                charge.Id,
                charge.BienImmobilierId,
                charge.UniteLocativeId,
                charge.DateDepense,
                charge.TypeCharge,
                charge.MontantTTC
            });
    }

    // ============================================================
    // PUT : api/ChargesBiens/{id}
    // ============================================================
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Charges.Update")]
    public async Task<IActionResult> ModifierCharge(
        Guid id,
        [FromBody] ModifierChargeBienRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var erreur = ValiderCharge(
            request.DateDepense,
            request.TypeCharge,
            request.Description,
            request.MontantTTC,
            request.MontantHT,
            request.MontantTVA,
            request.ReferenceFacture,
            request.EstDeductibleFiscalement,
            request.JustificationNonDeductibilite);

        if (erreur != null)
            return BadRequest(new { message = erreur });

        var charge = await _context.ChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value &&
                !c.BienImmobilier.EstSupprime,
                cancellationToken);

        if (charge == null)
        {
            return NotFound(new
            {
                message = "Charge introuvable."
            });
        }

        var bienExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                b.Id == request.BienImmobilierId &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime,
                cancellationToken);

        if (!bienExiste)
        {
            return BadRequest(new
            {
                message = "L'immeuble sélectionné est introuvable."
            });
        }

        if (request.UniteLocativeId.HasValue)
        {
            var uniteValide = await _context.UnitesLocatives
                .AsNoTracking()
                .AnyAsync(u =>
                    u.Id == request.UniteLocativeId.Value &&
                    u.BienImmobilierId == request.BienImmobilierId &&
                    !u.EstSupprime,
                    cancellationToken);

            if (!uniteValide)
            {
                return BadRequest(new
                {
                    message =
                        "L'unité locative sélectionnée n'appartient pas à cet immeuble ou n'existe pas."
                });
            }
        }

        charge.BienImmobilierId = request.BienImmobilierId;
        charge.UniteLocativeId = request.UniteLocativeId;
        charge.DateDepense = request.DateDepense;
        charge.TypeCharge = request.TypeCharge;
        charge.Description = request.Description.Trim();
        charge.MontantTTC = request.MontantTTC;
        charge.MontantHT = request.MontantHT;
        charge.MontantTVA = request.MontantTVA;
        charge.ReferenceFacture = NettoyerTexte(request.ReferenceFacture);
        charge.EstDeductibleFiscalement =
            request.EstDeductibleFiscalement;
        charge.JustificationNonDeductibilite =
            NettoyerTexte(request.JustificationNonDeductibilite);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Charge modifiée avec succès.",
            charge.Id,
            charge.BienImmobilierId,
            charge.UniteLocativeId,
            charge.DateDepense,
            charge.TypeCharge,
            charge.MontantTTC
        });
    }

    // ============================================================
    // DELETE : api/ChargesBiens/{id}
    // Suppression logique
    // ============================================================
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Charges.Delete")]
    public async Task<IActionResult> SupprimerCharge(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var charge = await _context.ChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (charge == null)
        {
            return NotFound(new
            {
                message = "Charge introuvable."
            });
        }

        charge.EstSupprime = true;
        charge.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Charge supprimée avec succès."
        });
    }

    // ============================================================
    // VALIDATION
    // ============================================================
    private static string? ValiderCharge(
        DateTime dateDepense,
        TypeCharge typeCharge,
        string? description,
        decimal montantTTC,
        decimal? montantHT,
        decimal? montantTVA,
        string? referenceFacture,
        bool estDeductibleFiscalement,
        string? justificationNonDeductibilite)
    {
        if (dateDepense == default)
            return "La date de dépense est obligatoire.";

        if (!Enum.IsDefined(typeof(TypeCharge), typeCharge))
            return "Le type de charge est invalide.";

        if (string.IsNullOrWhiteSpace(description))
            return "La description est obligatoire.";

        if (description.Trim().Length > 2000)
            return "La description ne doit pas dépasser 2000 caractères.";

        if (montantTTC < 0)
            return "Le montant TTC ne peut pas être négatif.";

        if (montantHT.HasValue && montantHT.Value < 0)
            return "Le montant HT ne peut pas être négatif.";

        if (montantTVA.HasValue && montantTVA.Value < 0)
            return "Le montant de TVA ne peut pas être négatif.";

        if (montantHT.HasValue &&
            montantTVA.HasValue &&
            montantHT.Value + montantTVA.Value > montantTTC + 0.01m)
        {
            return "Le montant HT et la TVA ne peuvent pas dépasser le montant TTC.";
        }

        if (!string.IsNullOrWhiteSpace(referenceFacture) &&
            referenceFacture.Trim().Length > 100)
        {
            return "La référence de facture ne doit pas dépasser 100 caractères.";
        }

        if (!string.IsNullOrWhiteSpace(justificationNonDeductibilite) &&
            justificationNonDeductibilite.Trim().Length > 1000)
        {
            return "La justification ne doit pas dépasser 1000 caractères.";
        }

        if (!estDeductibleFiscalement &&
            string.IsNullOrWhiteSpace(justificationNonDeductibilite))
        {
            return "Une justification est requise pour une charge déclarée non déductible.";
        }

        return null;
    }

    // ============================================================
    // UTILITAIRES
    // ============================================================
    private Guid? GetSocieteId()
    {
        var claim = User.FindFirst("SocieteId")?.Value;

        return Guid.TryParse(claim, out var societeId)
            ? societeId
            : null;
    }

    private static string? NettoyerTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }
}

// ============================================================
// DTOs
// ============================================================
public class ChargeBienListeDto
{
    public Guid Id { get; set; }

    public Guid BienImmobilierId { get; set; }
    public string NomBienImmobilier { get; set; } = string.Empty;

    public Guid? UniteLocativeId { get; set; }
    public string? ReferenceUnite { get; set; }

    public DateTime DateDepense { get; set; }
    public TypeCharge TypeCharge { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal MontantTTC { get; set; }
    public decimal? MontantHT { get; set; }
    public decimal? MontantTVA { get; set; }

    public string? ReferenceFacture { get; set; }
    public bool EstDeductibleFiscalement { get; set; }
    public string? JustificationNonDeductibilite { get; set; }

    public DateTime DateCreation { get; set; }
}

public class CreerChargeBienRequest
{
    public Guid BienImmobilierId { get; set; }
    public Guid? UniteLocativeId { get; set; }

    public DateTime DateDepense { get; set; }
    public TypeCharge TypeCharge { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal MontantTTC { get; set; }
    public decimal? MontantHT { get; set; }
    public decimal? MontantTVA { get; set; }

    public string? ReferenceFacture { get; set; }
    public bool EstDeductibleFiscalement { get; set; }
    public string? JustificationNonDeductibilite { get; set; }
}

public class ModifierChargeBienRequest : CreerChargeBienRequest
{
}
