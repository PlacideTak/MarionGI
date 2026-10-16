
using MarionGI.Domain.Enums;

namespace MarionGI.Domain.Entities;

public class Paiement : BaseEntity
{
    // Contrat concerné.
    public Guid ContratId { get; set; }

    public Contrat Contrat { get; set; } = null!;

    // Période locative concernée.
    // Convention : premier jour du mois, par exemple 2026-10-01.
    public DateTime MoisLoyer { get; set; }

    // Montant total exigible pour cette échéance,
    // enregistré comme référence au moment de son établissement.
    public decimal MontantDu { get; set; }

    // Montant de cette transaction.
    // Permet notamment de gérer les paiements partiels.
    public decimal Montant { get; set; }

    // Date effective du paiement, selon le moyen de paiement.
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;

    // Date à laquelle la transaction a été confirmée,
    // si une confirmation distincte est nécessaire.
    public DateTime? DateConfirmation { get; set; }

    public ModePaiement ModePaiement { get; set; }

    // État de la transaction.
    public StatutTransaction StatutTransaction { get; set; }
        = StatutTransaction.EnAttente;

    // Références de paiement.
    public string? ReferenceTransactionOperateur { get; set; }

    public string NumeroQuittance { get; set; } = string.Empty;

    // Frais éventuellement prélevés par l'opérateur.
    public decimal? FraisTransaction { get; set; }

    // Informations sur un échec ou une anomalie.
    public string? MotifEchec { get; set; }

    public string? Observations { get; set; }

    // Utilisateur ayant enregistré le paiement.
    public Guid? EnregistreParUtilisateurId { get; set; }

    // Premier jour du mois de loyer concerné.
    // DateCreation, héritée de BaseEntity, conserve
    // la date de création de l'enregistrement.
}
