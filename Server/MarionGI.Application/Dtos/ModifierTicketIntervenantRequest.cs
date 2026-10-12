namespace MarionGI.Application.Dtos;

public class ModifierTicketIntervenantRequest
{
    public string NomArtisan { get; set; } = string.Empty;
    public string Specialite { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? Courriel { get; set; }
    public decimal? MontantDevis { get; set; }
    public decimal? MontantFacture { get; set; }
    public DateTime? DateIntervention { get; set; }
}

