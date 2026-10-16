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
[Authorize(Roles = "Administrateur,Gestionnaire")]
public class UtilisateursController : ControllerBase
{
    private readonly MarionDbContext _context;

    public UtilisateursController(MarionDbContext context)
    {
        _context = context;
    }

    // GET: api/Utilisateurs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UtilisateurResponse>>> GetTous()
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        var requete = _context.Utilisateurs
            .AsNoTracking()
            .Where(u => !u.EstSupprime);

        // Un gestionnaire ne consulte que les utilisateurs
        // de sa propre société.
        if (connecte.Role == RoleUtilisateur.Gestionnaire)
        {
            requete = requete.Where(
                u => u.SocieteId == connecte.SocieteId);
        }

        var utilisateurs = await requete
            .OrderBy(u => u.Nom)
            .ThenBy(u => u.Prenom)
            .Select(u => new UtilisateurResponse(
                u.Id,
                u.Nom,
                u.Prenom,
                u.Telephone,
                u.Email,
                u.Role,
                u.Statut,
                u.SocieteId,
                u.Societe.Nom,
                u.DateCreation,
                u.DerniereConnexion,
                u.TelephoneVerifie,
                u.DateDerniereModificationProfil))
            .ToListAsync();

        return Ok(utilisateurs);
    }

    // GET: api/Utilisateurs/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UtilisateurResponse>> GetParId(Guid id)
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        var utilisateur = await _context.Utilisateurs
            .AsNoTracking()
            .Where(u => u.Id == id && !u.EstSupprime)
            .Select(u => new UtilisateurResponse(
                u.Id,
                u.Nom,
                u.Prenom,
                u.Telephone,
                u.Email,
                u.Role,
                u.Statut,
                u.SocieteId,
                u.Societe.Nom,
                u.DateCreation,
                u.DerniereConnexion,
                u.TelephoneVerifie,
                u.DateDerniereModificationProfil))
            .FirstOrDefaultAsync();

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        if (connecte.Role == RoleUtilisateur.Gestionnaire &&
            utilisateur.SocieteId != connecte.SocieteId)
        {
            return Forbid();
        }

        return Ok(utilisateur);
    }

    // POST: api/Utilisateurs
    [HttpPost]
    public async Task<ActionResult<UtilisateurResponse>> Creer(
        [FromBody] CreerUtilisateurRequest request)
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(request.Nom) ||
            string.IsNullOrWhiteSpace(request.Prenom) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.MotDePasse))
        {
            return BadRequest(
                "Le nom, le prénom, l'adresse courriel et le mot de passe sont obligatoires.");
        }

        if (request.MotDePasse.Length < 8)
            return BadRequest("Le mot de passe doit contenir au moins 8 caractères.");

        if (!Enum.IsDefined(typeof(RoleUtilisateur), request.Role))
            return BadRequest("Le rôle indiqué est invalide.");

        // Un gestionnaire ne peut pas créer un administrateur
        // ni créer un utilisateur dans une autre société.
        if (connecte.Role == RoleUtilisateur.Gestionnaire)
        {
            if (request.Role == RoleUtilisateur.Administrateur)
                return Forbid();

            if (request.SocieteId != connecte.SocieteId)
                return Forbid();
        }

        if (request.SocieteId == Guid.Empty)
            return BadRequest("La société est obligatoire.");

        var societe = await _context.Societes
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Id == request.SocieteId &&
                !s.EstSupprime &&
                s.Actif);

        if (societe is null)
            return BadRequest("La société est introuvable ou inactive.");

        var email = request.Email.Trim();

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(u =>
                !u.EstSupprime &&
                u.Email.ToLower() == email.ToLower());

        if (emailExiste)
            return Conflict("Cette adresse courriel est déjà utilisée.");

        var utilisateur = new Utilisateur
        {
            Nom = request.Nom.Trim(),
            Prenom = request.Prenom.Trim(),
            Telephone = request.Telephone?.Trim() ?? string.Empty,
            Email = email,
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(
                request.MotDePasse),
            Role = request.Role,
            Statut = true,
            SocieteId = request.SocieteId,
            TelephoneVerifie = false,
            TentativesConnexionEchouees = 0
        };

        _context.Utilisateurs.Add(utilisateur);
        await _context.SaveChangesAsync();

        var response = ToResponse(utilisateur, societe.Nom);

        return CreatedAtAction(
            nameof(GetParId),
            new { id = utilisateur.Id },
            response);
    }

    // PUT: api/Utilisateurs/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UtilisateurResponse>> Modifier(
        Guid id,
        [FromBody] ModifierUtilisateurRequest request)
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (string.IsNullOrWhiteSpace(request.Nom) ||
            string.IsNullOrWhiteSpace(request.Prenom) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(
                "Le nom, le prénom et l'adresse courriel sont obligatoires.");
        }

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        var estSonPropreCompte = utilisateur.Id == connecte.Id;

        if (connecte.Role == RoleUtilisateur.Gestionnaire &&
            utilisateur.SocieteId != connecte.SocieteId)
        {
            return Forbid();
        }

        if (!Enum.IsDefined(typeof(RoleUtilisateur), request.Role))
            return BadRequest("Le rôle indiqué est invalide.");

        // Un gestionnaire ne peut pas attribuer le rôle Administrateur.
        if (connecte.Role == RoleUtilisateur.Gestionnaire &&
            request.Role == RoleUtilisateur.Administrateur)
        {
            return Forbid();
        }

        // L'utilisateur ne peut pas changer lui-même son rôle
        // ni déplacer son compte vers une autre société.
        if (estSonPropreCompte &&
            (request.Role != utilisateur.Role ||
             request.SocieteId != utilisateur.SocieteId))
        {
            return BadRequest(
                "Vous ne pouvez pas modifier votre propre rôle ou votre société.");
        }

        // Un gestionnaire reste limité à sa société.
        if (connecte.Role == RoleUtilisateur.Gestionnaire &&
            request.SocieteId != connecte.SocieteId)
        {
            return Forbid();
        }

        // Un gestionnaire ne peut pas modifier un administrateur.
        if (connecte.Role == RoleUtilisateur.Gestionnaire &&
            utilisateur.Role == RoleUtilisateur.Administrateur)
        {
            return Forbid();
        }

        if (request.SocieteId == Guid.Empty)
            return BadRequest("La société est obligatoire.");

        var societe = await _context.Societes
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Id == request.SocieteId &&
                !s.EstSupprime &&
                s.Actif);

        if (societe is null)
            return BadRequest("La société est introuvable ou inactive.");

        var email = request.Email.Trim();

        var emailExiste = await _context.Utilisateurs
            .AnyAsync(u =>
                !u.EstSupprime &&
                u.Id != id &&
                u.Email.ToLower() == email.ToLower());

        if (emailExiste)
            return Conflict("Cette adresse courriel est déjà utilisée.");

        utilisateur.Nom = request.Nom.Trim();
        utilisateur.Prenom = request.Prenom.Trim();
        utilisateur.Telephone = request.Telephone?.Trim() ?? string.Empty;
        utilisateur.Email = email;
        utilisateur.Role = request.Role;
        utilisateur.SocieteId = request.SocieteId;
        utilisateur.DateDerniereModificationProfil = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ToResponse(utilisateur, societe.Nom));
    }

    // PATCH: api/Utilisateurs/{id}/statut
    [HttpPatch("{id:guid}/statut")]
    public async Task<ActionResult> ModifierStatut(
        Guid id,
        [FromBody] ModifierStatutUtilisateurRequest request)
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        if (utilisateur.Id == connecte.Id)
            return BadRequest(
                "Vous ne pouvez pas modifier le statut de votre propre compte.");

        if (connecte.Role == RoleUtilisateur.Gestionnaire)
        {
            if (utilisateur.SocieteId != connecte.SocieteId)
                return Forbid();

            if (utilisateur.Role == RoleUtilisateur.Administrateur)
                return Forbid();
        }

        // Un administrateur ne peut pas désactiver un autre administrateur
        // par cet endpoint. Cela évite de désactiver un compte privilégié
        // par inadvertance.
        if (utilisateur.Role == RoleUtilisateur.Administrateur)
            return BadRequest(
                "Le statut d'un compte administrateur ne peut pas être modifié ici.");

        utilisateur.Statut = request.Statut;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Utilisateurs/{id}
    // Suppression logique uniquement.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Supprimer(Guid id)
    {
        var connecte = await GetUtilisateurConnecteAsync();

        if (connecte is null)
            return Unauthorized("Utilisateur connecté introuvable ou inactif.");

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == id &&
                !u.EstSupprime);

        if (utilisateur is null)
            return NotFound("Utilisateur introuvable.");

        if (utilisateur.Id == connecte.Id)
            return BadRequest(
                "Vous ne pouvez pas supprimer votre propre compte.");

        if (utilisateur.Role == RoleUtilisateur.Administrateur)
            return BadRequest(
                "Un compte administrateur ne peut pas être supprimé par cet endpoint.");

        if (connecte.Role == RoleUtilisateur.Gestionnaire)
        {
            if (utilisateur.SocieteId != connecte.SocieteId)
                return Forbid();

            if (utilisateur.Role == RoleUtilisateur.Administrateur)
                return Forbid();
        }

        utilisateur.EstSupprime = true;
        utilisateur.DateSuppression = DateTime.UtcNow;
        utilisateur.Statut = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Récupération sécurisée de l'utilisateur authentifié.
    private async Task<Utilisateur?> GetUtilisateurConnecteAsync()
    {
        var idClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("Id");

        if (!Guid.TryParse(idClaim, out var utilisateurId))
            return null;

        return await _context.Utilisateurs
            .FirstOrDefaultAsync(u =>
                u.Id == utilisateurId &&
                !u.EstSupprime &&
                u.Statut);
    }

    private static UtilisateurResponse ToResponse(
        Utilisateur utilisateur,
        string nomSociete)
    {
        return new UtilisateurResponse(
            utilisateur.Id,
            utilisateur.Nom,
            utilisateur.Prenom,
            utilisateur.Telephone,
            utilisateur.Email,
            utilisateur.Role,
            utilisateur.Statut,
            utilisateur.SocieteId,
            nomSociete,
            utilisateur.DateCreation,
            utilisateur.DerniereConnexion,
            utilisateur.TelephoneVerifie,
            utilisateur.DateDerniereModificationProfil);
    }
}

// DTO de création.
public record CreerUtilisateurRequest(
    string Nom,
    string Prenom,
    string? Telephone,
    string Email,
    string MotDePasse,
    RoleUtilisateur Role,
    Guid SocieteId);

// DTO de modification.
public record ModifierUtilisateurRequest(
    string Nom,
    string Prenom,
    string? Telephone,
    string Email,
    RoleUtilisateur Role,
    Guid SocieteId);

// DTO de modification du statut.
public record ModifierStatutUtilisateurRequest(
    bool Statut);

// DTO de réponse : aucun mot de passe ni secret de sécurité n'est exposé.
public record UtilisateurResponse(
    Guid Id,
    string Nom,
    string Prenom,
    string Telephone,
    string Email,
    RoleUtilisateur Role,
    bool Statut,
    Guid SocieteId,
    string NomSociete,
    DateTime DateCreation,
    DateTime? DerniereConnexion,
    bool TelephoneVerifie,
    DateTime? DateDerniereModificationProfil);
