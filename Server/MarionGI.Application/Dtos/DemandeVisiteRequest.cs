using MarionGI.Domain.Enums;

namespace MarionGI.Application.Dtos;

public class DemandeVisiteRequest
{
    public Guid Id { get; set; }

    public Guid UniteLocativeId { get; set; }

    public Guid? AgentId { get; set; }

    public string NomProspect { get; set; } = string.Empty;

    public string TelephoneProspect { get; set; } = string.Empty;

    public DateTime DateSouhaitee { get; set; }

    public string? Observations { get; set; }

    public StatutDemandeVisite Statut { get; set; }
        = StatutDemandeVisite.EnAttente;
}