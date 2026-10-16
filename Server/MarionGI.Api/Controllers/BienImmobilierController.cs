
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
public class BienImmobilierController : ControllerBase
{
    private readonly MarionDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const int NombreMaxPhotos = 20;
    private const long TailleMaxPhoto = 10 * 1024 * 1024;

    private static readonly HashSet<string> ExtensionsAutorisees =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

    public BienImmobilierController(
        MarionDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ============================================================
    // GET : api/BienImmobilier
    // ============================================================
    [HttpGet]
    [Authorize(Policy = "Biens.Read")]
    public async Task<IActionResult> GetBiens(
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var utilisateurId = GetUtilisateurId();

        if (utilisateurId == null)
            return Unauthorized(new { message = "Utilisateur introuvable." });

        var estLocataire = User.IsInRole("Locataire");

        var query = _context.BiensImmobiliers
            .AsNoTracking()
            .Where(b =>
                b.SocieteId == societeId.Value &&
                !b.EstSupprime);

        if (estLocataire)
        {
            query = query.Where(b =>
                b.UnitesLocatives.Any(u =>
                    !u.EstSupprime &&
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.LocataireId == utilisateurId.Value &&
                        c.Statut == StatutContrat.Actif)));
        }

        var biens = await query
            .OrderBy(b => b.Nom)
            .Select(b => new
            {
                b.Id,
                b.Reference,
                b.Nom,
                b.Type,
                b.Adresse,
                b.Ville,
                b.Quartier,
                b.Region,
                b.Departement,
                b.Arrondissement,
                b.Superficie,
                b.AnneeConstruction,
                b.NombreEtages,
                b.NumeroTitreFoncier,
                b.NumeroLot,
                b.NumeroParcelle,
                b.ReferenceCadastrale,
                b.ValeurAcquisition,
                b.DateAcquisition,
                b.ValeurEstimee,
                b.DateDerniereEvaluation,
                b.StatutFoncier,
                b.Photos,
                b.SocieteId,
                SocieteNom = b.Societe.Nom,
                NombreUnites = b.UnitesLocatives.Count(u =>
                    !u.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(biens);
    }

    // ============================================================
    // GET : api/BienImmobilier/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Biens.Read")]
    public async Task<IActionResult> GetBien(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var utilisateurId = GetUtilisateurId();

        if (utilisateurId == null)
            return Unauthorized(new { message = "Utilisateur introuvable." });

        var estLocataire = User.IsInRole("Locataire");

        var query = _context.BiensImmobiliers
            .AsNoTracking()
            .Where(b =>
                b.Id == id &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime);

        if (estLocataire)
        {
            query = query.Where(b =>
                b.UnitesLocatives.Any(u =>
                    !u.EstSupprime &&
                    u.Contrats.Any(c =>
                        !c.EstSupprime &&
                        c.LocataireId == utilisateurId.Value &&
                        c.Statut == StatutContrat.Actif)));
        }

        var bien = await query
            .Select(b => new
            {
                b.Id,
                b.Reference,
                b.Nom,
                b.Type,
                b.Adresse,
                b.Ville,
                b.Quartier,
                b.Region,
                b.Departement,
                b.Arrondissement,
                b.Superficie,
                b.AnneeConstruction,
                b.NombreEtages,
                b.NumeroTitreFoncier,
                b.NumeroLot,
                b.NumeroParcelle,
                b.ReferenceCadastrale,
                b.ValeurAcquisition,
                b.DateAcquisition,
                b.ValeurEstimee,
                b.DateDerniereEvaluation,
                b.StatutFoncier,
                b.Photos,
                b.SocieteId,

                SocieteNom = b.Societe.Nom,

                UnitesLocatives = b.UnitesLocatives
                    .Where(u =>
                        !u.EstSupprime &&
                        (
                            !estLocataire ||
                            u.Contrats.Any(c =>
                                !c.EstSupprime &&
                                c.LocataireId == utilisateurId.Value &&
                                c.Statut == StatutContrat.Actif)
                        ))
                    .Select(u => new
                    {
                        u.Id,
                        u.Reference,
                        u.Statut,
                        u.Loyer
                    })
                    .ToList(),

                NombreUnites = b.UnitesLocatives.Count(u =>
                    !u.EstSupprime &&
                    (
                        !estLocataire ||
                        u.Contrats.Any(c =>
                            !c.EstSupprime &&
                            c.LocataireId == utilisateurId.Value &&
                            c.Statut == StatutContrat.Actif)
                    ))
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (bien == null)
            return NotFound(new { message = "Bien immobilier introuvable." });

        return Ok(bien);
    }

    // ============================================================
    // POST : api/BienImmobilier
    // ============================================================
    [HttpPost]
    [Authorize(Policy = "Biens.Create")]
    public async Task<IActionResult> CreerBien(
        [FromForm] CreerBienImmobilierRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var erreur = ValiderBien(
            request.Reference,
            request.Nom,
            request.Adresse,
            request.Ville,
            request.Superficie,
            request.Type,
            request.AnneeConstruction,
            request.NombreEtages,
            request.ValeurAcquisition,
            request.ValeurEstimee,
            request.StatutFoncier);

        if (erreur != null)
            return BadRequest(new { message = erreur });

        var reference = NettoyerTexte(request.Reference);
        var nom = NettoyerTexte(request.Nom);

        var referenceExiste = await _context.BiensImmobiliers
            .AnyAsync(b =>
                b.SocieteId == societeId.Value &&
                b.Reference == reference &&
                !b.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message = "Cette référence existe déjà dans cette société."
            });
        }

        var nomExiste = await _context.BiensImmobiliers
            .AnyAsync(b =>
                b.SocieteId == societeId.Value &&
                b.Nom == nom &&
                !b.EstSupprime,
                cancellationToken);

        if (nomExiste)
        {
            return Conflict(new
            {
                message = "Ce nom existe déjà dans cette société."
            });
        }

        var resultatPhotos = await TraiterPhotos(
            request.Fichiers,
            cancellationToken);

        if (!resultatPhotos.Succes)
        {
            return BadRequest(new { message = resultatPhotos.Message });
        }

        var bien = new BienImmobilier
        {
            Reference = reference,
            Nom = nom,
            Type = request.Type,

            Adresse = NettoyerTexte(request.Adresse),
            Ville = NettoyerTexte(request.Ville),
            Quartier = NettoyerTexteNullable(request.Quartier),
            Region = NettoyerTexteNullable(request.Region),
            Departement = NettoyerTexteNullable(request.Departement),
            Arrondissement = NettoyerTexteNullable(request.Arrondissement),

            Superficie = request.Superficie,
            AnneeConstruction = request.AnneeConstruction,
            NombreEtages = request.NombreEtages,

            NumeroTitreFoncier =
                NettoyerTexteNullable(request.NumeroTitreFoncier),
            NumeroLot = NettoyerTexteNullable(request.NumeroLot),
            NumeroParcelle = NettoyerTexteNullable(request.NumeroParcelle),
            ReferenceCadastrale =
                NettoyerTexteNullable(request.ReferenceCadastrale),

            ValeurAcquisition = request.ValeurAcquisition,
            DateAcquisition = request.DateAcquisition,
            ValeurEstimee = request.ValeurEstimee,
            DateDerniereEvaluation = request.DateDerniereEvaluation,
            StatutFoncier = request.StatutFoncier,

            Photos = resultatPhotos.Photos,
            SocieteId = societeId.Value
        };

        _context.BiensImmobiliers.Add(bien);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await SupprimerFichiersPhysiques(
                resultatPhotos.Photos,
                cancellationToken);

            return Conflict(new
            {
                message =
                    "L'enregistrement a échoué. Vérifie l'unicité des références et les contraintes de la base de données."
            });
        }

        return CreatedAtAction(
            nameof(GetBien),
            new { id = bien.Id },
            new
            {
                bien.Id,
                bien.Reference,
                bien.Nom,
                bien.Type,
                bien.SocieteId
            });
    }

    // ============================================================
    // PUT : api/BienImmobilier/{id}
    // ============================================================
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Biens.Update")]
    public async Task<IActionResult> ModifierBien(
        Guid id,
        [FromForm] ModifierBienImmobilierRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var erreur = ValiderBien(
            request.Reference,
            request.Nom,
            request.Adresse,
            request.Ville,
            request.Superficie,
            request.Type,
            request.AnneeConstruction,
            request.NombreEtages,
            request.ValeurAcquisition,
            request.ValeurEstimee,
            request.StatutFoncier);

        if (erreur != null)
            return BadRequest(new { message = erreur });

        var bien = await _context.BiensImmobiliers
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime,
                cancellationToken);

        if (bien == null)
            return NotFound(new { message = "Bien immobilier introuvable." });

        var reference = NettoyerTexte(request.Reference);
        var nom = NettoyerTexte(request.Nom);

        var referenceExiste = await _context.BiensImmobiliers
            .AnyAsync(b =>
                b.Id != id &&
                b.SocieteId == societeId.Value &&
                b.Reference == reference &&
                !b.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message = "Un autre bien utilise déjà cette référence."
            });
        }

        var nomExiste = await _context.BiensImmobiliers
            .AnyAsync(b =>
                b.Id != id &&
                b.SocieteId == societeId.Value &&
                b.Nom == nom &&
                !b.EstSupprime,
                cancellationToken);

        if (nomExiste)
        {
            return Conflict(new
            {
                message = "Un autre bien utilise déjà ce nom."
            });
        }

        // Si le client ne transmet pas PhotosExistantes, on conserve
        // les photos actuelles. Si la liste est fournie, on n'accepte
        // que les chemins déjà associés à ce bien.
        var photosConservees = request.PhotosExistantes == null
            ? bien.Photos.ToList()
            : NettoyerPhotosExistantes(
                request.PhotosExistantes,
                bien.Photos);

        var nombreNouvellesPhotos = request.Fichiers?
            .Count(f => f.Length > 0) ?? 0;

        if (photosConservees.Count + nombreNouvellesPhotos >
            NombreMaxPhotos)
        {
            return BadRequest(new
            {
                message =
                    $"Un maximum de {NombreMaxPhotos} photos est autorisé par bien."
            });
        }

        var resultatPhotos = await TraiterPhotos(
            request.Fichiers,
            cancellationToken);

        if (!resultatPhotos.Succes)
        {
            return BadRequest(new { message = resultatPhotos.Message });
        }

        var photosFinales = photosConservees
            .Concat(resultatPhotos.Photos)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        bien.Reference = reference;
        bien.Nom = nom;
        bien.Type = request.Type;

        bien.Adresse = NettoyerTexte(request.Adresse);
        bien.Ville = NettoyerTexte(request.Ville);
        bien.Quartier = NettoyerTexteNullable(request.Quartier);
        bien.Region = NettoyerTexteNullable(request.Region);
        bien.Departement = NettoyerTexteNullable(request.Departement);
        bien.Arrondissement = NettoyerTexteNullable(request.Arrondissement);

        bien.Superficie = request.Superficie;
        bien.AnneeConstruction = request.AnneeConstruction;
        bien.NombreEtages = request.NombreEtages;

        bien.NumeroTitreFoncier =
            NettoyerTexteNullable(request.NumeroTitreFoncier);
        bien.NumeroLot = NettoyerTexteNullable(request.NumeroLot);
        bien.NumeroParcelle = NettoyerTexteNullable(request.NumeroParcelle);
        bien.ReferenceCadastrale =
            NettoyerTexteNullable(request.ReferenceCadastrale);

        bien.ValeurAcquisition = request.ValeurAcquisition;
        bien.DateAcquisition = request.DateAcquisition;
        bien.ValeurEstimee = request.ValeurEstimee;
        bien.DateDerniereEvaluation = request.DateDerniereEvaluation;
        bien.StatutFoncier = request.StatutFoncier;

        bien.Photos = photosFinales;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await SupprimerFichiersPhysiques(
                resultatPhotos.Photos,
                cancellationToken);

            return Conflict(new
            {
                message =
                    "La modification a échoué. Vérifie l'unicité des références et les contraintes de la base de données."
            });
        }

        return Ok(new
        {
            message = "Bien immobilier modifié avec succès.",
            bien.Id,
            bien.Reference,
            bien.Nom,
            bien.Type,
            bien.Adresse,
            bien.Ville,
            bien.Quartier,
            bien.Region,
            bien.Departement,
            bien.Arrondissement,
            bien.Superficie,
            bien.AnneeConstruction,
            bien.NombreEtages,
            bien.NumeroTitreFoncier,
            bien.NumeroLot,
            bien.NumeroParcelle,
            bien.ReferenceCadastrale,
            bien.ValeurAcquisition,
            bien.DateAcquisition,
            bien.ValeurEstimee,
            bien.DateDerniereEvaluation,
            bien.StatutFoncier,
            bien.Photos,
            bien.SocieteId
        });
    }

    // ============================================================
    // DELETE : api/BienImmobilier/{id}
    // ============================================================
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Biens.Delete")]
    public async Task<IActionResult> SupprimerBien(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var bien = await _context.BiensImmobiliers
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime,
                cancellationToken);

        if (bien == null)
            return NotFound(new { message = "Bien immobilier introuvable." });

        var contratActifExiste = await _context.Contrats
            .AnyAsync(c =>
                !c.EstSupprime &&
                c.Statut == StatutContrat.Actif &&
                c.UniteLocative.BienImmobilierId == id &&
                c.UniteLocative.BienImmobilier.SocieteId == societeId.Value,
                cancellationToken);

        if (contratActifExiste)
        {
            return Conflict(new
            {
                message =
                    "Ce bien ne peut pas être supprimé car une de ses unités possède un contrat actif."
            });
        }

        // Les unités, charges et autres données historiques sont conservées.
        // La suppression du bien est logique.
        bien.EstSupprime = true;
        bien.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Bien immobilier supprimé avec succès."
        });
    }

    // ============================================================
    // VALIDATION
    // ============================================================
    private static string? ValiderBien(
        string? reference,
        string? nom,
        string? adresse,
        string? ville,
        decimal superficie,
        TypeBien type,
        int? anneeConstruction,
        int? nombreEtages,
        decimal? valeurAcquisition,
        decimal? valeurEstimee,
        StatutFoncier? statutFoncier)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return "La référence est obligatoire.";

        if (reference.Trim().Length > 50)
            return "La référence ne doit pas dépasser 50 caractères.";

        if (string.IsNullOrWhiteSpace(nom))
            return "Le nom du bien est obligatoire.";

        if (nom.Trim().Length > 150)
            return "Le nom du bien ne doit pas dépasser 150 caractères.";

        if (string.IsNullOrWhiteSpace(adresse))
            return "L'adresse est obligatoire.";

        if (adresse.Trim().Length > 250)
            return "L'adresse ne doit pas dépasser 250 caractères.";

        if (string.IsNullOrWhiteSpace(ville))
            return "La ville est obligatoire.";

        if (ville.Trim().Length > 100)
            return "La ville ne doit pas dépasser 100 caractères.";

        if (superficie <= 0 || superficie > 1_000_000)
            return "La superficie doit être supérieure à zéro et ne pas dépasser 1 000 000.";

        if (!Enum.IsDefined(typeof(TypeBien), type))
            return "Le type de bien est invalide.";

        if (anneeConstruction.HasValue &&
            (anneeConstruction.Value < 1000 ||
             anneeConstruction.Value > DateTime.UtcNow.Year + 1))
        {
            return "L'année de construction est invalide.";
        }

        if (nombreEtages.HasValue &&
            (nombreEtages.Value < 0 || nombreEtages.Value > 500))
        {
            return "Le nombre d'étages doit être compris entre 0 et 500.";
        }

        if (valeurAcquisition.HasValue && valeurAcquisition.Value < 0)
            return "La valeur d'acquisition ne peut pas être négative.";

        if (valeurEstimee.HasValue && valeurEstimee.Value < 0)
            return "La valeur estimée ne peut pas être négative.";

        if (statutFoncier.HasValue &&
            !Enum.IsDefined(typeof(StatutFoncier), statutFoncier.Value))
        {
            return "Le statut foncier est invalide.";
        }

        return null;
    }

    // ============================================================
    // IDENTITÉ ET SOCIÉTÉ
    // ============================================================
    private Guid? GetSocieteId()
    {
        var claim = User.FindFirst("SocieteId")?.Value;

        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? GetUtilisateurId()
    {
        var claim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(claim, out var id) ? id : null;
    }

    // ============================================================
    // NETTOYAGE TEXTE
    // ============================================================
    private static string NettoyerTexte(string? valeur)
        => valeur?.Trim() ?? string.Empty;

    private static string? NettoyerTexteNullable(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    // ============================================================
    // PHOTOS : NETTOYAGE DES CHEMINS CONSERVÉS
    // ============================================================
    private static List<string> NettoyerPhotosExistantes(
        IEnumerable<string>? photosDemandees,
        IEnumerable<string> photosDuBien)
    {
        if (photosDemandees == null)
            return [];

        var photosAutorisees = new HashSet<string>(
            photosDuBien.Where(p => !string.IsNullOrWhiteSpace(p)),
            StringComparer.OrdinalIgnoreCase);

        return photosDemandees
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Where(p => photosAutorisees.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(NombreMaxPhotos)
            .ToList();
    }

    // ============================================================
    // UPLOAD DES PHOTOS
    // Valide tous les fichiers avant de les enregistrer.
    // ============================================================
    private async Task<ResultatUploadPhotos> TraiterPhotos(
        IFormFileCollection? fichiers,
        CancellationToken cancellationToken)
    {
        if (fichiers == null || fichiers.Count == 0)
            return ResultatUploadPhotos.SuccesResult([]);

        if (fichiers.Count > NombreMaxPhotos)
        {
            return ResultatUploadPhotos.Echec(
                $"Un maximum de {NombreMaxPhotos} fichiers est autorisé par requête.");
        }

        foreach (var fichier in fichiers)
        {
            if (fichier.Length == 0)
                continue;

            if (fichier.Length > TailleMaxPhoto)
            {
                return ResultatUploadPhotos.Echec(
                    $"Le fichier {Path.GetFileName(fichier.FileName)} dépasse 10 Mo.");
            }

            var extension = Path.GetExtension(fichier.FileName);

            if (!ExtensionsAutorisees.Contains(extension))
            {
                return ResultatUploadPhotos.Echec(
                    $"Le format du fichier {Path.GetFileName(fichier.FileName)} n'est pas autorisé.");
            }
        }

        var webRoot = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var dossier = Path.Combine(webRoot, "uploads", "biens");
        Directory.CreateDirectory(dossier);

        var photos = new List<string>();

        try
        {
            foreach (var fichier in fichiers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fichier.Length == 0)
                    continue;

                var extension =
                    Path.GetExtension(fichier.FileName).ToLowerInvariant();

                var nomFichier = $"{Guid.NewGuid():N}{extension}";
                var chemin = Path.Combine(dossier, nomFichier);

                await using var stream = new FileStream(
                    chemin,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

                await fichier.CopyToAsync(stream, cancellationToken);

                photos.Add($"/uploads/biens/{nomFichier}");
            }
        }
        catch
        {
            await SupprimerFichiersPhysiques(
                photos,
                CancellationToken.None);

            throw;
        }

        return ResultatUploadPhotos.SuccesResult(photos);
    }

    // ============================================================
    // SUPPRESSION PHYSIQUE DES NOUVEAUX FICHIERS EN CAS D'ÉCHEC
    // ============================================================
    private async Task SupprimerFichiersPhysiques(
        IEnumerable<string> photos,
        CancellationToken cancellationToken)
    {
        var webRoot = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var dossier = Path.GetFullPath(
            Path.Combine(webRoot, "uploads", "biens"));

        foreach (var photo in photos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nomFichier = Path.GetFileName(photo);

            if (string.IsNullOrWhiteSpace(nomFichier))
                continue;

            var chemin = Path.GetFullPath(
                Path.Combine(dossier, nomFichier));

            if (!chemin.StartsWith(
                    dossier + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (System.IO.File.Exists(chemin))
            {
                await Task.Run(
                    () => System.IO.File.Delete(chemin),
                    cancellationToken);
            }
        }
    }
}

// ============================================================
// DTO : CRÉATION
// ============================================================
public class CreerBienImmobilierRequest
{
    public string Reference { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public TypeBien Type { get; set; }

    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string? Quartier { get; set; }
    public string? Region { get; set; }
    public string? Departement { get; set; }
    public string? Arrondissement { get; set; }

    public decimal Superficie { get; set; }
    public int? AnneeConstruction { get; set; }
    public int? NombreEtages { get; set; }

    public string? NumeroTitreFoncier { get; set; }
    public string? NumeroLot { get; set; }
    public string? NumeroParcelle { get; set; }
    public string? ReferenceCadastrale { get; set; }

    public decimal? ValeurAcquisition { get; set; }
    public DateTime? DateAcquisition { get; set; }
    public decimal? ValeurEstimee { get; set; }
    public DateTime? DateDerniereEvaluation { get; set; }

    public StatutFoncier? StatutFoncier { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

// ============================================================
// DTO : MODIFICATION
// ============================================================
public class ModifierBienImmobilierRequest
{
    public string Reference { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public TypeBien Type { get; set; }

    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string? Quartier { get; set; }
    public string? Region { get; set; }
    public string? Departement { get; set; }
    public string? Arrondissement { get; set; }

    public decimal Superficie { get; set; }
    public int? AnneeConstruction { get; set; }
    public int? NombreEtages { get; set; }

    public string? NumeroTitreFoncier { get; set; }
    public string? NumeroLot { get; set; }
    public string? NumeroParcelle { get; set; }
    public string? ReferenceCadastrale { get; set; }

    public decimal? ValeurAcquisition { get; set; }
    public DateTime? DateAcquisition { get; set; }
    public decimal? ValeurEstimee { get; set; }
    public DateTime? DateDerniereEvaluation { get; set; }

    public StatutFoncier? StatutFoncier { get; set; }

    public List<string>? PhotosExistantes { get; set; }
    public IFormFileCollection? Fichiers { get; set; }
}

// ============================================================
// RÉSULTAT D'UPLOAD
// ============================================================
public record ResultatUploadPhotos(
    bool Succes,
    string? Message,
    List<string> Photos)
{
    public static ResultatUploadPhotos SuccesResult(List<string> photos)
        => new(true, null, photos);

    public static ResultatUploadPhotos Echec(string message)
        => new(false, message, []);
}
