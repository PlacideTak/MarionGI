namespace MarionGI.Domain.Entities;

public class Societe : BaseEntity
{
    public string Nom { get; set; } = string.Empty;

    public string? NumeroEntreprise { get; set; }

    public string? Adresse { get; set; }

    public string? Ville { get; set; }

    public string? CodePostal { get; set; }

    public string? Telephone { get; set; }

    public string? Email { get; set; }

    public bool Actif { get; set; } = true;

    // Utilisateurs appartenant à la société
    public ICollection<Utilisateur> Utilisateurs { get; set; } = [];

    // Biens immobiliers appartenant à ou gérés par la société
    public ICollection<BienImmobilier> BiensImmobiliers { get; set; } = [];
}