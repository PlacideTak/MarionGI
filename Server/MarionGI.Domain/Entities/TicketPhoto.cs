using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class TicketPhoto : BaseEntity
{
    public Guid TicketIncidentId { get; set; }

    public TicketIncident TicketIncident { get; set; } = null!;

    public string UrlPhoto { get; set; } = string.Empty;

    public TypePhotoIncident Type { get; set; }

    public Guid? AjouteParUtilisateurId { get; set; }

    public DateTime DateAjout { get; set; } = DateTime.UtcNow;
}