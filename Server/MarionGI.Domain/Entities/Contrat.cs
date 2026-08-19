using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Contrat : BaseEntity
{
    public Guid BienId { get; set; }
    public Bien? Bien { get; set; }
    public Guid LocataireId { get; set; }
    public Utilisateur? Locataire { get; set; } 
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public decimal MontantLoyer { get; set; }
    public decimal MontantCaution { get; set; }
    public StatutContrat Statut { get; set; } = StatutContrat.Actif;
    public ICollection<Paiement> Paiements { get; set; } = [];
    public FrequencePaiement FrequencePaiement { get; set; } = FrequencePaiement.Mensuel;
    public int DelaiJoursTolerance { get; set; } = 5; // Par exemple, exigible le 5 du mois
}
