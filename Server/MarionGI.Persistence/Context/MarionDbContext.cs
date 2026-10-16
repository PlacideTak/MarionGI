
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarionGI.Persistence.Context;

public class MarionDbContext : DbContext
{
    public MarionDbContext(DbContextOptions<MarionDbContext> options)
        : base(options)
    {
    }

    // ============================================================
    // 1. DbSets
    // ============================================================

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Societe> Societes => Set<Societe>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<BienImmobilier> BiensImmobiliers =>
        Set<BienImmobilier>();

    public DbSet<UniteLocative> UnitesLocatives =>
        Set<UniteLocative>();

    public DbSet<Contrat> Contrats => Set<Contrat>();
    public DbSet<Paiement> Paiements => Set<Paiement>();

    public DbSet<DemandeVisite> DemandesVisite =>
        Set<DemandeVisite>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

    public DbSet<JournalAudit> JournauxAudit =>
        Set<JournalAudit>();

    public DbSet<TicketIncident> TicketsIncidents =>
        Set<TicketIncident>();

    public DbSet<TicketIntervenant> TicketsIntervenants =>
        Set<TicketIntervenant>();

    public DbSet<TicketPhoto> TicketsPhotos =>
        Set<TicketPhoto>();

    public DbSet<Prestataire> Prestataires =>
        Set<Prestataire>();

    // Fiscalité

    public DbSet<ChargeBien> ChargesBiens =>
        Set<ChargeBien>();

    public DbSet<TrancheIRPP> TranchesIRPP =>
        Set<TrancheIRPP>();

    public DbSet<ParametreFiscal> ParametresFiscaux =>
        Set<ParametreFiscal>();

    public DbSet<RegimeFiscal> RegimesFiscaux =>
        Set<RegimeFiscal>();

    public DbSet<ValeurFiscaleBien> ValeursFiscalesBiens =>
        Set<ValeurFiscaleBien>();

    public DbSet<DeclarationFiscale> DeclarationsFiscales =>
    Set<DeclarationFiscale>();

    // ============================================================
    // 2. Conventions EF Core
    // ============================================================

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTime>()
            .HaveColumnType("datetime");

        configurationBuilder
            .Properties<DateTime?>()
            .HaveColumnType("datetime");
    }

    // ============================================================
    // 3. Configuration du modèle
    // ============================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========================================================
        // 3.1. SOCIETE
        // ========================================================

        modelBuilder.Entity<Societe>(entity =>
        {
            entity.HasQueryFilter(s => !s.EstSupprime);

            entity.Property(s => s.Nom)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(s => s.NumeroEntreprise)
                .HasMaxLength(100);

            entity.Property(s => s.NumeroContribuable)
                .HasMaxLength(100);

            entity.Property(s => s.Adresse)
                .HasMaxLength(250);

            entity.Property(s => s.AdresseFiscale)
                .HasMaxLength(250);

            entity.Property(s => s.Ville)
                .HasMaxLength(100);

            entity.Property(s => s.Region)
                .HasMaxLength(100);

            entity.Property(s => s.Departement)
                .HasMaxLength(100);

            entity.Property(s => s.CodePostal)
                .HasMaxLength(20);

            entity.Property(s => s.Pays)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(s => s.CentreImpots)
                .HasMaxLength(200);

            entity.Property(s => s.ActivitePrincipale)
                .HasMaxLength(250);

            entity.Property(s => s.Telephone)
                .HasMaxLength(30);

            entity.Property(s => s.Email)
                .HasMaxLength(200);

            entity.Property(s => s.Actif)
                .HasDefaultValue(true);

            entity.HasMany(s => s.Utilisateurs)
                .WithOne(u => u.Societe)
                .HasForeignKey(u => u.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.BiensImmobiliers)
                .WithOne(b => b.Societe)
                .HasForeignKey(b => b.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.NumeroContribuable)
                .HasDatabaseName(
                    "IX_Societes_NumeroContribuable");
        });

        // ========================================================
        // 3.2. UTILISATEUR
        // ========================================================

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasQueryFilter(u => !u.EstSupprime);

            entity.Property(u => u.Nom)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Prenom)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Telephone)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(u => u.MotDePasseHash)
                .IsRequired();

            entity.Property(u => u.OtpSecret)
                .HasMaxLength(500);

            entity.Property(u => u.Statut)
                .HasDefaultValue(true);

            entity.Property(u => u.TelephoneVerifie)
                .HasDefaultValue(false);

            entity.Property(u => u.TentativesConnexionEchouees)
                .HasDefaultValue(0);

            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Email
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Utilisateurs_SocieteId_Email")
                .HasFilter("[EstSupprime] = 0");

            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Telephone
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Utilisateurs_SocieteId_Telephone")
                .HasFilter("[EstSupprime] = 0");

            entity.HasOne(u => u.Societe)
                .WithMany(s => s.Utilisateurs)
                .HasForeignKey(u => u.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.3. REFRESH TOKEN
        // ========================================================

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasQueryFilter(r => !r.EstSupprime);

            entity.Property(r => r.Token)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(r => r.DateExpiration)
                .IsRequired();

            entity.Property(r => r.EstRevoque)
                .HasDefaultValue(false);

            entity.HasIndex(r => r.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            entity.HasOne(r => r.Utilisateur)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.4. BIEN IMMOBILIER
        // ========================================================

        modelBuilder.Entity<BienImmobilier>(entity =>
        {
            entity.HasQueryFilter(b => !b.EstSupprime);

            entity.Property(b => b.Reference)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(b => b.Nom)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(b => b.Adresse)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(b => b.Ville)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(b => b.Quartier)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(b => b.Region)
                .HasMaxLength(100);

            entity.Property(b => b.Departement)
                .HasMaxLength(100);

            entity.Property(b => b.Arrondissement)
                .HasMaxLength(100);

            entity.Property(b => b.NumeroTitreFoncier)
                .HasMaxLength(100);

            entity.Property(b => b.NumeroLot)
                .HasMaxLength(100);

            entity.Property(b => b.NumeroParcelle)
                .HasMaxLength(100);

            entity.Property(b => b.ReferenceCadastrale)
                .HasMaxLength(150);

            entity.Property(b => b.Superficie)
                .HasPrecision(10, 2);

            entity.Property(b => b.ValeurAcquisition)
                .HasPrecision(18, 2);

            entity.Property(b => b.ValeurEstimee)
                .HasPrecision(18, 2);

            entity.Property(b => b.Type)
                .IsRequired();

            entity.Property(b => b.StatutFoncier)
                .IsRequired(false);

            entity.HasIndex(b => new
            {
                b.SocieteId,
                b.Reference
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_BiensImmobiliers_SocieteId_Reference")
                .HasFilter("[EstSupprime] = 0");

            entity.HasIndex(b => new
            {
                b.SocieteId,
                b.Nom
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_BiensImmobiliers_SocieteId_Nom")
                .HasFilter("[EstSupprime] = 0");

            entity.Property(b => b.Photos)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v,
                        (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(
                            v,
                            (JsonSerializerOptions?)null)
                          ?? new List<string>());

            entity.HasOne(b => b.Societe)
                .WithMany(s => s.BiensImmobiliers)
                .HasForeignKey(b => b.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.UnitesLocatives)
                .WithOne(u => u.BienImmobilier)
                .HasForeignKey(u => u.BienImmobilierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.Charges)
                .WithOne(c => c.BienImmobilier)
                .HasForeignKey(c => c.BienImmobilierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.5. UNITE LOCATIVE
        // ========================================================

        modelBuilder.Entity<UniteLocative>(entity =>
        {
            entity.HasQueryFilter(u => !u.EstSupprime);

            entity.Property(u => u.Reference)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Superficie)
                .HasPrecision(10, 2);

            entity.Property(u => u.Loyer)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(u => u.ChargesLocativesMensuelles)
                .HasPrecision(18, 2);

            entity.Property(u => u.MontantCaution)
                .HasPrecision(18, 2);

            entity.Property(u => u.MotifExoneration)
                .HasMaxLength(1000);

            entity.HasIndex(u => new
            {
                u.BienImmobilierId,
                u.Reference
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_UnitesLocatives_BienImmobilierId_Reference")
                .HasFilter("[EstSupprime] = 0");

            entity.Property(u => u.Photos)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v,
                        (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(
                            v,
                            (JsonSerializerOptions?)null)
                          ?? new List<string>());

            entity.HasOne(u => u.BienImmobilier)
                .WithMany(b => b.UnitesLocatives)
                .HasForeignKey(u => u.BienImmobilierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Contrats)
                .WithOne(c => c.UniteLocative)
                .HasForeignKey(c => c.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.DemandesVisite)
                .WithOne(d => d.UniteLocative)
                .HasForeignKey(d => d.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.Charges)
                .WithOne(c => c.UniteLocative)
                .HasForeignKey(c => c.UniteLocativeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.6. CONTRAT
        // ========================================================

        modelBuilder.Entity<Contrat>(entity =>
        {
            entity.HasQueryFilter(c => !c.EstSupprime);

            entity.Property(c => c.Reference)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(c => c.Reference)
                .IsUnique()
                .HasDatabaseName("IX_Contrats_Reference")
                .HasFilter("[EstSupprime] = 0");

            entity.Property(c => c.DateDebut)
                .IsRequired();

            entity.Property(c => c.DateFin)
                .IsRequired();

            entity.Property(c => c.MontantLoyer)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(c => c.MontantCaution)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(c => c.MontantChargesLocatives)
                .HasPrecision(18, 2);

            entity.Property(c => c.TauxRevisionLoyer)
                .HasPrecision(9, 6);

            entity.Property(c => c.FrequencePaiement)
                .IsRequired()
                .HasDefaultValue(FrequencePaiement.Mensuel);

            entity.Property(c => c.DelaiJoursTolerance)
                .IsRequired()
                .HasDefaultValue(5);

            entity.Property(c => c.Statut)
                .IsRequired()
                .HasDefaultValue(StatutContrat.Actif);

            entity.Property(c => c.CheminDocumentContrat)
                .HasMaxLength(1000);

            entity.Property(c => c.Observations)
                .HasMaxLength(4000);

            entity.HasOne(c => c.UniteLocative)
                .WithMany(u => u.Contrats)
                .HasForeignKey(c => c.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Locataire)
                .WithMany(u => u.ContratsLocataire)
                .HasForeignKey(c => c.LocataireId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.7. PAIEMENT
        // ========================================================

        modelBuilder.Entity<Paiement>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.MontantDu)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.Montant)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.FraisTransaction)
                .HasPrecision(18, 2);

            entity.Property(p => p.DatePaiement)
                .IsRequired();

            entity.Property(p => p.MoisLoyer)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(p => p.ReferenceTransactionOperateur)
                .HasMaxLength(200);

            entity.Property(p => p.NumeroQuittance)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(p => p.MotifEchec)
                .HasMaxLength(1000);

            entity.Property(p => p.Observations)
                .HasMaxLength(2000);

            entity.Property(p => p.ModePaiement)
                .IsRequired();

            entity.Property(p => p.StatutTransaction)
                .IsRequired()
                .HasDefaultValue(StatutTransaction.EnAttente);

            entity.HasIndex(p => p.ContratId)
                .HasDatabaseName("IX_Paiements_ContratId");

            entity.HasIndex(p => new
            {
                p.ContratId,
                p.MoisLoyer
            })
                .HasDatabaseName(
                    "IX_Paiements_ContratId_MoisLoyer");

            entity.HasIndex(p => p.NumeroQuittance)
                .IsUnique()
                .HasDatabaseName("IX_Paiements_NumeroQuittance")
                .HasFilter("[EstSupprime] = 0");

            entity.HasIndex(p => p.ReferenceTransactionOperateur)
                .IsUnique()
                .HasDatabaseName(
                    "IX_Paiements_ReferenceTransactionOperateur")
                .HasFilter(
                    "[EstSupprime] = 0 AND " +
                    "[ReferenceTransactionOperateur] IS NOT NULL");

            entity.HasOne(p => p.Contrat)
                .WithMany(c => c.Paiements)
                .HasForeignKey(p => p.ContratId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.8. CHARGE IMMOBILIERE
        // ========================================================

        modelBuilder.Entity<ChargeBien>(entity =>
        {
            entity.ToTable("ChargesBiens");

            entity.HasQueryFilter(c => !c.EstSupprime);

            entity.Property(c => c.DateDepense)
                .IsRequired();

            entity.Property(c => c.TypeCharge)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(c => c.NomFournisseur)
                .HasMaxLength(200);

            entity.Property(c => c.NumeroContribuableFournisseur)
                .HasMaxLength(100);

            entity.Property(c => c.MontantTTC)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(c => c.MontantHT)
                .HasPrecision(18, 2);

            entity.Property(c => c.MontantTVA)
                .HasPrecision(18, 2);

            entity.Property(c => c.ReferenceFacture)
                .HasMaxLength(100);

            entity.Property(c => c.CheminJustificatif)
                .HasMaxLength(1000);

            entity.Property(c => c.EstDeductibleFiscalement)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(c => c.JustificationNonDeductibilite)
                .HasMaxLength(1000);

            entity.Property(c => c.Observations)
                .HasMaxLength(2000);

            entity.HasIndex(c => new
            {
                c.BienImmobilierId,
                c.DateDepense
            })
                .HasDatabaseName(
                    "IX_ChargesBiens_BienImmobilierId_DateDepense");

            entity.HasIndex(c => c.UniteLocativeId)
                .HasDatabaseName(
                    "IX_ChargesBiens_UniteLocativeId");

            entity.HasOne(c => c.BienImmobilier)
                .WithMany(b => b.Charges)
                .HasForeignKey(c => c.BienImmobilierId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.UniteLocative)
                .WithMany(u => u.Charges)
                .HasForeignKey(c => c.UniteLocativeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });



        // ========================================================
        // VALEUR FISCALE DU BIEN IMMOBILIER
        // ========================================================

        modelBuilder.Entity<ValeurFiscaleBien>(entity =>
        {
            entity.ToTable("ValeursFiscalesBiens", table =>
            {
                table.HasCheckConstraint(
                    "CK_ValeursFiscalesBiens_Montant",
                    "[Montant] > 0");

                table.HasCheckConstraint(
                    "CK_ValeursFiscalesBiens_Dates",
                    "[DateValidationOfficielle] IS NULL " +
                    "OR [DateValidationOfficielle] >= [DateEvaluation]");
            });


            entity.HasQueryFilter(v => !v.EstSupprime);

            entity.Property(v => v.Montant)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(v => v.TypeValeur)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(v => v.DateEvaluation)
                .IsRequired();

            entity.Property(v => v.ReferenceJustificative)
                .HasMaxLength(500);

            entity.Property(v => v.CheminJustificatif)
                .HasMaxLength(1000);

            entity.Property(v => v.Observations)
                .HasMaxLength(2000);

            // Recherche par bien et année fiscale.
            entity.HasIndex(v => new
            {
                v.BienImmobilierId,
                v.AnneeFiscale,
                v.TypeValeur
            })
            .HasDatabaseName(
                "IX_ValeursFiscalesBiens_Bien_Annee_Type");

            entity.HasOne(v => v.BienImmobilier)
                .WithMany(b => b.ValeursFiscales)
                .HasForeignKey(v => v.BienImmobilierId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 3.9. DEMANDE DE VISITE
        // ========================================================

        modelBuilder.Entity<DemandeVisite>(entity =>
        {
            entity.HasQueryFilter(d => !d.EstSupprime);

            entity.Property(d => d.NomProspect)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(d => d.TelephoneProspect)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(d => d.EmailProspect)
                .HasMaxLength(200);

            entity.Property(d => d.Observations)
                .HasMaxLength(1000);

            entity.Property(d => d.CommentaireInterne)
                .HasMaxLength(2000);

            entity.Property(d => d.DateSouhaitee)
                .IsRequired();

            entity.Property(d => d.Statut)
                .IsRequired()
                .HasDefaultValue(StatutDemandeVisite.EnAttente);

            entity.HasIndex(d => new
            {
                d.UniteLocativeId,
                d.DateSouhaitee
            })
                .HasDatabaseName(
                    "IX_DemandesVisite_UniteLocativeId_DateSouhaitee");

            entity.HasOne(d => d.UniteLocative)
                .WithMany(u => u.DemandesVisite)
                .HasForeignKey(d => d.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Agent)
                .WithMany()
                .HasForeignKey(d => d.AgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.10. NOTIFICATION
        // ========================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasQueryFilter(n => !n.EstSupprime);

            entity.Property(n => n.Type)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(n => n.Message)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(n => n.Lu)
                .HasDefaultValue(false);

            entity.HasOne(n => n.Utilisateur)
                .WithMany()
                .HasForeignKey(n => n.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.11. JOURNAL D'AUDIT
        // ========================================================

        modelBuilder.Entity<JournalAudit>(entity =>
        {
            entity.Property(j => j.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(j => j.Entite)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(j => j.EntiteId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(j => j.DetailsJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
        });

        // ========================================================
        // 3.12. TICKET INCIDENT
        // ========================================================

        modelBuilder.Entity<TicketIncident>(entity =>
        {
            entity.ToTable("TicketsIncidents");

            entity.HasQueryFilter(t => !t.EstSupprime);

            entity.Property(t => t.Reference)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(t => t.Reference)
                .IsUnique()
                .HasDatabaseName("IX_TicketsIncidents_Reference")
                .HasFilter("[EstSupprime] = 0");

            entity.Property(t => t.ResultatReparation)
                .HasMaxLength(4000);

            entity.Property(t => t.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(t => t.DateIncident)
                .IsRequired();

            entity.Property(t => t.Priorite)
                .IsRequired()
                .HasDefaultValue(PrioriteIncident.Moyenne);

            entity.Property(t => t.Statut)
                .IsRequired()
                .HasDefaultValue(StatutIncident.Ouvert);

            entity.Property(t => t.CoutEstime)
                .HasPrecision(18, 2);

            entity.Property(t => t.CoutTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(t => t.ImputableA)
                .IsRequired()
                .HasDefaultValue(
                    ImputabiliteIncident.Indeterminee);

            entity.Property(t => t.ValidationDevisRequise)
                .HasDefaultValue(false);

            entity.Property(t => t.DevisValide)
                .HasDefaultValue(false);

            entity.HasIndex(t => new
            {
                t.BienImmobilierId,
                t.Statut
            })
                .HasDatabaseName(
                    "IX_TicketsIncidents_BienImmobilierId_Statut");

            entity.HasIndex(t => new
            {
                t.UniteLocativeId,
                t.Statut
            })
                .HasDatabaseName(
                    "IX_TicketsIncidents_UniteLocativeId_Statut");

            entity.HasIndex(t => t.LocataireId)
                .HasDatabaseName(
                    "IX_TicketsIncidents_LocataireId");

            entity.HasIndex(t => t.DeclarantId)
                .HasDatabaseName(
                    "IX_TicketsIncidents_DeclarantId");

            entity.HasOne(t => t.BienImmobilier)
                .WithMany()
                .HasForeignKey(t => t.BienImmobilierId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.UniteLocative)
                .WithMany()
                .HasForeignKey(t => t.UniteLocativeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Locataire)
                .WithMany()
                .HasForeignKey(t => t.LocataireId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Declarant)
                .WithMany()
                .HasForeignKey(t => t.DeclarantId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ValideParUtilisateur)
                .WithMany()
                .HasForeignKey(t => t.ValideParUtilisateurId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(t => t.Photos)
                .WithOne(p => p.TicketIncident)
                .HasForeignKey(p => p.TicketIncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(t => t.Intervenants)
                .WithOne(i => i.TicketIncident)
                .HasForeignKey(i => i.TicketIncidentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.13. PRESTATAIRE
        // ========================================================

        modelBuilder.Entity<Prestataire>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.TypePrestataire)
                .IsRequired()
                .HasDefaultValue(TypePrestataire.PersonnePhysique);

            entity.Property(p => p.Nom)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(p => p.Prenom)
                .HasMaxLength(150);

            entity.Property(p => p.NomEntreprise)
                .HasMaxLength(200);

            entity.Property(p => p.Specialite)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Telephone)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.Courriel)
                .HasMaxLength(200);

            entity.Property(p => p.Adresse)
                .HasMaxLength(250);

            entity.Property(p => p.Ville)
                .HasMaxLength(100);

            entity.Property(p => p.NumeroContribuable)
                .HasMaxLength(100);

            entity.Property(p => p.Observations)
                .HasMaxLength(2000);

            entity.Property(p => p.Actif)
                .HasDefaultValue(true);

            entity.HasIndex(p => new
            {
                p.SocieteId,
                p.Nom
            })
                .HasDatabaseName(
                    "IX_Prestataires_SocieteId_Nom")
                .HasFilter("[EstSupprime] = 0");

            entity.HasIndex(p => new
            {
                p.SocieteId,
                p.Specialite
            })
                .HasDatabaseName(
                    "IX_Prestataires_SocieteId_Specialite")
                .HasFilter("[EstSupprime] = 0");

            entity.HasOne(p => p.Societe)
                .WithMany()
                .HasForeignKey(p => p.SocieteId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Interventions)
                .WithOne(i => i.Prestataire)
                .HasForeignKey(i => i.PrestataireId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.14. TICKET INTERVENANT
        // ========================================================

        modelBuilder.Entity<TicketIntervenant>(entity =>
        {
            entity.HasQueryFilter(i => !i.EstSupprime);

            entity.Property(i => i.MontantDevis)
                .HasPrecision(18, 2);

            entity.Property(i => i.MontantFacture)
                .HasPrecision(18, 2);

            entity.Property(i => i.ReferenceDevis)
                .HasMaxLength(100);

            entity.Property(i => i.ReferenceFacture)
                .HasMaxLength(100);

            entity.Property(i => i.Observations)
                .HasMaxLength(2000);

            entity.HasIndex(i => i.TicketIncidentId)
                .HasDatabaseName(
                    "IX_TicketsIntervenants_TicketIncidentId");

            entity.HasIndex(i => new
            {
                i.TicketIncidentId,
                i.PrestataireId
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_TicketsIntervenants_TicketIncidentId_PrestataireId")
                .HasFilter("[EstSupprime] = 0");

            entity.HasOne(i => i.TicketIncident)
                .WithMany(t => t.Intervenants)
                .HasForeignKey(i => i.TicketIncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Prestataire)
                .WithMany(p => p.Interventions)
                .HasForeignKey(i => i.PrestataireId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.15. TICKET PHOTO
        // ========================================================

        modelBuilder.Entity<TicketPhoto>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.UrlPhoto)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(p => p.NomFichier)
                .HasMaxLength(255);

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.Type)
                .IsRequired();

            entity.Property(p => p.DateAjout)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => new
            {
                p.TicketIncidentId,
                p.DateAjout
            })
                .HasDatabaseName(
                    "IX_TicketsPhotos_TicketIncidentId_DateAjout");

            entity.HasOne(p => p.TicketIncident)
                .WithMany(t => t.Photos)
                .HasForeignKey(p => p.TicketIncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Utilisateur>()
                .WithMany()
                .HasForeignKey(p => p.AjouteParUtilisateurId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 3.16. TRANCHE IRPP
        // ========================================================

        modelBuilder.Entity<TrancheIRPP>(entity =>
        {
            entity.ToTable("TranchesIRPP");

            entity.HasQueryFilter(t => !t.EstSupprime);

            entity.Property(t => t.SeuilMin)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(t => t.SeuilMax)
                .HasPrecision(18, 2);

            // Convention : 0.10 représente 10 %.
            entity.Property(t => t.Taux)
                .HasPrecision(9, 6)
                .IsRequired();

            entity.Property(t => t.OrdreTranche)
                .IsRequired();

            entity.Property(t => t.Description)
                .HasMaxLength(500);

            entity.Property(t => t.ReferenceLegale)
                .HasMaxLength(500);

            entity.HasIndex(t => new
            {
                t.AnneeFiscale,
                t.OrdreTranche
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_TranchesIRPP_AnneeFiscale_OrdreTranche")
                .HasFilter("[EstSupprime] = 0");
        });

        // ========================================================
        // 3.17. PARAMETRE FISCAL
        // ========================================================

        modelBuilder.Entity<ParametreFiscal>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.CodeRegle)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(p => p.Valeur)
                .HasPrecision(18, 4);

            entity.Property(p => p.TypeImpot)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(p => p.TypeParametre)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(p => p.ReferenceLegale)
                .HasMaxLength(500);

            entity.Property(p => p.ConditionsApplication)
                .HasMaxLength(2000);

            // Plusieurs versions d'une même règle peuvent exister.
            entity.HasIndex(p => new
            {
                p.AnneeFiscale,
                p.CodeRegle,
                p.RegimeFiscalId
            })
                .HasDatabaseName(
                    "IX_ParametresFiscaux_Annee_Code_Regime")
                .HasFilter("[EstSupprime] = 0");

            entity.HasOne(p => p.RegimeFiscal)
                .WithMany(r => r.ParametresFiscaux)
                .HasForeignKey(p => p.RegimeFiscalId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("ParametresFiscaux", table =>
            {
                table.HasCheckConstraint(
                    "CK_ParametresFiscaux_DatesValidite",
                    "[DateFinValidite] IS NULL OR " +
                    "[DateFinValidite] >= [DateDebutValidite]");
            });
        });


        modelBuilder.Entity<DeclarationFiscale>(entity =>
        {
            entity.ToTable("DeclarationsFiscales");

            entity.HasQueryFilter(d => !d.EstSupprime);

            entity.Property(d => d.TypeImpot)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(d => d.Statut)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(d => d.BaseImposable)
                .HasPrecision(18, 2);

            entity.Property(d => d.MontantImpot)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(d => d.ReferenceDeclaration)
                .HasMaxLength(200);

            entity.Property(d => d.Observations)
                .HasMaxLength(2000);

            entity.HasIndex(d => new
            {
                d.SocieteId,
                d.AnneeFiscale,
                d.TypeImpot
            })
                .HasDatabaseName(
                    "IX_DeclarationsFiscales_Societe_Annee_Type");

            entity.HasOne(d => d.Societe)
                .WithMany(s => s.DeclarationsFiscales)
                .HasForeignKey(d => d.SocieteId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.BienImmobilier)
                .WithMany(b => b.DeclarationsFiscales)
                .HasForeignKey(d => d.BienImmobilierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 3.18. REGIME FISCAL
        // ========================================================

        modelBuilder.Entity<RegimeFiscal>(entity =>
        {
            entity.HasQueryFilter(r => !r.EstSupprime);

            entity.Property(r => r.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(r => r.Libelle)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(r => r.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(r => r.ReferenceLegale)
                .HasMaxLength(500);

            entity.Property(r => r.EstActif)
                .HasDefaultValue(true);

            entity.HasIndex(r => r.Code)
                .IsUnique()
                .HasDatabaseName("IX_RegimesFiscaux_Code")
                .HasFilter("[EstSupprime] = 0");

            entity.HasMany(r => r.ParametresFiscaux)
                .WithOne(p => p.RegimeFiscal)
                .HasForeignKey(p => p.RegimeFiscalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================================
        // 4. COMPORTEMENT GLOBAL DES CLES ETRANGERES
        // ========================================================
        // Désactive les suppressions en cascade.
        // Les suppressions logiques restent gérées par l'application.
        // ========================================================

        foreach (var foreignKey in modelBuilder.Model
             .GetEntityTypes()
             .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
