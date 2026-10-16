using System.Security.Claims;
using MarionGI.Application.Dtos;
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
public class DeclarationsFiscalesController : ControllerBase
{
    private readonly MarionDbContext _context;

    public DeclarationsFiscalesController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/DeclarationsFiscales
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "DeclarationsFiscales.Read")]
    public async Task<IActionResult> GetDeclarationsFiscales(
        CancellationToken cancellationToken)
    {
        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

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
                d.DateCreation
            })
            .ToListAsync(cancellationToken);

        return Ok(declarations);
    }

    // ============================================================
    // GET : api/DeclarationsFiscales/{id}
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

        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

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

        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer le propriétaire connecté."
            });
        }

        // Vérifier que le bien éventuel appartient au propriétaire.
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

        var declaration = new DeclarationFiscale
        {
            Id = Guid.NewGuid(),

            // Le propriétaire est déterminé par le serveur.
            SocieteId = societeId.Value,
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

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.DeclarationsFiscales.Add(declaration);

        await _context.SaveChangesAsync(cancellationToken);

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
                declaration.Statut
            });
    }

    // ============================================================
    // PUT : api/DeclarationsFiscales/{id}
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

        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

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

        // Vérifier le nouveau bien éventuel.
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

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // DELETE : api/DeclarationsFiscales/{id}
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

        var societeId = await GetSocieteIdUtilisateurConnecteAsync(
            cancellationToken);

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

        declaration.EstSupprime = true;
        declaration.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static string? ValiderDeclaration(
        DeclarationFiscaleRequest dto)
    {
        if (dto.AnneeFiscale < 1900 || dto.AnneeFiscale > 2100)
        {
            return "L'année fiscale doit être comprise entre 1900 et 2100.";
        }

        if (!Enum.IsDefined(typeof(TypeImpotFiscal), dto.TypeImpot))
        {
            return "Le type d'impôt sélectionné est invalide.";
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

        if (dto.ReferenceDeclaration?.Trim().Length > 150)
        {
            return "La référence de la déclaration ne doit pas dépasser 150 caractères.";
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
