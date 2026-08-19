using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Utilisateur : BaseEntity
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty; // Format +237XXXXXXXXX
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

    // Navigation
    public ICollection<Bien> BiensProprietaire { get; set; } = [];
    public ICollection<Contrat> ContratsLocataire { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
