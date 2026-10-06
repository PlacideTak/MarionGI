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
    public async Task<IActionResult> GetBiens(
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var biens = await _context.BiensImmobiliers
            .AsNoTracking()
            .Where(b =>
                b.SocieteId == societeId.Value &&
                !b.EstSupprime)
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
                b.Superficie,
                b.Photos,
                b.SocieteId,

                SocieteNom = b.Societe.Nom,

                NombreUnites = b.UnitesLocatives
                    .Count(u => !u.EstSupprime)
            })
            .ToListAsync(cancellationToken);

        return Ok(biens);
    }

    // ============================================================
    // GET : api/BienImmobilier/{id}
    // ============================================================
    // ============================================================
    // GET : api/BienImmobilier/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBien(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new
            {
                message = "Société introuvable."
            });

        var bien = await _context.BiensImmobiliers
            .AsNoTracking()
            .Where(b =>
                b.Id == id &&
                b.SocieteId == societeId.Value &&
                !b.EstSupprime)
            .Select(b => new
            {
                b.Id,
                b.Reference,
                b.Nom,
                b.Type,
                b.Adresse,
                b.Ville,
                b.Quartier,
                b.Superficie,
                b.Photos,
                b.SocieteId,

                SocieteNom = b.Societe.Nom,

                // IMPORTANT :
                // Le nom doit correspondre au BienDetailDto Angular.
                UnitesLocatives = b.UnitesLocatives
                    .Where(u => !u.EstSupprime)
                    .Select(u => new
                    {
                        u.Id,
                        u.Reference,
                        u.Statut,
                        u.Loyer
                    })
                    .ToList(),

                // Nombre calculé directement côté serveur.
                NombreUnites = b.UnitesLocatives
                    .Count(u => !u.EstSupprime)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (bien == null)
        {
            return NotFound(new
            {
                message = "Bien immobilier introuvable."
            });
        }

        return Ok(bien);
    }

    // ============================================================
    // POST : api/BienImmobilier
    // ============================================================
    [HttpPost]
    public async Task<IActionResult> CreerBien(
        [FromForm] CreerBienImmobilierRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------
        var erreurValidation = ValiderBien(
            request.Reference,
            request.Nom,
            request.Adresse,
            request.Ville,
            request.Superficie,
            request.Type);

        if (erreurValidation != null)
            return BadRequest(new { message = erreurValidation });

        // --------------------------------------------------------
        // Nettoyage des données
        // --------------------------------------------------------
        var reference = NettoyerTexte(request.Reference);
        var nom = NettoyerTexte(request.Nom);
        var adresse = NettoyerTexte(request.Adresse);
        var ville = NettoyerTexte(request.Ville);
        var quartier = NettoyerTexte(request.Quartier);

        // --------------------------------------------------------
        // Unicité de la référence
        // --------------------------------------------------------
        var referenceExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(
                b =>
                    b.SocieteId == societeId.Value &&
                    b.Reference == reference &&
                    !b.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message =
                    "Un bien immobilier avec cette référence existe déjà dans cette société."
            });
        }

        // --------------------------------------------------------
        // Unicité du NOM
        // --------------------------------------------------------
        var nomExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(
                b =>
                    b.SocieteId == societeId.Value &&
                    b.Nom == nom &&
                    !b.EstSupprime,
                cancellationToken);

        if (nomExiste)
        {
            return Conflict(new
            {
                message =
                    "Un bien immobilier avec ce nom existe déjà dans cette société."
            });
        }

        // --------------------------------------------------------
        // Upload des photos
        // --------------------------------------------------------
        var resultatPhotos = await TraiterPhotos(
            request.Fichiers,
            cancellationToken);

        if (!resultatPhotos.Succes)
        {
            return BadRequest(new
            {
                message = resultatPhotos.Message
            });
        }

        // --------------------------------------------------------
        // Création
        // --------------------------------------------------------
        var bien = new BienImmobilier
        {
            Id = Guid.NewGuid(),

            Reference = reference,
            Nom = nom,

            Type = request.Type,

            Adresse = adresse,
            Ville = ville,
            Quartier = quartier,

            Superficie = request.Superficie,

            Photos = resultatPhotos.Photos,

            SocieteId = societeId.Value,

            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        _context.BiensImmobiliers.Add(bien);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Protection supplémentaire contre une concurrence :
            // deux utilisateurs peuvent créer le même nom simultanément.
            return Conflict(new
            {
                message =
                    "Un bien immobilier avec cette référence ou ce nom existe déjà dans cette société."
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
                bien.Adresse,
                bien.Ville,
                bien.Quartier,
                bien.Superficie,
                bien.Photos,
                bien.SocieteId
            });
    }

    // ============================================================
    // PUT : api/BienImmobilier/{id}
    // ============================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> ModifierBien(
        Guid id,
        [FromForm] ModifierBienImmobilierRequest request,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------
        var erreurValidation = ValiderBien(
            request.Reference,
            request.Nom,
            request.Adresse,
            request.Ville,
            request.Superficie,
            request.Type);

        if (erreurValidation != null)
            return BadRequest(new { message = erreurValidation });

        // --------------------------------------------------------
        // Recherche du bien
        // --------------------------------------------------------
        var bien = await _context.BiensImmobiliers
            .FirstOrDefaultAsync(
                b =>
                    b.Id == id &&
                    b.SocieteId == societeId.Value &&
                    !b.EstSupprime,
                cancellationToken);

        if (bien == null)
        {
            return NotFound(new
            {
                message = "Bien immobilier introuvable."
            });
        }

        // --------------------------------------------------------
        // Nettoyage
        // --------------------------------------------------------
        var reference = NettoyerTexte(request.Reference);
        var nom = NettoyerTexte(request.Nom);
        var adresse = NettoyerTexte(request.Adresse);
        var ville = NettoyerTexte(request.Ville);
        var quartier = NettoyerTexte(request.Quartier);

        // --------------------------------------------------------
        // Unicité de la référence
        // --------------------------------------------------------
        var referenceExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(
                b =>
                    b.Id != id &&
                    b.SocieteId == societeId.Value &&
                    b.Reference == reference &&
                    !b.EstSupprime,
                cancellationToken);

        if (referenceExiste)
        {
            return Conflict(new
            {
                message =
                    "Un autre bien immobilier utilise déjà cette référence."
            });
        }

        // --------------------------------------------------------
        // Unicité du NOM
        // --------------------------------------------------------
        var nomExiste = await _context.BiensImmobiliers
            .AsNoTracking()
            .AnyAsync(
                b =>
                    b.Id != id &&
                    b.SocieteId == societeId.Value &&
                    b.Nom == nom &&
                    !b.EstSupprime,
                cancellationToken);

        if (nomExiste)
        {
            return Conflict(new
            {
                message =
                    "Un autre bien immobilier utilise déjà ce nom."
            });
        }

        // --------------------------------------------------------
        // Photos
        // --------------------------------------------------------
        var photosExistantes =
            NettoyerPhotos(request.PhotosExistantes);

        var nouvellesPhotos = await TraiterPhotos(
            request.Fichiers,
            cancellationToken);

        if (!nouvellesPhotos.Succes)
        {
            return BadRequest(new
            {
                message = nouvellesPhotos.Message
            });
        }

        var photosFinales = photosExistantes
            .Concat(nouvellesPhotos.Photos)
            .Distinct()
            .Take(20)
            .ToList();

        // --------------------------------------------------------
        // Mise à jour
        // --------------------------------------------------------
        bien.Reference = reference;
        bien.Nom = nom;

        bien.Type = request.Type;

        bien.Adresse = adresse;
        bien.Ville = ville;
        bien.Quartier = quartier;

        bien.Superficie = request.Superficie;

        bien.Photos = photosFinales;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message =
                    "Un bien immobilier avec cette référence ou ce nom existe déjà dans cette société."
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
            bien.Superficie,
            bien.Photos,
            bien.SocieteId
        });
    }

    // ============================================================
    // DELETE : api/BienImmobilier/{id}
    // ============================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SupprimerBien(
        Guid id,
        CancellationToken cancellationToken)
    {
        var societeId = GetSocieteId();

        if (societeId == null)
            return Unauthorized(new { message = "Société introuvable." });

        var bien = await _context.BiensImmobiliers
            .FirstOrDefaultAsync(
                b =>
                    b.Id == id &&
                    b.SocieteId == societeId.Value &&
                    !b.EstSupprime,
                cancellationToken);

        if (bien == null)
        {
            return NotFound(new
            {
                message = "Bien immobilier introuvable."
            });
        }

        // --------------------------------------------------------
        // Vérifier les contrats actifs
        // --------------------------------------------------------
        var contratActifExiste =
            await _context.Contrats
                .AnyAsync(
                    c =>
                        c.UniteLocative.BienImmobilierId == id &&
                        c.Statut == StatutContrat.Actif,
                    cancellationToken);

        if (contratActifExiste)
        {
            return Conflict(new
            {
                message =
                    "Ce bien ne peut pas être supprimé car une de ses unités possède un contrat actif."
            });
        }

        // --------------------------------------------------------
        // Suppression logique
        // --------------------------------------------------------
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
        TypeBien type)
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

        if (superficie <= 0)
            return "La superficie doit être supérieure à zéro.";

        if (superficie > 1_000_000)
            return "La superficie ne peut pas dépasser 1 000 000.";

        if (!Enum.IsDefined(typeof(TypeBien), type))
            return "Le type de bien est invalide.";

        return null;
    }

    // ============================================================
    // SOCIETE
    // ============================================================
    private Guid? GetSocieteId()
    {
        var claim = User.FindFirst("SocieteId")?.Value;

        if (Guid.TryParse(claim, out var societeId))
            return societeId;

        return null;
    }

    // ============================================================
    // NETTOYAGE TEXTE
    // ============================================================
    private static string NettoyerTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? string.Empty
            : valeur.Trim();
    }

    // ============================================================
    // PHOTOS
    // ============================================================
    private static List<string> NettoyerPhotos(
        IEnumerable<string>? photos)
    {
        if (photos == null)
            return [];

        return photos
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct()
            .Take(20)
            .ToList();
    }

    private async Task<ResultatUploadPhotos> TraiterPhotos(
        IFormFileCollection? fichiers,
        CancellationToken cancellationToken)
    {
        if (fichiers == null || fichiers.Count == 0)
            return ResultatUploadPhotos.SuccesResult([]);

        if (fichiers.Count > 20)
        {
            return ResultatUploadPhotos.Echec(
                "Un maximum de 20 photos est autorisé.");
        }

        var extensionsAutorisees =
            new[] { ".jpg", ".jpeg", ".png", ".webp" };

        const long tailleMax = 10 * 1024 * 1024;

        var dossier = Path.Combine(
            _environment.WebRootPath ?? "wwwroot",
            "uploads",
            "biens");

        Directory.CreateDirectory(dossier);

        var photos = new List<string>();

        foreach (var fichier in fichiers)
        {
            if (fichier.Length == 0)
                continue;

            if (fichier.Length > tailleMax)
            {
                return ResultatUploadPhotos.Echec(
                    $"Le fichier {fichier.FileName} dépasse la taille maximale de 10 Mo.");
            }

            var extension =
                Path.GetExtension(fichier.FileName)
                    .ToLowerInvariant();

            if (!extensionsAutorisees.Contains(extension))
            {
                return ResultatUploadPhotos.Echec(
                    $"Le format {extension} n'est pas autorisé.");
            }

            var nomFichier =
                $"{Guid.NewGuid():N}{extension}";

            var chemin = Path.Combine(
                dossier,
                nomFichier);

            await using var stream =
                new FileStream(
                    chemin,
                    FileMode.Create);

            await fichier.CopyToAsync(
                stream,
                cancellationToken);

            photos.Add(
                $"/uploads/biens/{nomFichier}");
        }

        return ResultatUploadPhotos.SuccesResult(photos);
    }
}

// ============================================================
// DTOs
// ============================================================

public class CreerBienImmobilierRequest
{
    public string Reference { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public TypeBien Type { get; set; }

    public string Adresse { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string? Quartier { get; set; }

    public decimal Superficie { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

public class ModifierBienImmobilierRequest
{
    public string Reference { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public TypeBien Type { get; set; }

    public string Adresse { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string? Quartier { get; set; }

    public decimal Superficie { get; set; }

    public List<string>? PhotosExistantes { get; set; }

    public IFormFileCollection? Fichiers { get; set; }
}

public record ResultatUploadPhotos(
    bool Succes,
    string? Message,
    List<string> Photos)
{
    public static ResultatUploadPhotos SuccesResult(
        List<string> photos)
        => new(true, null, photos);

    public static ResultatUploadPhotos Echec(
        string message)
        => new(false, message, []);
}