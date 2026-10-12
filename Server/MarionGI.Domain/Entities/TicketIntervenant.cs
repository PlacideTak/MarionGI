namespace MarionGI.Domain.Entities;

public class TicketIntervenant : BaseEntity
{
    public Guid TicketIncidentId { get; set; }

    public TicketIncident TicketIncident { get; set; } = null!;

    public string NomArtisan { get; set; } = string.Empty;

    public string Specialite { get; set; } = string.Empty;

    public string Telephone { get; set; } = string.Empty;
    public string Courriel { get; set; } = string.Empty;

    public decimal? MontantDevis { get; set; }

    public decimal? MontantFacture { get; set; }

    public DateTime? DateAssignation { get; set; }

    public DateTime? DateIntervention { get; set; }
}
