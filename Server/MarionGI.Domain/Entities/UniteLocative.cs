using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class UniteLocative : BaseEntity
{
    // Identification de l'unité dans MarionGI.
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    // Caractéristiques physiques.
    public decimal Superficie { get; set; }

    public int? NombrePieces { get; set; }

    public int? Etage { get; set; }

    // Informations locatives.
    public decimal Loyer { get; set; }

    public decimal? ChargesLocativesMensuelles { get; set; }

    public decimal? MontantCaution { get; set; }

    public StatutDisponibilite Statut { get; set; }
        = StatutDisponibilite.Disponible;

    // Dates de suivi de la mise en location.
    public DateTime? DatePremiereMiseEnLocation { get; set; }

    public DateTime? DateDerniereRenovation { get; set; }

    // Informations utiles à la préparation fiscale.
    // Le loyer contractuel ne représente pas nécessairement
    // le revenu effectivement encaissé.
    public bool EstExonereeFiscalement { get; set; }

    public string? MotifExoneration { get; set; }

    // Bien immobilier auquel appartient l'unité.
    public Guid BienImmobilierId { get; set; }

    public BienImmobilier BienImmobilier { get; set; } = null!;

    // Photos de l'unité.
    public List<string> Photos { get; set; } = [];

    // Contrats associés à l'unité.
    public ICollection<Contrat> Contrats { get; set; } = [];

    public ICollection<DemandeVisite> DemandesVisite { get; set; } = [];

    // Charges directement affectées à cette unité.
    public ICollection<ChargeBien> Charges { get; set; } = [];
}

