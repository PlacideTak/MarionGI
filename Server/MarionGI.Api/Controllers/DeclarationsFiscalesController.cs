using MarionGI.Application.Dtos;
using MarionGI.Application.Interfaces;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeclarationsFiscalesController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly ICalculFiscalService _calculFiscalService;

    public DeclarationsFiscalesController(
        MarionDbContext context,
        ICalculFiscalService calculFiscalService)
    {
        _context = context;
        _calculFiscalService = calculFiscalService;
    }

    // ============================================================
    // POST : api/DeclarationsFiscales/{id}/calculer
    // Calcul des données fiscales
    // ============================================================


    [HttpPost("{id:guid}/calculer")]
    [Authorize(Policy = "DeclarationsFiscales.Update")]
    public async Task<IActionResult> CalculerDeclarationFiscale(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
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

        try
        {
            var resultat =
                await _calculFiscalService.CalculerDeclarationAsync(
                    id,
                    societeId.Value,
                    cancellationToken);

            return Ok(resultat);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }


    // ============================================================
    // GET : api/DeclarationsFiscales
    // Liste des déclarations de la société connectée
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "DeclarationsFiscales.Read")]
    public async Task<IActionResult> GetDeclarationsFiscales(
        CancellationToken cancellationToken)
    {
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        var declarations = await _context.DeclarationsFiscales
            .AsNoTracking()
            .Where(d =>
                !d.EstSupprime &&
                d.SocieteId == societeId.Value)
            .OrderByDescending(d => d.AnneeFiscale)
            .ThenByDescending(d => d.DateCreation)
            .Select(d => new
            {
                d.Id,
                d.SocieteId,
                d.BienImmobilierId,

                BienImmobilier = d.BienImmobilier == null
                    ? null
                    : new
                    {
                        d.BienImmobilier.Id,
                        d.BienImmobilier.Reference,
                        d.BienImmobilier.Nom,
                        d.BienImmobilier.Adresse,
                        d.BienImmobilier.Ville
                    },

                d.AnneeFiscale,
                d.TypeImpot,
                d.BaseImposable,
                d.MontantImpot,
                d.DateEcheance,
                d.DateDeclaration,
                d.ReferenceDeclaration,
                d.Observations,
                d.Statut,

                d.OptionDeductionFiscaleId,
                d.ModeDeduction,
                d.TotalLoyersBruts,
                d.ChargesForfaitaires,
                d.ChargesReellesDeclarees,
                d.ChargesReellesAdmissibles,
                d.RevenuNetFoncier,
                d.AcomptesVerses,
                d.SoldeImpot,
                d.DateCalcul,

                NombreChargesRetenues = d.ChargesRetenues
                    .Count(c => !c.EstSupprime),

                d.DateCreation
            })
            .ToListAsync(cancellationToken);

        return Ok(declarations);
    }

    // ============================================================
    // GET : api/DeclarationsFiscales/{id}
    // Détail d'une déclaration
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "DeclarationsFiscales.Read")]
    public async Task<IActionResult> GetDeclarationFiscale(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
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
            .Where(d =>
                d.Id == id &&
                !d.EstSupprime &&
                d.SocieteId == societeId.Value)
            .Select(d => new
            {
                d.Id,
                d.SocieteId,
                d.BienImmobilierId,

                BienImmobilier = d.BienImmobilier == null
                    ? null
                    : new
                    {
                        d.BienImmobilier.Id,
                        d.BienImmobilier.Reference,
                        d.BienImmobilier.Nom,
                        d.BienImmobilier.Adresse,
                        d.BienImmobilier.Ville,
                        d.BienImmobilier.Quartier
                    },

                d.AnneeFiscale,
                d.TypeImpot,
                d.BaseImposable,
                d.MontantImpot,
                d.DateEcheance,
                d.DateDeclaration,
                d.ReferenceDeclaration,
                d.Observations,
                d.Statut,

                d.OptionDeductionFiscaleId,
                d.ModeDeduction,

                d.TotalLoyersBruts,
                d.ChargesForfaitaires,
                d.ChargesReellesDeclarees,
                d.ChargesReellesAdmissibles,
                d.RevenuNetFoncier,
                d.AcomptesVerses,
                d.SoldeImpot,
                d.DateCalcul,

                ChargesRetenues = d.ChargesRetenues
                    .Where(c => !c.EstSupprime)
                    .OrderBy(c => c.DateDepense)
                    .Select(c => new
                    {
                        c.Id,
                        c.ChargeBienId,
                        c.DescriptionCharge,
                        c.DateDepense,
                        c.MontantTTC,
                        c.StatutDeductibilite,
                        c.MontantRetenu
                    })
                    .ToList(),

                d.DateCreation
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = $"La déclaration fiscale {id} n'a pas été trouvée."
            });
        }

        return Ok(declaration);
    }

    // ============================================================
    // POST : api/DeclarationsFiscales
    // Création d'une déclaration
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "DeclarationsFiscales.Create")]
    public async Task<IActionResult> CreateDeclarationFiscale(
        [FromBody] DeclarationFiscaleRequest dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de la déclaration sont obligatoires."
            });
        }

        var erreur = ValiderDeclaration(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        erreur = ValiderPerimetreDeclaration(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
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

        // Vérifier l'unicité de la déclaration.
        var declarationExiste = await DeclarationExisteDejaAsync(
            societeId.Value,
            dto.AnneeFiscale,
            dto.TypeImpot,
            dto.BienImmobilierId,
            null,
            cancellationToken);

        if (declarationExiste)
        {
            return Conflict(new
            {
                Message = ObtenirMessageDoublon(dto.TypeImpot)
            });
        }

        // Vérifier le bien immobilier éventuel.
        if (dto.BienImmobilierId.HasValue)
        {
            var bienExiste = await _context.BiensImmobiliers
                .AsNoTracking()
                .AnyAsync(
                    b =>
                        b.Id == dto.BienImmobilierId.Value &&
                        !b.EstSupprime &&
                        b.SocieteId == societeId.Value,
                    cancellationToken);

            if (!bienExiste)
            {
                return BadRequest(new
                {
                    Message = "Le bien immobilier est introuvable ou n'appartient pas au propriétaire connecté."
                });
            }
        }

        // Vérifier l'option fiscale.
        OptionDeductionFiscale? option = null;

        if (dto.OptionDeductionFiscaleId.HasValue)
        {
            option = await _context.OptionsDeductionFiscales
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.Id == dto.OptionDeductionFiscaleId.Value &&
                        !o.EstSupprime &&
                        o.SocieteId == societeId.Value &&
                        o.AnneeFiscale == dto.AnneeFiscale,
                    cancellationToken);

            if (option == null)
            {
                return BadRequest(new
                {
                    Message = "L'option de déduction fiscale est introuvable, ne correspond pas à l'année fiscale ou n'appartient pas au propriétaire connecté."
                });
            }
        }

        var declaration = new DeclarationFiscale
        {
            Id = Guid.NewGuid(),

            SocieteId = societeId.Value,

            // Revenus fonciers : null = tous les immeubles
            // de la société.
            BienImmobilierId = dto.BienImmobilierId,

            AnneeFiscale = dto.AnneeFiscale,
            TypeImpot = dto.TypeImpot,

            BaseImposable = dto.BaseImposable,
            MontantImpot = dto.MontantImpot,

            DateEcheance = dto.DateEcheance,
            DateDeclaration = dto.DateDeclaration,

            ReferenceDeclaration =
                NormaliserTexteOptionnel(dto.ReferenceDeclaration),

            Observations =
                NormaliserTexteOptionnel(dto.Observations),

            Statut = dto.Statut,

            OptionDeductionFiscaleId = option?.Id,
            ModeDeduction = option?.ModeDeduction,

            TotalLoyersBruts = null,
            ChargesForfaitaires = null,
            ChargesReellesDeclarees = null,
            ChargesReellesAdmissibles = null,
            RevenuNetFoncier = null,
            AcomptesVerses = null,
            SoldeImpot = null,
            DateCalcul = null,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.DeclarationsFiscales.Add(declaration);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EstViolationUnicite(ex))
        {
            return Conflict(new
            {
                Message = ObtenirMessageDoublon(dto.TypeImpot)
            });
        }

        return CreatedAtAction(
            nameof(GetDeclarationFiscale),
            new { id = declaration.Id },
            new
            {
                declaration.Id,
                declaration.SocieteId,
                declaration.BienImmobilierId,
                declaration.AnneeFiscale,
                declaration.TypeImpot,
                declaration.BaseImposable,
                declaration.MontantImpot,
                declaration.DateEcheance,
                declaration.DateDeclaration,
                declaration.ReferenceDeclaration,
                declaration.Observations,
                declaration.Statut,
                declaration.OptionDeductionFiscaleId,
                declaration.ModeDeduction,
                declaration.DateCreation
            });
    }

    // ============================================================
    // PUT : api/DeclarationsFiscales/{id}
    // Modification d'une déclaration
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DeclarationsFiscales.Update")]
    public async Task<IActionResult> UpdateDeclarationFiscale(
        Guid id,
        [FromBody] DeclarationFiscaleRequest dto,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la déclaration est invalide."
            });
        }

        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de la déclaration sont obligatoires."
            });
        }

        if (dto.Id != id)
        {
            return BadRequest(new
            {
                Message = "L'ID fourni ne correspond pas à la déclaration."
            });
        }

        var erreur = ValiderDeclaration(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        erreur = ValiderPerimetreDeclaration(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
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
            .FirstOrDefaultAsync(
                d =>
                    d.Id == id &&
                    !d.EstSupprime &&
                    d.SocieteId == societeId.Value,
                cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = $"La déclaration fiscale {id} n'a pas été trouvée."
            });
        }

        // Empêcher la modification du périmètre après calcul
        // ou après association de charges.
        var declarationDejaCalculee =
            declaration.DateCalcul.HasValue ||
            await _context.DeclarationsChargesBiens
                .AnyAsync(
                    c =>
                        c.DeclarationFiscaleId == declaration.Id &&
                        !c.EstSupprime,
                    cancellationToken);

        if (declarationDejaCalculee &&
            (declaration.AnneeFiscale != dto.AnneeFiscale ||
             declaration.OptionDeductionFiscaleId != dto.OptionDeductionFiscaleId ||
             declaration.BienImmobilierId != dto.BienImmobilierId ||
             declaration.TypeImpot != dto.TypeImpot))
        {
            return BadRequest(new
            {
                Message = "Cette déclaration possède déjà un calcul fiscal ou des charges retenues. Son année, son bien, son type d'impôt et son option de déduction ne peuvent plus être modifiés directement. Un processus de recalcul est nécessaire."
            });
        }

        // Vérifier l'unicité en excluant la déclaration actuelle.
        var declarationExiste = await DeclarationExisteDejaAsync(
            societeId.Value,
            dto.AnneeFiscale,
            dto.TypeImpot,
            dto.BienImmobilierId,
            declaration.Id,
            cancellationToken);

        if (declarationExiste)
        {
            return Conflict(new
            {
                Message = ObtenirMessageDoublon(dto.TypeImpot)
            });
        }

        // Vérifier le bien immobilier éventuel.
        if (dto.BienImmobilierId.HasValue)
        {
            var bienExiste = await _context.BiensImmobiliers
                .AsNoTracking()
                .AnyAsync(
                    b =>
                        b.Id == dto.BienImmobilierId.Value &&
                        !b.EstSupprime &&
                        b.SocieteId == societeId.Value,
                    cancellationToken);

            if (!bienExiste)
            {
                return BadRequest(new
                {
                    Message = "Le bien immobilier est introuvable ou n'appartient pas au propriétaire connecté."
                });
            }
        }

        // Vérifier l'option fiscale.
        OptionDeductionFiscale? option = null;

        if (dto.OptionDeductionFiscaleId.HasValue)
        {
            option = await _context.OptionsDeductionFiscales
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o =>
                        o.Id == dto.OptionDeductionFiscaleId.Value &&
                        !o.EstSupprime &&
                        o.SocieteId == societeId.Value &&
                        o.AnneeFiscale == dto.AnneeFiscale,
                    cancellationToken);

            if (option == null)
            {
                return BadRequest(new
                {
                    Message = "L'option de déduction fiscale est introuvable, ne correspond pas à l'année fiscale ou n'appartient pas au propriétaire connecté."
                });
            }
        }

        // Appliquer les modifications.
        declaration.BienImmobilierId = dto.BienImmobilierId;

        declaration.AnneeFiscale = dto.AnneeFiscale;
        declaration.TypeImpot = dto.TypeImpot;

        declaration.BaseImposable = dto.BaseImposable;
        declaration.MontantImpot = dto.MontantImpot;

        declaration.DateEcheance = dto.DateEcheance;
        declaration.DateDeclaration = dto.DateDeclaration;

        declaration.ReferenceDeclaration =
            NormaliserTexteOptionnel(dto.ReferenceDeclaration);

        declaration.Observations =
            NormaliserTexteOptionnel(dto.Observations);

        declaration.Statut = dto.Statut;

        declaration.OptionDeductionFiscaleId = option?.Id;
        declaration.ModeDeduction = option?.ModeDeduction;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EstViolationUnicite(ex))
        {
            return Conflict(new
            {
                Message = ObtenirMessageDoublon(dto.TypeImpot)
            });
        }

        return NoContent();
    }

    // ============================================================
    // DELETE : api/DeclarationsFiscales/{id}
    // Suppression logique
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DeclarationsFiscales.Delete")]
    public async Task<IActionResult> DeleteDeclarationFiscale(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
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
            .FirstOrDefaultAsync(
                d =>
                    d.Id == id &&
                    !d.EstSupprime &&
                    d.SocieteId == societeId.Value,
                cancellationToken);

        if (declaration == null)
        {
            return NotFound(new
            {
                Message = $"La déclaration fiscale {id} n'a pas été trouvée."
            });
        }

        // Les associations de charges sont conservées
        // pour préserver l'historique fiscal.
        declaration.EstSupprime = true;
        declaration.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // VALIDATION DU TYPE ET DU PÉRIMÈTRE FISCAL
    // ============================================================

    private static string? ValiderPerimetreDeclaration(
        DeclarationFiscaleRequest dto)
    {
        switch (dto.TypeImpot)
        {
            case TypeImpotFiscal.RevenusFonciers:

                if (dto.BienImmobilierId.HasValue)
                {
                    return "Une déclaration de revenus fonciers doit être globale à la société. Aucun bien immobilier ne doit être sélectionné.";
                }

                if (!dto.OptionDeductionFiscaleId.HasValue)
                {
                    return "Une option de déduction fiscale doit être sélectionnée pour une déclaration de revenus fonciers.";
                }

                break;

            case TypeImpotFiscal.ProprieteFonciere:

                if (!dto.BienImmobilierId.HasValue)
                {
                    return "Une déclaration de propriété foncière doit obligatoirement être associée à un bien immobilier.";
                }

                if (dto.OptionDeductionFiscaleId.HasValue)
                {
                    return "L'option de déduction des revenus fonciers ne s'applique pas à une déclaration de propriété foncière.";
                }

                break;

            default:

                // Les autres types sont globaux à la société
                // tant qu'une règle métier spécifique n'est pas définie.
                if (dto.BienImmobilierId.HasValue)
                {
                    return "Ce type de déclaration doit être global à la société. Aucun bien immobilier ne doit être sélectionné.";
                }

                if (dto.OptionDeductionFiscaleId.HasValue)
                {
                    return "L'option de déduction fiscale est réservée aux déclarations de revenus fonciers.";
                }

                break;
        }

        return null;
    }

    private static string? ValiderDeclaration(
        DeclarationFiscaleRequest dto)
    {
        if (dto.AnneeFiscale < 1900 ||
            dto.AnneeFiscale > 2100)
        {
            return "L'année fiscale doit être comprise entre 1900 et 2100.";
        }

        if (!Enum.IsDefined(typeof(TypeImpotFiscal), dto.TypeImpot))
        {
            return "Le type d'impôt sélectionné est invalide.";
        }

        if (dto.TypeImpot == TypeImpotFiscal.NonDefini)
        {
            return "Vous devez sélectionner un type d'impôt valide.";
        }

        if (!Enum.IsDefined(
                typeof(StatutDeclarationFiscale),
                dto.Statut))
        {
            return "Le statut de la déclaration est invalide.";
        }

        if (dto.BaseImposable.HasValue &&
            dto.BaseImposable.Value < 0)
        {
            return "La base imposable ne peut pas être négative.";
        }

        if (dto.MontantImpot < 0)
        {
            return "Le montant de l'impôt ne peut pas être négatif.";
        }

        if (dto.ReferenceDeclaration?.Trim().Length > 200)
        {
            return "La référence de la déclaration ne doit pas dépasser 200 caractères.";
        }

        if (dto.Observations?.Trim().Length > 2000)
        {
            return "Les observations ne doivent pas dépasser 2 000 caractères.";
        }

        if (dto.DateDeclaration.HasValue &&
            dto.DateDeclaration.Value == default)
        {
            return "La date de déclaration est invalide.";
        }

        if (dto.DateEcheance.HasValue &&
            dto.DateEcheance.Value == default)
        {
            return "La date d'échéance est invalide.";
        }

        return null;
    }

    // ============================================================
    // VÉRIFICATION DES DOUBLONS
    // ============================================================

    private async Task<bool> DeclarationExisteDejaAsync(
        Guid societeId,
        int anneeFiscale,
        TypeImpotFiscal typeImpot,
        Guid? bienImmobilierId,
        Guid? declarationIdExclue,
        CancellationToken cancellationToken)
    {
        var query = _context.DeclarationsFiscales
            .AsNoTracking()
            .Where(d =>
                !d.EstSupprime &&
                d.SocieteId == societeId &&
                d.AnneeFiscale == anneeFiscale &&
                d.TypeImpot == typeImpot);

        if (declarationIdExclue.HasValue)
        {
            query = query.Where(
                d => d.Id != declarationIdExclue.Value);
        }

        if (typeImpot == TypeImpotFiscal.RevenusFonciers)
        {
            // Une seule déclaration globale par société et année.
            query = query.Where(
                d => d.BienImmobilierId == null);
        }
        else if (typeImpot == TypeImpotFiscal.ProprieteFonciere)
        {
            // Une déclaration par immeuble et par année.
            query = query.Where(
                d => d.BienImmobilierId == bienImmobilierId);
        }
        else
        {
            // Les autres types sont globaux à la société.
            query = query.Where(
                d => d.BienImmobilierId == null);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static string ObtenirMessageDoublon(
        TypeImpotFiscal typeImpot)
    {
        return typeImpot switch
        {
            TypeImpotFiscal.RevenusFonciers =>
                "Une déclaration globale de revenus fonciers existe déjà pour cette société et cette année fiscale.",

            TypeImpotFiscal.ProprieteFonciere =>
                "Une déclaration de propriété foncière existe déjà pour ce bien et cette année fiscale.",

            _ =>
                "Une déclaration de ce type existe déjà pour cette société et cette année fiscale."
        };
    }

    // ============================================================
    // GESTION DES VIOLATIONS D'UNICITÉ SQL SERVER
    // ============================================================

    private static bool EstViolationUnicite(
        DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException sqlException
            && (sqlException.Number == 2601 ||
                sqlException.Number == 2627);
    }

    // ============================================================
    // NORMALISATION DES TEXTES
    // ============================================================

    private static string? NormaliserTexteOptionnel(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
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
