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
public class BiensController : ControllerBase
{
    private readonly MarionDbContext _context;

    public BiensController(MarionDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBiens([FromQuery] StatutBien? statut, [FromQuery] TypeBien? type, [FromQuery] string? ville)
    {
        // Filtre automatique pour exclure les entités supprimées logiquement
        var query = _context.Biens
            .Where(b => !b.EstSupprime)
            .Include(b => b.Proprietaire)
            .AsQueryable();

        if (statut.HasValue) query = query.Where(b => b.Statut == statut.Value);
        if (type.HasValue) query = query.Where(b => b.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(ville)) query = query.Where(b => b.Ville.Contains(ville));

        var biens = await query.ToListAsync();
        return Ok(biens);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var bien = await _context.Biens
            .Include(b => b.Proprietaire)
            .Include(b => b.Contrats.Where(c => !c.EstSupprime))
                .ThenInclude(c => c.Locataire)
            .Include(b => b.Contrats.Where(c => !c.EstSupprime))
                .ThenInclude(c => c.Paiements)
            .FirstOrDefaultAsync(b => b.Id == id && !b.EstSupprime);

        if (bien == null) return NotFound("Bien non trouvé ou archivé.");

        var contratActif = bien.Contrats.FirstOrDefault(c => c.Statut == StatutContrat.Actif) ?? bien.Contrats.FirstOrDefault();

        var resultatDto = new
        {
            bien.Id,
            bien.Reference,
            bien.Type,
            bien.Adresse,
            bien.Ville,
            bien.Quartier,
            bien.Superficie,
            bien.Loyer,
            Caution = contratActif?.MontantCaution ?? 0,
            bien.Statut,
            bien.ProprietaireId,
            ProprietaireNom = bien.Proprietaire != null ? $"{bien.Proprietaire.Nom} {bien.Proprietaire.Prenom}" : "Non assigné",
            bien.Photos,
            LocataireActuel = contratActif?.Locataire != null ? new
            {
                ContratId = contratActif.Id,
                Nom = $"{contratActif.Locataire.Nom} {contratActif.Locataire.Prenom}",
                Telephone = contratActif.Locataire.Telephone,
                DebutBail = contratActif.DateDebut,
                FinBail = contratActif.DateFin
            } : null,
            HistoriquePaiements = bien.Contrats.SelectMany(c => c.Paiements)
                .Where(p => !p.EstSupprime)
                .Select(p => new {
                    Id = p.Id,
                    Date = p.DatePaiement,
                    Montant = p.Montant,
                    Mode = p.ModePaiement.ToString()
                }).ToList()
        };

        return Ok(resultatDto);
    }

    [HttpPost]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Creer([FromForm] Bien dto, [FromForm] IFormFileCollection fichiers)
    {
        // 1. Vérifier si la référence existe déjà (en ignorant la casse ou les espaces si besoin)
        if (!string.IsNullOrWhiteSpace(dto.Reference))
        {
            bool referenceExiste = await _context.Biens
                .AnyAsync(b => b.Reference == dto.Reference && !b.EstSupprime);

            if (referenceExiste)
            {
                return Conflict(new { message = "Un bien possède déjà cette référence." });
            }
        }

        var bien = new Bien
        {
            Id = Guid.NewGuid(),
            Reference = dto.Reference,
            Type = dto.Type,
            ProprietaireId = dto.ProprietaireId,
            Ville = dto.Ville,
            Quartier = dto.Quartier,
            Adresse = dto.Adresse,
            Superficie = dto.Superficie,
            Loyer = dto.Loyer,
            Statut = StatutBien.Disponible,
            DateCreation = DateTime.UtcNow,
            EstSupprime = false
        };

        // Gestion de l'enregistrement des photos sur le serveur
        if (fichiers != null && fichiers.Count > 0)
        {
            var dossierUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "biens");
            if (!Directory.Exists(dossierUploads))
            {
                Directory.CreateDirectory(dossierUploads);
            }

            foreach (var fichier in fichiers)
            {
                if (fichier.Length > 0)
                {
                    var nomFichierUnique = $"{Guid.NewGuid()}_{Path.GetFileName(fichier.FileName)}";
                    var cheminComplet = Path.Combine(dossierUploads, nomFichierUnique);

                    using (var stream = new FileStream(cheminComplet, FileMode.Create))
                    {
                        await fichier.CopyToAsync(stream);
                    }

                    bien.Photos.Add($"/uploads/biens/{nomFichierUnique}");
                }
            }
        }

        _context.Biens.Add(bien);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = bien.Id }, bien);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Modifier(Guid id, [FromForm] Bien model, IFormFileCollection? fichiers)
    {
        var bien = await _context.Biens.FirstOrDefaultAsync(b => b.Id == id && !b.EstSupprime);
        if (bien == null) return NotFound("Bien introuvable.");

        // 1. Vérification d'unicité de la référence
        if (!string.IsNullOrWhiteSpace(model.Reference))
        {
            bool referenceExiste = await _context.Biens
                .AnyAsync(b => b.Reference == model.Reference && b.Id != id && !b.EstSupprime);

            if (referenceExiste)
            {
                return Conflict(new { message = "Un bien possède déjà cette référence. Veuillez en choisir une autre." });
            }
        }

        // 2. Mise à jour des champs textuels
        bien.Reference = model.Reference;
        bien.Type = model.Type;
        bien.Adresse = model.Adresse;
        bien.Ville = model.Ville;
        bien.Quartier = model.Quartier;
        bien.Superficie = model.Superficie;
        bien.Loyer = model.Loyer;
        bien.Statut = model.Statut;
        bien.ProprietaireId = model.ProprietaireId;

        // 3. Gestion de la liste finale des photos
        var photosFinales = model.Photos ?? new List<string>();

        if (fichiers != null && fichiers.Count > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "biens");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var fichier in fichiers)
            {
                if (fichier.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fichier.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fichier.CopyToAsync(stream);
                    }

                    photosFinales.Add($"/uploads/biens/{uniqueFileName}");
                }
            }
        }

        bien.Photos = photosFinales;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Suppression Logique (Soft Delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "GestionnaireOrAdmin")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var bien = await _context.Biens.FirstOrDefaultAsync(b => b.Id == id && !b.EstSupprime);
        if (bien == null) return NotFound("Bien introuvable.");

        bien.EstSupprime = true;
        bien.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Bien supprimé avec succès (archivage logique)." });
    }
}