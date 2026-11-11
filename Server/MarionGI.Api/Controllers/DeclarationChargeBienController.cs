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
public class DeclarationChargeBiensController : ControllerBase
{
    private readonly MarionDbContext _context;

    public DeclarationChargeBiensController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/DeclarationChargeBiens/declaration/{declarationId}
    // Consulter les charges retenues dans une déclaration
    // ============================================================

    [HttpGet("declaration/{declarationId:guid}")]
    [Authorize(Policy = "DeclarationChargeBiens.Read")]
    public async Task<IActionResult> GetChargesDeclaration(
        Guid declarationId,
        CancellationToken cancellationToken)
    {
        if (declarationId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la déclaration est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declarationExiste = await _context.DeclarationsFiscales
            .AsNoTracking()
            .AnyAsync(d =>
                d.Id == declarationId &&
                !d.EstSupprime &&
                d.SocieteId == societeId.Value,
                cancellationToken);

        if (!declarationExiste)
        {
            return NotFound(new
            {
                Message = "La déclaration fiscale est introuvable."
            });
        }

        var charges = await _context.DeclarationsChargesBiens
            .AsNoTracking()
            .Where(c =>
                c.DeclarationFiscaleId == declarationId &&
                !c.EstSupprime &&
                c.DeclarationFiscale.SocieteId == societeId.Value)
            .OrderByDescending(c => c.DateDepense)
            .Select(c => new
            {
                c.Id,
                c.DeclarationFiscaleId,
                c.ChargeBienId,
                c.DescriptionCharge,
                c.DateDepense,
                c.MontantTTC,
                c.StatutDeductibilite,
                c.MontantDeductibleFiscalement,
                c.MontantRetenu,
                c.DateCreation
            })
            .ToListAsync(cancellationToken);

        return Ok(charges);
    }

    // ============================================================
    // GET : api/DeclarationChargeBiens/disponibles/{declarationId}
    // Charges admissibles de tous les immeubles de la société
    // ============================================================

    [HttpGet("disponibles/{declarationId:guid}")]
    [Authorize(Policy = "DeclarationChargeBiens.Read")]
    public async Task<IActionResult> GetChargesDisponibles(
        Guid declarationId,
        CancellationToken cancellationToken)
    {
        if (declarationId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la déclaration est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declaration = await _context.DeclarationsFiscales
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == declarationId &&
                !d.EstSupprime &&
                d.SocieteId == societeId.Value,
                cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = "La déclaration fiscale est introuvable."
            });
        }

        if (declaration.TypeImpot != TypeImpotFiscal.RevenusFonciers ||
            declaration.BienImmobilierId.HasValue)
        {
            return BadRequest(new
            {
                Message = "La sélection globale des charges est réservée aux déclarations de revenus fonciers sans immeuble associé."
            });
        }

        var modeResultat = await ObtenirModeDeductionAsync(
            declaration,
            societeId.Value,
            cancellationToken);

        if (modeResultat.Erreur != null)
        {
            return BadRequest(new
            {
                Message = modeResultat.Erreur
            });
        }

        if (modeResultat.Mode != ModeDeductionCharges.FraisReels)
        {
            return BadRequest(new
            {
                Message = "Les charges réelles ne peuvent être sélectionnées que lorsque l'option annuelle Frais réels est choisie."
            });
        }

        var debutAnnee = new DateTime(
            declaration.AnneeFiscale, 1, 1);

        var debutAnneeSuivante = debutAnnee.AddYears(1);

        var chargesDisponibles = await _context.ChargesBiens
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value &&
                !c.BienImmobilier.EstSupprime &&
                c.DateDepense >= debutAnnee &&
                c.DateDepense < debutAnneeSuivante &&

                (c.StatutDeductibilite ==
                    StatutDeductibilite.Deductible ||
                 c.StatutDeductibilite ==
                    StatutDeductibilite.PartiellementDeductible) &&

                c.MontantDeductibleFiscalement.HasValue &&
                c.MontantDeductibleFiscalement.Value > 0 &&

                !_context.DeclarationsChargesBiens.Any(a =>
                    a.ChargeBienId == c.Id &&
                    a.DeclarationFiscaleId == declaration.Id &&
                    !a.EstSupprime))
            .OrderByDescending(c => c.DateDepense)
            .Select(c => new
            {
                Id = c.Id,
                Description = c.Description,
                DateDepense = c.DateDepense,
                MontantTTC = c.MontantTTC,
                StatutDeductibilite = c.StatutDeductibilite,

                MontantDeductibleFiscalement =
                    c.MontantDeductibleFiscalement,

                BienImmobilierId = c.BienImmobilierId,
                BienImmobilierNom = c.BienImmobilier.Nom,
                BienImmobilierReference = c.BienImmobilier.Reference
            })
            .ToListAsync(cancellationToken);

        return Ok(chargesDisponibles);
    }

    // ============================================================
    // POST : api/DeclarationChargeBiens
    // Ajouter une charge à une déclaration
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "DeclarationChargeBiens.Update")]
    public async Task<IActionResult> AjouterCharge(
        [FromBody] AjouterDeclarationChargeBienRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                Message = "Les données de la charge sont obligatoires."
            });
        }

        if (request.DeclarationFiscaleId == Guid.Empty ||
            request.ChargeBienId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "Les identifiants de la déclaration et de la charge sont obligatoires."
            });
        }

        if (request.MontantRetenu <= 0)
        {
            return BadRequest(new
            {
                Message = "Le montant retenu doit être supérieur à zéro."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declaration = await _context.DeclarationsFiscales
            .FirstOrDefaultAsync(d =>
                d.Id == request.DeclarationFiscaleId &&
                !d.EstSupprime &&
                d.SocieteId == societeId.Value,
                cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = "La déclaration fiscale est introuvable."
            });
        }

        if (declaration.TypeImpot != TypeImpotFiscal.RevenusFonciers ||
            declaration.BienImmobilierId.HasValue)
        {
            return BadRequest(new
            {
                Message = "Les charges de plusieurs immeubles ne peuvent être associées qu'à une déclaration globale de revenus fonciers."
            });
        }

        var modeResultat = await ObtenirModeDeductionAsync(
            declaration,
            societeId.Value,
            cancellationToken);

        if (modeResultat.Erreur != null)
        {
            return BadRequest(new
            {
                Message = modeResultat.Erreur
            });
        }

        if (modeResultat.Mode != ModeDeductionCharges.FraisReels)
        {
            return BadRequest(new
            {
                Message = "Impossible d'ajouter une charge réelle : l'option annuelle Frais réels n'est pas sélectionnée."
            });
        }

        var charge = await _context.ChargesBiens
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == request.ChargeBienId &&
                !c.EstSupprime &&
                c.BienImmobilier.SocieteId == societeId.Value &&
                !c.BienImmobilier.EstSupprime,
                cancellationToken);

        if (charge == null)
        {
            return NotFound(new
            {
                Message = "La charge est introuvable ou n'appartient pas à votre société."
            });
        }

        if (charge.DateDepense.Year != declaration.AnneeFiscale)
        {
            return BadRequest(new
            {
                Message = "La date de cette charge ne correspond pas à l'année fiscale de la déclaration."
            });
        }

        if (charge.StatutDeductibilite != StatutDeductibilite.Deductible &&
            charge.StatutDeductibilite != StatutDeductibilite.PartiellementDeductible)
        {
            return BadRequest(new
            {
                Message = "Cette charge ne peut pas être retenue : elle doit être qualifiée comme déductible ou partiellement déductible."
            });
        }

        if (!charge.MontantDeductibleFiscalement.HasValue ||
            charge.MontantDeductibleFiscalement.Value <= 0)
        {
            return BadRequest(new
            {
                Message = "Le montant déductible fiscalement de cette charge n'est pas renseigné."
            });
        }

        var plafondFiscal = charge.MontantDeductibleFiscalement.Value;

        if (plafondFiscal > charge.MontantTTC)
        {
            return BadRequest(new
            {
                Message = "Le montant déductible fiscalement ne peut pas dépasser le montant TTC de la charge."
            });
        }

        if (request.MontantRetenu > plafondFiscal)
        {
            return BadRequest(new
            {
                Message = "Le montant retenu ne peut pas dépasser le montant déductible fiscalement de la charge."
            });
        }

        var existeDeja = await _context.DeclarationsChargesBiens
            .AnyAsync(c =>
                c.DeclarationFiscaleId == declaration.Id &&
                c.ChargeBienId == charge.Id &&
                !c.EstSupprime,
                cancellationToken);

        if (existeDeja)
        {
            return Conflict(new
            {
                Message = "Cette charge est déjà associée à la déclaration."
            });
        }

        var declarationCharge = new DeclarationChargeBien
        {
            Id = Guid.NewGuid(),

            DeclarationFiscaleId = declaration.Id,
            ChargeBienId = charge.Id,

            DescriptionCharge = charge.Description,
            DateDepense = charge.DateDepense,
            MontantTTC = charge.MontantTTC,
            StatutDeductibilite = charge.StatutDeductibilite,

            // Conservation du plafond fiscal historique.
            MontantDeductibleFiscalement = plafondFiscal,

            MontantRetenu = request.MontantRetenu,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.DeclarationsChargesBiens.Add(declarationCharge);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetChargesDeclaration),
            new { declarationId = declaration.Id },
            new
            {
                declarationCharge.Id,
                declarationCharge.DeclarationFiscaleId,
                declarationCharge.ChargeBienId,
                declarationCharge.DescriptionCharge,
                declarationCharge.DateDepense,
                declarationCharge.MontantTTC,
                declarationCharge.StatutDeductibilite,
                declarationCharge.MontantDeductibleFiscalement,
                declarationCharge.MontantRetenu
            });
    }

    // ============================================================
    // PUT : api/DeclarationChargeBiens/{id}
    // Modifier le montant retenu
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeclarationChargeBiens.Update")]
    public async Task<IActionResult> ModifierChargeRetenue(
        Guid id,
        [FromBody] ModifierDeclarationChargeBienRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'association est invalide."
            });
        }

        if (request == null || request.MontantRetenu <= 0)
        {
            return BadRequest(new
            {
                Message = "Le montant retenu doit être supérieur à zéro."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declarationCharge = await _context.DeclarationsChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.DeclarationFiscale.SocieteId == societeId.Value,
                cancellationToken);

        if (declarationCharge == null)
        {
            return NotFound(new
            {
                Message = "La charge retenue est introuvable."
            });
        }

        var declaration = await _context.DeclarationsFiscales
            .FirstOrDefaultAsync(d =>
                d.Id == declarationCharge.DeclarationFiscaleId &&
                !d.EstSupprime &&
                d.SocieteId == societeId.Value,
                cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = "La déclaration fiscale associée est introuvable."
            });
        }

        var modeResultat = await ObtenirModeDeductionAsync(
            declaration,
            societeId.Value,
            cancellationToken);

        if (modeResultat.Erreur != null)
        {
            return BadRequest(new
            {
                Message = modeResultat.Erreur
            });
        }

        if (modeResultat.Mode != ModeDeductionCharges.FraisReels)
        {
            return BadRequest(new
            {
                Message = "La modification des charges est réservée au mode Frais réels."
            });
        }

        if (declarationCharge.StatutDeductibilite !=
                StatutDeductibilite.Deductible &&
            declarationCharge.StatutDeductibilite !=
                StatutDeductibilite.PartiellementDeductible)
        {
            return BadRequest(new
            {
                Message = "La charge enregistrée dans la déclaration n'est pas fiscalement admissible."
            });
        }

        // Utiliser le plafond historique de l'association.
        // Ne pas relire le plafond actuel de ChargeBien.

        if (!declarationCharge.MontantDeductibleFiscalement.HasValue ||
            declarationCharge.MontantDeductibleFiscalement.Value <= 0)
        {
            return BadRequest(new
            {
                Message = "Le plafond fiscal historique de cette association n'est pas renseigné. Cette ancienne association doit être régularisée avant modification."
            });
        }

        var plafondHistorique =
            declarationCharge.MontantDeductibleFiscalement.Value;

        if (plafondHistorique > declarationCharge.MontantTTC)
        {
            return BadRequest(new
            {
                Message = "Le plafond fiscal historique de cette charge est invalide."
            });
        }

        if (request.MontantRetenu > plafondHistorique)
        {
            return BadRequest(new
            {
                Message = "Le montant retenu ne peut pas dépasser le plafond fiscal historique de cette charge."
            });
        }

        declarationCharge.MontantRetenu = request.MontantRetenu;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // DELETE : api/DeclarationChargeBiens/{id}
    // Retirer une charge d'une déclaration
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeclarationChargeBiens.Update")]
    public async Task<IActionResult> RetirerCharge(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de l'association est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declarationCharge = await _context.DeclarationsChargesBiens
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.DeclarationFiscale.SocieteId == societeId.Value,
                cancellationToken);

        if (declarationCharge == null)
        {
            return NotFound(new
            {
                Message = "La charge retenue est introuvable."
            });
        }

        declarationCharge.EstSupprime = true;
        declarationCharge.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // VÉRIFICATION DU MODE DE DÉDUCTION ANNUEL
    // ============================================================

    private async Task<(ModeDeductionCharges? Mode, string? Erreur)>
        ObtenirModeDeductionAsync(
            DeclarationFiscale declaration,
            Guid societeId,
            CancellationToken cancellationToken)
    {
        if (!declaration.OptionDeductionFiscaleId.HasValue)
        {
            return (null,
                "Aucune option de déduction fiscale n'est associée à cette déclaration.");
        }

        var option = await _context.OptionsDeductionFiscales
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.Id == declaration.OptionDeductionFiscaleId.Value &&
                o.SocieteId == societeId &&
                o.AnneeFiscale == declaration.AnneeFiscale &&
                !o.EstSupprime,
                cancellationToken);

        if (option == null)
        {
            return (null,
                "L'option annuelle de déduction fiscale est introuvable ou ne correspond pas à cette déclaration.");
        }

        if (!Enum.IsDefined(
                typeof(ModeDeductionCharges),
                option.ModeDeduction))
        {
            return (null,
                "Le mode de déduction de l'option annuelle est invalide.");
        }

        // L'option annuelle est la référence.
        return (option.ModeDeduction, null);
    }

    // ============================================================
    // IDENTITÉ DU PROPRIÉTAIRE CONNECTÉ
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
// DTO : AJOUTER UNE CHARGE À UNE DÉCLARATION
// ============================================================

public class AjouterDeclarationChargeBienRequest
{
    public Guid DeclarationFiscaleId { get; set; }

    public Guid ChargeBienId { get; set; }

    public decimal MontantRetenu { get; set; }
}

// ============================================================
// DTO : MODIFIER LE MONTANT RETENU
// ============================================================

public class ModifierDeclarationChargeBienRequest
{
    public decimal MontantRetenu { get; set; }
}
