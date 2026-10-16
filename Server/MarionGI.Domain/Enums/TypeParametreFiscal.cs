namespace MarionGI.Domain.Enums;

/// <summary>
/// Définit les différents types de paramètres utilisés pour configurer un régime fiscal.
/// </summary>
public enum TypeParametreFiscal
{
    /// <summary>
    /// Pourcentage utilisé directement pour calculer un impôt,
    /// une taxe ou une cotisation.
    /// Exemple : appliquer un taux de 10 % à une base imposable.
    /// </summary>
    Taux = 1,

    /// <summary>
    /// Montant fixe exprimé en monnaie, par exemple une taxe annuelle de 50 000 FCFA.
    /// </summary>
    MontantFixe = 2,

    /// <summary>
    /// Pourcentage fixe appliqué selon une règle forfaitaire,
    /// par exemple pour calculer un abattement ou une déduction
    /// sans tenir compte des dépenses réellement engagées.
    /// Exemple : déduire forfaitairement 30 % d'une base de revenus.
    /// </summary>
    PourcentageForfaitaire = 3,

    /// <summary>
    /// Valeur limite servant à déterminer si une règle fiscale s'applique,
    /// par exemple un seuil de revenus à partir duquel un impôt est exigible.
    /// </summary>
    Seuil = 4
}
