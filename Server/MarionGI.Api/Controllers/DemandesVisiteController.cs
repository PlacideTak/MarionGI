using MarionGI.Application.Dtos;
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
        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message =
                    "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }

        var query = _context.DemandesVisite
            .AsNoTracking()
            .Where(d =>
                !d.EstSupprime &&
                !d.UniteLocative.EstSupprime &&
                !d.UniteLocative.BienImmobilier.EstSupprime &&
                d.UniteLocative.BienImmobilier.SocieteId ==
                    societeId.Value);


        // ========================================================
        // AGENT :
        // uniquement ses demandes
        // ========================================================

        if (IsAgent())
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    Message =
                        "Impossible d'identifier l'agent connecté."
                });
            }

            query = query.Where(
                d => d.AgentId == currentUserId.Value);
        }


        var demandes =
            await query
                .OrderByDescending(d => d.DateSouhaitee)
                .Select(d => new
                {
                    d.Id,

                    d.UniteLocativeId,

                    // =================================================
                    // UNITÉ LOCATIVE
                    // =================================================

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

                        BienReference =
                            d.UniteLocative
                                .BienImmobilier
                                .Reference,

                        BienAdresse =
                            d.UniteLocative
                                .BienImmobilier
                                .Adresse,

                        BienVille =
                            d.UniteLocative
                                .BienImmobilier
                                .Ville,

                        SocieteId =
                            d.UniteLocative
                                .BienImmobilier
                                .SocieteId
                    },

                    // =================================================
                    // BIEN
                    // =================================================

                    BienId =
                        d.UniteLocative.BienImmobilierId,

                    Bien = new
                    {
                        d.UniteLocative
                            .BienImmobilier
                            .Id,

                        d.UniteLocative
                            .BienImmobilier
                            .Reference,

                        d.UniteLocative
                            .BienImmobilier
                            .Nom,

                        d.UniteLocative
                            .BienImmobilier
                            .Adresse,

                        d.UniteLocative
                            .BienImmobilier
                            .Ville,

                        d.UniteLocative
                            .BienImmobilier
                            .Quartier,

                        d.UniteLocative
                            .BienImmobilier
                            .Superficie,

                        d.UniteLocative
                            .BienImmobilier
                            .Type,

                        d.UniteLocative
                            .BienImmobilier
                            .SocieteId
                    },

                    // =================================================
                    // AGENT
                    // =================================================

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

                    // =================================================
                    // PROSPECT / VISITE
                    // =================================================

                    d.NomProspect,
                    d.TelephoneProspect,
                    d.DateSouhaitee,
                    d.Observations,
                    d.Statut
                })
                .ToListAsync(cancellationToken);

        return Ok(demandes);
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
                Message =
                    "L'identifiant de la demande est invalide."
            });
        }


        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message =
                    "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }


        var demande =
            await _context.DemandesVisite
                .AsNoTracking()
                .Where(d =>
                    d.Id == id &&
                    !d.EstSupprime &&
                    !d.UniteLocative.EstSupprime &&
                    !d.UniteLocative.BienImmobilier.EstSupprime &&
                    d.UniteLocative.BienImmobilier.SocieteId ==
                        societeId.Value)
                .Select(d => new
                {
                    d.Id,

                    d.UniteLocativeId,

                    // =================================================
                    // UNITÉ
                    // =================================================

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

                        BienReference =
                            d.UniteLocative
                                .BienImmobilier
                                .Reference,

                        BienAdresse =
                            d.UniteLocative
                                .BienImmobilier
                                .Adresse,

                        BienVille =
                            d.UniteLocative
                                .BienImmobilier
                                .Ville,

                        SocieteId =
                            d.UniteLocative
                                .BienImmobilier
                                .SocieteId
                    },

                    // =================================================
                    // BIEN
                    // =================================================

                    BienId =
                        d.UniteLocative.BienImmobilierId,

                    Bien = new
                    {
                        d.UniteLocative
                            .BienImmobilier
                            .Id,

                        d.UniteLocative
                            .BienImmobilier
                            .Reference,

                        d.UniteLocative
                            .BienImmobilier
                            .Nom,

                        d.UniteLocative
                            .BienImmobilier
                            .Adresse,

                        d.UniteLocative
                            .BienImmobilier
                            .Ville,

                        d.UniteLocative
                            .BienImmobilier
                            .Quartier,

                        d.UniteLocative
                            .BienImmobilier
                            .Superficie,

                        d.UniteLocative
                            .BienImmobilier
                            .Type,

                        d.UniteLocative
                            .BienImmobilier
                            .SocieteId
                    },

                    // =================================================
                    // AGENT
                    // =================================================

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

                    // =================================================
                    // DEMANDE
                    // =================================================

                    d.NomProspect,
                    d.TelephoneProspect,
                    d.DateSouhaitee,
                    d.Observations,
                    d.Statut
                })
                .FirstOrDefaultAsync(cancellationToken);


        if (demande == null)
        {
            return NotFound(new
            {
                Message =
                    $"La demande de visite {id} n'a pas été trouvée."
            });
        }


        // ========================================================
        // AGENT :
        // uniquement sa demande
        // ========================================================

        if (IsAgent())
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    Message =
                        "Impossible d'identifier l'agent connecté."
                });
            }

            if (demande.AgentId != currentUserId.Value)
            {
                return Forbid();
            }
        }


        return Ok(demande);
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
                Message =
                    "Les données de la demande sont obligatoires."
            });
        }


        // ========================================================
        // VALIDATION
        // ========================================================

        if (dto.UniteLocativeId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message =
                    "L'unité locative est obligatoire."
            });
        }


        if (string.IsNullOrWhiteSpace(dto.NomProspect))
        {
            return BadRequest(new
            {
                Message =
                    "Le nom du prospect est obligatoire."
            });
        }


        if (string.IsNullOrWhiteSpace(dto.TelephoneProspect))
        {
            return BadRequest(new
            {
                Message =
                    "Le téléphone du prospect est obligatoire."
            });
        }


        if (dto.DateSouhaitee == default)
        {
            return BadRequest(new
            {
                Message =
                    "La date souhaitée est obligatoire."
            });
        }


        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message =
                    "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }


        // ========================================================
        // VÉRIFIER L'UNITÉ
        // ========================================================

        var unite =
            await _context.UnitesLocatives
                .AsNoTracking()
                .Where(u =>
                    u.Id == dto.UniteLocativeId &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId ==
                        societeId.Value)
                .Select(u => new
                {
                    u.Id,
                    u.BienImmobilierId
                })
                .FirstOrDefaultAsync(cancellationToken);


        if (unite == null)
        {
            return BadRequest(new
            {
                Message =
                    "L'unité locative est introuvable ou n'appartient pas à votre société."
            });
        }


        // ========================================================
        // AGENT
        // ========================================================

        Guid? agentId = dto.AgentId;


        // --------------------------------------------------------
        // Si l'utilisateur connecté est Agent :
        // affectation forcée à lui-même.
        // --------------------------------------------------------

        if (IsAgent())
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    Message =
                        "Impossible d'identifier l'agent connecté."
                });
            }

            agentId = currentUserId.Value;
        }


        // --------------------------------------------------------
        // Vérifier l'agent sélectionné
        // --------------------------------------------------------

        if (agentId.HasValue)
        {
            var agentExiste =
                await _context.Utilisateurs
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
                    Message =
                        "L'agent sélectionné est invalide, inactif ou n'appartient pas à votre société."
                });
            }
        }


        // ========================================================
        // CRÉATION
        // ========================================================

        var nouvelleDemande = new DemandeVisite
        {
            Id = Guid.NewGuid(),

            UniteLocativeId =
                dto.UniteLocativeId,

            AgentId =
                agentId,

            NomProspect =
                dto.NomProspect.Trim(),

            TelephoneProspect =
                dto.TelephoneProspect.Trim(),

            DateSouhaitee =
                dto.DateSouhaitee,

            Observations =
                string.IsNullOrWhiteSpace(dto.Observations)
                    ? string.Empty
                    : dto.Observations.Trim(),

            // Le statut initial est toujours EnAttente.
            // Le client ne peut pas imposer un autre statut.
            Statut =
                StatutDemandeVisite.EnAttente,

            DateCreation =
                DateTime.UtcNow,

            EstSupprime = false
        };


        _context.DemandesVisite.Add(nouvelleDemande);

        await _context.SaveChangesAsync(cancellationToken);


        return CreatedAtAction(
            nameof(GetDemandeVisite),
            new
            {
                id = nouvelleDemande.Id
            },
            new
            {
                nouvelleDemande.Id,
                nouvelleDemande.UniteLocativeId,
                nouvelleDemande.AgentId,
                nouvelleDemande.NomProspect,
                nouvelleDemande.TelephoneProspect,
                nouvelleDemande.DateSouhaitee,
                nouvelleDemande.Observations,
                nouvelleDemande.Statut
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
                Message =
                    "L'identifiant de la demande est invalide."
            });
        }


        if (dto == null)
        {
            return BadRequest(new
            {
                Message =
                    "Les données de la demande sont obligatoires."
            });
        }


        if (id != dto.Id)
        {
            return BadRequest(new
            {
                Message =
                    "L'ID fourni ne correspond pas à la demande."
            });
        }


        if (dto.UniteLocativeId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message =
                    "L'unité locative est obligatoire."
            });
        }


        if (string.IsNullOrWhiteSpace(dto.NomProspect))
        {
            return BadRequest(new
            {
                Message =
                    "Le nom du prospect est obligatoire."
            });
        }


        if (string.IsNullOrWhiteSpace(dto.TelephoneProspect))
        {
            return BadRequest(new
            {
                Message =
                    "Le téléphone du prospect est obligatoire."
            });
        }


        if (dto.DateSouhaitee == default)
        {
            return BadRequest(new
            {
                Message =
                    "La date souhaitée est obligatoire."
            });
        }


        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message =
                    "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }


        // ========================================================
        // RÉCUPÉRER LA DEMANDE
        // ========================================================

        var demande =
            await _context.DemandesVisite
                .FirstOrDefaultAsync(
                    d =>
                        d.Id == id &&
                        !d.EstSupprime &&
                        !d.UniteLocative.EstSupprime &&
                        !d.UniteLocative.BienImmobilier.EstSupprime &&
                        d.UniteLocative.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);


        if (demande == null)
        {
            return NotFound(new
            {
                Message =
                    $"La demande de visite {id} n'a pas été trouvée."
            });
        }


        // ========================================================
        // AGENT
        // ========================================================

        Guid? agentId = dto.AgentId;

        if (IsAgent())
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    Message =
                        "Impossible d'identifier l'agent connecté."
                });
            }


            // Un Agent ne peut modifier que ses propres demandes.
            if (demande.AgentId != currentUserId.Value)
            {
                return Forbid();
            }


            // Un Agent ne peut pas modifier l'affectation.
            // Même si Angular envoie null, on conserve son ID.
            agentId = currentUserId.Value;
        }


        // ========================================================
        // VÉRIFIER L'UNITÉ
        // ========================================================

        var uniteExiste =
            await _context.UnitesLocatives
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.Id == dto.UniteLocativeId &&
                        !u.EstSupprime &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);


        if (!uniteExiste)
        {
            return BadRequest(new
            {
                Message =
                    "L'unité locative est introuvable ou n'appartient pas à votre société."
            });
        }


        // ========================================================
        // VÉRIFIER L'AGENT
        // ========================================================

        if (agentId.HasValue)
        {
            var agentExiste =
                await _context.Utilisateurs
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
                    Message =
                        "L'agent sélectionné est invalide, inactif ou n'appartient pas à votre société."
                });
            }
        }


        // ========================================================
        // MISE À JOUR
        // ========================================================

        demande.UniteLocativeId =
            dto.UniteLocativeId;

        demande.AgentId =
            agentId;

        demande.NomProspect =
            dto.NomProspect.Trim();

        demande.TelephoneProspect =
            dto.TelephoneProspect.Trim();

        demande.DateSouhaitee =
            dto.DateSouhaitee;

        demande.Observations =
            string.IsNullOrWhiteSpace(dto.Observations)
                ? string.Empty
                : dto.Observations.Trim();


        // --------------------------------------------------------
        // Statut
        // --------------------------------------------------------
        //
        // Le statut envoyé par Angular est accepté ici pour les
        // utilisateurs autorisés à modifier.
        //
        // Si l'utilisateur est Agent, on conserve le statut
        // existant afin qu'un Agent ne puisse pas valider,
        // annuler ou terminer lui-même une demande.
        // --------------------------------------------------------

        if (!IsAgent())
        {
            demande.Statut = dto.Statut;
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
                Message =
                    "L'identifiant de la demande est invalide."
            });
        }


        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(new
            {
                Message =
                    "Impossible de déterminer la société de l'utilisateur connecté."
            });
        }


        var demande =
            await _context.DemandesVisite
                .FirstOrDefaultAsync(
                    d =>
                        d.Id == id &&
                        !d.EstSupprime &&
                        !d.UniteLocative.EstSupprime &&
                        !d.UniteLocative.BienImmobilier.EstSupprime &&
                        d.UniteLocative.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);


        if (demande == null)
        {
            return NotFound(new
            {
                Message =
                    $"La demande de visite {id} n'a pas été trouvée."
            });
        }


        // ========================================================
        // AGENT :
        // uniquement sa propre demande
        // ========================================================

        if (IsAgent())
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
            {
                return Unauthorized(new
                {
                    Message =
                        "Impossible d'identifier l'agent connecté."
                });
            }


            if (demande.AgentId != currentUserId.Value)
            {
                return Forbid();
            }
        }


        // ========================================================
        // SUPPRESSION LOGIQUE
        // ========================================================

        demande.EstSupprime = true;
        demande.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
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

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }


    // ============================================================
    // SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private async Task<Guid?>
        GetSocieteIdUtilisateurConnecteAsync(
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
    // DÉTECTER LE RÔLE AGENT
    // ============================================================

    private bool IsAgent()
    {
        var role =
            User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role");

        return
            string.Equals(
                role,
                nameof(RoleUtilisateur.Agent),
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                role,
                "agent",
                StringComparison.OrdinalIgnoreCase);
    }
}


// =================================================================
// DTO DE REQUÊTE
// =================================================================
//
// Ce DTO est utilisé uniquement pour POST / PUT.
//
// IMPORTANT :
// On ne reçoit PAS UniteLocative, Bien ou Agent comme objets.
//
// Angular envoie uniquement les IDs.
// Le serveur vérifie ensuite que ces IDs appartiennent à la
// société de l'utilisateur connecté.
//

