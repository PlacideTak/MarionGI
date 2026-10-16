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
public class UnitesLocativesController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const int NombreMaxPhotos = 20;
    private const long TailleMaxPhoto = 10 * 1024 * 1024;
    private const int LongueurMaxMotifExoneration = 1000;

    private const string PrefixPhotos = "/uploads/unites/";

    private static readonly HashSet<string> ExtensionsAutorisees =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    public UnitesLocativesController(
        MarionDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ============================================================
    // GET : api/UnitesLocatives
    // ============================================================

    [HttpGet]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Agent) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetUnites(
        CancellationToken cancellationToken)
    {
        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        IQueryable<UniteLocative> query =
            _context.UnitesLocatives
                .AsNoTracking()
                .Where(u =>
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime);

        if (EstPersonnel(utilisateur.Role))
        {
            if (utilisateur.SocieteId == Guid.Empty)
            {
                return Forbid();
            }

            query = query.Where(u =>
                u.BienImmobilier.SocieteId ==
                utilisateur.SocieteId);
        }
        else if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            query = query.Where(u =>
                u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.LocataireId == utilisateur.Id));
        }
        else
        {
            return Forbid();
        }

        var unites = await query
            .OrderBy(u => u.BienImmobilier.Reference)
            .ThenBy(u => u.Reference)
            .Select(u => new
            {
                u.Id,
                u.Reference,
                u.Type,
                u.Superficie,
                u.NombrePieces,
                u.Etage,

                u.Loyer,
                u.ChargesLocativesMensuelles,
                u.MontantCaution,
                u.Statut,

                u.DatePremiereMiseEnLocation,
                u.DateDerniereRenovation,

                u.EstExonereeFiscalement,
                u.MotifExoneration,

                u.Photos,
                u.BienImmobilierId,

                BienReference = u.BienImmobilier.Reference,
                BienAdresse = u.BienImmobilier.Adresse,
                BienVille = u.BienImmobilier.Ville,
                BienQuartier = u.BienImmobilier.Quartier,
                BienNom = u.BienImmobilier.Nom,
                SocieteId = u.BienImmobilier.SocieteId,
                SocieteNom = u.BienImmobilier.Societe.Nom,

                NombreContrats = u.Contrats.Count(c =>
                    !c.EstSupprime &&
                    (
                        utilisateur.Role != RoleUtilisateur.Locataire ||
                        c.LocataireId == utilisateur.Id
                    )),

                AContratActif = u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.Statut == StatutContrat.Actif &&
                    (
                        utilisateur.Role != RoleUtilisateur.Locataire ||
                        c.LocataireId == utilisateur.Id
                    )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d => !d.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(unites);
    }

    // ============================================================
    // GET : api/UnitesLocatives/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Agent) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de l'unité est invalide.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        IQueryable<UniteLocative> query =
            _context.UnitesLocatives
                .AsNoTracking()
                .Where(u =>
                    u.Id == id &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime);

        if (EstPersonnel(utilisateur.Role))
        {
            if (utilisateur.SocieteId == Guid.Empty)
            {
                return Forbid();
            }

            query = query.Where(u =>
                u.BienImmobilier.SocieteId ==
                utilisateur.SocieteId);
        }
        else if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            query = query.Where(u =>
                u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.LocataireId == utilisateur.Id));
        }
        else
        {
            return Forbid();
        }

        var unite = await query
            .Select(u => new
            {
                u.Id,
                u.Reference,
                u.Type,
                u.Superficie,
                u.NombrePieces,
                u.Etage,

                u.Loyer,
                u.ChargesLocativesMensuelles,
                u.MontantCaution,
                u.Statut,

                u.DatePremiereMiseEnLocation,
                u.DateDerniereRenovation,

                u.EstExonereeFiscalement,
                u.MotifExoneration,

                u.Photos,
                u.BienImmobilierId,

                Bien = new
                {
                    u.BienImmobilier.Id,
                    u.BienImmobilier.Reference,
                    u.BienImmobilier.Adresse,
                    u.BienImmobilier.Ville,
                    u.BienImmobilier.Quartier,
                    u.BienImmobilier.Nom,
                    u.BienImmobilier.SocieteId
                },

                Contrats = u.Contrats
                    .Where(c =>
                        !c.EstSupprime &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire ||
                            c.LocataireId == utilisateur.Id
                        ))
                    .Select(c => new
                    {
                        c.Id,
                        c.Reference,
                        c.Statut,
                        c.DateDebut,
                        c.DateFin,
                        c.MontantLoyer
                    })
                    .ToList(),

                AContratActif = u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.Statut == StatutContrat.Actif &&
                    (
                        utilisateur.Role != RoleUtilisateur.Locataire ||
                        c.LocataireId == utilisateur.Id
                    )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d => !d.EstSupprime)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (unite == null)
        {
            return NotFound("Unité locative introuvable.");
        }

        return Ok(unite);
    }

    // ============================================================
    // GET : api/UnitesLocatives/bien/{bienImmobilierId}
    // ============================================================

    [HttpGet("bien/{bienImmobilierId:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire) + "," +
        nameof(RoleUtilisateur.Agent) + "," +
        nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetParBien(
        Guid bienImmobilierId,
        CancellationToken cancellationToken)
    {
        if (bienImmobilierId == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant du bien est invalide.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        var bienQuery = _context.BiensImmobiliers
            .AsNoTracking()
            .Where(b =>
                b.Id == bienImmobilierId &&
                !b.EstSupprime);

        if (EstPersonnel(utilisateur.Role))
        {
            if (utilisateur.SocieteId == Guid.Empty)
            {
                return Forbid();
            }

            bienQuery = bienQuery.Where(b =>
                b.SocieteId == utilisateur.SocieteId);
        }
        else if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            // Le locataire ne peut accéder au bien que si
            // celui-ci contient au moins une de ses unités.
            bienQuery = bienQuery.Where(b =>
                b.UnitesLocatives.Any(u =>
                    !u.EstSupprime &&
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.LocataireId == utilisateur.Id)));
        }
        else
        {
            return Forbid();
        }

        var bienExiste = await bienQuery.AnyAsync(cancellationToken);

        if (!bienExiste)
        {
            return NotFound(
                "Bien immobilier introuvable ou inaccessible.");
        }

        IQueryable<UniteLocative> query =
            _context.UnitesLocatives
                .AsNoTracking()
                .Where(u =>
                    u.BienImmobilierId == bienImmobilierId &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime);

        if (utilisateur.Role == RoleUtilisateur.Locataire)
        {
            query = query.Where(u =>
                u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.LocataireId == utilisateur.Id));
        }

        var unites = await query
            .OrderBy(u => u.Reference)
            .Select(u => new
            {
                u.Id,
                u.Reference,
                u.Type,
                u.Superficie,
                u.NombrePieces,
                u.Etage,

                u.Loyer,
                u.ChargesLocativesMensuelles,
                u.MontantCaution,
                u.Statut,

                u.DatePremiereMiseEnLocation,
                u.DateDerniereRenovation,

                u.EstExonereeFiscalement,
                u.MotifExoneration,

                u.Photos,
                u.BienImmobilierId,

                NombreContrats = u.Contrats.Count(c =>
                    !c.EstSupprime &&
                    (
                        utilisateur.Role != RoleUtilisateur.Locataire ||
                        c.LocataireId == utilisateur.Id
                    )),

                AContratActif = u.Contrats.Any(c =>
                    !c.EstSupprime &&
                    c.Statut == StatutContrat.Actif &&
                    (
                        utilisateur.Role != RoleUtilisateur.Locataire ||
                        c.LocataireId == utilisateur.Id
                    )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d => !d.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(unites);
    }

    // ============================================================
    // POST : api/UnitesLocatives
    // ============================================================

    [HttpPost]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> CreerUnite(
        [FromForm] CreerUniteLocativeRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(
                "Les données de l'unité sont obligatoires.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        if (!EstPersonnelAutoriseAModifier(utilisateur.Role))
        {
            return Forbid();
        }

        var validation = ValiderUnite(
            request.Reference,
            request.Type,
            request.Superficie,
            request.NombrePieces,
            request.Loyer,
            request.ChargesLocativesMensuelles,
            request.MontantCaution,
            request.Statut,
            request.DatePremiereMiseEnLocation,
            request.DateDerniereRenovation,
            request.EstExonereeFiscalement,
            request.MotifExoneration);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        if (utilisateur.SocieteId == Guid.Empty)
        {
            return BadRequest(
                "Aucune société n'est associée à cet utilisateur.");
        }

        if (request.BienImmobilierId == Guid.Empty)
        {
            return BadRequest(
                "Le bien immobilier est obligatoire.");
        }

        var bienExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                    b.Id == request.BienImmobilierId &&
                    b.SocieteId == utilisateur.SocieteId &&
                    !b.EstSupprime,
                cancellationToken);

        if (!bienExiste)
        {
            return NotFound(
                "Le bien immobilier est introuvable ou n'appartient pas à votre société.");
        }

        var reference = NormaliserReference(request.Reference);

        var referenceExiste = await _context.UnitesLocatives
            .AnyAsync(u =>
                    u.BienImmobilierId == request.BienImmobilierId &&
                    u.Reference == reference &&
                    !u.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(
                "Une unité avec cette référence existe déjà dans ce bien.");
        }

        var unite = new UniteLocative
        {
            Id = Guid.NewGuid(),
            Reference = reference,
            Type = request.Type,
            Superficie = request.Superficie,
            NombrePieces = request.NombrePieces,
            Etage = request.Etage,

            Loyer = request.Loyer,
            ChargesLocativesMensuelles =
                request.ChargesLocativesMensuelles,
            MontantCaution = request.MontantCaution,
            Statut = request.Statut,

            DatePremiereMiseEnLocation =
                request.DatePremiereMiseEnLocation,
            DateDerniereRenovation =
                request.DateDerniereRenovation,

            EstExonereeFiscalement =
                request.EstExonereeFiscalement,
            MotifExoneration =
                NormaliserMotifExoneration(
                    request.EstExonereeFiscalement,
                    request.MotifExoneration),

            BienImmobilierId = request.BienImmobilierId,
            Photos = [],
            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        var fichiersCrees = new List<string>();

        try
        {
            var resultatUpload = await EnregistrerPhotosAsync(
                request.Fichiers,
                [],
                fichiersCrees,
                cancellationToken);

            if (!resultatUpload.Succes)
            {
                SupprimerFichiersPhysiques(fichiersCrees);
                return BadRequest(resultatUpload.Message);
            }

            unite.Photos = resultatUpload.Photos;

            _context.UnitesLocatives.Add(unite);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            SupprimerFichiersPhysiques(fichiersCrees);
            throw;
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = unite.Id },
            new
            {
                unite.Id,
                unite.Reference,
                unite.Type,
                unite.Superficie,
                unite.NombrePieces,
                unite.Etage,
                unite.Loyer,
                unite.ChargesLocativesMensuelles,
                unite.MontantCaution,
                unite.Statut,
                unite.DatePremiereMiseEnLocation,
                unite.DateDerniereRenovation,
                unite.EstExonereeFiscalement,
                unite.MotifExoneration,
                unite.BienImmobilierId,
                unite.Photos,
                AContratActif = false
            });
    }

    // ============================================================
    // PUT : api/UnitesLocatives/{id}
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> ModifierUnite(
        Guid id,
        [FromForm] ModifierUniteLocativeRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de l'unité est invalide.");
        }

        if (request == null)
        {
            return BadRequest(
                "Les données de l'unité sont obligatoires.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        if (!EstPersonnelAutoriseAModifier(utilisateur.Role) ||
            utilisateur.SocieteId == Guid.Empty)
        {
            return Forbid();
        }

        var validation = ValiderUnite(
            request.Reference,
            request.Type,
            request.Superficie,
            request.NombrePieces,
            request.Loyer,
            request.ChargesLocativesMensuelles,
            request.MontantCaution,
            request.Statut,
            request.DatePremiereMiseEnLocation,
            request.DateDerniereRenovation,
            request.EstExonereeFiscalement,
            request.MotifExoneration);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        var unite = await _context.UnitesLocatives
            .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId ==
                        utilisateur.SocieteId,
                cancellationToken);

        if (unite == null)
        {
            return NotFound("Unité locative introuvable.");
        }

        if (request.BienImmobilierId == Guid.Empty)
        {
            return BadRequest(
                "Le bien immobilier est obligatoire.");
        }

        var bienExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(b =>
                    b.Id == request.BienImmobilierId &&
                    b.SocieteId == utilisateur.SocieteId &&
                    !b.EstSupprime,
                cancellationToken);

        if (!bienExiste)
        {
            return NotFound(
                "Le bien immobilier est introuvable ou n'appartient pas à votre société.");
        }

        var contratActifExiste = await _context.Contrats
            .AsNoTracking()
            .AnyAsync(c =>
                    c.UniteLocativeId == id &&
                    !c.EstSupprime &&
                    c.Statut == StatutContrat.Actif,
                cancellationToken);

        if (contratActifExiste &&
            unite.BienImmobilierId != request.BienImmobilierId)
        {
            return Conflict(
                "Une unité possédant un contrat actif ne peut pas être déplacée vers un autre bien.");
        }

        if (contratActifExiste &&
            request.Statut != StatutDisponibilite.Loue)
        {
            return Conflict(
                "Une unité possédant un contrat actif doit conserver le statut Louée.");
        }

        var reference = NormaliserReference(request.Reference);

        var referenceExiste = await _context.UnitesLocatives
            .AnyAsync(u =>
                    u.Id != id &&
                    u.BienImmobilierId == request.BienImmobilierId &&
                    u.Reference == reference &&
                    !u.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(
                "Une autre unité utilise déjà cette référence dans ce bien.");
        }

        var anciennesPhotos = unite.Photos?.ToList() ?? [];

        // Si PhotosExistantes est omis, conserver les photos actuelles.
        // Si elle est fournie, seules les photos déjà associées à
        // cette unité peuvent être conservées.
        List<string> photosFinales;

        if (request.PhotosExistantes == null)
        {
            photosFinales = anciennesPhotos.ToList();
        }
        else
        {
            var photosDemandees =
                NettoyerPhotos(request.PhotosExistantes);

            var photosNonAutorisees = photosDemandees
                .Except(
                    anciennesPhotos,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (photosNonAutorisees.Count > 0)
            {
                return BadRequest(
                    "La liste contient une ou plusieurs photos qui ne sont pas associées à cette unité.");
            }

            photosFinales = FiltrerPhotosUnite(photosDemandees);
        }

        if (photosFinales.Count > NombreMaxPhotos)
        {
            return BadRequest(
                $"Une unité ne peut pas contenir plus de {NombreMaxPhotos} photos.");
        }

        var fichiersCrees = new List<string>();

        try
        {
            var resultatUpload = await EnregistrerPhotosAsync(
                request.Fichiers,
                photosFinales,
                fichiersCrees,
                cancellationToken);

            if (!resultatUpload.Succes)
            {
                SupprimerFichiersPhysiques(fichiersCrees);
                return BadRequest(resultatUpload.Message);
            }

            photosFinales = resultatUpload.Photos;

            unite.Reference = reference;
            unite.Type = request.Type;
            unite.Superficie = request.Superficie;
            unite.NombrePieces = request.NombrePieces;
            unite.Etage = request.Etage;

            unite.Loyer = request.Loyer;
            unite.ChargesLocativesMensuelles =
                request.ChargesLocativesMensuelles;
            unite.MontantCaution = request.MontantCaution;
            unite.Statut = request.Statut;

            unite.DatePremiereMiseEnLocation =
                request.DatePremiereMiseEnLocation;
            unite.DateDerniereRenovation =
                request.DateDerniereRenovation;

            unite.EstExonereeFiscalement =
                request.EstExonereeFiscalement;
            unite.MotifExoneration =
                NormaliserMotifExoneration(
                    request.EstExonereeFiscalement,
                    request.MotifExoneration);

            unite.BienImmobilierId = request.BienImmobilierId;
            unite.Photos = photosFinales;

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            SupprimerFichiersPhysiques(fichiersCrees);
            throw;
        }

        // Les fichiers retirés ne sont supprimés qu'après
        // l'enregistrement réussi de la modification.
        var photosSupprimees = anciennesPhotos
            .Except(
                photosFinales,
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        SupprimerFichiersPhysiques(photosSupprimees);

        return NoContent();
    }

    // ============================================================
    // PATCH : api/UnitesLocatives/{id}/statut
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutUniteRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de l'unité est invalide.");
        }

        if (request == null ||
            !Enum.IsDefined(request.Statut))
        {
            return BadRequest(
                "Le statut de l'unité est invalide.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        if (!EstPersonnelAutoriseAModifier(utilisateur.Role) ||
            utilisateur.SocieteId == Guid.Empty)
        {
            return Forbid();
        }

        var unite = await _context.UnitesLocatives
            .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId ==
                        utilisateur.SocieteId,
                cancellationToken);

        if (unite == null)
        {
            return NotFound("Unité locative introuvable.");
        }

        var contratActifExiste = await _context.Contrats
            .AsNoTracking()
            .AnyAsync(c =>
                    c.UniteLocativeId == id &&
                    !c.EstSupprime &&
                    c.Statut == StatutContrat.Actif,
                cancellationToken);

        if (contratActifExiste &&
            request.Statut != StatutDisponibilite.Loue)
        {
            return Conflict(
                "Cette unité possède un contrat actif et doit conserver le statut Louée.");
        }

        unite.Statut = request.Statut;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = "Statut de l'unité modifié avec succès.",
            unite.Id,
            unite.Statut
        });
    }

    // ============================================================
    // DELETE : suppression logique
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> SupprimerUnite(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(
                "L'identifiant de l'unité est invalide.");
        }

        var utilisateur =
            await GetUtilisateurConnecteAsync(cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        if (!EstPersonnelAutoriseAModifier(utilisateur.Role) ||
            utilisateur.SocieteId == Guid.Empty)
        {
            return Forbid();
        }

        var unite = await _context.UnitesLocatives
            .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime &&
                    u.BienImmobilier.SocieteId ==
                        utilisateur.SocieteId,
                cancellationToken);

        if (unite == null)
        {
            return NotFound("Unité locative introuvable.");
        }

        var historiqueContratsExiste = await _context.Contrats
            .AsNoTracking()
            .AnyAsync(c =>
                    c.UniteLocativeId == id &&
                    !c.EstSupprime,
                cancellationToken);

        if (historiqueContratsExiste)
        {
            return Conflict(
                "Cette unité possède un historique de contrats. Elle ne peut pas être supprimée afin de préserver l'historique locatif et fiscal.");
        }

        unite.EstSupprime = true;
        unite.DateSuppression = DateTime.UtcNow;

        // Les photos sont conservées lors de la suppression logique.
        // Cela évite de détruire des éléments potentiellement utiles
        // à l'historique ou à un audit.
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            Message = "Unité locative supprimée logiquement avec succès."
        });
    }

    // ============================================================
    // UTILISATEUR CONNECTÉ
    // ============================================================

    private async Task<UtilisateurConnecte?> GetUtilisateurConnecteAsync(
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (!currentUserId.HasValue)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == currentUserId.Value &&
                !u.EstSupprime &&
                u.Statut)
            .Select(u => new UtilisateurConnecte
            {
                Id = u.Id,
                SocieteId = u.SocieteId,
                Role = u.Role
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var claims = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("sub"),
            User.FindFirstValue("Id")
        };

        foreach (var claim in claims)
        {
            if (Guid.TryParse(claim, out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static bool EstPersonnel(RoleUtilisateur role)
    {
        return role == RoleUtilisateur.Administrateur ||
               role == RoleUtilisateur.Gestionnaire ||
               role == RoleUtilisateur.Agent;
    }

    private static bool EstPersonnelAutoriseAModifier(
        RoleUtilisateur role)
    {
        return role == RoleUtilisateur.Administrateur ||
               role == RoleUtilisateur.Gestionnaire;
    }

    // ============================================================
    // UPLOAD DES PHOTOS
    // ============================================================

    private async Task<ResultatUploadPhotosUnite> EnregistrerPhotosAsync(
        IFormFileCollection? fichiers,
        List<string> photosExistantes,
        List<string> fichiersCrees,
        CancellationToken cancellationToken)
    {
        var photos = photosExistantes.ToList();

        if (photos.Count > NombreMaxPhotos)
        {
            return ResultatUploadPhotosUnite.Echec(
                $"Une unité ne peut pas contenir plus de {NombreMaxPhotos} photos.");
        }

        if (fichiers == null || fichiers.Count == 0)
        {
            return ResultatUploadPhotosUnite.SuccesResult(photos);
        }

        // Vérifier les limites avant d'écrire les fichiers.
        var fichiersNonVides = fichiers
            .Where(f => f != null && f.Length > 0)
            .ToList();

        if (photos.Count + fichiersNonVides.Count > NombreMaxPhotos)
        {
            return ResultatUploadPhotosUnite.Echec(
                $"Une unité ne peut pas contenir plus de {NombreMaxPhotos} photos.");
        }

        foreach (var fichier in fichiersNonVides)
        {
            if (fichier.Length > TailleMaxPhoto)
            {
                return ResultatUploadPhotosUnite.Echec(
                    $"Le fichier '{fichier.FileName}' dépasse la taille maximale de 10 Mo.");
            }

            var extension = Path.GetExtension(fichier.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !ExtensionsAutorisees.Contains(extension))
            {
                return ResultatUploadPhotosUnite.Echec(
                    $"Le format du fichier '{fichier.FileName}' n'est pas autorisé. Formats acceptés : JPG, JPEG, PNG et WEBP.");
            }
        }

        var dossierUploads = GetDossierUploads();
        Directory.CreateDirectory(dossierUploads);

        foreach (var fichier in fichiersNonVides)
        {
            var extension =
                Path.GetExtension(fichier.FileName).ToLowerInvariant();

            var nomFichier = $"{Guid.NewGuid():N}{extension}";
            var cheminComplet = Path.Combine(dossierUploads, nomFichier);

            try
            {
                await using var stream = new FileStream(
                    cheminComplet,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await fichier.CopyToAsync(stream, cancellationToken);

                var cheminRelatif = $"{PrefixPhotos}{nomFichier}";

                photos.Add(cheminRelatif);
                fichiersCrees.Add(cheminRelatif);
            }
            catch
            {
                // Le nettoyage des fichiers déjà créés est effectué
                // par l'appelant si l'opération échoue.
                throw;
            }
        }

        return ResultatUploadPhotosUnite.SuccesResult(photos);
    }

    private string GetDossierUploads()
    {
        var webRoot = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot");
        }

        return Path.Combine(webRoot, "uploads", "unites");
    }

    private void SupprimerFichiersPhysiques(IEnumerable<string> chemins)
    {
        foreach (var chemin in chemins)
        {
            try
            {
                var cheminComplet = GetCheminPhysique(chemin);

                if (cheminComplet != null &&
                    System.IO.File.Exists(cheminComplet))
                {
                    System.IO.File.Delete(cheminComplet);
                }
            }
            catch
            {
                // Une erreur de nettoyage ne doit pas masquer
                // l'erreur initiale de l'opération.
            }
        }
    }

    private string? GetCheminPhysique(string cheminRelatif)
    {
        if (string.IsNullOrWhiteSpace(cheminRelatif) ||
            !cheminRelatif.StartsWith(
                PrefixPhotos,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var nomFichier = cheminRelatif[PrefixPhotos.Length..];

        if (string.IsNullOrWhiteSpace(nomFichier) ||
            nomFichier.Contains('/') ||
            nomFichier.Contains('\\') ||
            nomFichier.Contains("..") ||
            Path.GetFileName(nomFichier) != nomFichier)
        {
            return null;
        }

        return Path.Combine(GetDossierUploads(), nomFichier);
    }

    private static List<string> FiltrerPhotosUnite(
        IEnumerable<string> photos)
    {
        return photos
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(p => p.StartsWith(
                PrefixPhotos,
                StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains(".."))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NettoyerPhotos(
        IEnumerable<string>? photos)
    {
        if (photos == null)
        {
            return [];
        }

        return photos
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(p => p.Length <= 500)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ============================================================
    // VALIDATION MÉTIER
    // ============================================================

    private static string? ValiderUnite(
        string? reference,
        TypeUniteLocative type,
        decimal superficie,
        int? nombrePieces,
        decimal loyer,
        decimal? chargesLocativesMensuelles,
        decimal? montantCaution,
        StatutDisponibilite statut,
        DateTime? datePremiereMiseEnLocation,
        DateTime? dateDerniereRenovation,
        bool estExonereeFiscalement,
        string? motifExoneration)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return "La référence de l'unité est obligatoire.";
        }

        if (reference.Trim().Length > 50)
        {
            return "La référence ne peut pas dépasser 50 caractères.";
        }

        if (!Enum.IsDefined(type))
        {
            return "Le type de l'unité est invalide.";
        }

        if (superficie <= 0 || superficie > 1_000_000)
        {
            return "La superficie doit être supérieure à zéro et ne pas dépasser 1 000 000.";
        }

        if (nombrePieces.HasValue && nombrePieces.Value <= 0)
        {
            return "Le nombre de pièces doit être supérieur à zéro.";
        }

        if (loyer < 0)
        {
            return "Le loyer ne peut pas être négatif.";
        }

        if (chargesLocativesMensuelles.HasValue &&
            chargesLocativesMensuelles.Value < 0)
        {
            return "Les charges locatives mensuelles ne peuvent pas être négatives.";
        }

        if (montantCaution.HasValue && montantCaution.Value < 0)
        {
            return "Le montant de la caution ne peut pas être négatif.";
        }

        if (!Enum.IsDefined(statut))
        {
            return "Le statut de l'unité est invalide.";
        }

        if (estExonereeFiscalement)
        {
            if (string.IsNullOrWhiteSpace(motifExoneration))
            {
                return "Un motif doit être renseigné lorsque l'unité est déclarée exonérée fiscalement.";
            }

            if (motifExoneration.Trim().Length >
                LongueurMaxMotifExoneration)
            {
                return $"Le motif d'exonération ne peut pas dépasser {LongueurMaxMotifExoneration} caractères.";
            }
        }

        if (!estExonereeFiscalement &&
            !string.IsNullOrWhiteSpace(motifExoneration) &&
            motifExoneration.Trim().Length >
                LongueurMaxMotifExoneration)
        {
            return $"Le motif d'exonération ne peut pas dépasser {LongueurMaxMotifExoneration} caractères.";
        }

        return null;
    }

    private static string? NormaliserMotifExoneration(
        bool estExonereeFiscalement,
        string? motif)
    {
        if (!estExonereeFiscalement ||
            string.IsNullOrWhiteSpace(motif))
        {
            return null;
        }

        return motif.Trim();
    }

    private static string NormaliserReference(string reference)
    {
        return reference.Trim().ToUpperInvariant();
    }
}

// ==================================================================
// DTO UTILISATEUR CONNECTÉ
// ==================================================================

internal sealed class UtilisateurConnecte
{
    public Guid Id { get; set; }

    public Guid SocieteId { get; set; }

    public RoleUtilisateur Role { get; set; }
}

// ==================================================================
// DTO DE CRÉATION
// ==================================================================

public class CreerUniteLocativeRequest
{
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    public decimal Superficie { get; set; }

    public int? NombrePieces { get; set; }

    public int? Etage { get; set; }

    public decimal Loyer { get; set; }

    public decimal? ChargesLocativesMensuelles { get; set; }

    public decimal? MontantCaution { get; set; }

    public StatutDisponibilite Statut { get; set; }
        = StatutDisponibilite.Disponible;

    public DateTime? DatePremiereMiseEnLocation { get; set; }

    public DateTime? DateDerniereRenovation { get; set; }

    public bool EstExonereeFiscalement { get; set; }

    public string? MotifExoneration { get; set; }

    public Guid BienImmobilierId { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

// ==================================================================
// DTO DE MODIFICATION
// ==================================================================

public class ModifierUniteLocativeRequest
{
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    public decimal Superficie { get; set; }

    public int? NombrePieces { get; set; }

    public int? Etage { get; set; }

    public decimal Loyer { get; set; }

    public decimal? ChargesLocativesMensuelles { get; set; }

    public decimal? MontantCaution { get; set; }

    public StatutDisponibilite Statut { get; set; }

    public DateTime? DatePremiereMiseEnLocation { get; set; }

    public DateTime? DateDerniereRenovation { get; set; }

    public bool EstExonereeFiscalement { get; set; }

    public string? MotifExoneration { get; set; }

    public Guid BienImmobilierId { get; set; }

    public List<string>? PhotosExistantes { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

// ==================================================================
// DTO DE MODIFICATION DU STATUT
// ==================================================================

public record ModifierStatutUniteRequest(
    StatutDisponibilite Statut);

// ==================================================================
// RÉSULTAT DE L'UPLOAD
// ==================================================================

public record ResultatUploadPhotosUnite(
    bool Succes,
    string? Message,
    List<string> Photos)
{
    public static ResultatUploadPhotosUnite SuccesResult(
        List<string> photos)
        => new(true, null, photos);

    public static ResultatUploadPhotosUnite Echec(
        string message)
        => new(false, message, []);
}
