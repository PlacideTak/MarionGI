
using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Contrat : BaseEntity
{
    // Identification du contrat.
    public string Reference { get; set; } = string.Empty;

    // Unité locative concernée.
    public Guid UniteLocativeId { get; set; }

    public UniteLocative UniteLocative { get; set; } = null!;

    // Locataire associé au contrat.
    public Guid LocataireId { get; set; }

    public Utilisateur Locataire { get; set; } = null!;

    // Période contractuelle.
    public DateTime DateDebut { get; set; }

    public DateTime DateFin { get; set; }

    public DateTime? DateSignature { get; set; }

    // Conditions financières.
    public decimal MontantLoyer { get; set; }

    public decimal MontantCaution { get; set; }

    public decimal? MontantChargesLocatives { get; set; }

    public FrequencePaiement FrequencePaiement { get; set; }
        = FrequencePaiement.Mensuel;

    public int DelaiJoursTolerance { get; set; } = 5;

    // Révision du loyer.
    public bool RevisionLoyerAutorisee { get; set; }

    public DateTime? DateProchaineRevisionLoyer { get; set; }

    public decimal? TauxRevisionLoyer { get; set; }

    // Modalités de renouvellement.
    public bool RenouvellementAutomatique { get; set; }

    public int? DelaiPreavisJours { get; set; }

    // Informations documentaires.
    public string? CheminDocumentContrat { get; set; }

    public string? Observations { get; set; }

    // État du contrat.
    public StatutContrat Statut { get; set; }
        = StatutContrat.Actif;

    // Paiements associés.
    public ICollection<Paiement> Paiements { get; set; } = [];
}