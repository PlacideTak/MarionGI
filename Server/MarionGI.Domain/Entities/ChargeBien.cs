
using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class ChargeBien : BaseEntity
{
    // Bien immobilier concerné.
    public Guid BienImmobilierId { get; set; }

    public virtual BienImmobilier BienImmobilier { get; set; } = null!;

    // Unité concernée, si la dépense est spécifique à celle-ci.
    // Null signifie que la dépense concerne le bien dans son ensemble.
    public Guid? UniteLocativeId { get; set; }

    public virtual UniteLocative? UniteLocative { get; set; }

    // Dates de la dépense.
    public DateTime DateDepense { get; set; }

    public DateTime? DateFacture { get; set; }

    public DateTime? DatePaiement { get; set; }

    // Nature et description.
    public TypeCharge TypeCharge { get; set; }

    public string Description { get; set; } = string.Empty;

    // Fournisseur ou prestataire.
    public string? NomFournisseur { get; set; }

    public string? NumeroContribuableFournisseur { get; set; }

    // Montants financiers, en FCFA sauf convention contraire.
    public decimal MontantTTC { get; set; }

    public decimal? MontantHT { get; set; }

    public decimal? MontantTVA { get; set; }

    // Références et justificatifs.
    public string? ReferenceFacture { get; set; }

    //Référencer une facture ou une autre pièce justificative conservée par l'application.
    public string? CheminJustificatif { get; set; }

    // Informations fiscales.
    // Cette propriété représente une qualification fiscale
    // validée, et non une déduction automatique.
    public bool EstDeductibleFiscalement { get; set; }

    public string? JustificationNonDeductibilite { get; set; }

    public DateTime? DateValidationFiscale { get; set; }

    public string? Observations { get; set; }
}
