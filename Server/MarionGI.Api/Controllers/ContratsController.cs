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
        if (!TryGetIdentite(out var societeId, out var utilisateurId))
            return Unauthorized();

        if (statut.HasValue &&
            !Enum.IsDefined(typeof(StatutContrat), statut.Value))
        {
            return BadRequest("Le statut du contrat est invalide.");
        }

        var query = ConstruireQueryAccessible(societeId, utilisateurId);

        if (statut.HasValue)
            query = query.Where(c => c.Statut == statut.Value);

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
        if (!TryGetIdentite(out var societeId, out var utilisateurId))
            return Unauthorized();

        var contrat = await ConstruireQueryAccessible(
                societeId, utilisateurId)
            .Where(c => c.Id == id)
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
        if (!TryGetIdentite(out var societeId, out _))
            return Unauthorized();

        var erreur = ValiderContrat(
            request.DateDebut,
            request.DateFin,
            request.DateSignature,
            request.MontantLoyer,
            request.MontantCaution,
            request.MontantChargesLocatives,
            request.DelaiJoursTolerance,
            request.FrequencePaiement,
            request.RevisionLoyerAutorisee,
            request.DateProchaineRevisionLoyer,
            request.TauxRevisionLoyer,
            request.RenouvellementAutomatique,
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

        if (request.UniteLocativeId == Guid.Empty ||
            request.LocataireId == Guid.Empty)
        {
            return BadRequest(
                "L'unité locative et le locataire sont obligatoires.");
        }

        var referenceExiste = await _context.Contrats.AnyAsync(c =>
            !c.EstSupprime &&
            c.Reference == reference &&
            c.UniteLocative.BienImmobilier.SocieteId == societeId,
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
                u.BienImmobilier.SocieteId == societeId,
                cancellationToken);

        if (unite == null)
        {
            return BadRequest(
                "L'unité locative est introuvable ou n'appartient pas à votre société.");
        }

        var locataire = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == request.LocataireId &&
                u.SocieteId == societeId &&
                !u.EstSupprime &&
                u.Role == RoleUtilisateur.Locataire,
                cancellationToken);

        if (locataire == null)
        {
            return BadRequest(
                "Le locataire est introuvable, supprimé ou ne possède pas le rôle Locataire.");
        }

        var contratActif = await _context.Contrats.AnyAsync(c =>
            !c.EstSupprime &&
            c.UniteLocativeId == unite.Id &&
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
            MontantChargesLocatives =    request.MontantChargesLocatives  ?? unite.ChargesLocativesMensuelles,

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

            // La création ne rend pas automatiquement le contrat actif.
            Statut = StatutContrat.EnAttente,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.Contrats.Add(contrat);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contrat {ContratId} créé pour l'unité {UniteLocativeId}.",
            contrat.Id,
            contrat.UniteLocativeId);

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
                contrat.MontantCaution,
                contrat.MontantChargesLocatives
            });
    }

    // =========================================================
    // PUT : api/Contrats/{id}
    // La référence, l'unité et le locataire sont immuables.
    // =========================================================

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Contrats.Update")]
    public async Task<ActionResult<object>> Modifier(
        Guid id,
        [FromBody] ModifierContratRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentite(out var societeId, out var utilisateurId))
            return Unauthorized();

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId,
                cancellationToken);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        // Défense supplémentaire : le locataire ne modifie que son contrat.
        if (EstLocataire() && contrat.LocataireId != utilisateurId)
            return Forbid();

        var erreur = ValiderContrat(
            request.DateDebut,
            request.DateFin,
            request.DateSignature,
            request.MontantLoyer,
            request.MontantCaution,
            request.MontantChargesLocatives,
            request.DelaiJoursTolerance,
            request.FrequencePaiement,
            request.RevisionLoyerAutorisee,
            request.DateProchaineRevisionLoyer,
            request.TauxRevisionLoyer,
            request.RenouvellementAutomatique,
            request.DelaiPreavisJours,
            request.CheminDocumentContrat,
            request.Observations);

        if (erreur != null)
            return BadRequest(erreur);

        if (!Enum.IsDefined(typeof(StatutContrat), request.Statut))
            return BadRequest("Le statut du contrat est invalide.");

        if (!TransitionStatutAutorisee(contrat.Statut, request.Statut))
        {
            return BadRequest(
                $"La transition du statut '{contrat.Statut}' vers '{request.Statut}' n'est pas autorisée.");
        }

        if (request.Statut == StatutContrat.Actif)
        {
            if (contrat.DateDebut.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(
                    "Un contrat dont la date de début est future ne peut pas être activé.");
            }

            var autreContratActif = await _context.Contrats.AnyAsync(c =>
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

            if (contrat.UniteLocative.Statut != StatutDisponibilite.Disponible &&
                contrat.UniteLocative.Statut != StatutDisponibilite.Loue)
            {
                return Conflict(
                    "Cette unité n'est pas disponible pour l'activation du contrat.");
            }
        }

        // Mise à jour des champs modifiables.
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

        SynchroniserStatutUnite(contrat);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contrat {ContratId} modifié. Nouveau statut : {Statut}.",
            contrat.Id,
            contrat.Statut);

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
        if (!TryGetIdentite(out var societeId, out var utilisateurId))
            return Unauthorized();

        var contrat = await _context.Contrats
            .Include(c => c.UniteLocative)
                .ThenInclude(u => u.BienImmobilier)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId,
                cancellationToken);

        if (contrat == null)
            return NotFound("Contrat introuvable.");

        if (EstLocataire() && contrat.LocataireId != utilisateurId)
            return Forbid();

        contrat.EstSupprime = true;
        contrat.DateSuppression = DateTime.UtcNow;

        if (contrat.Statut == StatutContrat.Actif)
        {
            var autreContratActif = await _context.Contrats.AnyAsync(c =>
                c.Id != contrat.Id &&
                !c.EstSupprime &&
                c.UniteLocativeId == contrat.UniteLocativeId &&
                c.Statut == StatutContrat.Actif,
                cancellationToken);

            if (!autreContratActif &&
                contrat.UniteLocative.Statut == StatutDisponibilite.Loue)
            {
                contrat.UniteLocative.Statut =
                    StatutDisponibilite.Disponible;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contrat {ContratId} supprimé logiquement.",
            contrat.Id);

        return NoContent();
    }

    // =========================================================
    // QUERY : CONTRATS ACCESSIBLES
    // =========================================================

    private IQueryable<Contrat> ConstruireQueryAccessible(
        Guid societeId,
        Guid utilisateurId)
    {
        var query = _context.Contrats
            .AsNoTracking()
            .Where(c =>
                !c.EstSupprime &&
                !c.UniteLocative.EstSupprime &&
                !c.UniteLocative.BienImmobilier.EstSupprime &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId &&
                !c.Locataire.EstSupprime);

        if (EstAdministrateurOuGestionnaire())
            return query;

        if (EstLocataire())
            return query.Where(c => c.LocataireId == utilisateurId);

        return query.Where(_ => false);
    }

    // =========================================================
    // SYNCHRONISATION DU STATUT DE L'UNITÉ
    // =========================================================

    private void SynchroniserStatutUnite(Contrat contrat)
    {
        if (contrat.Statut == StatutContrat.Actif)
        {
            contrat.UniteLocative.Statut = StatutDisponibilite.Loue;
            return;
        }

        var statutSignifieFin =
            contrat.Statut == StatutContrat.Resilie ||
            contrat.Statut == StatutContrat.Expire ||
            contrat.Statut == StatutContrat.EnAttente;

        if (!statutSignifieFin)
            return;

        // Ne jamais remplacer EnTravaux ou Réservé par Disponible.
        // Ne libérer l'unité que si elle est actuellement marquée Loué.
        if (contrat.UniteLocative.Statut != StatutDisponibilite.Loue)
            return;

        var autreContratActif = _context.Contrats.Local.Any(c =>
            c.Id != contrat.Id &&
            !c.EstSupprime &&
            c.UniteLocativeId == contrat.UniteLocativeId &&
            c.Statut == StatutContrat.Actif);

        if (!autreContratActif)
        {
            // La vérification locale ne couvre que les entités suivies.
            // Une vérification en base est effectuée dans Modifier avant
            // de libérer l'unité, pour tenir compte des autres contrats.
            contrat.UniteLocative.Statut = StatutDisponibilite.Disponible;
        }
    }

    // =========================================================
    // TRANSITIONS DE STATUT
    // =========================================================

    private static bool TransitionStatutAutorisee(
        StatutContrat actuel,
        StatutContrat cible)
    {
        if (actuel == cible)
            return true;

        return actuel switch
        {
            StatutContrat.EnAttente =>
                cible == StatutContrat.Actif ||
                cible == StatutContrat.Resilie,

            StatutContrat.Actif =>
                cible == StatutContrat.Resilie ||
                cible == StatutContrat.Expire,

            // Statuts terminaux : pas de réactivation directe.
            StatutContrat.Resilie => false,
            StatutContrat.Expire => false,

            _ => false
        };
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static string? ValiderContrat(
        DateTime dateDebut,
        DateTime dateFin,
        DateTime? dateSignature,
        decimal montantLoyer,
        decimal montantCaution,
        decimal? montantChargesLocatives,
        int delaiJoursTolerance,
        FrequencePaiement frequencePaiement,
        bool revisionLoyerAutorisee,
        DateTime? dateProchaineRevisionLoyer,
        decimal? tauxRevisionLoyer,
        bool renouvellementAutomatique,
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

        if (dateSignature.HasValue && dateSignature.Value == default)
            return "La date de signature est invalide.";

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
            (tauxRevisionLoyer.Value < 0 ||
             tauxRevisionLoyer.Value > 100))
        {
            return "Le taux de révision du loyer doit être compris entre 0 et 100.";
        }

        if (revisionLoyerAutorisee)
        {
            if (!tauxRevisionLoyer.HasValue)
                return "Le taux de révision est obligatoire lorsque la révision du loyer est autorisée.";

            if (!dateProchaineRevisionLoyer.HasValue)
                return "La date de prochaine révision est obligatoire lorsque la révision du loyer est autorisée.";

            if (dateProchaineRevisionLoyer.Value.Date < dateDebut.Date)
                return "La date de prochaine révision ne peut pas précéder le début du contrat.";
        }
        else if (dateProchaineRevisionLoyer.HasValue ||
                 tauxRevisionLoyer.HasValue)
        {
            return "Les paramètres de révision doivent être vides lorsque la révision du loyer n'est pas autorisée.";
        }

        if (renouvellementAutomatique &&
            !delaiPreavisJours.HasValue)
        {
            return "Le délai de préavis est obligatoire lorsque le renouvellement automatique est activé.";
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
    // IDENTITÉ ET RÔLES
    // =========================================================

    private bool TryGetIdentite(
        out Guid societeId,
        out Guid utilisateurId)
    {
        societeId = Guid.Empty;
        utilisateurId = Guid.Empty;

        var societeValue =
            User.FindFirstValue("SocieteId") ??
            User.FindFirstValue("societeId");

        var utilisateurValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        return Guid.TryParse(societeValue, out societeId) &&
               Guid.TryParse(utilisateurValue, out utilisateurId);
    }

    private string? ObtenirRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role");
    }

    private bool EstLocataire()
    {
        return string.Equals(
            ObtenirRole(),
            "Locataire",
            StringComparison.OrdinalIgnoreCase);
    }

    private bool EstAdministrateurOuGestionnaire()
    {
        var role = ObtenirRole();

        return string.Equals(
                   role,
                   "Administrateur",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   role,
                   "Gestionnaire",
                   StringComparison.OrdinalIgnoreCase);
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
