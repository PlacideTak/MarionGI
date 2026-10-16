namespace MarionGI.Domain.Entities;

public class RegimeFiscal : BaseEntity
{
    // Identification du régime.
    public string Code { get; set; } = string.Empty;

    public string Libelle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Période d'application du régime.
    public DateTime? DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    // Référence juridique justifiant le régime.
    // Exemple : référence d'un texte fiscal applicable.
    public string? ReferenceLegale { get; set; }

    public DateTime? DateDerniereVerification { get; set; }

    // Activation dans MarionGI.
    // Un régime actif dans l'application n'est pas nécessairement
    // applicable à toutes les périodes fiscales.
    public bool EstActif { get; set; } = true;

    // Paramètres associés au régime.
    public virtual ICollection<ParametreFiscal> ParametresFiscaux
    {
        get;
        set;
    } = new List<ParametreFiscal>();
}
