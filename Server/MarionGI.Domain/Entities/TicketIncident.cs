using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class TicketIncident : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public string? ResultatReparation { get; set; }

    // Bien immobilier concerné.
    public Guid BienImmobilierId { get; set; }

    public BienImmobilier BienImmobilier { get; set; } = null!;

    // Unité locative concernée, si applicable.
    public Guid? UniteLocativeId { get; set; }

    public UniteLocative? UniteLocative { get; set; }

    // Locataire ayant déclaré ou concerné par l'incident.
    // Facultatif pour permettre les incidents des parties communes.
    public Guid? LocataireId { get; set; }

    public Utilisateur? Locataire { get; set; }

    // À ajouter si l'application doit identifier explicitement
    // l'utilisateur qui a créé le ticket.
    public Guid? DeclarantId { get; set; }

    public Utilisateur? Declarant { get; set; }

    public DateTime DateIncident { get; set; }

    public CategorieIncident Categorie { get; set; }

    public string Description { get; set; } = string.Empty;

    public PrioriteIncident Priorite { get; set; }
        = PrioriteIncident.Moyenne;

    public StatutIncident Statut { get; set; }
        = StatutIncident.Ouvert;

    public DateTime? DateResolution { get; set; }

    // Suivi financier global du ticket.
    public decimal? CoutEstime { get; set; }

    public decimal CoutTotal { get; set; } = 0m;

    public ImputabiliteIncident ImputableA { get; set; }
        = ImputabiliteIncident.Indeterminee;

    // Validation du devis global, si nécessaire.
    public bool ValidationDevisRequise { get; set; }

    public bool DevisValide { get; set; }

    public DateTime? DateValidationDevis { get; set; }

    public Guid? ValideParUtilisateurId { get; set; }

    public Utilisateur? ValideParUtilisateur { get; set; }

    // Photos du ticket.
    public ICollection<TicketPhoto> Photos { get; set; } = [];

    // Prestataires affectés à ce ticket.
    public ICollection<TicketIntervenant> Intervenants { get; set; } = [];
}
