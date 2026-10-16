using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class ValeurFiscaleBien : BaseEntity
{
    // Bien immobilier concerné.
    public Guid BienImmobilierId { get; set; }

    public virtual BienImmobilier BienImmobilier { get; set; }
        = null!;

    // Année fiscale à laquelle la valeur se rapporte.
    public int AnneeFiscale { get; set; }

    // Montant enregistré en FCFA.
    public decimal Montant { get; set; }

    // Estimée, déclarée, officielle ou issue d'une expertise.
    public TypeValeurFiscale TypeValeur { get; set; }

    // Date d'évaluation ou de déclaration.
    public DateTime DateEvaluation { get; set; }

    // Date de validation officielle, si connue.
    public DateTime? DateValidationOfficielle { get; set; }

    // Référence du document ou de la décision justificative.
    public string? ReferenceJustificative { get; set; }

    // Chemin du justificatif conservé dans MarionGI.
    public string? CheminJustificatif { get; set; }

    // Commentaire complémentaire.
    public string? Observations { get; set; }
}