using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class BienImmobilier : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public TypeBien Type { get; set; }

    public string Adresse { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string Quartier { get; set; } = string.Empty;

    public decimal Superficie { get; set; }

    public string Nom { get; set; } = string.Empty;

    public Guid SocieteId { get; set; }

    public Societe Societe { get; set; } = null!;

    public List<string> Photos { get; set; } = [];

    public ICollection<UniteLocative> UnitesLocatives { get; set; } = [];
}
