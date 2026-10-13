namespace MarionGI.Domain.Entities;

public class RegimeFiscal : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Libelle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool EstActif { get; set; } = true;
}
