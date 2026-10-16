using System.Security.Claims;
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
        [FromQuery] StatutContrat? statut = null,
        CancellationToken cancellationToken = default)
    {
        var societeId = GetSocieteId();
        var utilisateurId = GetUtilisateurId();

        if (societeId == null || utilisateurId == null)
            return Unauthorized();

        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value &&
                !c.Locataire.EstSupprime);

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

                c.UniteLocativeId,
                c.LocataireId,

                c.DateDebut,
                c.DateFin,
                c.DateSignature,

                c.MontantLoyer,
                c.MontantCaution,
                c.MontantChargesLocatives,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.RevisionLoyerAutorisee,
                c.DateProchaineRevisionLoyer,
                c.TauxRevisionLoyer,

                c.RenouvellementAutomatique,
                c.DelaiPreavisJours,

                c.CheminDocumentContrat,
                c.Observations,

                c.Statut,
                c.DateCreation,

                UniteLocative = new
                {
                    c.UniteLocative.Id,
                    c.UniteLocative.Reference,
                    c.UniteLocative.Type,
                    c.UniteLocative.Superficie,
                    c.UniteLocative.Loyer,
                    c.UniteLocative.Statut
                },

                BienImmobilier = new
                {
                    c.UniteLocative.BienImmobilier.Id,
                    c.UniteLocative.BienImmobilier.Reference,
                    c.UniteLocative.BienImmobilier.Nom,
                    c.UniteLocative.BienImmobilier.Adresse,
                    c.UniteLocative.BienImmobilier.Ville,
                    c.UniteLocative.BienImmobilier.Quartier
                },

                Locataire = new
                {
                    c.Locataire.Id,
                    c.Locataire.Nom,
                    c.Locataire.Prenom,
                    c.Locataire.Email
                }
            })
            .ToListAsync(cancellationToken);

        return Ok(contrats);
    }

    // =========================================================
    // GET : api/Contrats/{id}
    // =========================================================
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Contrats.Read")]
    public async Task<ActionResult<object>> GetContrat(
        Guid id,
        CancellationToken cancellationToken)
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
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value &&
                !c.Locataire.EstSupprime);

        query = ApplyOwnershipFilter(query, utilisateurId.Value);

        var contrat = await query
            .Select(c => new
            {
                c.Id,
                c.Reference,

                c.UniteLocativeId,
                c.LocataireId,

                c.DateDebut,
                c.DateFin,
                c.DateSignature,

                c.MontantLoyer,
                c.MontantCaution,
                c.MontantChargesLocatives,

                c.FrequencePaiement,
                c.DelaiJoursTolerance,

                c.RevisionLoyerAutorisee,
                c.DateProchaineRevisionLoyer,
                c.TauxRevisionLoyer,

                c.RenouvellementAutomatique,
                c.DelaiPreavisJours,

                c.CheminDocumentContrat,
                c.Observations,

                c.Statut,
                c.DateCreation,

                UniteLocative = new
                {
                    c.UniteLocative.Id,
                    c.UniteLocative.Reference,
                    c.UniteLocative.Type,
                    c.UniteLocative.Superficie,
                    c.UniteLocative.Loyer,
                    c.UniteLocative.Statut
                },

                BienImmobilier = new
                {
                    c.UniteLocative.BienImmobilier.Id,
                    c.UniteLocative.BienImmobilier.Reference,
                    c.UniteLocative.BienImmobilier.Nom,
                    c.UniteLocative.BienImmobilier.Adresse,
                    c.UniteLocative.BienImmobilier.Ville,
                    c.UniteLocative.BienImmobilier.Quartier
                },

                Locataire = new
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
            .FirstOrDefaultAsync(cancellationToken);

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
        [FromBody] CreerContratRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized();

        var erreur = ValiderContrat(
            request.DateDebut,
            request.DateFin,
            request.MontantLoyer,
            request.MontantCaution,
            request.MontantChargesLocatives,
            request.DelaiJoursTolerance,
            request.FrequencePaiement,
            request.RevisionLoyerAutorisee,
            request.TauxRevisionLoyer,
            request.DelaiPreavisJours,
            request.CheminDocumentContrat,
            request.Observations);

        if (erreur != null)
            return BadRequest(erreur);

        if (string.IsNullOrWhiteSpace(request.Reference))
            return BadRequest("La référence du contrat est obligatoire.");

        var reference = request.Reference.Trim();

        if (reference.Length > 100)
            return BadRequest(
                "La référence du contrat ne doit pas dépasser 100 caractères.");

        var referenceExiste = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.Reference == reference &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(
                $"La référence '{reference}' est déjà utilisée dans cette société.");
        }

        var unite = await _context.UnitesLocatives
            .Include(u => u.BienImmobilier)
            .FirstOrDefaultAsync(u =>
                u.Id == request.UniteLocativeId &&
                !u.EstSupprime &&
                !u.BienImmobilier.EstSupprime &&
                u.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (unite == null)
        {
            return BadRequest(
                "L'unité locative est introuvable ou n'appartient pas à votre société.");
        }

        var locataire = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == request.LocataireId &&
                u.SocieteId == societeId.Value &&
                !u.EstSupprime &&
                u.Role == RoleUtilisateur.Locataire,
                cancellationToken);

        if (locataire == null)
        {
            return BadRequest(
                "Le locataire est introuvable, supprimé ou ne possède pas le rôle Locataire.");
        }

        var contratActif = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.UniteLocativeId == request.UniteLocativeId &&
                c.Statut == StatutContrat.Actif,
                cancellationToken);

        if (contratActif)
        {
            return Conflict(
                "Cette unité locative possède déjà un contrat actif.");
        }

        var contrat = new Contrat
        {
            Id = Guid.NewGuid(),

            Reference = reference,

            UniteLocativeId = unite.Id,
            LocataireId = locataire.Id,

            DateDebut = request.DateDebut,
            DateFin = request.DateFin,
            DateSignature = request.DateSignature,

            MontantLoyer = request.MontantLoyer,
            MontantCaution = request.MontantCaution,
            MontantChargesLocatives = request.MontantChargesLocatives,

            FrequencePaiement = request.FrequencePaiement,
            DelaiJoursTolerance = request.DelaiJoursTolerance,

            RevisionLoyerAutorisee = request.RevisionLoyerAutorisee,
            DateProchaineRevisionLoyer =
                request.DateProchaineRevisionLoyer,
            TauxRevisionLoyer = request.TauxRevisionLoyer,

            RenouvellementAutomatique = request.RenouvellementAutomatique,
            DelaiPreavisJours = request.DelaiPreavisJours,

            CheminDocumentContrat =
                NettoyerTexte(request.CheminDocumentContrat),
            Observations = NettoyerTexte(request.Observations),

            // Un nouveau contrat reste en attente d'activation.
            Statut = StatutContrat.EnAttente,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.Contrats.Add(contrat);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetContrat),
            new { id = contrat.Id },
            new
            {
                contrat.Id,
                contrat.Reference,
                contrat.Statut,
                contrat.UniteLocativeId,
                contrat.LocataireId,
                contrat.DateDebut,
                contrat.DateFin,
                contrat.MontantLoyer,
                contrat.MontantCaution
            });
    }

    // =========================================================
    // PUT : api/Contrats/{id}
    // La référence, l'unité et le locataire restent inchangés.
    // =========================================================
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Contrats.Update")]
    public async Task<ActionResult<object>> Modifier(
        Guid id,
        [FromBody] ModifierContratRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized();

        var erreur = ValiderContrat(
            request.DateDebut,
            request.DateFin,
            request.MontantLoyer,
            request.MontantCaution,
            request.MontantChargesLocatives,
            request.DelaiJoursTolerance,
            request.FrequencePaiement,
            request.RevisionLoyerAutorisee,
            request.TauxRevisionLoyer,
            request.DelaiPreavisJours,
            request.CheminDocumentContrat,
            request.Observations);

        if (erreur != null)
            return BadRequest(erreur);

        if (!Enum.IsDefined(typeof(StatutContrat), request.Statut))
            return BadRequest("Le statut du contrat est invalide.");

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        if (request.Statut == StatutContrat.Actif)
        {
            var autreContratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.Id != contrat.Id &&
                    !c.EstSupprime &&
                    c.UniteLocativeId == contrat.UniteLocativeId &&
                    c.Statut == StatutContrat.Actif,
                    cancellationToken);

            if (autreContratActif)
            {
                return Conflict(
                    "Cette unité locative possède déjà un autre contrat actif.");
            }
        }

        contrat.DateDebut = request.DateDebut;
        contrat.DateFin = request.DateFin;
        contrat.DateSignature = request.DateSignature;

        contrat.MontantLoyer = request.MontantLoyer;
        contrat.MontantCaution = request.MontantCaution;
        contrat.MontantChargesLocatives = request.MontantChargesLocatives;

        contrat.FrequencePaiement = request.FrequencePaiement;
        contrat.DelaiJoursTolerance = request.DelaiJoursTolerance;

        contrat.RevisionLoyerAutorisee = request.RevisionLoyerAutorisee;
        contrat.DateProchaineRevisionLoyer =
            request.DateProchaineRevisionLoyer;
        contrat.TauxRevisionLoyer = request.TauxRevisionLoyer;

        contrat.RenouvellementAutomatique = request.RenouvellementAutomatique;
        contrat.DelaiPreavisJours = request.DelaiPreavisJours;

        contrat.CheminDocumentContrat =
            NettoyerTexte(request.CheminDocumentContrat);
        contrat.Observations = NettoyerTexte(request.Observations);

        contrat.Statut = request.Statut;

        // Synchronisation du statut de l'unité.
        if (request.Statut == StatutContrat.Actif)
        {
            contrat.UniteLocative.Statut = StatutDisponibilite.Loue;
        }
        else if (
            request.Statut == StatutContrat.Resilie ||
            request.Statut == StatutContrat.Expire ||
            request.Statut == StatutContrat.EnAttente)
        {
            var autreContratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.Id != contrat.Id &&
                    !c.EstSupprime &&
                    c.UniteLocativeId == contrat.UniteLocativeId &&
                    c.Statut == StatutContrat.Actif,
                    cancellationToken);

            if (!autreContratActif)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Contrat modifié avec succès.",
            contrat.Id,
            contrat.Reference,
            contrat.DateDebut,
            contrat.DateFin,
            contrat.DateSignature,
            contrat.MontantLoyer,
            contrat.MontantCaution,
            contrat.MontantChargesLocatives,
            contrat.FrequencePaiement,
            contrat.DelaiJoursTolerance,
            contrat.RevisionLoyerAutorisee,
            contrat.DateProchaineRevisionLoyer,
            contrat.TauxRevisionLoyer,
            contrat.RenouvellementAutomatique,
            contrat.DelaiPreavisJours,
            contrat.CheminDocumentContrat,
            contrat.Observations,
            contrat.Statut
        });
    }

    // =========================================================
    // DELETE : api/Contrats/{id}
    // Suppression logique
    // =========================================================
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Contrats.Delete")]
    public async Task<IActionResult> Supprimer(
        Guid id,
        CancellationToken cancellationToken)
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
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        contrat.EstSupprime = true;
        contrat.DateSuppression = DateTime.UtcNow;

        if (contrat.Statut == StatutContrat.Actif)
        {
            var autreContratActif = await _context.Contrats
                .AnyAsync(c =>
                    c.Id != contrat.Id &&
                    !c.EstSupprime &&
                    c.UniteLocativeId == contrat.UniteLocativeId &&
                    c.Statut == StatutContrat.Actif,
                    cancellationToken);

            if (!autreContratActif)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // =========================================================
    // VALIDATION DES DONNÉES
    // =========================================================
    private static string? ValiderContrat(
        DateTime dateDebut,
        DateTime dateFin,
        decimal montantLoyer,
        decimal montantCaution,
        decimal? montantChargesLocatives,
        int delaiJoursTolerance,
        FrequencePaiement frequencePaiement,
        bool revisionLoyerAutorisee,
        decimal? tauxRevisionLoyer,
        int? delaiPreavisJours,
        string? cheminDocumentContrat,
        string? observations)
    {
        if (dateDebut == default)
            return "La date de début est obligatoire.";

        if (dateFin == default)
            return "La date de fin est obligatoire.";

        if (dateFin.Date < dateDebut.Date)
            return "La date de fin doit être supérieure ou égale à la date de début.";

        if (montantLoyer <= 0)
            return "Le montant du loyer doit être supérieur à zéro.";

        if (montantCaution < 0)
            return "Le montant de la caution ne peut pas être négatif.";

        if (montantChargesLocatives.HasValue &&
            montantChargesLocatives.Value < 0)
        {
            return "Le montant des charges locatives ne peut pas être négatif.";
        }

        if (delaiJoursTolerance < 0)
            return "Le délai de tolérance ne peut pas être négatif.";

        if (!Enum.IsDefined(typeof(FrequencePaiement), frequencePaiement))
            return "La fréquence de paiement est invalide.";

        if (tauxRevisionLoyer.HasValue &&
            (tauxRevisionLoyer.Value < 0 || tauxRevisionLoyer.Value > 100))
        {
            return "Le taux de révision du loyer doit être compris entre 0 et 100.";
        }

        if (revisionLoyerAutorisee &&
            tauxRevisionLoyer.HasValue &&
            tauxRevisionLoyer.Value < 0)
        {
            return "Le taux de révision du loyer ne peut pas être négatif.";
        }

        if (delaiPreavisJours.HasValue &&
            delaiPreavisJours.Value < 0)
        {
            return "Le délai de préavis ne peut pas être négatif.";
        }

        if (!string.IsNullOrWhiteSpace(cheminDocumentContrat) &&
            cheminDocumentContrat.Trim().Length > 500)
        {
            return "Le chemin du document ne doit pas dépasser 500 caractères.";
        }

        if (!string.IsNullOrWhiteSpace(observations) &&
            observations.Trim().Length > 2000)
        {
            return "Les observations ne doivent pas dépasser 2000 caractères.";
        }

        return null;
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
            return query.Where(_ => false);

        if (role == "Administrateur" || role == "Gestionnaire")
            return query;

        if (role == "Locataire")
            return query.Where(c => c.LocataireId == utilisateurId);

        return query.Where(_ => false);
    }

    // =========================================================
    // CLAIM : SOCIÉTÉ
    // =========================================================
    private Guid? GetSocieteId()
    {
        var value = User.FindFirstValue("SocieteId")
                    ?? User.FindFirstValue("societeId");

        return Guid.TryParse(value, out var id) ? id : null;
    }

    // =========================================================
    // CLAIM : UTILISATEUR
    // =========================================================
    private Guid? GetUtilisateurId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string? NettoyerTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }
}

// =============================================================
// DTO : CRÉATION
// =============================================================
public class CreerContratRequest
{
    public string Reference { get; set; } = string.Empty;

    public Guid UniteLocativeId { get; set; }
    public Guid LocataireId { get; set; }

    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public DateTime? DateSignature { get; set; }

    public decimal MontantLoyer { get; set; }
    public decimal MontantCaution { get; set; }
    public decimal? MontantChargesLocatives { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }
        = FrequencePaiement.Mensuel;

    public int DelaiJoursTolerance { get; set; } = 5;

    public bool RevisionLoyerAutorisee { get; set; }
    public DateTime? DateProchaineRevisionLoyer { get; set; }
    public decimal? TauxRevisionLoyer { get; set; }

    public bool RenouvellementAutomatique { get; set; }
    public int? DelaiPreavisJours { get; set; }

    public string? CheminDocumentContrat { get; set; }
    public string? Observations { get; set; }
}

// =============================================================
// DTO : MODIFICATION
// La référence, l'unité et le locataire sont immuables ici.
// =============================================================
public class ModifierContratRequest
{
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public DateTime? DateSignature { get; set; }

    public decimal MontantLoyer { get; set; }
    public decimal MontantCaution { get; set; }
    public decimal? MontantChargesLocatives { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }
        = FrequencePaiement.Mensuel;

    public int DelaiJoursTolerance { get; set; } = 5;

    public bool RevisionLoyerAutorisee { get; set; }
    public DateTime? DateProchaineRevisionLoyer { get; set; }
    public decimal? TauxRevisionLoyer { get; set; }

    public bool RenouvellementAutomatique { get; set; }
    public int? DelaiPreavisJours { get; set; }

    public string? CheminDocumentContrat { get; set; }
    public string? Observations { get; set; }

    public StatutContrat Statut { get; set; }
}
