using System.Text.Json;
using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

    // Fiscalité

    public DbSet<ChargeBien> ChargesBiens => Set<ChargeBien>();

    public DbSet<TrancheIRPP> TranchesIRPP =>
        Set<TrancheIRPP>();

    public DbSet<ParametreFiscal> ParametresFiscaux =>
        Set<ParametreFiscal>();

    public DbSet<RegimeFiscal> RegimesFiscaux =>
        Set<RegimeFiscal>();

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
        // 3.1. SOCIÉTÉ
        // ========================================================

        modelBuilder.Entity<Societe>(entity =>
        {
            entity.Property(s => s.Nom)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(s => s.NumeroEntreprise)
                .HasMaxLength(100);

            entity.Property(s => s.Adresse)
                .HasMaxLength(250);

            entity.Property(s => s.Ville)
                .HasMaxLength(100);

            entity.Property(s => s.CodePostal)
                .HasMaxLength(20);

            entity.Property(s => s.Telephone)
                .HasMaxLength(30);

            entity.Property(s => s.Email)
                .HasMaxLength(200);

            entity.HasMany(s => s.Utilisateurs)
                .WithOne(u => u.Societe)
                .HasForeignKey(u => u.SocieteId);

            entity.HasMany(s => s.BiensImmobiliers)
                .WithOne(b => b.Societe)
                .HasForeignKey(b => b.SocieteId);
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

            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Email
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Utilisateurs_SocieteId_Email");

            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Telephone
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Utilisateurs_SocieteId_Telephone");

            entity.HasOne(u => u.Societe)
                .WithMany(s => s.Utilisateurs)
                .HasForeignKey(u => u.SocieteId);
        });

        // ========================================================
        // 3.3. REFRESH TOKEN
        // ========================================================

        modelBuilder.Entity<RefreshToken>(entity =>
        {
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
                .HasForeignKey(r => r.UtilisateurId);
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

            entity.Property(b => b.Superficie)
                .HasPrecision(10, 2);

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
                .HasForeignKey(b => b.SocieteId);

            entity.HasMany(b => b.UnitesLocatives)
                .WithOne(u => u.BienImmobilier)
                .HasForeignKey(u => u.BienImmobilierId);

            // Toutes les charges du bien, y compris les charges
            // qui concernent une unité locative particulière.
            entity.HasMany(b => b.Charges)
                .WithOne(c => c.BienImmobilier)
                .HasForeignKey(c => c.BienImmobilierId)
                .IsRequired();
        });

        // ========================================================
        // 3.5. UNITÉ LOCATIVE
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
                .HasPrecision(18, 2);

            entity.HasIndex(u => new
            {
                u.BienImmobilierId,
                u.Reference
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_UnitesLocatives_BienImmobilierId_Reference");

            entity.HasOne(u => u.BienImmobilier)
                .WithMany(b => b.UnitesLocatives)
                .HasForeignKey(u => u.BienImmobilierId);

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

            entity.HasMany(u => u.Contrats)
                .WithOne(c => c.UniteLocative)
                .HasForeignKey(c => c.UniteLocativeId);

            entity.HasMany(u => u.DemandesVisite)
                .WithOne(d => d.UniteLocative)
                .HasForeignKey(d => d.UniteLocativeId);

            // Charges spécifiques à cette unité.
            entity.HasMany(u => u.Charges)
                .WithOne(c => c.UniteLocative)
                .HasForeignKey(c => c.UniteLocativeId)
                .IsRequired(false);
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
                .HasDatabaseName("IX_Contrats_Reference");

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

            entity.Property(c => c.FrequencePaiement)
                .IsRequired()
                .HasDefaultValue(FrequencePaiement.Mensuel);

            entity.Property(c => c.DelaiJoursTolerance)
                .IsRequired()
                .HasDefaultValue(5);

            entity.Property(c => c.Statut)
                .IsRequired()
                .HasDefaultValue(StatutContrat.Actif);

            entity.HasOne(c => c.UniteLocative)
                .WithMany(u => u.Contrats)
                .HasForeignKey(c => c.UniteLocativeId);

            entity.HasOne(c => c.Locataire)
                .WithMany(u => u.ContratsLocataire)
                .HasForeignKey(c => c.LocataireId);
        });

        // ========================================================
        // 3.7. PAIEMENT
        // ========================================================

        modelBuilder.Entity<Paiement>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.Montant)
                .HasPrecision(18, 2)
                .IsRequired();

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

            entity.Property(p => p.ModePaiement)
                .IsRequired();

            entity.Property(p => p.StatutTransaction)
                .IsRequired()
                .HasDefaultValue(StatutTransaction.EnAttente);

            entity.HasIndex(p => p.ContratId)
                .HasDatabaseName("IX_Paiements_ContratId");

            // Vérifier les valeurs numériques réelles de l'enum
            // StatutTransaction avant d'appliquer ces filtres.
            entity.HasIndex(p => new
            {
                p.ContratId,
                p.MoisLoyer
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Paiements_ContratId_MoisLoyer_EnAttente")
                .HasFilter(
                    "[EstSupprime] = 0 AND [StatutTransaction] = 1");

            entity.HasIndex(p => new
            {
                p.ContratId,
                p.MoisLoyer
            })
                .IsUnique()
                .HasDatabaseName(
                    "IX_Paiements_ContratId_MoisLoyer_Confirme")
                .HasFilter(
                    "[EstSupprime] = 0 AND [StatutTransaction] = 2");

            entity.HasIndex(p => p.NumeroQuittance)
                .IsUnique()
                .HasDatabaseName("IX_Paiements_NumeroQuittance");

            entity.HasOne(p => p.Contrat)
                .WithMany(c => c.Paiements)
                .HasForeignKey(p => p.ContratId);
        });

        // ========================================================
        // 3.8. CHARGE IMMOBILIÈRE
        // ========================================================

        modelBuilder.Entity<ChargeBien>(entity =>
        {
            entity.ToTable("ChargesBiens");

            entity.HasQueryFilter(c => !c.EstSupprime);

            entity.Property(c => c.DateDepense)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(c => c.TypeCharge)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(c => c.MontantTTC)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(c => c.MontantHT)
                .HasPrecision(18, 2);

            entity.Property(c => c.MontantTVA)
                .HasPrecision(18, 2);

            entity.Property(c => c.ReferenceFacture)
                .HasMaxLength(100);

            entity.Property(c => c.EstDeductibleFiscalement)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(c => c.JustificationNonDeductibilite)
                .HasMaxLength(1000);

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
                .IsRequired();

            entity.HasOne(c => c.UniteLocative)
                .WithMany(u => u.Charges)
                .HasForeignKey(c => c.UniteLocativeId)
                .IsRequired(false);
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

            entity.Property(d => d.Observations)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasOne(d => d.UniteLocative)
                .WithMany(u => u.DemandesVisite)
                .HasForeignKey(d => d.UniteLocativeId);

            entity.HasOne(d => d.Agent)
                .WithMany()
                .HasForeignKey(d => d.AgentId);
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
                .HasForeignKey(n => n.UtilisateurId);
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

            // UtilisateurId reste sans navigation EF explicite.
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
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(t => t.DateResolution)
                .HasColumnType("datetime");

            entity.Property(t => t.DateValidationDevis)
                .HasColumnType("datetime");

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

            // Le bien est obligatoire pour chaque incident.
            entity.HasOne(t => t.BienImmobilier)
                .WithMany()
                .HasForeignKey(t => t.BienImmobilierId)
                .IsRequired();

            // L'unité est facultative pour les incidents généraux.
            entity.HasOne(t => t.UniteLocative)
                .WithMany()
                .HasForeignKey(t => t.UniteLocativeId)
                .IsRequired(false);

            entity.HasOne(t => t.Locataire)
                .WithMany()
                .HasForeignKey(t => t.LocataireId)
                .IsRequired();

            entity.HasOne(t => t.ValideParUtilisateur)
                .WithMany()
                .HasForeignKey(t => t.ValideParUtilisateurId)
                .IsRequired(false);

            entity.HasMany(t => t.Photos)
                .WithOne(p => p.TicketIncident)
                .HasForeignKey(p => p.TicketIncidentId);

            entity.HasMany(t => t.Intervenants)
                .WithOne(i => i.TicketIncident)
                .HasForeignKey(i => i.TicketIncidentId);
        });

        // ========================================================
        // 3.13. TICKET INTERVENANT
        // ========================================================

        modelBuilder.Entity<TicketIntervenant>(entity =>
        {
            entity.HasQueryFilter(i => !i.EstSupprime);

            entity.Property(i => i.NomArtisan)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(i => i.Specialite)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(i => i.Telephone)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(i => i.MontantDevis)
                .HasPrecision(18, 2);

            entity.Property(i => i.MontantFacture)
                .HasPrecision(18, 2);

            entity.HasIndex(i => i.TicketIncidentId)
                .HasDatabaseName(
                    "IX_TicketsIntervenants_TicketIncidentId");

            entity.HasOne(i => i.TicketIncident)
                .WithMany(t => t.Intervenants)
                .HasForeignKey(i => i.TicketIncidentId);
        });

        // ========================================================
        // 3.14. TICKET PHOTO
        // ========================================================

        modelBuilder.Entity<TicketPhoto>(entity =>
        {
            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.UrlPhoto)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(p => p.Type)
                .IsRequired();

            entity.Property(p => p.DateAjout)
                .HasColumnType("datetime")
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
                .HasForeignKey(p => p.TicketIncidentId);

            entity.HasOne<Utilisateur>()
                .WithMany()
                .HasForeignKey(p => p.AjouteParUtilisateurId)
                .IsRequired(false);
        });

        // ========================================================
        // 3.15. TRANCHE IRPP
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

            // 0.10 = 10 %
            entity.Property(t => t.Taux)
                .HasPrecision(9, 6)
                .IsRequired();

            entity.Property(t => t.OrdreTranche)
                .IsRequired();

            entity.Property(t => t.Description)
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
        // 3.16. PARAMÈTRE FISCAL
        // ========================================================

        modelBuilder.Entity<ParametreFiscal>(entity =>
        {
            entity.ToTable("ParametresFiscaux");

            entity.HasQueryFilter(p => !p.EstSupprime);

            entity.Property(p => p.CodeRegle)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired();

            // 0.10 = 10 %
            entity.Property(p => p.Valeur)
                .HasPrecision(18, 6)
                .IsRequired();

            entity.Property(p => p.TypeParametre)
                .IsRequired();

            entity.Property(p => p.DateDebutValidite)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(p => p.DateFinValidite)
                .HasColumnType("datetime");

            // Non unique pour permettre plusieurs versions
            // d'une règle si les périodes de validité diffèrent.
            entity.HasIndex(p => new
            {
                p.AnneeFiscale,
                p.CodeRegle
            })
                .HasDatabaseName(
                    "IX_ParametresFiscaux_AnneeFiscale_CodeRegle")
                .HasFilter("[EstSupprime] = 0");
        });

        // ========================================================
        // 3.17. RÉGIME FISCAL
        // ========================================================

        modelBuilder.Entity<RegimeFiscal>(entity =>
        {
            entity.ToTable("RegimesFiscaux");

            entity.HasQueryFilter(r => !r.EstSupprime);

            entity.Property(r => r.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(r => r.Libelle)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(r => r.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(r => r.EstActif)
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasIndex(r => r.Code)
                .IsUnique()
                .HasDatabaseName("IX_RegimesFiscaux_Code")
                .HasFilter("[EstSupprime] = 0");
        });

        // ========================================================
        // 4. COMPORTEMENT GLOBAL DES CLÉS ÉTRANGÈRES
        // ========================================================
        // Désactive les suppressions en cascade pour toutes les
        // relations configurées, y compris les relations facultatives.
        // Les suppressions logiques doivent être gérées explicitement.
        // ========================================================

        foreach (var foreignKey in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
