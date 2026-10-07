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
public class UnitesLocativesController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const int NombreMaxPhotos = 20;
    private const long TailleMaxPhoto = 10 * 1024 * 1024;

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
    //
    // ADMINISTRATEUR / GESTIONNAIRE / AGENT
    //     -> toutes les unités non supprimées de leur société
    //
    // LOCATAIRE
    //     -> uniquement les unités pour lesquelles il possède
    //        au moins un contrat non supprimé
    // ============================================================

    [HttpGet]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
                nameof(RoleUtilisateur.Gestionnaire) + "," +
                nameof(RoleUtilisateur.Agent) + "," +
                nameof(RoleUtilisateur.Locataire))]
    public async Task<IActionResult> GetUnites(
        CancellationToken cancellationToken)
    {
        var utilisateur = await GetUtilisateurConnecteAsync(
            cancellationToken);

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

        // ========================================================
        // ADMINISTRATEUR / GESTIONNAIRE / AGENT
        // ========================================================

        if (EstGestionnaireOuAgent(utilisateur.Role))
        {
            query = query.Where(u =>
                u.BienImmobilier.SocieteId ==
                utilisateur.SocieteId);
        }

        // ========================================================
        // LOCATAIRE
        // ========================================================

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
                u.Loyer,
                u.Statut,
                u.Photos,

                u.BienImmobilierId,

                BienReference =
                    u.BienImmobilier.Reference,

                BienAdresse =
                    u.BienImmobilier.Adresse,

                BienVille =
                    u.BienImmobilier.Ville,

                BienQuartier =
                    u.BienImmobilier.Quartier,

                BienNom =
                    u.BienImmobilier.Nom,

                SocieteId =
                    u.BienImmobilier.SocieteId,

                SocieteNom =
                    u.BienImmobilier.Societe.Nom,

                NombreContrats =
                    u.Contrats.Count(c =>
                        !c.EstSupprime &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
                        )),

                AContratActif =
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.Statut == StatutContrat.Actif &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
                        )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d =>
                        !d.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(unites);
    }

    // ============================================================
    // GET : api/UnitesLocatives/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        var utilisateur = await GetUtilisateurConnecteAsync(
            cancellationToken);

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

        // ========================================================
        // ADMINISTRATEUR / GESTIONNAIRE / AGENT
        // ========================================================

        if (EstGestionnaireOuAgent(utilisateur.Role))
        {
            query = query.Where(u =>
                u.BienImmobilier.SocieteId ==
                utilisateur.SocieteId);
        }

        // ========================================================
        // LOCATAIRE
        // ========================================================

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
                u.Loyer,
                u.Statut,
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
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
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

                AContratActif =
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.Statut == StatutContrat.Actif &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
                        )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d =>
                        !d.EstSupprime)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (unite == null)
        {
            return NotFound(
                "Unité locative introuvable.");
        }

        return Ok(unite);
    }

    // ============================================================
    // GET : api/UnitesLocatives/bien/{bienImmobilierId}
    // ============================================================

    [HttpGet("bien/{bienImmobilierId:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        var utilisateur = await GetUtilisateurConnecteAsync(
            cancellationToken);

        if (utilisateur == null)
        {
            return Unauthorized(
                "Utilisateur introuvable ou inactif.");
        }

        // ========================================================
        // VERIFICATION DU BIEN
        // ========================================================

        var bienExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(
                b =>
                    b.Id == bienImmobilierId &&
                    !b.EstSupprime &&
                    b.SocieteId == utilisateur.SocieteId,
                cancellationToken);

        if (!bienExiste)
        {
            return NotFound(
                "Bien immobilier introuvable.");
        }

        IQueryable<UniteLocative> query =
            _context.UnitesLocatives
                .AsNoTracking()
                .Where(u =>
                    u.BienImmobilierId == bienImmobilierId &&
                    !u.EstSupprime &&
                    !u.BienImmobilier.EstSupprime);

        // ========================================================
        // ADMINISTRATEUR / GESTIONNAIRE / AGENT
        // ========================================================

        if (EstGestionnaireOuAgent(utilisateur.Role))
        {
            // Le bien a déjà été vérifié comme appartenant
            // à la société de l'utilisateur.
        }

        // ========================================================
        // LOCATAIRE
        // ========================================================

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
            .OrderBy(u => u.Reference)
            .Select(u => new
            {
                u.Id,
                u.Reference,
                u.Type,
                u.Superficie,
                u.Loyer,
                u.Statut,
                u.Photos,

                u.BienImmobilierId,

                NombreContrats =
                    u.Contrats.Count(c =>
                        !c.EstSupprime &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
                        )),

                AContratActif =
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.Statut == StatutContrat.Actif &&
                        (
                            utilisateur.Role !=
                                RoleUtilisateur.Locataire
                            ||
                            c.LocataireId ==
                                utilisateur.Id
                        )),

                NombreDemandesVisite =
                    u.DemandesVisite.Count(d =>
                        !d.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(unites);
    }

    // ============================================================
    // POST
    // ============================================================

    [HttpPost]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var validation = ValiderUnite(
            request.Reference,
            request.Type,
            request.Superficie,
            request.Loyer,
            request.Statut);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        if (request.BienImmobilierId == Guid.Empty)
        {
            return BadRequest(
                "Le bien immobilier est obligatoire.");
        }

        var bien = await _context.BiensImmobiliers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b =>
                    b.Id == request.BienImmobilierId &&
                    b.SocieteId == societeId.Value &&
                    !b.EstSupprime,
                cancellationToken);

        if (bien == null)
        {
            return NotFound(
                "Le bien immobilier est introuvable ou n'appartient pas à votre société.");
        }

        var reference =
            NormaliserReference(request.Reference);

        var referenceExiste =
            await _context.UnitesLocatives
                .AnyAsync(
                    u =>
                        u.BienImmobilierId ==
                            request.BienImmobilierId &&
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
            Loyer = request.Loyer,
            Statut = request.Statut,
            BienImmobilierId = request.BienImmobilierId,
            Photos = [],
            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        var fichiersCrees = new List<string>();

        try
        {
            if (request.Fichiers != null &&
                request.Fichiers.Count > 0)
            {
                var resultatUpload =
                    await EnregistrerPhotosAsync(
                        request.Fichiers,
                        unite.Photos,
                        fichiersCrees,
                        cancellationToken);

                if (!resultatUpload.Succes)
                {
                    SupprimerFichiersPhysiques(
                        fichiersCrees);

                    return BadRequest(
                        resultatUpload.Message);
                }

                unite.Photos =
                    resultatUpload.Photos;
            }

            _context.UnitesLocatives.Add(unite);

            await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            SupprimerFichiersPhysiques(
                fichiersCrees);

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
                unite.Loyer,
                unite.Statut,
                unite.BienImmobilierId,
                unite.Photos,
                AContratActif = false
            });
    }

    // ============================================================
    // PUT
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var unite =
            await _context.UnitesLocatives
                .FirstOrDefaultAsync(
                    u =>
                        u.Id == id &&
                        !u.EstSupprime &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);

        if (unite == null)
        {
            return NotFound(
                "Unité locative introuvable.");
        }

        var validation = ValiderUnite(
            request.Reference,
            request.Type,
            request.Superficie,
            request.Loyer,
            request.Statut);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        if (request.BienImmobilierId == Guid.Empty)
        {
            return BadRequest(
                "Le bien immobilier est obligatoire.");
        }

        var bienExiste =
            await _context.BiensImmobiliers
                .AsNoTracking()
                .AnyAsync(
                    b =>
                        b.Id == request.BienImmobilierId &&
                        b.SocieteId == societeId.Value &&
                        !b.EstSupprime,
                    cancellationToken);

        if (!bienExiste)
        {
            return NotFound(
                "Le bien immobilier est introuvable ou n'appartient pas à votre société.");
        }

        // ========================================================
        // CONTRAT ACTIF
        //
        // Une unité louée ne doit pas être déplacée vers
        // un autre bien.
        // ========================================================

        var contratActifExiste =
            await _context.Contrats
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.UniteLocativeId == id &&
                        !c.EstSupprime &&
                        c.Statut == StatutContrat.Actif,
                    cancellationToken);

        if (contratActifExiste &&
            unite.BienImmobilierId !=
                request.BienImmobilierId)
        {
            return Conflict(
                "Cette unité possède un contrat actif et ne peut pas être déplacée vers un autre bien immobilier.");
        }

        // ========================================================
        // REFERENCE UNIQUE DANS LE BIEN
        // ========================================================

        var reference =
            NormaliserReference(request.Reference);

        var referenceExiste =
            await _context.UnitesLocatives
                .AnyAsync(
                    u =>
                        u.Id != id &&
                        u.BienImmobilierId ==
                            request.BienImmobilierId &&
                        u.Reference == reference &&
                        !u.EstSupprime,
                    cancellationToken);

        if (referenceExiste)
        {
            return Conflict(
                "Une autre unité utilise déjà cette référence dans ce bien.");
        }

        // ========================================================
        // COHERENCE CONTRAT ACTIF / STATUT UNITE
        // ========================================================

        if (contratActifExiste &&
            request.Statut != StatutDisponibilite.Loue)
        {
            return Conflict(
                "Une unité possédant un contrat actif doit conserver le statut Louée.");
        }

        var anciennesPhotos =
            unite.Photos?.ToList() ?? [];

        var photosFinales =
            FiltrerPhotosUnite(
                NettoyerPhotos(
                    request.PhotosExistantes));

        var fichiersCrees =
            new List<string>();

        try
        {
            if (request.Fichiers != null &&
                request.Fichiers.Count > 0)
            {
                var resultatUpload =
                    await EnregistrerPhotosAsync(
                        request.Fichiers,
                        photosFinales,
                        fichiersCrees,
                        cancellationToken);

                if (!resultatUpload.Succes)
                {
                    SupprimerFichiersPhysiques(
                        fichiersCrees);

                    return BadRequest(
                        resultatUpload.Message);
                }

                photosFinales =
                    resultatUpload.Photos;
            }

            unite.Reference =
                reference;

            unite.Type =
                request.Type;

            unite.Superficie =
                request.Superficie;

            unite.Loyer =
                request.Loyer;

            unite.Statut =
                request.Statut;

            unite.BienImmobilierId =
                request.BienImmobilierId;

            unite.Photos =
                photosFinales;

            await _context.SaveChangesAsync(
                cancellationToken);

            var photosSupprimees =
                anciennesPhotos
                    .Except(
                        photosFinales,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            SupprimerFichiersPhysiques(
                photosSupprimees);
        }
        catch
        {
            SupprimerFichiersPhysiques(
                fichiersCrees);

            throw;
        }

        return NoContent();
    }

    // ============================================================
    // PATCH : STATUT
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        if (request == null)
        {
            return BadRequest(
                "Les données du statut sont obligatoires.");
        }

        if (!Enum.IsDefined(request.Statut))
        {
            return BadRequest(
                "Le statut de l'unité est invalide.");
        }

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var unite =
            await _context.UnitesLocatives
                .FirstOrDefaultAsync(
                    u =>
                        u.Id == id &&
                        !u.EstSupprime &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);

        if (unite == null)
        {
            return NotFound(
                "Unité locative introuvable.");
        }

        // ========================================================
        // CONTRAT ACTIF
        // ========================================================

        var contratActifExiste =
            await _context.Contrats
                .AsNoTracking()
                .AnyAsync(
                    c =>
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

        unite.Statut =
            request.Statut;

        await _context.SaveChangesAsync(
            cancellationToken);

        return Ok(new
        {
            Message =
                "Statut de l'unité modifié avec succès.",

            unite.Id,
            unite.Statut
        });
    }

    // ============================================================
    // DELETE
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(
        Roles = nameof(RoleUtilisateur.Administrateur) + "," +
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

        var societeId =
            await GetSocieteIdUtilisateurConnecteAsync(
                cancellationToken);

        if (societeId == null)
        {
            return Unauthorized(
                "Impossible de déterminer la société de l'utilisateur connecté.");
        }

        var unite =
            await _context.UnitesLocatives
                .FirstOrDefaultAsync(
                    u =>
                        u.Id == id &&
                        !u.EstSupprime &&
                        !u.BienImmobilier.EstSupprime &&
                        u.BienImmobilier.SocieteId ==
                            societeId.Value,
                    cancellationToken);

        if (unite == null)
        {
            return NotFound(
                "Unité locative introuvable.");
        }

        var contratActifExiste =
            await _context.Contrats
                .AsNoTracking()
                .AnyAsync(
                    c =>
                        c.UniteLocativeId == id &&
                        !c.EstSupprime &&
                        c.Statut == StatutContrat.Actif,
                    cancellationToken);

        if (contratActifExiste)
        {
            return Conflict(
                "Cette unité ne peut pas être supprimée car elle possède un contrat actif.");
        }

        var photos =
            unite.Photos?.ToList() ?? [];

        unite.EstSupprime = true;
        unite.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(
            cancellationToken);

        SupprimerFichiersPhysiques(photos);

        return Ok(new
        {
            Message =
                "Unité locative supprimée avec succès."
        });
    }

    // ============================================================
    // UTILISATEUR CONNECTE
    // ============================================================

    private async Task<UtilisateurConnecte?> GetUtilisateurConnecteAsync(
        CancellationToken cancellationToken)
    {
        var currentUserId =
            GetCurrentUserId();

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
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    private async Task<Guid?>
        GetSocieteIdUtilisateurConnecteAsync(
            CancellationToken cancellationToken)
    {
        var utilisateur =
            await GetUtilisateurConnecteAsync(
                cancellationToken);

        if (utilisateur == null ||
            utilisateur.SocieteId == Guid.Empty)
        {
            return null;
        }

        return utilisateur.SocieteId;
    }

    // ============================================================
    // UTILISATEUR CONNECTE
    // ============================================================

    private Guid? GetCurrentUserId()
    {
        var claims = new[]
        {
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),

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

    // ============================================================
    // ROLES
    // ============================================================

    private static bool EstGestionnaireOuAgent(
        RoleUtilisateur role)
    {
        return role == RoleUtilisateur.Administrateur ||
               role == RoleUtilisateur.Gestionnaire ||
               role == RoleUtilisateur.Agent;
    }

    // ============================================================
    // UPLOAD
    // ============================================================

    private async Task<ResultatUploadPhotosUnite>
        EnregistrerPhotosAsync(
            IFormFileCollection fichiers,
            List<string> photosExistantes,
            List<string> fichiersCrees,
            CancellationToken cancellationToken)
    {
        var photos =
            new List<string>(photosExistantes);

        if (photos.Count + fichiers.Count >
            NombreMaxPhotos)
        {
            return ResultatUploadPhotosUnite.Echec(
                $"Une unité locative ne peut pas contenir plus de {NombreMaxPhotos} photos.");
        }

        var dossierUploads =
            GetDossierUploads();

        Directory.CreateDirectory(
            dossierUploads);

        foreach (var fichier in fichiers)
        {
            if (fichier == null ||
                fichier.Length <= 0)
            {
                continue;
            }

            if (fichier.Length > TailleMaxPhoto)
            {
                return ResultatUploadPhotosUnite.Echec(
                    $"Le fichier '{fichier.FileName}' dépasse la taille maximale de 10 Mo.");
            }

            var extension =
                Path.GetExtension(
                    fichier.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !ExtensionsAutorisees.Contains(extension))
            {
                return ResultatUploadPhotosUnite.Echec(
                    $"Le format du fichier '{fichier.FileName}' n'est pas autorisé. Formats acceptés : JPG, JPEG, PNG et WEBP.");
            }

            if (photos.Count >= NombreMaxPhotos)
            {
                return ResultatUploadPhotosUnite.Echec(
                    $"Une unité locative ne peut pas contenir plus de {NombreMaxPhotos} photos.");
            }

            var nomFichier =
                $"{Guid.NewGuid():N}" +
                extension.ToLowerInvariant();

            var cheminComplet =
                Path.Combine(
                    dossierUploads,
                    nomFichier);

            await using var stream =
                new FileStream(
                    cheminComplet,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

            await fichier.CopyToAsync(
                stream,
                cancellationToken);

            var cheminRelatif =
                $"/uploads/unites/{nomFichier}";

            photos.Add(cheminRelatif);
            fichiersCrees.Add(cheminRelatif);
        }

        return ResultatUploadPhotosUnite.SuccesResult(
            photos);
    }

    // ============================================================
    // DOSSIER UPLOAD
    // ============================================================

    private string GetDossierUploads()
    {
        var webRoot =
            _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");
        }

        return Path.Combine(
            webRoot,
            "uploads",
            "unites");
    }

    // ============================================================
    // SUPPRESSION PHYSIQUE
    // ============================================================

    private void SupprimerFichiersPhysiques(
        IEnumerable<string> chemins)
    {
        foreach (var chemin in chemins)
        {
            try
            {
                var cheminComplet =
                    GetCheminPhysique(chemin);

                if (cheminComplet == null)
                {
                    continue;
                }

                if (System.IO.File.Exists(
                        cheminComplet))
                {
                    System.IO.File.Delete(
                        cheminComplet);
                }
            }
            catch
            {
                // Ne pas faire échouer la suppression logique
                // à cause d'une erreur physique de fichier.
            }
        }
    }

    // ============================================================
    // CHEMIN PHYSIQUE
    // ============================================================

    private string? GetCheminPhysique(
        string cheminRelatif)
    {
        if (string.IsNullOrWhiteSpace(
                cheminRelatif))
        {
            return null;
        }

        const string prefix =
            "/uploads/unites/";

        if (!cheminRelatif.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var nomFichier =
            cheminRelatif[prefix.Length..];

        if (string.IsNullOrWhiteSpace(nomFichier) ||
            nomFichier.Contains('/') ||
            nomFichier.Contains('\\') ||
            nomFichier.Contains(".."))
        {
            return null;
        }

        return Path.Combine(
            GetDossierUploads(),
            nomFichier);
    }

    // ============================================================
    // FILTRER PHOTOS
    // ============================================================

    private static List<string> FiltrerPhotosUnite(
        IEnumerable<string> photos)
    {
        const string prefix =
            "/uploads/unites/";

        return photos
            .Where(p =>
                !string.IsNullOrWhiteSpace(p))
            .Select(p =>
                p.Trim())
            .Where(p =>
                p.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            .Where(p =>
                !p.Contains(".."))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Take(NombreMaxPhotos)
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
            .Where(p =>
                !string.IsNullOrWhiteSpace(p))
            .Select(p =>
                p.Trim())
            .Where(p =>
                p.Length <= 500)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Take(NombreMaxPhotos)
            .ToList();
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static string? ValiderUnite(
        string? reference,
        TypeUniteLocative type,
        decimal superficie,
        decimal loyer,
        StatutDisponibilite statut)
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

        if (superficie <= 0)
        {
            return "La superficie doit être supérieure à zéro.";
        }

        if (superficie > 1_000_000)
        {
            return "La superficie fournie est trop élevée.";
        }

        if (loyer < 0)
        {
            return "Le loyer ne peut pas être négatif.";
        }

        if (!Enum.IsDefined(statut))
        {
            return "Le statut de l'unité est invalide.";
        }

        return null;
    }

    // ============================================================
    // NORMALISATION
    // ============================================================

    private static string NormaliserReference(
        string reference)
    {
        return reference
            .Trim()
            .ToUpperInvariant();
    }
}

// ==================================================================
// DTO UTILISATEUR CONNECTE
// ==================================================================

internal sealed class UtilisateurConnecte
{
    public Guid Id { get; set; }

    public Guid SocieteId { get; set; }

    public RoleUtilisateur Role { get; set; }
}

// ==================================================================
// DTO CREATION
// ==================================================================

public class CreerUniteLocativeRequest
{
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    public decimal Superficie { get; set; }

    public decimal Loyer { get; set; }

    public StatutDisponibilite Statut { get; set; }

    public Guid BienImmobilierId { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

// ==================================================================
// DTO MODIFICATION
// ==================================================================

public class ModifierUniteLocativeRequest
{
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    public decimal Superficie { get; set; }

    public decimal Loyer { get; set; }

    public StatutDisponibilite Statut { get; set; }

    public Guid BienImmobilierId { get; set; }

    public List<string>? PhotosExistantes { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

// ==================================================================
// DTO STATUT
// ==================================================================

public record ModifierStatutUniteRequest(
    StatutDisponibilite Statut);

// ==================================================================
// RESULTAT UPLOAD
// ==================================================================

public record ResultatUploadPhotosUnite(
    bool Succes,
    string? Message,
    List<string> Photos)
{
    public static ResultatUploadPhotosUnite SuccesResult(
        List<string> photos)
        => new(
            true,
            null,
            photos);

    public static ResultatUploadPhotosUnite Echec(
        string message)
        => new(
            false,
            message,
            []);
}