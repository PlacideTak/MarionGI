using MarionGI.Application.Dtos;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using MarionGI.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Mail;
using System.Security.Claims;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const int TailleMaxPhoto = 10 * 1024 * 1024;
    private const int NombreMaxPhotos = 20;

    private static readonly HashSet<string> ExtensionsAutorisees =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

    public TicketsController(
        MarionDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ============================================================
    // UTILISATEUR CONNECTÉ ET AUTORISATIONS
    // ============================================================

    private async Task<Utilisateur?> ObtenirUtilisateurConnecteAsync()
    {
        var valeurId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("Id");

        if (!Guid.TryParse(valeurId, out var utilisateurId))
            return null;

        return await _context.Utilisateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == utilisateurId &&
                u.Statut &&
                !u.EstSupprime);
    }

    private static bool EstAdministrateurOuGestionnaire(
        RoleUtilisateur role)
    {
        return role is RoleUtilisateur.Administrateur
            or RoleUtilisateur.Gestionnaire;
    }

    private static bool EstPersonnel(RoleUtilisateur role)
    {
        return role is RoleUtilisateur.Administrateur
            or RoleUtilisateur.Gestionnaire
            or RoleUtilisateur.Agent;
    }

    private static bool EstEnumValide<TEnum>(TEnum valeur)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), valeur);
    }

    private static bool TransitionStatutAutorisee(
        StatutIncident actuel,
        StatutIncident nouveau)
    {
        return actuel switch
        {
            StatutIncident.Ouvert =>
                nouveau == StatutIncident.EnCours,

            StatutIncident.EnCours =>
                nouveau == StatutIncident.EnAttenteValidation
                || nouveau == StatutIncident.Resolu,

            StatutIncident.EnAttenteValidation =>
                nouveau == StatutIncident.EnCours
                || nouveau == StatutIncident.Resolu,

            StatutIncident.Resolu =>
                nouveau == StatutIncident.EnCours,

            StatutIncident.Cloture => false,

            _ => false
        };
    }

    // ============================================================
    // CALCUL DU COÛT TOTAL
    // ============================================================

    private static void RecalculerCoutTotal(TicketIncident ticket)
    {
        ticket.CoutTotal = ticket.Intervenants
            .Where(i => !i.EstSupprime)
            .Sum(i => i.MontantFacture ?? 0m);
    }

    // Invalide l'approbation lorsqu'un devis change.
    private static void InvaliderValidationDevis(TicketIncident ticket)
    {
        ticket.DevisValide = false;
        ticket.DateValidationDevis = null;
        ticket.ValideParUtilisateurId = null;
    }

    private IQueryable<TicketIncident> TicketsVisiblesPour(
        Utilisateur utilisateur)
    {
        var requete = _context.TicketsIncidents
            .AsNoTracking()
            .Where(t => !t.EstSupprime);

        if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            return requete.Where(t =>
                t.LocataireId == utilisateur.Id);
        }

        if (EstPersonnel(utilisateur.Role))
        {
            return requete.Where(t =>
                t.UniteLocative.BienImmobilier.SocieteId
                == utilisateur.SocieteId);
        }

        return requete.Where(t => false);
    }

    private async Task<TicketIncident?> ObtenirTicketAccessibleAsync(
        Guid ticketId,
        Utilisateur utilisateur)
    {
        var requete = _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Locataire)
            .Include(t => t.Photos)
            .Include(t => t.Intervenants)
            .Where(t =>
                t.Id == ticketId &&
                !t.EstSupprime);

        if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            requete = requete.Where(t =>
                t.LocataireId == utilisateur.Id);
        }
        else if (EstPersonnel(utilisateur.Role))
        {
            requete = requete.Where(t =>
                t.UniteLocative.BienImmobilier.SocieteId
                == utilisateur.SocieteId);
        }
        else
        {
            return null;
        }

        return await requete.FirstOrDefaultAsync();
    }

    // ============================================================
    // 1. LISTE DES TICKETS
    // GET api/Tickets
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> ObtenirTous(
        [FromQuery] StatutIncident? statut = null,
        [FromQuery] PrioriteIncident? priorite = null,
        [FromQuery] CategorieIncident? categorie = null,
        [FromQuery] Guid? uniteLocativeId = null)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized(new
            {
                message = "Utilisateur non authentifié ou inactif."
            });

        if (statut.HasValue && !EstEnumValide(statut.Value))
            return BadRequest(new { message = "Statut invalide." });

        if (priorite.HasValue && !EstEnumValide(priorite.Value))
            return BadRequest(new { message = "Priorité invalide." });

        if (categorie.HasValue && !EstEnumValide(categorie.Value))
            return BadRequest(new { message = "Catégorie invalide." });

        var requete = TicketsVisiblesPour(utilisateur);

        if (statut.HasValue)
            requete = requete.Where(t => t.Statut == statut.Value);

        if (priorite.HasValue)
            requete = requete.Where(t => t.Priorite == priorite.Value);

        if (categorie.HasValue)
            requete = requete.Where(t => t.Categorie == categorie.Value);

        if (uniteLocativeId.HasValue)
        {
            requete = requete.Where(t =>
                t.UniteLocativeId == uniteLocativeId.Value);
        }

        var tickets = await requete
            .OrderByDescending(t => t.DateCreation)
            .Select(t => new TicketIncidentListeDto
            {
                Id = t.Id,
                Reference = t.Reference,
                DateCreation = t.DateCreation,
                UniteLocativeId = t.UniteLocativeId,
                ReferenceUnite = t.UniteLocative.Reference,
                NomBienImmobilier = t.UniteLocative.BienImmobilier.Nom,
                LocataireId = t.LocataireId,
                NomLocataire = t.Locataire.Nom,
                PrenomLocataire = t.Locataire.Prenom,
                Categorie = t.Categorie,
                Description = t.Description,
                Priorite = t.Priorite,
                Statut = t.Statut,
                CoutEstime = t.CoutEstime,
                CoutTotal = t.CoutTotal,
                ValidationDevisRequise = t.ValidationDevisRequise,
                DevisValide = t.DevisValide,
                DateIncident = t.DateIncident
            })
            .ToListAsync();

        return Ok(tickets);
    }

    // ============================================================
    // 2. LOCATAIRES D'UNE UNITÉ
    // GET api/Tickets/locataires-par-unite/{uniteLocativeId}
    // ============================================================

    [HttpGet("locataires-par-unite/{uniteLocativeId:guid}")]
    public async Task<IActionResult> ObtenirLocatairesParUnite(
        Guid uniteLocativeId)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var uniteExiste = await _context.UnitesLocatives
            .AsNoTracking()
            .AnyAsync(u =>
                u.Id == uniteLocativeId &&
                !u.EstSupprime &&
                !u.BienImmobilier.EstSupprime &&
                u.BienImmobilier.SocieteId == utilisateur.SocieteId);

        if (!uniteExiste)
        {
            return NotFound(new
            {
                message = "Unité locative introuvable."
            });
        }


        var aujourdHui = DateTime.UtcNow.Date;

        var locataires = await _context.Contrats
            .AsNoTracking()
            .Where(c =>
                c.UniteLocativeId == uniteLocativeId &&
                c.Statut == StatutContrat.Actif &&
                c.DateDebut.Date <= aujourdHui &&
                c.DateFin.Date >= aujourdHui &&
                !c.EstSupprime &&
                c.Locataire.Statut &&
                !c.Locataire.EstSupprime &&
                c.Locataire.Role == RoleUtilisateur.Locataire &&
                c.Locataire.SocieteId == utilisateur.SocieteId)
            .Select(c => new
            {
                Id = c.Locataire.Id,
                Nom = c.Locataire.Nom,
                Prenom = c.Locataire.Prenom
            })
            .Distinct()
            .OrderBy(l => l.Nom)
            .ThenBy(l => l.Prenom)
            .ToListAsync();


        return Ok(locataires);
    }


    // ============================================================
    // 3. DÉTAIL D'UN TICKET
    // GET api/Tickets/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenirParId(Guid id)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        var ticket = await ObtenirTicketAccessibleAsync(id, utilisateur);

        if (ticket is null)
        {
            return NotFound(new
            {
                message = "Ticket introuvable ou accès non autorisé."
            });
        }

        return Ok(new TicketIncidentDetailDto
        {
            Id = ticket.Id,
            Reference = ticket.Reference,
            DateCreation = ticket.DateCreation,
            UniteLocativeId = ticket.UniteLocativeId,
            ReferenceUnite = ticket.UniteLocative.Reference,
            NomBienImmobilier = ticket.UniteLocative.BienImmobilier.Nom,
            LocataireId = ticket.LocataireId,
            NomLocataire = ticket.Locataire.Nom,
            PrenomLocataire = ticket.Locataire.Prenom,
            Categorie = ticket.Categorie,
            Description = ticket.Description,
            DateIncident = ticket.DateIncident,
            Priorite = ticket.Priorite,
            Statut = ticket.Statut,
            DateResolution = ticket.DateResolution,
            ResultatReparation = ticket.ResultatReparation,
            CoutEstime = ticket.CoutEstime,
            CoutTotal = ticket.CoutTotal,
            ImputableA = ticket.ImputableA,
            ValidationDevisRequise = ticket.ValidationDevisRequise,
            DevisValide = ticket.DevisValide,
            DateValidationDevis = ticket.DateValidationDevis,
            ValideParUtilisateurId = ticket.ValideParUtilisateurId,

            Photos = ticket.Photos
                .Where(p => !p.EstSupprime)
                .OrderBy(p => p.DateAjout)
                .Select(p => new TicketPhotoDto
                {
                    Id = p.Id,
                    UrlPhoto = p.UrlPhoto,
                    Type = p.Type,
                    DateAjout = p.DateAjout,
                    AjouteParUtilisateurId = p.AjouteParUtilisateurId
                })
                .ToList(),

            Intervenants = ticket.Intervenants
                .Where(i => !i.EstSupprime)
                .OrderByDescending(i => i.DateAssignation)
                .Select(i => new TicketIntervenantDto
                {
                    Id = i.Id,
                    NomArtisan = i.NomArtisan,
                    Specialite = i.Specialite,
                    Telephone = i.Telephone,
                    Courriel = i.Courriel,
                    MontantDevis = i.MontantDevis,
                    MontantFacture = i.MontantFacture,
                    DateAssignation = i.DateAssignation,
                    DateIntervention = i.DateIntervention
                })
                .ToList()
        });
    }

    // ============================================================
    // 4. CRÉER UN TICKET
    // POST api/Tickets
    // ============================================================

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Creer(
        [FromForm] CreerTicketIncidentRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (utilisateur.Role is not
            (RoleUtilisateur.Administrateur
            or RoleUtilisateur.Gestionnaire
            or RoleUtilisateur.Locataire))
        {
            return Forbid();
        }

        if (!EstEnumValide(request.Categorie))
            return BadRequest(new { message = "Catégorie invalide." });

        if (!EstEnumValide(request.Priorite))
            return BadRequest(new { message = "Priorité invalide." });

        if (!EstEnumValide(request.TypePhoto))
            return BadRequest(new { message = "Type de photo invalide." });

        if (string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length > 4000)
        {
            return BadRequest(new
            {
                message =
                    "La description est obligatoire et limitée à 4000 caractères."
            });
        }

        if (request.DateIncident == default)
        {
            return BadRequest(new
            {
                message = "La date de l'incident est obligatoire."
            });
        }

        if (request.CoutEstime is < 0)
        {
            return BadRequest(new
            {
                message = "Le coût estimé ne peut pas être négatif."
            });
        }

        if (!EstEnumValide(request.ImputableA))
        {
            return BadRequest(new
            {
                message = "L'imputabilité est invalide."
            });
        }

        var reference = request.Reference?.Trim();

        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 50)
        {
            return BadRequest(new
            {
                message =
                    "La référence est obligatoire et limitée à 50 caractères."
            });
        }

        var referenceExiste = await _context.TicketsIncidents
            .AnyAsync(t =>
                t.Reference == reference &&
                !t.EstSupprime);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message = $"La référence « {reference} » est déjà utilisée."
            });
        }

        var unite = await _context.UnitesLocatives
            .Include(u => u.BienImmobilier)
            .FirstOrDefaultAsync(u =>
                u.Id == request.UniteLocativeId &&
                !u.EstSupprime &&
                !u.BienImmobilier.EstSupprime);

        if (unite is null)
        {
            return BadRequest(new
            {
                message = "L'unité locative n'existe pas."
            });
        }

        Guid locataireId;
        var maintenant = DateTime.UtcNow;

        if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            locataireId = utilisateur.Id;

            var contratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.LocataireId == locataireId &&
                    c.UniteLocativeId == unite.Id &&
                    c.Statut == StatutContrat.Actif &&
                    c.DateDebut <= maintenant &&
                    c.DateFin >= maintenant &&
                    !c.EstSupprime);

            if (!contratActif)
                return Forbid();
        }
        else
        {
            if (unite.BienImmobilier.SocieteId != utilisateur.SocieteId)
                return NotFound();

            if (!request.LocataireId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Le locataire doit être renseigné."
                });
            }

            var locataire = await _context.Utilisateurs
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Id == request.LocataireId.Value &&
                    u.Role == RoleUtilisateur.Locataire &&
                    u.SocieteId == utilisateur.SocieteId &&
                    u.Statut &&
                    !u.EstSupprime);

            if (locataire is null)
            {
                return BadRequest(new
                {
                    message = "Le locataire est introuvable ou inactif."
                });
            }

            locataireId = locataire.Id;

            var contratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.LocataireId == locataireId &&
                    c.UniteLocativeId == unite.Id &&
                    c.Statut == StatutContrat.Actif &&
                    c.DateDebut <= maintenant &&
                    c.DateFin >= maintenant &&
                    !c.EstSupprime);

            if (!contratActif)
            {
                return BadRequest(new
                {
                    message =
                        "Ce locataire ne possède pas de contrat actif pour cette unité."
                });
            }
        }

        var fichiers = request.Photos ?? [];

        if (fichiers.Count > NombreMaxPhotos)
        {
            return BadRequest(new
            {
                message = $"Un maximum de {NombreMaxPhotos} photos est autorisé."
            });
        }

        var validationFichiers = await ValiderPhotosAsync(fichiers);

        if (validationFichiers is not null)
            return BadRequest(new { message = validationFichiers });

        var ticket = new TicketIncident
        {
            Reference = reference,
            UniteLocativeId = unite.Id,
            LocataireId = locataireId,
            Categorie = request.Categorie,
            Description = request.Description.Trim(),
            Priorite = request.Priorite,
            DateIncident = request.DateIncident,
            Statut = StatutIncident.Ouvert,
            CoutEstime = request.CoutEstime,
            CoutTotal = 0m,
            ResultatReparation = null,
            ImputableA = utilisateur.Role == RoleUtilisateur.Locataire
                ? ImputabiliteIncident.Indeterminee
                : request.ImputableA,
            ValidationDevisRequise =
                utilisateur.Role != RoleUtilisateur.Locataire
                && request.ValidationDevisRequise,
            DevisValide = false
        };

        var cheminsCrees = new List<string>();

        try
        {
            foreach (var fichier in fichiers)
            {
                var url = await EnregistrerPhotoAsync(fichier, cheminsCrees);

                ticket.Photos.Add(new TicketPhoto
                {
                    UrlPhoto = url,
                    Type = request.TypePhoto,
                    AjouteParUtilisateurId = utilisateur.Id,
                    DateAjout = DateTime.UtcNow
                });
            }

            _context.TicketsIncidents.Add(ticket);
            await _context.SaveChangesAsync();
        }
        catch
        {
            SupprimerFichiersPhysiques(cheminsCrees);
            throw;
        }

        return CreatedAtAction(
            nameof(ObtenirParId),
            new { id = ticket.Id },
            new
            {
                ticket.Id,
                ticket.Reference,
                message = "Le ticket a été créé avec succès."
            });
    }

    // ============================================================
    // 5. MODIFIER UN TICKET
    // PUT api/Tickets/{id}
    // ============================================================

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Modifier(
        Guid id,
        [FromBody] ModifierTicketIncidentRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message = "Un ticket clôturé ne peut plus être modifié."
            });
        }

        if (!EstEnumValide(request.Categorie) ||
            !EstEnumValide(request.Priorite) ||
            !EstEnumValide(request.ImputableA))
        {
            return BadRequest(new
            {
                message = "Une ou plusieurs valeurs sont invalides."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length > 4000)
        {
            return BadRequest(new
            {
                message =
                    "La description est obligatoire et limitée à 4000 caractères."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ResultatReparation) &&
            request.ResultatReparation.Trim().Length > 4000)
        {
            return BadRequest(new
            {
                message =
                    "Le résultat des réparations ne peut pas dépasser 4000 caractères."
            });
        }

        if (request.DateIncident == default)
        {
            return BadRequest(new
            {
                message = "La date de l'incident est obligatoire."
            });
        }

        if (request.CoutEstime is < 0)
        {
            return BadRequest(new
            {
                message = "Le coût estimé ne peut pas être négatif."
            });
        }

        var reference = request.Reference?.Trim();

        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 50)
        {
            return BadRequest(new
            {
                message =
                    "La référence est obligatoire et limitée à 50 caractères."
            });
        }

        var referenceExiste = await _context.TicketsIncidents
            .AnyAsync(t =>
                t.Reference == reference &&
                t.Id != ticket.Id &&
                !t.EstSupprime);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message = $"La référence « {reference} » est déjà utilisée."
            });
        }

        // Une modification du coût estimé invalide la validation précédente.
        if (ticket.CoutEstime != request.CoutEstime)
            InvaliderValidationDevis(ticket);

        if (ticket.ValidationDevisRequise != request.ValidationDevisRequise)
            InvaliderValidationDevis(ticket);

        ticket.Categorie = request.Categorie;
        ticket.Description = request.Description.Trim();
        ticket.DateIncident = request.DateIncident;
        ticket.Priorite = request.Priorite;
        ticket.CoutEstime = request.CoutEstime;
        ticket.ImputableA = request.ImputableA;
        ticket.Reference = reference;

        ticket.ResultatReparation =
            string.IsNullOrWhiteSpace(request.ResultatReparation)
                ? null
                : request.ResultatReparation.Trim();

        ticket.ValidationDevisRequise = request.ValidationDevisRequise;

        RecalculerCoutTotal(ticket);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Ticket modifié avec succès.",
            ticket.Id,
            ticket.Reference,
            ticket.ResultatReparation,
            ticket.CoutTotal
        });
    }

    // ============================================================
    // 6. SUPPRIMER UN TICKET
    // DELETE api/Tickets/{id}
    // ============================================================

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
        {
            return NotFound(new { message = "Ticket introuvable." });
        }

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message = "Un ticket clôturé ne peut pas être supprimé."
            });
        }

        ticket.EstSupprime = true;
        ticket.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Ticket supprimé avec succès.",
            ticket.Id,
            ticket.Reference
        });
    }

    // ============================================================
    // 7. MODIFIER LE STATUT
    // PATCH api/Tickets/{id}/statut
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutTicketRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        if (!EstEnumValide(request.Statut))
        {
            return BadRequest(new { message = "Statut invalide." });
        }

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (request.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message = "Utilisez l'opération de clôture dédiée."
            });
        }

        if (!TransitionStatutAutorisee(ticket.Statut, request.Statut))
        {
            return BadRequest(new
            {
                message =
                    $"Transition interdite : {ticket.Statut} vers {request.Statut}."
            });
        }

        if (request.Statut == StatutIncident.Resolu)
        {
            if (string.IsNullOrWhiteSpace(request.ResultatReparation) ||
                request.ResultatReparation.Trim().Length > 4000)
            {
                return BadRequest(new
                {
                    message =
                        "Le résultat de la réparation est obligatoire et limité à 4000 caractères."
                });
            }

            if (ticket.ValidationDevisRequise && !ticket.DevisValide)
            {
                return BadRequest(new
                {
                    message = "Le devis doit être validé avant la résolution."
                });
            }

            ticket.ResultatReparation = request.ResultatReparation.Trim();
            ticket.DateResolution = DateTime.UtcNow;

            RecalculerCoutTotal(ticket);
        }
        else if (request.Statut == StatutIncident.EnCours)
        {
            // Le ticket est rouvert.
            // On conserve l'ancienne résolution jusqu'à son remplacement
            // par un nouveau résultat de réparation.
            ticket.DateResolution = null;
        }

        ticket.Statut = request.Statut;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Statut modifié avec succès.",
            ticket.Id,
            ticket.Reference,
            ticket.Statut,
            ticket.ResultatReparation,
            ticket.DateResolution,
            ticket.CoutTotal
        });
    }

    // ============================================================
    // 8. VALIDER UN DEVIS
    // POST api/Tickets/{id}/valider-devis
    // ============================================================

    [HttpPost("{id:guid}/valider-devis")]
    public async Task<IActionResult> ValiderDevis(Guid id)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (ticket.Statut is StatutIncident.Cloture
            or StatutIncident.Resolu)
        {
            return BadRequest(new
            {
                message =
                    "Le devis ne peut pas être validé pour un ticket résolu ou clôturé."
            });
        }

        if (!ticket.ValidationDevisRequise)
        {
            return BadRequest(new
            {
                message =
                    "La validation du devis n'est pas requise pour ce ticket."
            });
        }

        if (ticket.DevisValide)
        {
            return BadRequest(new
            {
                message = "Le devis a déjà été validé."
            });
        }

        var devisDisponible =
            ticket.CoutEstime.HasValue ||
            ticket.Intervenants.Any(i =>
                !i.EstSupprime &&
                i.MontantDevis.HasValue);

        if (!devisDisponible)
        {
            return BadRequest(new
            {
                message = "Aucun montant de devis n'a été renseigné."
            });
        }

        ticket.DevisValide = true;
        ticket.DateValidationDevis = DateTime.UtcNow;
        ticket.ValideParUtilisateurId = utilisateur.Id;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Devis validé avec succès.",
            ticket.Id,
            ticket.Reference,
            ticket.DevisValide,
            ticket.DateValidationDevis,
            ticket.ValideParUtilisateurId
        });
    }

    // ============================================================
    // 9. CLÔTURER UN TICKET
    // POST api/Tickets/{id}/cloturer
    // ============================================================

    [HttpPost("{id:guid}/cloturer")]
    public async Task<IActionResult> Cloturer(Guid id)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message = "Le ticket est déjà clôturé."
            });
        }

        if (ticket.Statut != StatutIncident.Resolu)
        {
            return BadRequest(new
            {
                message = "Seul un ticket résolu peut être clôturé."
            });
        }

        if (ticket.ValidationDevisRequise && !ticket.DevisValide)
        {
            return BadRequest(new
            {
                message = "Le devis doit être validé avant la clôture."
            });
        }

        RecalculerCoutTotal(ticket);

        ticket.Statut = StatutIncident.Cloture;
        ticket.DateResolution ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Ticket clôturé avec succès.",
            ticket.Id,
            ticket.Reference,
            ticket.Statut,
            ticket.DateResolution,
            ticket.CoutTotal
        });
    }

    // ============================================================
    // 10. AJOUTER UN INTERVENANT
    // POST api/Tickets/{id}/intervenants
    // ============================================================

    [HttpPost("{id:guid}/intervenants")]
    public async Task<IActionResult> AjouterIntervenant(
        Guid id,
        [FromBody] AjouterTicketIntervenantRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message =
                    "Impossible d'ajouter un intervenant à un ticket clôturé."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NomArtisan) ||
            request.NomArtisan.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message =
                    "Le nom de l'artisan est obligatoire et limité à 150 caractères."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Specialite) ||
            request.Specialite.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message =
                    "La spécialité est obligatoire et limitée à 150 caractères."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Telephone) ||
            request.Telephone.Trim().Length > 30)
        {
            return BadRequest(new
            {
                message =
                    "Le téléphone est obligatoire et limité à 30 caractères."
            });
        }

        var courriel = request.Courriel?.Trim();

        if (!string.IsNullOrWhiteSpace(courriel) &&
            (courriel.Length > 254 ||
             !MailAddress.TryCreate(courriel, out var adresse) ||
             !string.Equals(
                 adresse.Address,
                 courriel,
                 StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "L'adresse courriel de l'intervenant est invalide."
            });
        }

        if (request.MontantDevis is < 0 ||
            request.MontantFacture is < 0)
        {
            return BadRequest(new
            {
                message = "Les montants ne peuvent pas être négatifs."
            });
        }

        var intervenant = new TicketIntervenant
        {
            TicketIncidentId = ticket.Id,
            NomArtisan = request.NomArtisan.Trim(),
            Specialite = request.Specialite.Trim(),
            Telephone = request.Telephone.Trim(),
            Courriel = string.IsNullOrWhiteSpace(courriel)
                ? null
                : courriel,
            MontantDevis = request.MontantDevis,
            MontantFacture = request.MontantFacture,
            DateAssignation = DateTime.UtcNow,
            DateIntervention = request.DateIntervention
        };

        // Ajouter à la navigation permet au calcul d'inclure immédiatement
        // ce nouvel intervenant.
        _context.TicketsIntervenants.Add(intervenant);

        // Une nouvelle offre de prix rend l'ancienne validation caduque.
        if (ticket.ValidationDevisRequise &&
            intervenant.MontantDevis.HasValue)
        {
            InvaliderValidationDevis(ticket);
        }

        RecalculerCoutTotal(ticket);


        await _context.SaveChangesAsync();


        return Ok(new
        {
            message = "Intervenant ajouté avec succès.",
            ticket.Reference,
            intervenant.Id,
            ticket.CoutTotal
        });
    }

    // ============================================================
    // 10.1 MODIFIER UN INTERVENANT
    // PUT api/Tickets/{id}/intervenants/{intervenantId}
    // ============================================================

    [HttpPut("{id:guid}/intervenants/{intervenantId:guid}")]
    public async Task<IActionResult> ModifierIntervenant(
        Guid id,
        Guid intervenantId,
        [FromBody] ModifierTicketIntervenantRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
        {
            return NotFound(new
            {
                message = "Ticket introuvable ou accès non autorisé."
            });
        }

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message =
                    "Impossible de modifier un intervenant d'un ticket clôturé."
            });
        }

        var intervenant = ticket.Intervenants.FirstOrDefault(i =>
            i.Id == intervenantId &&
            !i.EstSupprime);

        if (intervenant is null)
        {
            return NotFound(new
            {
                message = "Intervenant introuvable."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NomArtisan) ||
            request.NomArtisan.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message = "Le nom est obligatoire et limité à 150 caractères."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Specialite) ||
            request.Specialite.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message =
                    "La spécialité est obligatoire et limitée à 150 caractères."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Telephone) ||
            request.Telephone.Trim().Length > 30)
        {
            return BadRequest(new
            {
                message =
                    "Le téléphone est obligatoire et limité à 30 caractères."
            });
        }

        var courriel = request.Courriel?.Trim();

        if (!string.IsNullOrWhiteSpace(courriel) &&
            (courriel.Length > 254 ||
             !MailAddress.TryCreate(courriel, out var adresse) ||
             !string.Equals(
                 adresse.Address,
                 courriel,
                 StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "L'adresse courriel de l'intervenant est invalide."
            });
        }

        if (request.MontantDevis is < 0 ||
            request.MontantFacture is < 0)
        {
            return BadRequest(new
            {
                message = "Les montants ne peuvent pas être négatifs."
            });
        }

        var devisModifie =
            intervenant.MontantDevis != request.MontantDevis;

        intervenant.NomArtisan = request.NomArtisan.Trim();
        intervenant.Specialite = request.Specialite.Trim();
        intervenant.Telephone = request.Telephone.Trim();
        intervenant.Courriel =
            string.IsNullOrWhiteSpace(courriel) ? null : courriel;
        intervenant.MontantDevis = request.MontantDevis;
        intervenant.MontantFacture = request.MontantFacture;
        intervenant.DateIntervention = request.DateIntervention;

        if (devisModifie && ticket.ValidationDevisRequise)
            InvaliderValidationDevis(ticket);

        RecalculerCoutTotal(ticket);


        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Intervenant modifié avec succès.",
            intervenant.Id,
            ticket.Reference,
            ticket.CoutTotal,
            ticket.DevisValide
        });
    }

    // ============================================================
    // 10.2 SUPPRIMER UN INTERVENANT
    // DELETE api/Tickets/{id}/intervenants/{intervenantId}
    // ============================================================

    [HttpDelete("{id:guid}/intervenants/{intervenantId:guid}")]
    public async Task<IActionResult> SupprimerIntervenant(
        Guid id,
        Guid intervenantId)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .Include(t => t.Intervenants)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
        {
            return NotFound(new
            {
                message = "Ticket introuvable ou accès non autorisé."
            });
        }

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message =
                    "Impossible de supprimer un intervenant d'un ticket clôturé."
            });
        }

        var intervenant = ticket.Intervenants.FirstOrDefault(i =>
            i.Id == intervenantId &&
            !i.EstSupprime);

        if (intervenant is null)
        {
            return NotFound(new
            {
                message = "Intervenant introuvable."
            });
        }

        intervenant.EstSupprime = true;
        intervenant.DateSuppression = DateTime.UtcNow;

        RecalculerCoutTotal(ticket);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Intervenant supprimé avec succès.",
            intervenant.Id,
            ticket.Reference,
            ticket.CoutTotal
        });
    }

    // ============================================================
    // 11. AJOUTER DES PHOTOS
    // POST api/Tickets/{id}/photos
    // ============================================================

    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AjouterPhotos(
        Guid id,
        [FromForm] AjouterPhotosTicketRequest request)
    {
        var utilisateur = await ObtenirUtilisateurConnecteAsync();

        if (utilisateur is null)
            return Unauthorized();

        if (!EstAdministrateurOuGestionnaire(utilisateur.Role))
            return Forbid();

        if (!EstEnumValide(request.Type))
        {
            return BadRequest(new { message = "Type de photo invalide." });
        }

        var ticket = await _context.TicketsIncidents
            .Include(t => t.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                !t.EstSupprime &&
                t.UniteLocative.BienImmobilier.SocieteId
                    == utilisateur.SocieteId);

        if (ticket is null)
            return NotFound();

        if (ticket.Statut == StatutIncident.Cloture)
        {
            return BadRequest(new
            {
                message =
                    "Impossible d'ajouter des photos à un ticket clôturé."
            });
        }

        var fichiers = request.Photos ?? [];

        if (fichiers.Count == 0)
        {
            return BadRequest(new
            {
                message = "Sélectionnez au moins une photo."
            });
        }

        if (fichiers.Count > NombreMaxPhotos)
        {
            return BadRequest(new
            {
                message = $"Un maximum de {NombreMaxPhotos} photos est autorisé."
            });
        }

        var nombreExistant = await _context.TicketsPhotos
            .CountAsync(p =>
                p.TicketIncidentId == id &&
                !p.EstSupprime);

        if (nombreExistant + fichiers.Count > NombreMaxPhotos)
        {
            return BadRequest(new
            {
                message =
                    $"Un maximum de {NombreMaxPhotos} photos par ticket est autorisé."
            });
        }

        var validation = await ValiderPhotosAsync(fichiers);

        if (validation is not null)
            return BadRequest(new { message = validation });

        var cheminsCrees = new List<string>();

        try
        {
            foreach (var fichier in fichiers)
            {
                var url = await EnregistrerPhotoAsync(fichier, cheminsCrees);

                _context.TicketsPhotos.Add(new TicketPhoto
                {
                    TicketIncidentId = ticket.Id,
                    UrlPhoto = url,
                    Type = request.Type,
                    AjouteParUtilisateurId = utilisateur.Id,
                    DateAjout = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            SupprimerFichiersPhysiques(cheminsCrees);
            throw;
        }

        return Ok(new
        {
            message = "Photos ajoutées avec succès.",
            ticket.Reference,
            nombrePhotosAjoutees = fichiers.Count
        });
    }

    // ============================================================
    // 12. VALIDATION DES PHOTOS
    // ============================================================

    private static async Task<string?> ValiderPhotosAsync(
        List<IFormFile> fichiers)
    {
        foreach (var fichier in fichiers)
        {
            if (fichier is null || fichier.Length == 0)
                return "Un fichier photo est vide.";

            if (fichier.Length > TailleMaxPhoto)
                return "Chaque photo doit peser au maximum 10 Mo.";

            var extension = Path.GetExtension(fichier.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !ExtensionsAutorisees.Contains(extension))
            {
                return "Formats autorisés : JPG, JPEG, PNG et WEBP.";
            }

            if (!await EstImageValideAsync(fichier, extension))
                return "Un fichier ne possède pas une signature d'image valide.";
        }

        return null;
    }

    private static async Task<bool> EstImageValideAsync(
        IFormFile fichier,
        string extension)
    {
        var entete = new byte[12];

        await using var flux = fichier.OpenReadStream();

        var lus = await flux.ReadAsync(entete.AsMemory(0, entete.Length));

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return lus >= 3 &&
                   entete[0] == 0xFF &&
                   entete[1] == 0xD8 &&
                   entete[2] == 0xFF;
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            byte[] signature =
            [
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A
            ];

            return lus >= 8 &&
                   entete.Take(8).SequenceEqual(signature);
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return lus >= 12 &&
                   System.Text.Encoding.ASCII.GetString(entete, 0, 4)
                       == "RIFF" &&
                   System.Text.Encoding.ASCII.GetString(entete, 8, 4)
                       == "WEBP";
        }

        return false;
    }

    // ============================================================
    // 13. ENREGISTREMENT DES PHOTOS
    // ============================================================

    private async Task<string> EnregistrerPhotoAsync(
        IFormFile fichier,
        List<string> cheminsCrees)
    {
        var extension = Path.GetExtension(fichier.FileName)
            .ToLowerInvariant();

        var racineWeb = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        var dossier = Path.Combine(racineWeb, "uploads", "tickets");

        Directory.CreateDirectory(dossier);

        var nomFichier = $"{Guid.NewGuid():N}{extension}";
        var chemin = Path.Combine(dossier, nomFichier);

        await using (var flux = new FileStream(
            chemin,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await fichier.CopyToAsync(flux);
        }

        cheminsCrees.Add(chemin);

        return $"/uploads/tickets/{nomFichier}";
    }

    private static void SupprimerFichiersPhysiques(
        IEnumerable<string> chemins)
    {
        foreach (var chemin in chemins)
        {
            try
            {
                if (System.IO.File.Exists(chemin))
                    System.IO.File.Delete(chemin);
            }
            catch
            {
                // Ne pas masquer l'erreur initiale de sauvegarde.
            }
        }
    }
}

// ============================================================
// DTO : LISTE ET DÉTAIL
// ============================================================


public class TicketIncidentListeDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }

    // L'incident peut concerner l'immeuble entier.
    public Guid BienImmobilierId { get; set; }
    public string NomBienImmobilier { get; set; } = string.Empty;

    // L'unité locative est facultative.
    public Guid? UniteLocativeId { get; set; }
    public string? ReferenceUnite { get; set; }

    public Guid LocataireId { get; set; }
    public string NomLocataire { get; set; } = string.Empty;
    public string PrenomLocataire { get; set; } = string.Empty;

    public CategorieIncident Categorie { get; set; }
    public string Description { get; set; } = string.Empty;
    public PrioriteIncident Priorite { get; set; }
    public StatutIncident Statut { get; set; }

    public decimal? CoutEstime { get; set; }
    public decimal CoutTotal { get; set; }

    public bool ValidationDevisRequise { get; set; }
    public bool DevisValide { get; set; }
    public DateTime DateIncident { get; set; }
}


public class TicketIncidentDetailDto : TicketIncidentListeDto
{
    public DateTime? DateResolution { get; set; }
    public string? ResultatReparation { get; set; }
    public ImputabiliteIncident ImputableA { get; set; }
    public DateTime? DateValidationDevis { get; set; }
    public Guid? ValideParUtilisateurId { get; set; }

    public List<TicketPhotoDto> Photos { get; set; } = [];
    public List<TicketIntervenantDto> Intervenants { get; set; } = [];
}

public class TicketPhotoDto
{
    public Guid Id { get; set; }
    public string UrlPhoto { get; set; } = string.Empty;
    public TypePhotoIncident Type { get; set; }
    public DateTime DateAjout { get; set; }
    public Guid? AjouteParUtilisateurId { get; set; }
}

public class TicketIntervenantDto
{
    public Guid Id { get; set; }
    public string NomArtisan { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Courriel { get; set; }
    public decimal? MontantDevis { get; set; }
    public decimal? MontantFacture { get; set; }
    public DateTime? DateAssignation { get; set; }
    public DateTime? DateIntervention { get; set; }
}

// ============================================================
// DTO : REQUÊTES
// ============================================================

public class CreerTicketIncidentRequest
{
    public Guid UniteLocativeId { get; set; }
    public Guid? LocataireId { get; set; }

    public CategorieIncident Categorie { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;

    public PrioriteIncident Priorite { get; set; } =
        PrioriteIncident.Moyenne;

    public DateTime DateIncident { get; set; }
    public decimal? CoutEstime { get; set; }

    public ImputabiliteIncident ImputableA { get; set; } =
        ImputabiliteIncident.Indeterminee;

    public bool ValidationDevisRequise { get; set; }

    public TypePhotoIncident TypePhoto { get; set; } =
        TypePhotoIncident.AvantIntervention;

    public List<IFormFile>? Photos { get; set; }
}

public class ModifierTicketIncidentRequest
{
    public CategorieIncident Categorie { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public PrioriteIncident Priorite { get; set; }
    public decimal? CoutEstime { get; set; }
    public ImputabiliteIncident ImputableA { get; set; }
    public bool ValidationDevisRequise { get; set; }
    public DateTime DateIncident { get; set; }
    public string? ResultatReparation { get; set; }
}

public class ModifierStatutTicketRequest
{
    public StatutIncident Statut { get; set; }
    public string? ResultatReparation { get; set; }
}

public class AjouterTicketIntervenantRequest
{
    public string NomArtisan { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Courriel { get; set; }
    public decimal? MontantDevis { get; set; }
    public decimal? MontantFacture { get; set; }
    public DateTime? DateIntervention { get; set; }
}

public class AjouterPhotosTicketRequest
{
    public TypePhotoIncident Type { get; set; } =
        TypePhotoIncident.AvantIntervention;

    public List<IFormFile>? Photos { get; set; }
}