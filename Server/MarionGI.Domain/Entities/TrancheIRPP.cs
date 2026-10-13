namespace MarionGI.Domain.Entities;

public class TrancheIRPP : BaseEntity
{
    public int AnneeFiscale { get; set; }

    public decimal SeuilMin { get; set; }

    public decimal? SeuilMax { get; set; }

    // Exemple : 0.10 représente 10 %.
    public decimal Taux { get; set; }

    public int OrdreTranche { get; set; }

    public string? Description { get; set; }
}
