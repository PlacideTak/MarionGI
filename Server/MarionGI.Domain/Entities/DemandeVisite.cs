using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class DemandeVisite : BaseEntity
{
    // Unité locative concernée par la visite.
    public Guid UniteLocativeId { get; set; }

    public UniteLocative UniteLocative { get; set; } = null!;

    // Agent chargé de la demande, facultatif au départ.
    public Guid? AgentId { get; set; }

    public Utilisateur? Agent { get; set; }

    // Coordonnées du prospect.
    public string NomProspect { get; set; } = string.Empty;

    public string TelephoneProspect { get; set; } = string.Empty;

    public string? EmailProspect { get; set; }

    // Date et heure souhaitées par le prospect.
    public DateTime DateSouhaitee { get; set; }

    // Date et heure effectivement confirmées.
    public DateTime? DateConfirmee { get; set; }

    // Motif ou commentaires du prospect.
    public string? Observations { get; set; }

    // Informations internes de suivi.
    public string? CommentaireInterne { get; set; }

    public DateTime? DateTraitement { get; set; }

    // Statut de la demande.
    public StatutDemandeVisite Statut { get; set; }
        = StatutDemandeVisite.EnAttente;
}
