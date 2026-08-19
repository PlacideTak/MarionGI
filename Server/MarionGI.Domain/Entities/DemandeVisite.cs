using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class DemandeVisite : BaseEntity
{
    public Guid BienId { get; set; }
    public Bien? Bien { get; set; } 
    public Guid? AgentId { get; set; }
    public Utilisateur? Agent { get; set; }
    public string NomProspect { get; set; } = string.Empty;
    public string TelephoneProspect { get; set; } = string.Empty;
    public DateTime DateSouhaitee { get; set; }
    public StatutDemandeVisite Statut { get; set; } = StatutDemandeVisite.EnAttente;
}