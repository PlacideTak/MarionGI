using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class ParametreFiscal : BaseEntity
{
    public int AnneeFiscale { get; set; }

    public string CodeRegle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Exemple : 0.10 représente 10 %.
    public decimal Valeur { get; set; }

    public TypeParametreFiscal TypeParametre { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }
}