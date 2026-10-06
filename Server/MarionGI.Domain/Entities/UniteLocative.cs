using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class UniteLocative : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public TypeUniteLocative Type { get; set; }

    public decimal Superficie { get; set; }

    public decimal Loyer { get; set; }

    public StatutDisponibilite Statut { get; set; }
        = StatutDisponibilite.Disponible;

    // Bien immobilier auquel appartient l'unité
    public Guid BienImmobilierId { get; set; }

    public BienImmobilier BienImmobilier { get; set; } = null!;

    public List<string> Photos { get; set; } = [];

    public ICollection<Contrat> Contrats { get; set; } = [];

    public ICollection<DemandeVisite> DemandesVisite { get; set; } = [];
}
