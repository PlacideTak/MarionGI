namespace MarionGI.Domain.Entities;

public class JournalAudit : BaseEntity
{
    public Guid? UtilisateurId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entite { get; set; } = string.Empty;
    public string EntiteId { get; set; } = string.Empty;
    public DateTime DateAction { get; set; } = DateTime.UtcNow;
    public string DetailsJson { get; set; } = "{}";
}
