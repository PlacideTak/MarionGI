using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using System.Security.Claims;
using MarionGI.Persistence.Context;

namespace MarionGI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratsController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly ILogger<ContratsController> _logger;

    public ContratsController(
        MarionDbContext context,
        ILogger<ContratsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================
    // GET : api/Contrats
    // =========================================================

    [HttpGet]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<ActionResult<IEnumerable<object>>> GetContrats(
        [FromQuery] StatutContrat? statut = null)
    {
        var societeId = GetSocieteId();
        var utilisateurId = GetUtilisateurId();

        if (societeId == null || utilisateurId == null)
            return Unauthorized();

        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                c.UniteLocative != null &&
                !c.UniteLocative.EstSupprime &&
                c.UniteLocative.BienImmobilier != null &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        // -----------------------------------------------------
        // Filtre selon le rôle
        // -----------------------------------------------------

        query = ApplyOwnershipFilter(query, utilisateurId.Value);

        if (statut.HasValue)
        {
            if (!Enum.IsDefined(typeof(StatutContrat), statut.Value))
                return BadRequest("Le statut du contrat est invalide.");

            query = query.Where(c => c.Statut == statut.Value);
        }

        var contrats = await query
            .OrderByDescending(c => c.DateDebut)
            .Select(c => new
            {
                c.Id,
                c.Reference,

                c.DateDebut,
                c.DateFin,

                c.MontantLoyer,
                c.MontantCaution,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.Statut,
                c.EstSupprime,

                UniteLocative = new
                {
                    c.UniteLocative!.Id,
                    c.UniteLocative.Reference,
                    c.UniteLocative.Type,
                    c.UniteLocative.Superficie,
                    c.UniteLocative.Loyer,
                    c.UniteLocative.Statut
                },

                BienImmobilier = new
                {
                    c.UniteLocative.BienImmobilier!.Id,
                    c.UniteLocative.BienImmobilier.Reference,
                    c.UniteLocative.BienImmobilier.Nom,
                    c.UniteLocative.BienImmobilier.Adresse,
                    c.UniteLocative.BienImmobilier.Ville,
                    c.UniteLocative.BienImmobilier.Quartier
                },

                Locataire = c.Locataire == null
                    ? null
                    : new
                    {
                        c.Locataire.Id,
                        c.Locataire.Nom,
                        c.Locataire.Prenom,
                        c.Locataire.Email
                    }
            })
            .ToListAsync();

        return Ok(contrats);
    }


    // =========================================================
    // GET : api/Contrats/{id}
    // =========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<ActionResult<object>> GetContrat(Guid id)
    {
        var societeId = GetSocieteId();
        var utilisateurId = GetUtilisateurId();

        if (societeId == null || utilisateurId == null)
            return Unauthorized();

        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.UniteLocative != null &&
                !c.UniteLocative.EstSupprime &&
                c.UniteLocative.BienImmobilier != null &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        // Très important :
        // le filtre est également appliqué sur l'accès direct par ID.
        query = ApplyOwnershipFilter(query, utilisateurId.Value);

        var contrat = await query
            .Select(c => new
            {
                c.Id,
                c.Reference,

                c.DateDebut,
                c.DateFin,

                c.MontantLoyer,
                c.MontantCaution,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.Statut,

                UniteLocative = new
                {
                    c.UniteLocative!.Id,
                    c.UniteLocative.Reference,
                    c.UniteLocative.Type,
                    c.UniteLocative.Superficie,
                    c.UniteLocative.Loyer,
                    c.UniteLocative.Statut
                },

                BienImmobilier = new
                {
                    c.UniteLocative.BienImmobilier!.Id,
                    c.UniteLocative.BienImmobilier.Reference,
                    c.UniteLocative.BienImmobilier.Nom,
                    c.UniteLocative.BienImmobilier.Adresse,
                    c.UniteLocative.BienImmobilier.Ville,
                    c.UniteLocative.BienImmobilier.Quartier
                },

                Locataire = c.Locataire == null
                    ? null
                    : new
                    {
                        c.Locataire.Id,
                        c.Locataire.Nom,
                        c.Locataire.Prenom,
                        c.Locataire.Email
                    },

                Paiements = c.Paiements
                    .Where(p => !p.EstSupprime)
                    .OrderByDescending(p => p.DatePaiement)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.DatePaiement,
                        p.ModePaiement
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        return Ok(contrat);
    }


    // =========================================================
    // POST : api/Contrats
    // =========================================================

    [HttpPost]
    [Authorize(Policy = "Contrats.Create")]
    public async Task<ActionResult<object>> Creer(
        [FromBody] CreerContratRequest request)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized();

        // -----------------------------------------------------
        // Validation de base
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Reference))
            return BadRequest("La référence du contrat est obligatoire.");

        var reference = request.Reference.Trim();

        if (request.DateFin < request.DateDebut)
            return BadRequest(
                "La date de fin doit être supérieure ou égale à la date de début.");

        if (request.MontantLoyer <= 0)
            return BadRequest(
                "Le montant du loyer doit être supérieur à zéro.");

        if (request.MontantCaution < 0)
            return BadRequest(
                "Le montant de la caution ne peut pas être négatif.");

        if (request.DelaiJoursTolerance < 0)
            return BadRequest(
                "Le délai de tolérance ne peut pas être négatif.");

        if (!Enum.IsDefined(
                typeof(FrequencePaiement),
                request.FrequencePaiement))
        {
            return BadRequest("La fréquence de paiement est invalide.");
        }

        // -----------------------------------------------------
        // Vérification référence
        // Unique dans la société
        // -----------------------------------------------------

        var referenceExiste = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.Reference == reference &&
                c.UniteLocative != null &&
                c.UniteLocative.BienImmobilier != null &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        if (referenceExiste)
        {
            return Conflict(
                $"La référence '{reference}' est déjà utilisée dans cette société.");
        }

        // -----------------------------------------------------
        // Vérification unité locative
        // -----------------------------------------------------

        var unite = await _context.UnitesLocatives
            .Include(u => u.BienImmobilier)
            .FirstOrDefaultAsync(u =>
                u.Id == request.UniteLocativeId &&
                !u.EstSupprime &&
                u.BienImmobilier != null &&
                !u.BienImmobilier.EstSupprime &&
                u.BienImmobilier.SocieteId == societeId.Value);

        if (unite == null)
        {
            return BadRequest(
                "L'unité locative est introuvable ou n'appartient pas à votre société.");
        }

        // -----------------------------------------------------
        // Vérification du locataire
        // -----------------------------------------------------

        var locataire = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == request.LocataireId &&
                u.SocieteId == societeId.Value &&
                !u.EstSupprime &&
                u.Role == RoleUtilisateur.Locataire);

        if (locataire == null)
        {
            return BadRequest(
                "Le locataire est introuvable, supprimé ou ne possède pas le rôle Locataire.");
        }

        // -----------------------------------------------------
        // Vérification qu'il n'existe pas déjà un contrat actif
        // -----------------------------------------------------

        var contratActif = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.UniteLocativeId == request.UniteLocativeId &&
                c.Statut == StatutContrat.Actif);

        if (contratActif)
        {
            return Conflict(
                "Cette unité locative possède déjà un contrat actif.");
        }

        // -----------------------------------------------------
        // Création
        //
        // Nouveau contrat = EnAttente.
        // L'unité ne devient Louée qu'à l'activation.
        // -----------------------------------------------------

        var contrat = new Contrat
        {
            Id = Guid.NewGuid(),

            Reference = reference,

            UniteLocativeId = unite.Id,
            LocataireId = locataire.Id,

            DateDebut = request.DateDebut,
            DateFin = request.DateFin,

            MontantLoyer = request.MontantLoyer,
            MontantCaution = request.MontantCaution,

            FrequencePaiement = request.FrequencePaiement,

            DelaiJoursTolerance =
                request.DelaiJoursTolerance,

            Statut = StatutContrat.EnAttente,

            EstSupprime = false
        };

        _context.Contrats.Add(contrat);

        // Une unité avec contrat EnAttente reste disponible.
        if (unite.Statut == StatutDisponibilite.Loue)
        {
            unite.Statut = StatutDisponibilite.Disponible;
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetContrat),
            new { id = contrat.Id },
            new
            {
                contrat.Id,
                contrat.Reference,
                contrat.Statut,
                contrat.DateDebut,
                contrat.DateFin,
                contrat.MontantLoyer,
                contrat.MontantCaution,
                contrat.UniteLocativeId,
                contrat.LocataireId
            });
    }


    // =========================================================
    // PUT : api/Contrats/{id}
    // =========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Contrats.Update")]
    public async Task<ActionResult<object>> Modifier(
        Guid id,
        [FromBody] ModifierContratRequest request)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized();

        // -----------------------------------------------------
        // Validation
        // -----------------------------------------------------

        if (request.DateFin < request.DateDebut)
            return BadRequest(
                "La date de fin doit être supérieure ou égale à la date de début.");

        if (request.MontantLoyer <= 0)
            return BadRequest(
                "Le montant du loyer doit être supérieur à zéro.");

        if (request.MontantCaution < 0)
            return BadRequest(
                "Le montant de la caution ne peut pas être négatif.");

        if (request.DelaiJoursTolerance < 0)
            return BadRequest(
                "Le délai de tolérance ne peut pas être négatif.");

        if (!Enum.IsDefined(
                typeof(FrequencePaiement),
                request.FrequencePaiement))
        {
            return BadRequest("La fréquence de paiement est invalide.");
        }

        if (!Enum.IsDefined(
                typeof(StatutContrat),
                request.Statut))
        {
            return BadRequest("Le statut du contrat est invalide.");
        }

        // -----------------------------------------------------
        // Recherche sécurisée du contrat
        // -----------------------------------------------------

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.UniteLocative != null &&
                !c.UniteLocative.EstSupprime &&
                c.UniteLocative.BienImmobilier != null &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        // -----------------------------------------------------
        // Si activation :
        // vérifier qu'aucun autre contrat actif n'existe
        // -----------------------------------------------------

        if (request.Statut == StatutContrat.Actif)
        {
            var autreContratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.Id != contrat.Id &&
                    !c.EstSupprime &&
                    c.UniteLocativeId == contrat.UniteLocativeId &&
                    c.Statut == StatutContrat.Actif);

            if (autreContratActif)
            {
                return Conflict(
                    "Cette unité locative possède déjà un autre contrat actif.");
            }
        }

        // -----------------------------------------------------
        // Mise à jour
        // -----------------------------------------------------

        contrat.DateDebut = request.DateDebut;
        contrat.DateFin = request.DateFin;

        contrat.MontantLoyer = request.MontantLoyer;
        contrat.MontantCaution = request.MontantCaution;

        contrat.FrequencePaiement =
            request.FrequencePaiement;

        contrat.DelaiJoursTolerance =
            request.DelaiJoursTolerance;

        contrat.Statut = request.Statut;

        // -----------------------------------------------------
        // Mise à jour du statut de l'unité
        // -----------------------------------------------------

        if (contrat.UniteLocative != null)
        {
            if (request.Statut == StatutContrat.Actif)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Loue;
            }
            else if (
                request.Statut == StatutContrat.Resilie ||
                request.Statut == StatutContrat.Expire)
            {
                var autreContratActif = await _context.Contrats
                    .AnyAsync(c =>
                        c.Id != contrat.Id &&
                        !c.EstSupprime &&
                        c.UniteLocativeId == contrat.UniteLocativeId &&
                        c.Statut == StatutContrat.Actif);

                if (!autreContratActif)
                {
                    contrat.UniteLocative.Statut =
                        StatutDisponibilite.Disponible;
                }
            }
            else if (request.Statut == StatutContrat.EnAttente)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            contrat.Id,
            contrat.Reference,
            contrat.DateDebut,
            contrat.DateFin,
            contrat.MontantLoyer,
            contrat.MontantCaution,
            contrat.FrequencePaiement,
            contrat.DelaiJoursTolerance,
            contrat.Statut
        });
    }


    // =========================================================
    // DELETE : api/Contrats/{id}
    // =========================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Contrats.Delete")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized();

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                c.UniteLocative != null &&
                !c.UniteLocative.EstSupprime &&
                c.UniteLocative.BienImmobilier != null &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        // -----------------------------------------------------
        // Suppression logique
        // -----------------------------------------------------

        contrat.EstSupprime = true;

        // -----------------------------------------------------
        // Si aucun autre contrat actif :
        // l'unité redevient disponible
        // -----------------------------------------------------

        if (contrat.UniteLocative != null)
        {
            var autreContratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.Id != contrat.Id &&
                    !c.EstSupprime &&
                    c.UniteLocativeId == contrat.UniteLocativeId &&
                    c.Statut == StatutContrat.Actif);

            if (!autreContratActif)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // =========================================================
    // FILTRE D'ACCÈS AUX CONTRATS
    // =========================================================

    private IQueryable<Contrat> ApplyOwnershipFilter(
        IQueryable<Contrat> query,
        Guid utilisateurId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role)
                   ?? User.FindFirstValue("role");

        if (string.IsNullOrWhiteSpace(role))
        {
            return query.Where(_ => false);
        }

        // -----------------------------------------------------
        // Administrateur / Gestionnaire
        // -----------------------------------------------------
        //
        // La société est déjà filtrée dans les requêtes
        // appelantes.
        // -----------------------------------------------------

        if (role == "Administrateur" ||
            role == "Gestionnaire")
        {
            return query;
        }

        // -----------------------------------------------------
        // Locataire
        // -----------------------------------------------------

        if (role == "Locataire")
        {
            return query.Where(c =>
                c.LocataireId == utilisateurId);
        }

        // -----------------------------------------------------
        // Agent / autres rôles :
        // pas d'accès aux contrats
        // -----------------------------------------------------

        return query.Where(_ => false);
    }


    // =========================================================
    // CLAIM : SOCIETE
    // =========================================================

    private Guid? GetSocieteId()
    {
        var value =
            User.FindFirstValue("SocieteId")
            ?? User.FindFirstValue("societeId");

        if (Guid.TryParse(value, out var id))
            return id;

        return null;
    }


    // =========================================================
    // CLAIM : UTILISATEUR
    // =========================================================

    private Guid? GetUtilisateurId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(value, out var id))
            return id;

        return null;
    }
}


// =============================================================
// DTO : CREATION
// =============================================================

public class CreerContratRequest
{
    public string Reference { get; set; } = string.Empty;

    public Guid UniteLocativeId { get; set; }

    public Guid LocataireId { get; set; }

    public DateTime DateDebut { get; set; }

    public DateTime DateFin { get; set; }

    public decimal MontantLoyer { get; set; }

    public decimal MontantCaution { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }

    public int DelaiJoursTolerance { get; set; } = 5;
}


// =============================================================
// DTO : MODIFICATION
// =============================================================

public class ModifierContratRequest
{
    public DateTime DateDebut { get; set; }

    public DateTime DateFin { get; set; }

    public decimal MontantLoyer { get; set; }

    public decimal MontantCaution { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }

    public int DelaiJoursTolerance { get; set; } = 5;

    public StatutContrat Statut { get; set; }
}