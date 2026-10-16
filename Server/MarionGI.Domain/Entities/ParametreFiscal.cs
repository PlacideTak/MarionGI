using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class ParametreFiscal : BaseEntity
{
    // Exercice fiscal auquel le paramètre se rapporte.
    public int AnneeFiscale { get; set; }

    // Identifiant stable de la règle dans MarionGI.
    // Exemple : TAUX_REVENU_FONCIER.
    public string CodeRegle { get; set; } = string.Empty;
    public TypeImpotFiscal TypeImpot { get; set; }

    public string Description { get; set; } = string.Empty;

    // Valeur numérique de la règle.
    // Exemple : 0.10 représente 10 % pour un taux exprimé
    // sous forme décimale.
    public decimal Valeur { get; set; }

    public TypeParametreFiscal TypeParametre { get; set; }

    // Période de validité de la règle.
    public DateTime DateDebutValidite { get; set; }

    public DateTime? DateFinValidite { get; set; }

    // Source juridique propre à cette règle, si connue.
    public string? ReferenceLegale { get; set; }

    // Date à laquelle la règle a été vérifiée.
    public DateTime? DateDerniereVerification { get; set; }

    // Conditions ou précisions nécessaires à son application.
    public string? ConditionsApplication { get; set; }

    // Régime fiscal concerné, si la règle est spécifique.
    public Guid? RegimeFiscalId { get; set; }

    public virtual RegimeFiscal? RegimeFiscal { get; set; }
}
