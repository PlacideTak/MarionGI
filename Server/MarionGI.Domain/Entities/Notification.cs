namespace MarionGI.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Lu { get; set; } = false;
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;
}
