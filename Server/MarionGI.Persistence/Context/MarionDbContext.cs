using MarionGI.Domain.Entities;
using MarionGI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace MarionGI.Persistence.Context;

public class MarionDbContext : DbContext
{
    public MarionDbContext(DbContextOptions<MarionDbContext> options)
        : base(options)
    {
    }

    // ============================================================
    // DbSets
    // ============================================================

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Societe> Societes => Set<Societe>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BienImmobilier> BiensImmobiliers => Set<BienImmobilier>();
    public DbSet<UniteLocative> UnitesLocatives => Set<UniteLocative>();
    public DbSet<Contrat> Contrats => Set<Contrat>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<DemandeVisite> DemandesVisite => Set<DemandeVisite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<JournalAudit> JournauxAudit => Set<JournalAudit>();


    // ============================================================
    // CONVENTIONS EF CORE
    // ============================================================

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Toutes les dates sont stockées en datetime SQL Server.
        configurationBuilder
            .Properties<DateTime>()
            .HaveColumnType("datetime");

        configurationBuilder
            .Properties<DateTime?>()
            .HaveColumnType("datetime");
    }


    // ============================================================
    // CONFIGURATION DU MODÈLE
    // ============================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ========================================================
        // 1. DELETE BEHAVIOR GLOBAL
        // ========================================================

        foreach (var foreignKey in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }


        // ========================================================
        // 2. SOFT DELETE
        // ========================================================

        modelBuilder.Entity<Utilisateur>()
            .HasQueryFilter(u => !u.EstSupprime);

        modelBuilder.Entity<BienImmobilier>()
            .HasQueryFilter(b => !b.EstSupprime);

        modelBuilder.Entity<UniteLocative>()
            .HasQueryFilter(u => !u.EstSupprime);

        modelBuilder.Entity<Contrat>()
            .HasQueryFilter(c => !c.EstSupprime);

        modelBuilder.Entity<Paiement>()
            .HasQueryFilter(p => !p.EstSupprime);

        modelBuilder.Entity<DemandeVisite>()
            .HasQueryFilter(d => !d.EstSupprime);

        modelBuilder.Entity<Notification>()
            .HasQueryFilter(n => !n.EstSupprime);


        // ========================================================
        // 3. SOCIETE
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

            // Société -> Utilisateurs
            entity.HasMany(s => s.Utilisateurs)
                .WithOne(u => u.Societe)
                .HasForeignKey(u => u.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Société -> Biens
            entity.HasMany(s => s.BiensImmobiliers)
                .WithOne(b => b.Societe)
                .HasForeignKey(b => b.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 4. UTILISATEUR
        // ========================================================

        modelBuilder.Entity<Utilisateur>(entity =>
        {
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

            // Email unique dans une société
            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Email
            })
            .IsUnique();

            // Téléphone unique dans une société
            entity.HasIndex(u => new
            {
                u.SocieteId,
                u.Telephone
            })
            .IsUnique();

            // Société de l'utilisateur
            entity.HasOne(u => u.Societe)
                .WithMany(s => s.Utilisateurs)
                .HasForeignKey(u => u.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 5. REFRESH TOKEN
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

            // Un token doit être unique
            entity.HasIndex(r => r.Token)
                .IsUnique();

            // RefreshToken -> Utilisateur
            entity.HasOne(r => r.Utilisateur)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 6. BIEN IMMOBILIER
        // ========================================================

        modelBuilder.Entity<BienImmobilier>(entity =>
        {
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

            // Référence unique dans une société
            entity.HasIndex(b => new
            {
                b.SocieteId,
                b.Reference
            })
            .IsUnique()
            .HasFilter("[EstSupprime] = 0");

            // Nom unique dans une société
            entity.HasIndex(b => new
            {
                b.SocieteId,
                b.Nom
            })
            .IsUnique()
            .HasFilter("[EstSupprime] = 0");

            // Photos stockées en JSON
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
                            ?? new List<string>()
                );

            // Bien -> Société
            entity.HasOne(b => b.Societe)
                .WithMany(s => s.BiensImmobiliers)
                .HasForeignKey(b => b.SocieteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bien -> Unités locatives
            entity.HasMany(b => b.UnitesLocatives)
                .WithOne(u => u.BienImmobilier)
                .HasForeignKey(u => u.BienImmobilierId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 7. UNITE LOCATIVE
        // ========================================================

        modelBuilder.Entity<UniteLocative>(entity =>
        {
            entity.Property(u => u.Reference)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.Superficie)
                .HasPrecision(10, 2);

            entity.Property(u => u.Loyer)
                .HasPrecision(18, 2);

            // Référence unique à l'intérieur du bien
            entity.HasIndex(u => new
            {
                u.BienImmobilierId,
                u.Reference
            })
            .IsUnique();

            // Unité -> Bien
            entity.HasOne(u => u.BienImmobilier)
                .WithMany(b => b.UnitesLocatives)
                .HasForeignKey(u => u.BienImmobilierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Photos stockées en JSON
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
                            ?? new List<string>()
                );
        });


        // ========================================================
        // 8. CONTRAT
        // ========================================================

        modelBuilder.Entity<Contrat>(entity =>
        {
            entity.Property(c => c.MontantLoyer)
                .HasPrecision(18, 2);

            entity.Property(c => c.MontantCaution)
                .HasPrecision(18, 2);

            entity.Property(c => c.FrequencePaiement)
                .IsRequired()
                .HasDefaultValue(FrequencePaiement.Mensuel);

            entity.Property(c => c.DelaiJoursTolerance)
                .IsRequired()
                .HasDefaultValue(5);

            // Contrat -> Unité locative
            entity.HasOne(c => c.UniteLocative)
                .WithMany(u => u.Contrats)
                .HasForeignKey(c => c.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Contrat -> Locataire
            entity.HasOne(c => c.Locataire)
                .WithMany(u => u.ContratsLocataire)
                .HasForeignKey(c => c.LocataireId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 9. PAIEMENT
        // ========================================================

        modelBuilder.Entity<Paiement>(entity =>
        {
            // ----------------------------------------------------
            // Propriétés
            // ----------------------------------------------------

            entity.Property(p => p.Montant)
                .HasPrecision(18, 2);

            entity.Property(p => p.MoisLoyer)
                .HasColumnType("datetime");

            entity.Property(p => p.ReferenceTransactionOperateur)
                .HasMaxLength(200);

            entity.Property(p => p.NumeroQuittance)
                .HasMaxLength(50)
                .IsRequired();


            // ----------------------------------------------------
            // Index ContratId existant
            // ----------------------------------------------------
            //
            // On le conserve explicitement afin qu'EF Core ne
            // génère pas sa suppression lors de la migration.
            //
            entity.HasIndex(p => p.ContratId)
                .HasDatabaseName("IX_Paiements_ContratId");


            // ----------------------------------------------------
            // Un seul paiement EN ATTENTE par contrat et par mois
            // ----------------------------------------------------
            //
            // StatutTransaction = 1 => EnAttente
            //
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


            // ----------------------------------------------------
            // Un seul paiement CONFIRMÉ par contrat et par mois
            // ----------------------------------------------------
            //
            // StatutTransaction = 2 => Confirme
            //
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


            // ----------------------------------------------------
            // Numéro de quittance unique
            // ----------------------------------------------------

            entity.HasIndex(p => p.NumeroQuittance)
                .IsUnique();


            // ----------------------------------------------------
            // Paiement -> Contrat
            // ----------------------------------------------------

            entity.HasOne(p => p.Contrat)
                .WithMany(c => c.Paiements)
                .HasForeignKey(p => p.ContratId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 10. DEMANDE DE VISITE
        // ========================================================

        modelBuilder.Entity<DemandeVisite>(entity =>
        {
            entity.Property(d => d.NomProspect)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(d => d.TelephoneProspect)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(d => d.Observations)
                .HasMaxLength(1000)
                .IsRequired();

            // Demande -> Unité
            entity.HasOne(d => d.UniteLocative)
                .WithMany(u => u.DemandesVisite)
                .HasForeignKey(d => d.UniteLocativeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Demande -> Agent
            entity.HasOne(d => d.Agent)
                .WithMany()
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 11. NOTIFICATION
        // ========================================================

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Type)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(n => n.Message)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(n => n.Lu)
                .HasDefaultValue(false);

            // Notification -> Utilisateur
            entity.HasOne(n => n.Utilisateur)
                .WithMany()
                .HasForeignKey(n => n.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ========================================================
        // 12. JOURNAL D'AUDIT
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

            // UtilisateurId volontairement conservé comme Guid?
            // sans navigation EF.
        });
    }
}