using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Paiement : BaseEntity
{
    // Contrat concerné
    public Guid ContratId { get; set; }
    public Contrat Contrat { get; set; } = null!;

    // Informations financières
    public decimal Montant { get; set; }

    // Date réelle du paiement ou de son enregistrement,
    // selon la convention retenue dans l'application
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;

    public ModePaiement ModePaiement { get; set; }

    // État de la transaction
    public StatutTransaction StatutTransaction { get; set; }
        = StatutTransaction.EnAttente;

    // Référence fournie par l'opérateur de paiement
    public string? ReferenceTransactionOperateur { get; set; }

    // Numéro unique de quittance
    // Exemple : Q-20261013-0001
    public string NumeroQuittance { get; set; } = string.Empty;

    // Utilisateur ayant enregistré le paiement, si applicable
    public Guid? EnregistreParUtilisateurId { get; set; }

    // Premier jour du mois de loyer concerné
    // Exemple : 2026-10-01 pour le loyer d'octobre 2026
    public DateTime MoisLoyer { get; set; }
}