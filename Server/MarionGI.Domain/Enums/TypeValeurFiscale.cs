namespace MarionGI.Domain.Enums;

/// <summary>
/// Nature de la valeur enregistrée pour un bien immobilier.
/// </summary>
public enum TypeValeurFiscale
{
    /// <summary>
    /// Estimation indicative réalisée par le propriétaire
    /// ou enregistrée dans MarionGI.
    /// </summary>
    Estimee = 1,

    /// <summary>
    /// Valeur déclarée par le contribuable à l'administration.
    /// </summary>
    Declaree = 2,

    /// <summary>
    /// Valeur officiellement retenue par l'administration fiscale.
    /// </summary>
    Officielle = 3,

    /// <summary>
    /// Valeur issue d'une expertise immobilière documentée.
    /// </summary>
    Expertise = 4
}
