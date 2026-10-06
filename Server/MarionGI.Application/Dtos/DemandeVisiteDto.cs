using MarionGI.Domain.Enums;

namespace MarionGI.Application.DTOs;

public class DemandeVisiteDto
{
    public Guid Id { get; set; }

    public Guid UniteLocativeId { get; set; }

    // Informations de navigation utilisées uniquement
    // pour les réponses GET.
    public object? UniteLocative { get; set; }

    public Guid? BienId { get; set; }

    public object? Bien { get; set; }

    public Guid? AgentId { get; set; }

    public object? Agent { get; set; }

    public string NomProspect { get; set; } = string.Empty;

    public string TelephoneProspect { get; set; } = string.Empty;

    public DateTime DateSouhaitee { get; set; }

    public string? Observations { get; set; }

    public StatutDemandeVisite Statut { get; set; }
        = StatutDemandeVisite.EnAttente;
}