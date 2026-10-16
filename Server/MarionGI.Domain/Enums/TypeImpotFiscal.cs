namespace MarionGI.Domain.Enums;

public enum TypeImpotFiscal
{
    NonDefini = 0,

    // Imposition liée à la propriété immobilière.
    ProprieteFonciere = 1,

    // Imposition des revenus issus de la location.
    RevenusFonciers = 2,

    // Précompte ou retenue applicable aux loyers.
    PrecompteSurLoyers = 3,

    // IRPP, uniquement lorsque les règles applicables le prévoient.
    IRPP = 4,

    // Imposition des bénéfices d'une société.
    ImpotSocietes = 5,

    PlusValueImmobiliere = 6,

    DroitsEnregistrement = 7,

    TVA = 8,

    Autre = 99
}
