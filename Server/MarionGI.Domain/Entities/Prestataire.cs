using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Prestataire : BaseEntity
{
    public TypePrestataire TypePrestataire { get; set; }
        = TypePrestataire.PersonnePhysique;

    // Identité d'une personne physique ou nom du contact.
    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    // Renseignés principalement pour une entreprise.
    public string? NomEntreprise { get; set; }

    public string Specialite { get; set; } = string.Empty;

    public string Telephone { get; set; } = string.Empty;

    public string? Courriel { get; set; }

    public string? Adresse { get; set; }

    public string? Ville { get; set; }

    // Informations fiscales, selon le type de prestataire.
    public string? NumeroContribuable { get; set; }

    public string? Observations { get; set; }

    public bool Actif { get; set; } = true;

    // Cloisonnement par société dans MarionGI.
    public Guid SocieteId { get; set; }

    public Societe Societe { get; set; } = null!;

    // Historique des interventions.
    public ICollection<TicketIntervenant> Interventions { get; set; } = [];
}