using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class TicketIncident : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public string? ResultatReparation { get; set; }

    // Bien immobilier concerné : obligatoire
    public Guid BienImmobilierId { get; set; }
    public BienImmobilier BienImmobilier { get; set; } = null!;

    // Unité locative concernée : facultative
    // Null si l'incident concerne le bien dans son ensemble
    public Guid? UniteLocativeId { get; set; }
    public UniteLocative? UniteLocative { get; set; }

    public Guid LocataireId { get; set; }
    public Utilisateur Locataire { get; set; } = null!;

    public DateTime DateIncident { get; set; }

    public CategorieIncident Categorie { get; set; }

    public string Description { get; set; } = string.Empty;

    public PrioriteIncident Priorite { get; set; }
        = PrioriteIncident.Moyenne;

    public StatutIncident Statut { get; set; }
        = StatutIncident.Ouvert;

    public DateTime? DateResolution { get; set; }

    public decimal? CoutEstime { get; set; }
    public decimal CoutTotal { get; set; } = 0m;

    public ImputabiliteIncident ImputableA { get; set; }
        = ImputabiliteIncident.Indeterminee;

    public bool ValidationDevisRequise { get; set; }
    public bool DevisValide { get; set; }
    public DateTime? DateValidationDevis { get; set; }

    public Guid? ValideParUtilisateurId { get; set; }
    public Utilisateur? ValideParUtilisateur { get; set; }

    public ICollection<TicketPhoto> Photos { get; set; } = [];
    public ICollection<TicketIntervenant> Intervenants { get; set; } = [];
}