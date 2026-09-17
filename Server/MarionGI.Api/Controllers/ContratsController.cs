using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratsController: ControllerBase
{
    private readonly MarionDbContext _context;

    public ContratsController(MarionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<IActionResult> GetContrats([FromQuery] StatutContrat? statut)
    {
        var query = _context.Contrats
            .Where(c => !c.EstSupprime)
            .Include(c => c.Bien)
            .Include(c => c.Locataire)
            .AsQueryable();

        // 🔐 Isolation sécurisée des données selon les rôles (ABAC)
        query = ApplyOwnershipFilter(query);

        if (statut.HasValue)
            query = query.Where(c => c.Statut == statut.Value);

        // 🎯 Projection DTO claire pour le Frontend
        var result = await query.Select(c => new
        {
            c.Id,
            c.BienId,
            c.LocataireId,
            c.DateDebut,
            c.DateFin,
            c.MontantLoyer,
            c.MontantCaution,
            c.Statut,
            c.FrequencePaiement,
            c.DelaiJoursTolerance,

            // Navigation Bien
            Bien = c.Bien != null ? new
            {
                c.Bien.Id,
                Adresse = c.Bien.Reference + " ("+ c.Bien.Adresse + ")" // ou Adresse selon votre entité Bien
            } : null,

            // Navigation Locataire (Nom + Prénom)
            Locataire = c.Locataire != null ? new
            {
                c.Locataire.Id,
                c.Locataire.Nom,
                c.Locataire.Prenom,
                NomComplet = $"{c.Locataire.Nom} {c.Locataire.Prenom}"
            } : null
        }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = _context.Contrats
            .Include(c => c.Bien)
            .Include(c => c.Locataire)
            .Include(c => c.Paiements.Where(p => !p.EstSupprime))
            .Where(c => c.Id == id && !c.EstSupprime);

        var contrat = await ApplyOwnershipFilter(query).FirstOrDefaultAsync();

        if (contrat == null)
            return NotFound("Contrat introuvable ou accès non autorisé.");

        return Ok(contrat);
    }

    [HttpPost]
    [Authorize(Policy = "Contrats.Create")]
    public async Task<IActionResult> Creer([FromBody] Contrat contrat)
    {
        var bien = await _context.Biens.FirstOrDefaultAsync(b => b.Id == contrat.BienId && !b.EstSupprime);
        if (bien == null) return BadRequest("Bien introuvable.");

        bien.Statut = StatutBien.Loue;

        contrat.Id = Guid.NewGuid();
        contrat.DateCreation = DateTime.UtcNow;
        contrat.EstSupprime = false;

        _context.Contrats.Add(contrat);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = contrat.Id }, contrat);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Contrats.Update")]
    public async Task<IActionResult> Modifier(Guid id, [FromBody] Contrat contratDto)
    {
        if (id != contratDto.Id) return BadRequest("IDs non correspondants.");

        var contrat = await _context.Contrats.FirstOrDefaultAsync(c => c.Id == id && !c.EstSupprime);
        if (contrat == null) return NotFound("Contrat introuvable.");

        contrat.DateDebut = contratDto.DateDebut;
        contrat.DateFin = contratDto.DateFin;
        contrat.MontantLoyer = contratDto.MontantLoyer;
        contrat.MontantCaution = contratDto.MontantCaution;
        contrat.Statut = contratDto.Statut;
        contrat.FrequencePaiement = contratDto.FrequencePaiement;
        contrat.DelaiJoursTolerance = contratDto.DelaiJoursTolerance;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Contrats.Delete")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var contrat = await _context.Contrats
            .Include(c => c.Bien)
            .FirstOrDefaultAsync(c => c.Id == id && !c.EstSupprime);

        if (contrat == null) return NotFound("Contrat introuvable.");

        contrat.EstSupprime = true;
        contrat.DateSuppression = DateTime.UtcNow;

        if (contrat.Bien != null)
        {
            contrat.Bien.Statut = StatutBien.Disponible;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Contrat résilié et archivé logiquement." });
    }

    /// <summary>
    /// Extraction robuste du rôle et de l'ID utilisateur + Filtrage ABAC
    /// </summary>
    private IQueryable<Contrat> ApplyOwnershipFilter(IQueryable<Contrat> query)
    {
        // 1. Récupération robuste des rôles (supporte les clés standard et JWT courtes)
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        // 2. Si Administrateur ou Gestionnaire : Récupération TOTALE des contrats
        if (roles.Any(r => r.Equals("Administrateur", StringComparison.OrdinalIgnoreCase) ||
                           r.Equals("Gestionnaire", StringComparison.OrdinalIgnoreCase) ||
                           r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return query;
        }

        // 3. Récupération de l'ID utilisateur (NameIdentifier ou sub)
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            // ID invalide ou introuvable : bloquer immédiatement
            return query.Where(_ => false);
        }

        // 4. Si Locataire : Filtre strict sur son propre ID
        if (roles.Any(r => r.Equals("Locataire", StringComparison.OrdinalIgnoreCase)))
        {
            return query.Where(c => c.LocataireId == userId);
        }

        // 5. Si Propriétaire : Filtre sur les contrats liés à ses biens
        if (roles.Any(r => r.Equals("Proprietaire", StringComparison.OrdinalIgnoreCase)))
        {
            return query.Where(c => c.Bien.ProprietaireId == userId);
        }

        // Sécurité par défaut : interdire si aucun rôle reconnu
        return query.Where(_ => false);
    }
}