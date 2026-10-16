
using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class TicketPhoto : BaseEntity
{
    public Guid TicketIncidentId { get; set; }

    public TicketIncident TicketIncident { get; set; } = null!;

    // Chemin ou URL interne du fichier stocké.
    public string UrlPhoto { get; set; } = string.Empty;

    // Nature de la photo : avant travaux, après travaux, etc.
    public TypePhotoIncident Type { get; set; }

    // Nom d'origine du fichier, facultatif.
    public string? NomFichier { get; set; }

    // Utilisateur ayant ajouté la photo.
    public Guid? AjouteParUtilisateurId { get; set; }

    public DateTime DateAjout { get; set; } = DateTime.UtcNow;

    // Description facultative de la photo.
    public string? Description { get; set; }
}
