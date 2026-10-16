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
public class SocietesController : ControllerBase
{
    private readonly MarionDbContext _context;

    public SocietesController(MarionDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET : Liste des propriétaires
    // GET /api/Societes
    // GET /api/Societes?typePersonne=PersonnePhysique
    // ============================================================

    [HttpGet]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> GetSocietes(
        [FromQuery] TypePersonne? typePersonne,
        [FromQuery] bool? actif)
    {
        var query = _context.Societes
            .AsNoTracking()
            .Where(s => !s.EstSupprime);

        // Un administrateur peut consulter tous les propriétaires.
        // Un gestionnaire ne consulte que son propre compte.

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
            {
                return Unauthorized(new
                {
                    message =
                        "Impossible de déterminer le compte propriétaire de l'utilisateur connecté."
                });
            }

            query = query.Where(s => s.Id == societeId.Value);
        }

        if (typePersonne.HasValue)
        {
            query = query.Where(
                s => s.TypePersonne == typePersonne.Value);
        }

        if (actif.HasValue)
        {
            query = query.Where(
                s => s.Actif == actif.Value);
        }

        var resultats = await query
            .OrderBy(s => s.Nom)
            .Select(s => new SocieteDto
            {
                Id = s.Id,
                TypePersonne = s.TypePersonne,
                Nom = s.Nom,
                Prenom = s.Prenom,
                FormeJuridique = s.FormeJuridique,
                NumeroEntreprise = s.NumeroEntreprise,
                NumeroContribuable = s.NumeroContribuable,
                Adresse = s.Adresse,
                AdresseFiscale = s.AdresseFiscale,
                Ville = s.Ville,
                Region = s.Region,
                Departement = s.Departement,
                CodePostal = s.CodePostal,
                Pays = s.Pays,
                CentreImpots = s.CentreImpots,
                ActivitePrincipale = s.ActivitePrincipale,
                Telephone = s.Telephone,
                Email = s.Email,
                Actif = s.Actif,
                DateCreation = s.DateCreation,

                NombreUtilisateurs = s.Utilisateurs.Count(
                    u => !u.EstSupprime),

                NombreBiens = s.BiensImmobiliers.Count(
                    b => !b.EstSupprime),

                NombreDeclarationsFiscales =
                    s.DeclarationsFiscales.Count(
                        d => !d.EstSupprime)
            })
            .ToListAsync();

        return Ok(resultats);
    }

    // ============================================================
    // GET : Détail d'un propriétaire
    // GET /api/Societes/{id}
    // ============================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "L'identifiant du propriétaire est invalide."
            });
        }

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
            {
                return Unauthorized(new
                {
                    message =
                        "Impossible de déterminer le compte propriétaire de l'utilisateur connecté."
                });
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }
        }

        var societe = await _context.Societes
            .AsNoTracking()
            .Where(s => s.Id == id && !s.EstSupprime)
            .Select(s => new SocieteDto
            {
                Id = s.Id,
                TypePersonne = s.TypePersonne,
                Nom = s.Nom,
                Prenom = s.Prenom,
                FormeJuridique = s.FormeJuridique,
                NumeroEntreprise = s.NumeroEntreprise,
                NumeroContribuable = s.NumeroContribuable,
                Adresse = s.Adresse,
                AdresseFiscale = s.AdresseFiscale,
                Ville = s.Ville,
                Region = s.Region,
                Departement = s.Departement,
                CodePostal = s.CodePostal,
                Pays = s.Pays,
                CentreImpots = s.CentreImpots,
                ActivitePrincipale = s.ActivitePrincipale,
                Telephone = s.Telephone,
                Email = s.Email,
                Actif = s.Actif,
                DateCreation = s.DateCreation,

                NombreUtilisateurs = s.Utilisateurs.Count(
                    u => !u.EstSupprime),

                NombreBiens = s.BiensImmobiliers.Count(
                    b => !b.EstSupprime),

                NombreDeclarationsFiscales =
                    s.DeclarationsFiscales.Count(
                        d => !d.EstSupprime)
            })
            .FirstOrDefaultAsync();

        if (societe == null)
        {
            return NotFound(new
            {
                message = "Le propriétaire est introuvable."
            });
        }

        return Ok(societe);
    }

    // ============================================================
    // POST : Créer un propriétaire
    // Administrateur uniquement
    // ============================================================

    [HttpPost]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> CreerSociete(
        [FromBody] CreerSocieteRequest request)
    {
        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        var erreurIdentifiants =
            await VerifierIdentifiantsExistantsAsync(
                request.NumeroEntreprise,
                request.NumeroContribuable);

        if (erreurIdentifiants != null)
        {
            return Conflict(new { message = erreurIdentifiants });
        }

        var societe = new Societe
        {
            TypePersonne = request.TypePersonne,
            Nom = request.Nom.Trim(),
            Prenom = NormaliserTexte(request.Prenom),
            FormeJuridique = request.FormeJuridique,

            NumeroEntreprise =
                NormaliserTexte(request.NumeroEntreprise),

            NumeroContribuable =
                NormaliserTexte(request.NumeroContribuable),

            Adresse = NormaliserTexte(request.Adresse),
            AdresseFiscale = NormaliserTexte(request.AdresseFiscale),
            Ville = NormaliserTexte(request.Ville),
            Region = NormaliserTexte(request.Region),
            Departement = NormaliserTexte(request.Departement),
            CodePostal = NormaliserTexte(request.CodePostal),
            Pays = NormaliserTexte(request.Pays) ?? "Cameroun",
            CentreImpots = NormaliserTexte(request.CentreImpots),

            ActivitePrincipale =
                NormaliserTexte(request.ActivitePrincipale),

            Telephone = NormaliserTexte(request.Telephone),

            Email = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim().ToLowerInvariant(),

            Actif = request.Actif
        };

        _context.Societes.Add(societe);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = societe.Id },
            VersDto(societe));
    }

    // ============================================================
    // PUT : Modifier un propriétaire
    // Administrateur : tous les propriétaires
    // Gestionnaire : son propre compte
    // ============================================================

    [HttpPut("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierSociete(
        Guid id,
        [FromBody] ModifierSocieteRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "L'identifiant du propriétaire est invalide."
            });
        }

        var erreur = Valider(request);

        if (erreur != null)
        {
            return BadRequest(new { message = erreur });
        }

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
            {
                return Unauthorized(new
                {
                    message =
                        "Impossible de déterminer le compte propriétaire de l'utilisateur connecté."
                });
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }
        }

        var societe = await _context.Societes
            .FirstOrDefaultAsync(
                s => s.Id == id && !s.EstSupprime);

        if (societe == null)
        {
            return NotFound(new
            {
                message = "Le propriétaire est introuvable."
            });
        }

        var erreurIdentifiants =
            await VerifierIdentifiantsExistantsAsync(
                request.NumeroEntreprise,
                request.NumeroContribuable,
                id);

        if (erreurIdentifiants != null)
        {
            return Conflict(new { message = erreurIdentifiants });
        }

        societe.TypePersonne = request.TypePersonne;
        societe.Nom = request.Nom.Trim();
        societe.Prenom = NormaliserTexte(request.Prenom);
        societe.FormeJuridique = request.FormeJuridique;

        societe.NumeroEntreprise =
            NormaliserTexte(request.NumeroEntreprise);

        societe.NumeroContribuable =
            NormaliserTexte(request.NumeroContribuable);

        societe.Adresse = NormaliserTexte(request.Adresse);
        societe.AdresseFiscale =
            NormaliserTexte(request.AdresseFiscale);

        societe.Ville = NormaliserTexte(request.Ville);
        societe.Region = NormaliserTexte(request.Region);
        societe.Departement = NormaliserTexte(request.Departement);
        societe.CodePostal = NormaliserTexte(request.CodePostal);
        societe.Pays = NormaliserTexte(request.Pays) ?? "Cameroun";
        societe.CentreImpots = NormaliserTexte(request.CentreImpots);

        societe.ActivitePrincipale =
            NormaliserTexte(request.ActivitePrincipale);

        societe.Telephone = NormaliserTexte(request.Telephone);

        societe.Email = string.IsNullOrWhiteSpace(request.Email)
            ? null
            : request.Email.Trim().ToLowerInvariant();

        // Actif est géré séparément par l'endpoint de statut.

        await _context.SaveChangesAsync();

        return Ok(VersDto(societe));
    }

    // ============================================================
    // PATCH : Modifier le statut
    // ============================================================

    [HttpPatch("{id:guid}/statut")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur) + "," +
        nameof(RoleUtilisateur.Gestionnaire))]
    public async Task<IActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutSocieteRequest request)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "L'identifiant du propriétaire est invalide."
            });
        }

        if (User.IsInRole(nameof(RoleUtilisateur.Gestionnaire)))
        {
            var societeId =
                await GetSocieteIdUtilisateurConnecteAsync();

            if (!societeId.HasValue ||
                societeId.Value == Guid.Empty)
            {
                return Unauthorized(new
                {
                    message =
                        "Impossible de déterminer le compte propriétaire de l'utilisateur connecté."
                });
            }

            if (id != societeId.Value)
            {
                return Forbid();
            }

            // Un gestionnaire ne peut pas désactiver son propre
            // compte propriétaire et ainsi contourner ses accès.
            if (!request.Actif)
            {
                return Forbid();
            }
        }

        var societe = await _context.Societes
            .FirstOrDefaultAsync(
                s => s.Id == id && !s.EstSupprime);

        if (societe == null)
        {
            return NotFound(new
            {
                message = "Le propriétaire est introuvable."
            });
        }

        if (!request.Actif)
        {
            var utilisateursActifs =
                await _context.Utilisateurs
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.SocieteId == id &&
                        u.Statut &&
                        !u.EstSupprime);

            if (utilisateursActifs)
            {
                return Conflict(new
                {
                    message =
                        "Le propriétaire ne peut pas être désactivé tant qu'il possède des utilisateurs actifs."
                });
            }
        }

        societe.Actif = request.Actif;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = societe.Actif
                ? "Compte propriétaire activé avec succès."
                : "Compte propriétaire désactivé avec succès.",
            societe.Id,
            societe.Actif
        });
    }

    // ============================================================
    // DELETE : Suppression logique
    // Administrateur uniquement
    // ============================================================

    [HttpDelete("{id:guid}")]
    [Authorize(Roles =
        nameof(RoleUtilisateur.Administrateur))]
    public async Task<IActionResult> SupprimerSociete(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "L'identifiant du propriétaire est invalide."
            });
        }

        var societe = await _context.Societes
            .FirstOrDefaultAsync(
                s => s.Id == id && !s.EstSupprime);

        if (societe == null)
        {
            return NotFound(new
            {
                message = "Le propriétaire est introuvable."
            });
        }

        var utilisateursActifs =
            await _context.Utilisateurs
                .AsNoTracking()
                .AnyAsync(u =>
                    u.SocieteId == id &&
                    !u.EstSupprime &&
                    u.Statut);

        if (utilisateursActifs)
        {
            return Conflict(new
            {
                message =
                    "Ce propriétaire ne peut pas être supprimé car il possède encore des utilisateurs actifs."
            });
        }

        var biensExistants =
            await _context.BiensImmobiliers
                .AsNoTracking()
                .AnyAsync(b =>
                    b.SocieteId == id &&
                    !b.EstSupprime);

        if (biensExistants)
        {
            return Conflict(new
            {
                message =
                    "Ce propriétaire possède encore des biens immobiliers. Archivez ou réaffectez ces biens avant de supprimer son compte."
            });
        }

        var declarationsExistantes =
            await _context.DeclarationsFiscales
                .AsNoTracking()
                .AnyAsync(d =>
                    d.SocieteId == id &&
                    !d.EstSupprime);

        if (declarationsExistantes)
        {
            return Conflict(new
            {
                message =
                    "Ce propriétaire possède un historique de déclarations fiscales. Désactivez son compte plutôt que de le supprimer."
            });
        }

        societe.EstSupprime = true;
        societe.Actif = false;
        societe.DateSuppression = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private static string? Valider(
        CreerSocieteRequest request)
    {
        if (!Enum.IsDefined(
                typeof(TypePersonne),
                request.TypePersonne))
        {
            return "Le type de personne est invalide.";
        }

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            return "Le nom ou la dénomination du propriétaire est obligatoire.";
        }

        if (request.Nom.Trim().Length > 200)
        {
            return "Le nom ne peut pas dépasser 200 caractères.";
        }

        if (request.TypePersonne == TypePersonne.PersonnePhysique &&
            string.IsNullOrWhiteSpace(request.Prenom))
        {
            return "Le prénom est obligatoire pour une personne physique.";
        }

        if (request.Prenom?.Trim().Length > 150)
        {
            return "Le prénom ne peut pas dépasser 150 caractères.";
        }

        if (request.TypePersonne == TypePersonne.PersonnePhysique &&
            request.FormeJuridique.HasValue)
        {
            return "Une personne physique ne doit pas avoir de forme juridique de personne morale.";
        }

        if (request.TypePersonne == TypePersonne.PersonneMorale &&
            !request.FormeJuridique.HasValue)
        {
            return "La forme juridique est obligatoire pour une personne morale.";
        }

        if (request.FormeJuridique.HasValue &&
            !Enum.IsDefined(
                typeof(FormeJuridique),
                request.FormeJuridique.Value))
        {
            return "La forme juridique sélectionnée est invalide.";
        }

        if (request.NumeroEntreprise?.Trim().Length > 100)
        {
            return "Le numéro d'entreprise ne peut pas dépasser 100 caractères.";
        }

        if (request.NumeroContribuable?.Trim().Length > 100)
        {
            return "Le numéro de contribuable ne peut pas dépasser 100 caractères.";
        }

        if (request.Adresse?.Trim().Length > 300 ||
            request.AdresseFiscale?.Trim().Length > 300)
        {
            return "Une adresse ne peut pas dépasser 300 caractères.";
        }

        if (request.Ville?.Trim().Length > 100 ||
            request.Region?.Trim().Length > 100 ||
            request.Departement?.Trim().Length > 100)
        {
            return "La ville, la région ou le département ne peut pas dépasser 100 caractères.";
        }

        if (request.CodePostal?.Trim().Length > 20)
        {
            return "Le code postal ne peut pas dépasser 20 caractères.";
        }

        if (request.Pays?.Trim().Length > 100)
        {
            return "Le pays ne peut pas dépasser 100 caractères.";
        }

        if (request.CentreImpots?.Trim().Length > 200)
        {
            return "Le centre des impôts ne peut pas dépasser 200 caractères.";
        }

        if (request.ActivitePrincipale?.Trim().Length > 200)
        {
            return "L'activité principale ne peut pas dépasser 200 caractères.";
        }

        if (request.Telephone?.Trim().Length > 30)
        {
            return "Le téléphone ne peut pas dépasser 30 caractères.";
        }

        if (request.Email?.Trim().Length > 254)
        {
            return "L'adresse courriel ne peut pas dépasser 254 caractères.";
        }

        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !System.Net.Mail.MailAddress.TryCreate(
                request.Email.Trim(),
                out _))
        {
            return "L'adresse courriel est invalide.";
        }

        return null;
    }

    // ============================================================
    // VÉRIFICATION DES IDENTIFIANTS
    // ============================================================

    private async Task<string?> VerifierIdentifiantsExistantsAsync(
        string? numeroEntreprise,
        string? numeroContribuable,
        Guid? idExclu = null)
    {
        var query = _context.Societes
            .AsNoTracking()
            .Where(s => !s.EstSupprime);

        if (idExclu.HasValue)
        {
            query = query.Where(s => s.Id != idExclu.Value);
        }

        if (!string.IsNullOrWhiteSpace(numeroEntreprise))
        {
            var numero = numeroEntreprise.Trim();

            var existe = await query.AnyAsync(s =>
                s.NumeroEntreprise != null &&
                s.NumeroEntreprise == numero);

            if (existe)
            {
                return "Ce numéro d'entreprise est déjà associé à un autre propriétaire.";
            }
        }

        if (!string.IsNullOrWhiteSpace(numeroContribuable))
        {
            var numero = numeroContribuable.Trim();

            var existe = await query.AnyAsync(s =>
                s.NumeroContribuable != null &&
                s.NumeroContribuable == numero);

            if (existe)
            {
                return "Ce numéro de contribuable est déjà associé à un autre propriétaire.";
            }
        }

        return null;
    }

    // ============================================================
    // RÉCUPÉRER LA SOCIÉTÉ DE L'UTILISATEUR CONNECTÉ
    // ============================================================

    private async Task<Guid?> GetSocieteIdUtilisateurConnecteAsync()
    {
        var utilisateurId = GetCurrentUserId();

        if (!utilisateurId.HasValue)
        {
            return null;
        }

        return await _context.Utilisateurs
            .AsNoTracking()
            .Where(u =>
                u.Id == utilisateurId.Value &&
                !u.EstSupprime &&
                u.Statut &&
                u.SocieteId != Guid.Empty)
            .Select(u => (Guid?)u.SocieteId)
            .FirstOrDefaultAsync();
    }

    private Guid? GetCurrentUserId()
    {
        var claims = new[]
        {
            ClaimTypes.NameIdentifier,
            "sub",
            "Id"
        };

        foreach (var claim in claims)
        {
            var valeur = User.FindFirstValue(claim);

            if (Guid.TryParse(valeur, out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string? NormaliserTexte(string? valeur)
    {
        return string.IsNullOrWhiteSpace(valeur)
            ? null
            : valeur.Trim();
    }

    // ============================================================
    // MAPPING DTO
    // ============================================================

    private static SocieteDto VersDto(Societe societe)
    {
        return new SocieteDto
        {
            Id = societe.Id,
            TypePersonne = societe.TypePersonne,
            Nom = societe.Nom,
            Prenom = societe.Prenom,
            FormeJuridique = societe.FormeJuridique,
            NumeroEntreprise = societe.NumeroEntreprise,
            NumeroContribuable = societe.NumeroContribuable,
            Adresse = societe.Adresse,
            AdresseFiscale = societe.AdresseFiscale,
            Ville = societe.Ville,
            Region = societe.Region,
            Departement = societe.Departement,
            CodePostal = societe.CodePostal,
            Pays = societe.Pays,
            CentreImpots = societe.CentreImpots,
            ActivitePrincipale = societe.ActivitePrincipale,
            Telephone = societe.Telephone,
            Email = societe.Email,
            Actif = societe.Actif,
            DateCreation = societe.DateCreation
        };
    }
}

// ============================================================
// DTO DE RÉPONSE
// ============================================================

public class SocieteDto
{
    public Guid Id { get; set; }

    public TypePersonne TypePersonne { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string? Prenom { get; set; }

    public FormeJuridique? FormeJuridique { get; set; }

    public string? NumeroEntreprise { get; set; }

    public string? NumeroContribuable { get; set; }

    public string? Adresse { get; set; }

    public string? AdresseFiscale { get; set; }

    public string? Ville { get; set; }

    public string? Region { get; set; }

    public string? Departement { get; set; }

    public string? CodePostal { get; set; }

    public string? Pays { get; set; }

    public string? CentreImpots { get; set; }

    public string? ActivitePrincipale { get; set; }

    public string? Telephone { get; set; }

    public string? Email { get; set; }

    public bool Actif { get; set; }

    public DateTime DateCreation { get; set; }

    public int NombreUtilisateurs { get; set; }

    public int NombreBiens { get; set; }

    public int NombreDeclarationsFiscales { get; set; }
}

// ============================================================
// DTO DE CRÉATION
// ============================================================

public class CreerSocieteRequest
{
    public TypePersonne TypePersonne { get; set; }
        = TypePersonne.PersonneMorale;

    public string Nom { get; set; } = string.Empty;

    public string? Prenom { get; set; }

    public FormeJuridique? FormeJuridique { get; set; }

    public string? NumeroEntreprise { get; set; }

    public string? NumeroContribuable { get; set; }

    public string? Adresse { get; set; }

    public string? AdresseFiscale { get; set; }

    public string? Ville { get; set; }

    public string? Region { get; set; }

    public string? Departement { get; set; }

    public string? CodePostal { get; set; }

    public string? Pays { get; set; } = "Cameroun";

    public string? CentreImpots { get; set; }

    public string? ActivitePrincipale { get; set; }

    public string? Telephone { get; set; }

    public string? Email { get; set; }

    public bool Actif { get; set; } = true;
}

// ============================================================
// DTO DE MODIFICATION
// ============================================================

public class ModifierSocieteRequest : CreerSocieteRequest
{
}

// ============================================================
// DTO DE MODIFICATION DU STATUT
// ============================================================

public class ModifierStatutSocieteRequest
{
    public bool Actif { get; set; }
}
