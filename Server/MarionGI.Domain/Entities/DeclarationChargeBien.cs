using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class DeclarationChargeBien : BaseEntity
{
    public Guid DeclarationFiscaleId { get; set; }

    public virtual DeclarationFiscale DeclarationFiscale { get; set;} = null!;

    public Guid ChargeBienId { get; set; }

    public virtual ChargeBien ChargeBien { get; set; } = null!;

    // Informations conservées au moment du calcul.
    public string DescriptionCharge { get; set; } = string.Empty;

    public DateTime DateDepense { get; set; }

    public decimal MontantTTC { get; set; }

    public StatutDeductibilite StatutDeductibilite { get; set; }

    public decimal MontantRetenu { get; set; }

    public decimal? MontantDeductibleFiscalement { get; set; }
}