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
public class ContratsController : ControllerBase
{
    private readonly MarionDbContext _context;

    public ContratsController(MarionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetContrats([FromQuery] StatutContrat? statut)
    {
        var query = _context.Contrats
            .Where(c => !c.EstSupprime)
            .Include(c => c.Bien)
            .Include(c => c.Locataire)
            .AsQueryable();

        if (statut.HasValue) query = query.Where(c => c.Statut == statut.Value);

        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var contrat = await _context.Contrats
            .Include(c => c.Bien)
            .Include(c => c.Locataire)
            .Include(c => c.Paiements.Where(p => !p.EstSupprime))
            .FirstOrDefaultAsync(c => c.Id == id && !c.EstSupprime);

        if (contrat == null) return NotFound("Contrat introuvable.");
        return Ok(contrat);
    }

    [HttpPost]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Creer([FromBody] Contrat contrat)
    {
        var bien = await _context.Biens.FirstOrDefaultAsync(b => b.Id == contrat.BienId && !b.EstSupprime);
        if (bien == null) return BadRequest("Bien introuvable.");

        bien.Statut = StatutBien.Loue;

        contrat.Id = Guid.NewGuid();
        contrat.DateCreation = DateTime.UtcNow;
        contrat.EstSupprime = false;

        // Prise en compte explicite des deux nouveaux champs à la création
        contrat.FrequencePaiement = contrat.FrequencePaiement;
        contrat.DelaiJoursTolerance = contrat.DelaiJoursTolerance;

        _context.Contrats.Add(contrat);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = contrat.Id }, contrat);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GestionnaireOrAdmin")]
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

        // Mise à jour des deux nouveaux champs de fréquence et de délai de tolérance
        contrat.FrequencePaiement = contratDto.FrequencePaiement;
        contrat.DelaiJoursTolerance = contratDto.DelaiJoursTolerance;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Suppression Logique (Soft Delete) du contrat
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var contrat = await _context.Contrats
            .Include(c => c.Bien)
            .FirstOrDefaultAsync(c => c.Id == id && !c.EstSupprime);

        if (contrat == null) return NotFound("Contrat introuvable.");

        contrat.EstSupprime = true;
        contrat.DateSuppression = DateTime.UtcNow;

        // Libération automatique du bien associé
        if (contrat.Bien != null)
        {
            contrat.Bien.Statut = StatutBien.Disponible;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Contrat résilié et archivé logiquement." });
    }
}