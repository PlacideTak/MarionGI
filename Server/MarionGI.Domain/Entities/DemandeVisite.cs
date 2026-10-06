using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class DemandeVisite : BaseEntity
{
    public Guid UniteLocativeId { get; set; }
    public UniteLocative UniteLocative { get; set; } = null!;

    public Guid? AgentId { get; set; }
    public Utilisateur? Agent { get; set; }

    public string NomProspect { get; set; } = string.Empty;

    public string TelephoneProspect { get; set; } = string.Empty;

    public DateTime DateSouhaitee { get; set; }

    public string Observations { get; set; } = string.Empty;

    public StatutDemandeVisite Statut { get; set; }
        = StatutDemandeVisite.EnAttente;
}