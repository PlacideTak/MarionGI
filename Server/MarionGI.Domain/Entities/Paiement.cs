using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Paiement : BaseEntity
{
    public Guid ContratId { get; set; }
    public Contrat Contrat { get; set; } = null!;
    public decimal Montant { get; set; }
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;
    public ModePaiement ModePaiement { get; set; }
    public StatutTransaction StatutTransaction { get; set; } = StatutTransaction.EnAttente;
    public string? ReferenceTransactionOperateur { get; set; }
    public string NumeroQuittance { get; set; } = string.Empty; // Format: Q-YYYYMMDD-XXXX
    public Guid? EnregistreParUtilisateurId { get; set; }

}
