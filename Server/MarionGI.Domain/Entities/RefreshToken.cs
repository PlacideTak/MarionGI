namespace MarionGI.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime DateExpiration { get; set; }
    public bool EstRevoque { get; set; } = false;
}
