using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Utilisateur : BaseEntity
{
    // Identité de l'utilisateur.
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    // Coordonnées.
    public string Telephone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Authentification.
    public string MotDePasseHash { get; set; } = string.Empty;

    public RoleUtilisateur Role { get; set; }

    public bool Statut { get; set; } = true;

    public DateTime? DerniereConnexion { get; set; }

    // Vérification du téléphone.
    public bool TelephoneVerifie { get; set; } = false;

    // OTP : à utiliser avec un mécanisme sécurisé d'expiration
    // et de suppression ou rotation du secret.
    public string? OtpSecret { get; set; }

    public DateTime? OtpExpiration { get; set; }

    // Protection contre les tentatives répétées.
    public int TentativesConnexionEchouees { get; set; } = 0;

    public DateTime? VerrouilleJusquA { get; set; }

    // Société / compte MarionGI auquel l'utilisateur est rattaché.
    public Guid SocieteId { get; set; }

    public Societe Societe { get; set; } = null!;

    // Traçabilité du compte.
    public DateTime? DateDerniereModificationProfil { get; set; }

    // Contrats pour lesquels l'utilisateur est locataire.
    public ICollection<Contrat> ContratsLocataire { get; set; } = [];

    // Tokens de rafraîchissement.
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
