namespace MarionGI.Domain.Entities;

public class TicketIntervenant : BaseEntity
{
    public Guid TicketIncidentId { get; set; }

    public TicketIncident TicketIncident { get; set; } = null!;

    // Prestataire affecté au ticket.
    public Guid PrestataireId { get; set; }

    public Prestataire Prestataire { get; set; } = null!;

    // Informations propres à cette affectation.
    public decimal? MontantDevis { get; set; }

    public decimal? MontantFacture { get; set; }

    public string? ReferenceDevis { get; set; }

    public string? ReferenceFacture { get; set; }

    public DateTime? DateAssignation { get; set; }

    public DateTime? DateIntervention { get; set; }

    public DateTime? DateFinIntervention { get; set; }

    public string? Observations { get; set; }
}
