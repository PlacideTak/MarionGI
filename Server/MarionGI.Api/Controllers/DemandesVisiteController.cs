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
public class DemandesVisiteController : ControllerBase
{
    private readonly MarionDbContext _context;

    public DemandesVisiteController(MarionDbContext context)
    {
        _context = context;
    }

    // GET: api/DemandesVisite
    [HttpGet]
    [Authorize(Policy = "DemandesVisite.Read")]
    public async Task<ActionResult<IEnumerable<DemandeVisite>>> GetDemandesVisite()
    {
        var query = _context.DemandesVisite
            .Include(d => d.Bien)
            .Include(d => d.Agent)
            .AsNoTracking();

        // Vérification du rôle et de l'identité
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                       ?? User.FindFirst("role")?.Value;

        // Si c'est un Agent, il ne voit que ses propres demandes
        if (userRole != null && userRole.Equals("Agent", StringComparison.OrdinalIgnoreCase))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("Id")?.Value;

            if (Guid.TryParse(userIdClaim, out Guid agentId))
            {
                query = query.Where(d => d.AgentId == agentId);
            }
            else
            {
                return Unauthorized(new { Message = "Impossible d'identifier l'agent connecté." });
            }
        }
        // Les Administrateurs et Gestionnaires récupèrent l'intégralité sans filtre

        var result = await query.ToListAsync();
        return Ok(result);
    }

    // GET: api/DemandesVisite/5
    [HttpGet("{id}")]
    [Authorize(Policy = "DemandesVisite.Read")]
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

        // Sécurité supplémentaire pour l'agent
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (userRole != null && userRole.Equals("Agent", StringComparison.OrdinalIgnoreCase))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("Id")?.Value;

            if (Guid.TryParse(userIdClaim, out Guid agentId) && demande.AgentId != agentId)
            {
                return Forbid();
            }
        }

        return demande;
    }

    // POST: api/DemandesVisite
    [HttpPost]
    [Authorize(Policy = "DemandesVisite.Create")]
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
    [Authorize(Policy = "DemandesVisite.Update")]
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
    [Authorize(Policy = "DemandesVisite.Delete")]
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