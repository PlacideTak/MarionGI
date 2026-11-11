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
                message = "La date de début doit précéder ou être égale à la date de fin."
            });
        }

        var query = _context.ChargesBiens
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                !c.BienImmobilier.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value);

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
            if (!Enum.IsDefined(typeof(TypeCharge), typeCharge.Value))
            {
                return BadRequest(new
                {
                    message = "Le type de charge est invalide."
                });
            }

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
                DateFacture = c.DateFacture,
                DatePaiement = c.DatePaiement,

                TypeCharge = c.TypeCharge,
                Description = c.Description,

                NomFournisseur = c.NomFournisseur,
                NumeroContribuableFournisseur =
                    c.NumeroContribuableFournisseur,

                MontantTTC = c.MontantTTC,
                MontantHT = c.MontantHT,
                MontantTVA = c.MontantTVA,

                ReferenceFacture = c.ReferenceFacture,
                CheminJustificatif = c.CheminJustificatif,

                StatutDeductibilite = c.StatutDeductibilite,
                MontantDeductibleFiscalement =
                    c.MontantDeductibleFiscalement,

                JustificationNonDeductibilite =
                    c.JustificationNonDeductibilite,

                DateValidationFiscale = c.DateValidationFiscale,
                Observations = c.Observations,

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
                !c.BienImmobilier.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value)
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
                DateFacture = c.DateFacture,
                DatePaiement = c.DatePaiement,

                TypeCharge = c.TypeCharge,
                Description = c.Description,

                NomFournisseur = c.NomFournisseur,
                NumeroContribuableFournisseur =
                    c.NumeroContribuableFournisseur,

                MontantTTC = c.MontantTTC,
                MontantHT = c.MontantHT,
                MontantTVA = c.MontantTVA,

                ReferenceFacture = c.ReferenceFacture,
                CheminJustificatif = c.CheminJustificatif,

                StatutDeductibilite = c.StatutDeductibilite,
                MontantDeductibleFiscalement =
                    c.MontantDeductibleFiscalement,

                JustificationNonDeductibilite =
                    c.JustificationNonDeductibilite,

                DateValidationFiscale = c.DateValidationFiscale,
                Observations = c.Observations,

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
            request.NomFournisseur,
            request.NumeroContribuableFournisseur,
            request.CheminJustificatif,
            request.Observations);

        if (erreur != null)
            return BadRequest(new { message = erreur });

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

        var charge = new ChargeBien
        {
            Id = Guid.NewGuid(),

            BienImmobilierId = request.BienImmobilierId,
            UniteLocativeId = request.UniteLocativeId,

            DateDepense = request.DateDepense,
            DateFacture = request.DateFacture,
            DatePaiement = request.DatePaiement,

            TypeCharge = request.TypeCharge,
            Description = request.Description.Trim(),

            NomFournisseur = NettoyerTexte(request.NomFournisseur),
            NumeroContribuableFournisseur =
                NettoyerTexte(request.NumeroContribuableFournisseur),

            MontantTTC = request.MontantTTC,
            MontantHT = request.MontantHT,
            MontantTVA = request.MontantTVA,

            ReferenceFacture = NettoyerTexte(request.ReferenceFacture),
            CheminJustificatif = NettoyerTexte(request.CheminJustificatif),

            // Toute nouvelle charge doit être qualifiée fiscalement.
            StatutDeductibilite = StatutDeductibilite.AQualifier,
            MontantDeductibleFiscalement = null,
            JustificationNonDeductibilite = null,
            DateValidationFiscale = null,

            Observations = NettoyerTexte(request.Observations),

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
                charge.MontantTTC,
                charge.StatutDeductibilite
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
            request.NomFournisseur,
            request.NumeroContribuableFournisseur,
            request.CheminJustificatif,
            request.Observations);

        if (erreur != null)
            return BadRequest(new { message = erreur });

        var charge = await _context.ChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                !c.BienImmobilier.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value,
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

        // Vérifier si les données ayant une incidence fiscale
        // ont été modifiées.
        var qualificationARevoir =
            charge.BienImmobilierId != request.BienImmobilierId ||
            charge.UniteLocativeId != request.UniteLocativeId ||
            charge.DateDepense != request.DateDepense ||
            charge.DateFacture != request.DateFacture ||
            charge.DatePaiement != request.DatePaiement ||
            charge.TypeCharge != request.TypeCharge ||
            charge.Description != request.Description.Trim() ||
            charge.NomFournisseur != NettoyerTexte(request.NomFournisseur) ||
            charge.NumeroContribuableFournisseur !=
                NettoyerTexte(request.NumeroContribuableFournisseur) ||
            charge.MontantTTC != request.MontantTTC ||
            charge.MontantHT != request.MontantHT ||
            charge.MontantTVA != request.MontantTVA ||
            charge.ReferenceFacture != NettoyerTexte(request.ReferenceFacture) ||
            charge.CheminJustificatif != NettoyerTexte(request.CheminJustificatif);

        charge.BienImmobilierId = request.BienImmobilierId;
        charge.UniteLocativeId = request.UniteLocativeId;

        charge.DateDepense = request.DateDepense;
        charge.DateFacture = request.DateFacture;
        charge.DatePaiement = request.DatePaiement;

        charge.TypeCharge = request.TypeCharge;
        charge.Description = request.Description.Trim();

        charge.NomFournisseur = NettoyerTexte(request.NomFournisseur);
        charge.NumeroContribuableFournisseur =
            NettoyerTexte(request.NumeroContribuableFournisseur);

        charge.MontantTTC = request.MontantTTC;
        charge.MontantHT = request.MontantHT;
        charge.MontantTVA = request.MontantTVA;

        charge.ReferenceFacture = NettoyerTexte(request.ReferenceFacture);
        charge.CheminJustificatif = NettoyerTexte(request.CheminJustificatif);

        charge.Observations = NettoyerTexte(request.Observations);

        // Une modification des données fiscales invalide
        // la qualification précédemment effectuée.
        if (qualificationARevoir)
        {
            charge.StatutDeductibilite =
                StatutDeductibilite.AQualifier;

            charge.MontantDeductibleFiscalement = null;
            charge.JustificationNonDeductibilite = null;
            charge.DateValidationFiscale = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = qualificationARevoir
                ? "Charge modifiée. Sa qualification fiscale doit être réexaminée."
                : "Charge modifiée avec succès.",

            charge.Id,
            charge.BienImmobilierId,
            charge.UniteLocativeId,
            charge.DateDepense,
            charge.TypeCharge,
            charge.MontantTTC,
            charge.StatutDeductibilite,
            charge.MontantDeductibleFiscalement
        });
    }


    // ============================================================
    // PUT : api/ChargesBiens/{id}/qualification-fiscale
    // Qualification fiscale d'une charge
    // ============================================================
    [HttpPut("{id:guid}/qualification-fiscale")]
    [Authorize(Policy = "Charges.Update")]
    public async Task<IActionResult> QualifierCharge(
        Guid id,
        [FromBody] QualifierChargeBienRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        if (!Enum.IsDefined(
            typeof(StatutDeductibilite),
            request.StatutDeductibilite))
        {
            return BadRequest(new
            {
                message = "Le statut de déductibilité est invalide."
            });
        }

        var charge = await _context.ChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                !c.BienImmobilier.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (charge == null)
        {
            return NotFound(new
            {
                message = "Charge introuvable."
            });
        }

        var justification = NettoyerTexte(
            request.JustificationNonDeductibilite);

        // Vérification du montant déductible
        switch (request.StatutDeductibilite)
        {
            case StatutDeductibilite.AQualifier:

                if (request.MontantDeductibleFiscalement.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "Une charge à qualifier ne peut pas avoir de montant déductible."
                    });
                }

                break;

            case StatutDeductibilite.NonDeductible:

                if (request.MontantDeductibleFiscalement.HasValue &&
                    request.MontantDeductibleFiscalement.Value != 0)
                {
                    return BadRequest(new
                    {
                        message = "Une charge non déductible doit avoir un montant déductible égal à zéro ou nul."
                    });
                }

                if (string.IsNullOrWhiteSpace(justification))
                {
                    return BadRequest(new
                    {
                        message = "La justification de la non-déductibilité est obligatoire."
                    });
                }

                break;

            case StatutDeductibilite.Deductible:

                if (!request.MontantDeductibleFiscalement.HasValue ||
                    request.MontantDeductibleFiscalement.Value <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Le montant déductible doit être supérieur à zéro."
                    });
                }

                if (request.MontantDeductibleFiscalement.Value >
                    charge.MontantTTC)
                {
                    return BadRequest(new
                    {
                        message = "Le montant déductible ne peut pas dépasser le montant TTC de la charge."
                    });
                }

                break;

            case StatutDeductibilite.PartiellementDeductible:

                if (!request.MontantDeductibleFiscalement.HasValue ||
                    request.MontantDeductibleFiscalement.Value <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Le montant partiellement déductible doit être supérieur à zéro."
                    });
                }

                if (request.MontantDeductibleFiscalement.Value >=
                    charge.MontantTTC)
                {
                    return BadRequest(new
                    {
                        message = "Pour une déductibilité partielle, le montant déductible doit être inférieur au montant TTC."
                    });
                }

                break;
        }

        // Enregistrement de la qualification fiscale
        charge.StatutDeductibilite = request.StatutDeductibilite;

        charge.MontantDeductibleFiscalement =
            request.StatutDeductibilite == StatutDeductibilite.Deductible ||
            request.StatutDeductibilite == StatutDeductibilite.PartiellementDeductible
                ? request.MontantDeductibleFiscalement
                : null;

        charge.JustificationNonDeductibilite =
            request.StatutDeductibilite == StatutDeductibilite.NonDeductible
                ? justification
                : null;

        charge.DateValidationFiscale =
            request.StatutDeductibilite == StatutDeductibilite.AQualifier
                ? null
                : DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Qualification fiscale enregistrée avec succès.",
            charge.Id,
            charge.StatutDeductibilite,
            charge.MontantDeductibleFiscalement,
            charge.JustificationNonDeductibilite,
            charge.DateValidationFiscale
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
                !c.BienImmobilier.EstSupprime &&
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
    // VALIDATION DES DONNÉES
    // ============================================================
    private static string? ValiderCharge(
        DateTime dateDepense,
        TypeCharge typeCharge,
        string? description,
        decimal montantTTC,
        decimal? montantHT,
        decimal? montantTVA,
        string? referenceFacture,
        string? nomFournisseur,
        string? numeroContribuableFournisseur,
        string? cheminJustificatif,
        string? observations)
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

        if (DepasseLongueur(referenceFacture, 100))
            return "La référence de facture ne doit pas dépasser 100 caractères.";

        if (DepasseLongueur(nomFournisseur, 200))
            return "Le nom du fournisseur ne doit pas dépasser 200 caractères.";

        if (DepasseLongueur(numeroContribuableFournisseur, 100))
            return "Le numéro de contribuable du fournisseur ne doit pas dépasser 100 caractères.";

        if (DepasseLongueur(cheminJustificatif, 1000))
            return "Le chemin du justificatif ne doit pas dépasser 1000 caractères.";

        if (DepasseLongueur(observations, 2000))
            return "Les observations ne doivent pas dépasser 2000 caractères.";

        return null;
    }

    private static bool DepasseLongueur(
        string? valeur,
        int longueurMax)
    {
        return !string.IsNullOrWhiteSpace(valeur) &&
               valeur.Trim().Length > longueurMax;
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
// DTO DE CONSULTATION
// ============================================================
public class ChargeBienListeDto
{
    public Guid Id { get; set; }

    public Guid BienImmobilierId { get; set; }
    public string NomBienImmobilier { get; set; } = string.Empty;

    public Guid? UniteLocativeId { get; set; }
    public string? ReferenceUnite { get; set; }

    public DateTime DateDepense { get; set; }
    public DateTime? DateFacture { get; set; }
    public DateTime? DatePaiement { get; set; }

    public TypeCharge TypeCharge { get; set; }
    public string Description { get; set; } = string.Empty;

    public string? NomFournisseur { get; set; }
    public string? NumeroContribuableFournisseur { get; set; }

    public decimal MontantTTC { get; set; }
    public decimal? MontantHT { get; set; }
    public decimal? MontantTVA { get; set; }

    public string? ReferenceFacture { get; set; }
    public string? CheminJustificatif { get; set; }

    public StatutDeductibilite StatutDeductibilite { get; set; }

    public decimal? MontantDeductibleFiscalement { get; set; }

    public string? JustificationNonDeductibilite { get; set; }

    public DateTime? DateValidationFiscale { get; set; }

    public string? Observations { get; set; }

    public DateTime DateCreation { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================
public class CreerChargeBienRequest
{
    public Guid BienImmobilierId { get; set; }
    public Guid? UniteLocativeId { get; set; }

    public DateTime DateDepense { get; set; }
    public DateTime? DateFacture { get; set; }
    public DateTime? DatePaiement { get; set; }

    public TypeCharge TypeCharge { get; set; }
    public string Description { get; set; } = string.Empty;

    public string? NomFournisseur { get; set; }
    public string? NumeroContribuableFournisseur { get; set; }

    public decimal MontantTTC { get; set; }
    public decimal? MontantHT { get; set; }
    public decimal? MontantTVA { get; set; }

    public string? ReferenceFacture { get; set; }
    public string? CheminJustificatif { get; set; }

    public string? Observations { get; set; }
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================
public class ModifierChargeBienRequest : CreerChargeBienRequest
{
}

// ============================================================
// DTO DE QUALIFICATION FISCALE
// ============================================================
public class QualifierChargeBienRequest
{
    public StatutDeductibilite StatutDeductibilite { get; set; }

    public decimal? MontantDeductibleFiscalement { get; set; }

    public string? JustificationNonDeductibilite { get; set; }
}
