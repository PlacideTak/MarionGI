using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Societe : BaseEntity
{
    // Identifie si le propriétaire est un particulier ou une société.
    public TypePersonne TypePersonne { get; set; }
        = TypePersonne.PersonneMorale;

    // Identité du propriétaire.
    // Pour une personne physique : nom et prénom.
    // Pour une personne morale : dénomination sociale dans Nom.
    public string Nom { get; set; } = string.Empty;

    public string? Prenom { get; set; }

    // Informations juridiques de la personne morale.
    // Exemple : SARL, SA, SAS, selon la forme juridique applicable.
    public FormeJuridique? FormeJuridique { get; set; }

    // Identifiants administratifs et fiscaux.
    public string? NumeroEntreprise { get; set; }

    public string? NumeroContribuable { get; set; }

    // Coordonnées administratives du propriétaire.
    public string? Adresse { get; set; }

    public string? AdresseFiscale { get; set; }

    public string? Ville { get; set; }

    public string? Region { get; set; }

    public string? Departement { get; set; }

    public string? CodePostal { get; set; }

    public string? Pays { get; set; } = "Cameroun";

    // Informations complémentaires pour le suivi fiscal.
    public string? CentreImpots { get; set; }

    public string? ActivitePrincipale { get; set; }

    // Coordonnées de contact.
    public string? Telephone { get; set; }

    public string? Email { get; set; }

    // Statut du compte dans MarionGI.
    public bool Actif { get; set; } = true;

    // Utilisateurs autorisés à accéder à ce compte.
    public ICollection<Utilisateur> Utilisateurs { get; set; }
        = new List<Utilisateur>();

    // Biens immobiliers associés à ce propriétaire/compte.
    public ICollection<BienImmobilier> BiensImmobiliers { get; set; }
        = new List<BienImmobilier>();

    // Déclarations fiscales de ce propriétaire/compte.
    public ICollection<DeclarationFiscale> DeclarationsFiscales { get; set; }
        = new List<DeclarationFiscale>();
}
