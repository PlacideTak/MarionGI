using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemandesVisiteController : ControllerBase
{
    private readonly MarionDbContext _context;

    public DemandesVisiteController(MarionDbContext context)
    {
        _context = context;
    }

    // GET: api/DemandesVisite
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DemandeVisite>>> GetDemandesVisite()
    {
        return await _context.DemandesVisite
            .Include(d => d.Bien)
            .Include(d => d.Agent) // ou le candidat/demandeur selon la propriété de navigation
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/DemandesVisite/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DemandeVisite>> GetDemandeVisite(Guid id)
    {
        var demande = await _context.DemandesVisite
            .Include(d => d.Bien)
            .Include(d => d.Agent)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (demande == null)
        {
            return NotFound(new { Message = $"La demande de visite {id} n'a pas été trouvée." });
        }

        return demande;
    }

    // POST: api/DemandesVisite
    [HttpPost]
    public async Task<ActionResult<DemandeVisite>> CreateDemandeVisite([FromBody] DemandeVisite dto)
    {
        var nouvelleDemande = new DemandeVisite
        {
            BienId = dto.BienId,
            AgentId = dto.AgentId,
            NomProspect = dto.NomProspect,
            TelephoneProspect = dto.TelephoneProspect,
            DateSouhaitee = dto.DateSouhaitee,
            Statut = StatutDemandeVisite.EnAttente
        };

        _context.DemandesVisite.Add(nouvelleDemande);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDemandeVisite), new { id = nouvelleDemande.Id }, nouvelleDemande);
    }

    // PUT: api/DemandesVisite/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDemandeVisite(Guid id, DemandeVisite demande)
    {
        if (id != demande.Id)
        {
            return BadRequest(new { Message = "L'ID fourni ne correspond pas à la demande." });
        }

        _context.Entry(demande).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DemandeExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/DemandesVisite/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDemandeVisite(Guid id)
    {
        var demande = await _context.DemandesVisite.FindAsync(id);
        if (demande == null)
        {
            return NotFound();
        }

        _context.DemandesVisite.Remove(demande);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DemandeExists(Guid id)
    {
        return _context.DemandesVisite.Any(e => e.Id == id);
    }
}