using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Bien : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public TypeBien Type { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty; 
    public string Quartier { get; set; } = string.Empty; 
    public decimal Superficie { get; set; }
    public decimal Loyer { get; set; }
    public StatutBien Statut { get; set; } = StatutBien.Disponible;
    public Guid ProprietaireId { get; set; }
    public Utilisateur? Proprietaire { get; set; } = null!;
    public List<string> Photos { get; set; } = [];

    public ICollection<Contrat> Contrats { get; set; } = [];
    public ICollection<DemandeVisite> DemandesVisite { get; set; } = [];
}
