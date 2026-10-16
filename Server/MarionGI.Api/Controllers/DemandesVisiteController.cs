
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
public class DemandesVisiteController : ControllerBase
{
    private readonly MarionDbContext _context;

    public DemandesVisiteController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : api/DemandesVisite
    // ============================================================

    [HttpGet]
    [Authorize(Policy = "DemandesVisite.Read")]
    public async Task<IActionResult> GetDemandesVisite(
        CancellationToken cancellationToken)
    {
        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier l'utilisateur connecté."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var query = ConstruireRequeteSociete(societeId.Value);

        if (IsAgent())
        {
            query = query.Where(
                d => d.AgentId == utilisateurId.Value);
        }

        var demandes = await query
            .Include(d => d.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(d => d.Agent)
            .OrderByDescending(d => d.DateSouhaitee)
            .ToListAsync(cancellationToken);

        return Ok(demandes.Select(ProjeterDemande));
    }

    // ============================================================
    // GET : api/DemandesVisite/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "DemandesVisite.Read")]
    public async Task<IActionResult> GetDemandeVisite(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la demande est invalide."
            });
        }

        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier l'utilisateur connecté."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var query = ConstruireRequeteSociete(societeId.Value)
            .Where(d => d.Id == id);

        if (IsAgent())
        {
            query = query.Where(
                d => d.AgentId == utilisateurId.Value);
        }

        var demande = await query
            .Include(d => d.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(d => d.Agent)
            .FirstOrDefaultAsync(cancellationToken);

        if (demande == null)
        {
            return NotFound(new
            {
                Message = $"La demande de visite {id} n'a pas été trouvée."
            });
        }

        return Ok(ProjeterDemande(demande));
    }

    // ============================================================
    // POST : api/DemandesVisite
    // ============================================================

    [HttpPost]
    [Authorize(Policy = "DemandesVisite.Create")]
    public async Task<IActionResult> CreateDemandeVisite(
        [FromBody] DemandeVisiteRequest dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de la demande sont obligatoires."
            });
        }

        var erreur = ValiderDemande(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier l'utilisateur connecté."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var uniteExiste = await _context.UnitesLocatives
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.Id == dto.UniteLocativeId &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (!uniteExiste)
        {
            return BadRequest(new
            {
                Message = "L'unité locative est introuvable ou n'appartient pas à votre société."
            });
        }

        Guid? agentId = dto.AgentId;

        // Un agent ne peut pas créer une demande au nom d'un autre agent.
        if (IsAgent())
        {
            agentId = utilisateurId.Value;
        }

        if (agentId.HasValue)
        {
            var agentExiste = await _context.Utilisateurs
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.Id == agentId.Value &&
                        u.SocieteId == societeId.Value &&
                        u.Role == RoleUtilisateur.Agent &&
                        u.Statut &&
                        !u.EstSupprime,
                    cancellationToken);

            if (!agentExiste)
            {
                return BadRequest(new
                {
                    Message = "L'agent sélectionné est invalide, inactif ou n'appartient pas à votre société."
                });
            }
        }

        var demande = new DemandeVisite
        {
            Id = Guid.NewGuid(),
            UniteLocativeId = dto.UniteLocativeId,
            AgentId = agentId,

            NomProspect = dto.NomProspect.Trim(),
            TelephoneProspect = dto.TelephoneProspect.Trim(),
            EmailProspect = NormaliserTexteOptionnel(dto.EmailProspect),

            DateSouhaitee = dto.DateSouhaitee,
            DateConfirmee = dto.DateConfirmee,

            Observations = NormaliserTexteOptionnel(dto.Observations),

            // Le commentaire interne n'est pas fourni par un agent
            // lors de la création.
            CommentaireInterne = IsAgent()
                ? null
                : NormaliserTexteOptionnel(dto.CommentaireInterne),

            DateTraitement = null,

            // Le statut initial est contrôlé par le serveur.
            Statut = StatutDemandeVisite.EnAttente,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.DemandesVisite.Add(demande);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetDemandeVisite),
            new { id = demande.Id },
            new
            {
                demande.Id,
                demande.UniteLocativeId,
                demande.AgentId,
                demande.NomProspect,
                demande.TelephoneProspect,
                demande.EmailProspect,
                demande.DateSouhaitee,
                demande.DateConfirmee,
                demande.Observations,
                demande.CommentaireInterne,
                demande.DateTraitement,
                demande.Statut
            });
    }

    // ============================================================
    // PUT : api/DemandesVisite/{id}
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "DemandesVisite.Update")]
    public async Task<IActionResult> UpdateDemandeVisite(
        Guid id,
        [FromBody] DemandeVisiteRequest dto,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la demande est invalide."
            });
        }

        if (dto == null)
        {
            return BadRequest(new
            {
                Message = "Les données de la demande sont obligatoires."
            });
        }

        if (id != dto.Id)
        {
            return BadRequest(new
            {
                Message = "L'ID fourni ne correspond pas à la demande."
            });
        }

        var erreur = ValiderDemande(dto);

        if (erreur != null)
        {
            return BadRequest(new { Message = erreur });
        }

        var utilisateurId = GetCurrentUserId();

        if (utilisateurId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible d'identifier l'utilisateur connecté."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var demande = await ConstruireRequeteSociete(societeId.Value)
            .FirstOrDefaultAsync(
                d => d.Id == id,
                cancellationToken);

        if (demande == null)
        {
            return NotFound(new
            {
                Message = $"La demande de visite {id} n'a pas été trouvée."
            });
        }

        var estAgent = IsAgent();

        if (estAgent)
        {
            // Un agent ne peut modifier que ses propres demandes.
            if (demande.AgentId != utilisateurId.Value)
            {
                return Forbid();
            }
        }

        var uniteExiste = await _context.UnitesLocatives
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.Id == dto.UniteLocativeId &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (!uniteExiste)
        {
            return BadRequest(new
            {
                Message = "L'unité locative est introuvable ou n'appartient pas à votre société."
            });
        }

        Guid? agentId = dto.AgentId;

        if (estAgent)
        {
            // L'agent ne peut pas réaffecter la demande.
            agentId = utilisateurId.Value;
        }

        if (agentId.HasValue)
        {
            var agentExiste = await _context.Utilisateurs
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.Id == agentId.Value &&
                        u.SocieteId == societeId.Value &&
                        u.Role == RoleUtilisateur.Agent &&
                        u.Statut &&
                        !u.EstSupprime,
                    cancellationToken);

            if (!agentExiste)
            {
                return BadRequest(new
                {
                    Message = "L'agent sélectionné est invalide, inactif ou n'appartient pas à votre société."
                });
            }
        }

        var ancienStatut = demande.Statut;

        demande.UniteLocativeId = dto.UniteLocativeId;
        demande.AgentId = agentId;

        demande.NomProspect = dto.NomProspect.Trim();
        demande.TelephoneProspect = dto.TelephoneProspect.Trim();
        demande.EmailProspect = NormaliserTexteOptionnel(dto.EmailProspect);

        demande.DateSouhaitee = dto.DateSouhaitee;
        demande.DateConfirmee = dto.DateConfirmee;
        demande.Observations = NormaliserTexteOptionnel(dto.Observations);

        if (!estAgent)
        {
            if (!Enum.IsDefined(typeof(StatutDemandeVisite), dto.Statut))
            {
                return BadRequest(new
                {
                    Message = "Le statut de la demande est invalide."
                });
            }

            demande.CommentaireInterne =
                NormaliserTexteOptionnel(dto.CommentaireInterne);

            demande.Statut = dto.Statut;

            // La date de traitement est enregistrée automatiquement
            // lorsqu'un membre autorisé change le statut.
            if (demande.Statut != ancienStatut)
            {
                demande.DateTraitement = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // DELETE : api/DemandesVisite/{id}
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "DemandesVisite.Delete")]
    public async Task<IActionResult> DeleteDemandeVisite(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "L'identifiant de la demande est invalide."
            });
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message = "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var demande = await ConstruireRequeteSociete(societeId.Value)
            .FirstOrDefaultAsync(
                d => d.Id == id,
                cancellationToken);

        if (demande == null)
        {
            return NotFound(new
            {
                Message = $"La demande de visite {id} n'a pas été trouvée."
            });
        }

        demande.EstSupprime = true;
        demande.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ============================================================
    // PROJECTION POUR LES RÉPONSES API
    // ============================================================

    private static object ProjeterDemande(DemandeVisite d)
    {
        return new
        {
            d.Id,
            d.UniteLocativeId,

            UniteLocative = new
            {
                d.UniteLocative.Id,
                d.UniteLocative.Reference,
                d.UniteLocative.Type,
                d.UniteLocative.Superficie,
                d.UniteLocative.Loyer,
                d.UniteLocative.Statut,
                d.UniteLocative.Photos,
                d.UniteLocative.BienImmobilierId,

                BienReference = d.UniteLocative.BienImmobilier.Reference,
                BienAdresse = d.UniteLocative.BienImmobilier.Adresse,
                BienVille = d.UniteLocative.BienImmobilier.Ville,
                SocieteId = d.UniteLocative.BienImmobilier.SocieteId
            },

            BienId = d.UniteLocative.BienImmobilierId,

            Bien = new
            {
                d.UniteLocative.BienImmobilier.Id,
                d.UniteLocative.BienImmobilier.Reference,
                d.UniteLocative.BienImmobilier.Nom,
                d.UniteLocative.BienImmobilier.Adresse,
                d.UniteLocative.BienImmobilier.Ville,
                d.UniteLocative.BienImmobilier.Quartier,
                d.UniteLocative.BienImmobilier.Superficie,
                d.UniteLocative.BienImmobilier.Type,
                d.UniteLocative.BienImmobilier.SocieteId
            },

            d.AgentId,

            Agent = d.Agent == null
                ? null
                : new
                {
                    d.Agent.Id,
                    d.Agent.Nom,
                    d.Agent.Prenom,
                    d.Agent.Telephone,
                    d.Agent.Email,
                    d.Agent.Role,
                    d.Agent.Statut,
                    d.Agent.SocieteId
                },

            d.NomProspect,
            d.TelephoneProspect,
            d.EmailProspect,
            d.DateSouhaitee,
            d.DateConfirmee,
            d.Observations,
            d.CommentaireInterne,
            d.DateTraitement,
            d.Statut,
            d.DateCreation
        };
    }

    // ============================================================
    // REQUÊTE DE BASE : SOCIÉTÉ ET SUPPRESSIONS LOGIQUES
    // ============================================================

    private IQueryable<DemandeVisite> ConstruireRequeteSociete(
        Guid societeId)
    {
        return _context.DemandesVisite
            .Where(d =>
                !d.EstSupprime &&
                !d.UniteLocative.EstSupprime &&
                !d.UniteLocative.BienImmobilier.EstSupprime &&
                d.UniteLocative.BienImmobilier.SocieteId == societeId);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static string? ValiderDemande(DemandeVisiteRequest dto)
    {
        if (dto.UniteLocativeId == Guid.Empty)
        {
            return "L'unité locative est obligatoire.";
        }

        if (string.IsNullOrWhiteSpace(dto.NomProspect))
        {
            return "Le nom du prospect est obligatoire.";
        }

        if (string.IsNullOrWhiteSpace(dto.TelephoneProspect))
        {
            return "Le téléphone du prospect est obligatoire.";
        }

        if (dto.DateSouhaitee == default)
        {
            return "La date souhaitée est obligatoire.";
        }

        if (dto.EmailProspect?.Length > 254)
        {
            return "L'adresse courriel du prospect ne doit pas dépasser 254 caractères.";
        }

        if (dto.NomProspect.Trim().Length > 200)
        {
            return "Le nom du prospect ne doit pas dépasser 200 caractères.";
        }

        if (dto.TelephoneProspect.Trim().Length > 50)
        {
            return "Le téléphone du prospect ne doit pas dépasser 50 caractères.";
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
    // UTILISATEUR CONNECTÉ
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

    // ============================================================
    // SOCIÉTÉ DE L'UTILISATEUR
    // ============================================================

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

    // ============================================================
    // RÔLE AGENT
    // ============================================================

    private bool IsAgent()
    {
        var role =
            User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role");

        return string.Equals(
            role,
            nameof(RoleUtilisateur.Agent),
            StringComparison.OrdinalIgnoreCase);
    }
}
