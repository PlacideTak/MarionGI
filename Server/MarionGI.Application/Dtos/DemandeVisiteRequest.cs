using MarionGI.Domain.Enums;

namespace MarionGI.Application.Dtos;


public class DemandeVisiteRequest
{
    public Guid Id { get; set; }

    // Unité locative
    public Guid UniteLocativeId { get; set; }

    // Agent affecté
    public Guid? AgentId { get; set; }

    // Coordonnées du prospect
    public string NomProspect { get; set; } = string.Empty;

    public string TelephoneProspect { get; set; } = string.Empty;

    public string? EmailProspect { get; set; }

    // Dates de visite
    public DateTime DateSouhaitee { get; set; }

    public DateTime? DateConfirmee { get; set; }

    // Commentaires
    public string? Observations { get; set; }

    public string? CommentaireInterne { get; set; }

    // Statut : sa valeur doit être contrôlée par l'API
    public StatutDemandeVisite Statut { get; set; }
        = StatutDemandeVisite.EnAttente;
}
