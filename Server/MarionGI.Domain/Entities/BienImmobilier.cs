using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class BienImmobilier : BaseEntity
{
    // Identification du bien dans MarionGI.
    public string Reference { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public TypeBien Type { get; set; }

    // Localisation du bien.
    public string Adresse { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string Quartier { get; set; } = string.Empty;

    public string? Region { get; set; }

    public string? Departement { get; set; }

    public string? Arrondissement { get; set; }

    // Caractéristiques physiques.
    public decimal Superficie { get; set; }

    public int? AnneeConstruction { get; set; }

    public int? NombreEtages { get; set; }

    // Références administratives et foncières.
    public string? NumeroTitreFoncier { get; set; }

    public string? NumeroLot { get; set; }

    public string? NumeroParcelle { get; set; }

    public string? ReferenceCadastrale { get; set; }

    // Informations utiles à l'évaluation fiscale.
    // Ce sont des données déclarées ou estimées,
    // pas nécessairement la valeur officielle retenue par l'administration.
    public decimal? ValeurAcquisition { get; set; }

    public DateTime? DateAcquisition { get; set; }

    public decimal? ValeurEstimee { get; set; }

    public DateTime? DateDerniereEvaluation { get; set; }

    // Statut administratif du dossier foncier.
    public StatutFoncier? StatutFoncier { get; set; }

    // Propriétaire ou compte auquel le bien est rattaché.
    public Guid SocieteId { get; set; }

    public Societe Societe { get; set; } = null!;

    // Photos du bien.
    public List<string> Photos { get; set; } = [];

    // Unités locatives rattachées à ce bien.
    public ICollection<UniteLocative> UnitesLocatives { get; set; } = [];

    // Charges générales et charges des unités.
    public ICollection<ChargeBien> Charges { get; set; } = [];


    // Historique des valeurs fiscales et évaluations du bien.
    public ICollection<ValeurFiscaleBien> ValeursFiscales { get; set; }
        = new List<ValeurFiscaleBien>();

    // Déclarations fiscales associées à ce bien.
    public ICollection<DeclarationFiscale> DeclarationsFiscales { get; set; }
        = new List<DeclarationFiscale>();
}
