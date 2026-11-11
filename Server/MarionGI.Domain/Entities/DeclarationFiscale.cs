using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class DeclarationFiscale : BaseEntity
{
    // Propriétaire concerné : particulier ou société.
    public Guid SocieteId { get; set; }

    public virtual Societe Societe { get; set; } = null!;

    // Bien concerné, si la déclaration porte sur un bien précis.
    public Guid? BienImmobilierId { get; set; }

    public virtual BienImmobilier? BienImmobilier { get; set; }

    // Année fiscale concernée.
    public int AnneeFiscale { get; set; }

    // Nature de l'impôt concerné.
    public TypeImpotFiscal TypeImpot { get; set; }

    // Base imposable, si elle est connue.
    public decimal? BaseImposable { get; set; }

    // Montant déclaré ou calculé.
    public decimal MontantImpot { get; set; }

    // Date limite de déclaration ou de paiement,
    // selon l'obligation suivie.
    public DateTime? DateEcheance { get; set; }

    // Date effective de déclaration.
    public DateTime? DateDeclaration { get; set; }

    // Référence de la déclaration ou de l'avis fiscal.
    public string? ReferenceDeclaration { get; set; }

    // Observations et informations complémentaires.
    public string? Observations { get; set; }

    // Statut de suivi dans MarionGI.
    public StatutDeclarationFiscale Statut { get; set; }

    // Option annuelle de déduction retenue.
    public Guid? OptionDeductionFiscaleId { get; set; }

    public virtual OptionDeductionFiscale? OptionDeductionFiscale {get; set; }

    // Copie du mode retenu au moment du calcul.
    // Permet de préserver l'historique.
    public ModeDeductionCharges? ModeDeduction { get; set; }

    // Données du calcul des revenus fonciers.
    public decimal? TotalLoyersBruts { get; set; }

    public decimal? ChargesForfaitaires { get; set; }

    public decimal? ChargesReellesDeclarees { get; set; }

    public decimal? ChargesReellesAdmissibles { get; set; }

    public decimal? RevenuNetFoncier { get; set; }

    // Acomptes déjà versés.
    public decimal? AcomptesVerses { get; set; }

    // Solde après imputation des acomptes.
    public decimal? SoldeImpot { get; set; }

    public DateTime? DateCalcul { get; set; }

    public virtual ICollection<DeclarationChargeBien> ChargesRetenues { get; set; }
    = new List<DeclarationChargeBien>();
}