namespace MarionGI.Domain.Entities;

public class TrancheIRPP : BaseEntity
{
    // Année fiscale d'application.
    public int AnneeFiscale { get; set; }

    // Bornes de la tranche.
    public decimal SeuilMin { get; set; }

    // Null signifie qu'aucune borne supérieure n'est définie.
    public decimal? SeuilMax { get; set; }

    // Taux sous forme décimale.
    // Exemple : 0.10 représente 10 %.
    public decimal Taux { get; set; }

    // Ordre d'application de la tranche.
    public int OrdreTranche { get; set; }

    public string? Description { get; set; }

    // Traçabilité de la règle fiscale.
    public string? ReferenceLegale { get; set; }

    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    public DateTime? DateDerniereVerification { get; set; }
}
