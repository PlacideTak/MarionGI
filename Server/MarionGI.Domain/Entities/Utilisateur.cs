using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Utilisateur : BaseEntity
{
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public string Telephone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string MotDePasseHash { get; set; } = string.Empty;

    public RoleUtilisateur Role { get; set; }

    public bool Statut { get; set; } = true;

    public DateTime? DerniereConnexion { get; set; }

    public bool TelephoneVerifie { get; set; } = false;

    public string? OtpSecret { get; set; }

    public DateTime? OtpExpiration { get; set; }

    public int TentativesConnexionEchouees { get; set; } = 0;

    public DateTime? VerrouilleJusquA { get; set; }

    // Société à laquelle appartient l'utilisateur
    public Guid SocieteId { get; set; }

    public Societe Societe { get; set; } = null!;

    // Contrats pour lesquels l'utilisateur est locataire
    public ICollection<Contrat> ContratsLocataire { get; set; } = [];

    // Tokens de rafraîchissement pour l'authentification
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
