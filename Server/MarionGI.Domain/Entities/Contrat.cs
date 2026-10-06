using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Contrat : BaseEntity
{
    // Référence unique du contrat
    public string Reference { get; set; } = string.Empty;

    // Unité locative concernée par le contrat
    public Guid UniteLocativeId { get; set; }

    public UniteLocative UniteLocative { get; set; } = null!;

    // Locataire
    public Guid LocataireId { get; set; }

    public Utilisateur Locataire { get; set; } = null!;

    // Période du contrat
    public DateTime DateDebut { get; set; }

    public DateTime DateFin { get; set; }

    // Conditions financières
    public decimal MontantLoyer { get; set; }

    public decimal MontantCaution { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }
        = FrequencePaiement.Mensuel;

    public int DelaiJoursTolerance { get; set; } = 5;

    // État du contrat
    public StatutContrat Statut { get; set; }
        = StatutContrat.Actif;

    // Paiements associés
    public ICollection<Paiement> Paiements { get; set; } = [];
}
