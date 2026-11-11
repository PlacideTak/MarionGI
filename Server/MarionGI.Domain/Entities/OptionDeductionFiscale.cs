using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class OptionDeductionFiscale : BaseEntity
{
    public Guid SocieteId { get; set; }

    public virtual Societe Societe { get; set; } = null!;

    public int AnneeFiscale { get; set; }

    public ModeDeductionCharges ModeDeduction { get; set; }

    public DateTime? DateConfirmation { get; set; }

    public bool EstVerrouillee { get; set; }

    public DateTime? DateVerrouillage { get; set; }

    public string? Observations { get; set; }
}