using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class ChargeBien : BaseEntity
{
    public Guid BienImmobilierId { get; set; }
    public virtual BienImmobilier BienImmobilier { get; set; } = null!;

    public Guid? UniteLocativeId { get; set; }
    public virtual UniteLocative? UniteLocative { get; set; }

    public DateTime DateDepense { get; set; }
    public TypeCharge TypeCharge { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal MontantTTC { get; set; }
    public decimal? MontantHT { get; set; }
    public decimal? MontantTVA { get; set; }

    public string? ReferenceFacture { get; set; }

    public bool EstDeductibleFiscalement { get; set; }
    public string? JustificationNonDeductibilite { get; set; }
}
